using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Service;
using DTOs.Common;
using DTOs.Prescriptions;
using Service.Prescriptions;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrescriptionQuotesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PrescriptionQuoteService _quoteService;
    private readonly PrescriptionWorkflowService _workflowService;
    private readonly QuoteWhatsAppService _whatsAppService;
    private readonly QuoteEmailService _emailService;
    private readonly OpenAIPrescriptionParserService _parserService;
    private readonly IngredientMatcherService _matcherService;

    public PrescriptionQuotesController(
        AppDbContext context,
        PrescriptionQuoteService quoteService,
        PrescriptionWorkflowService workflowService,
        QuoteWhatsAppService whatsAppService,
        QuoteEmailService emailService,
        OpenAIPrescriptionParserService parserService,
        IngredientMatcherService matcherService)
    {
        _context = context;
        _quoteService = quoteService;
        _workflowService = workflowService;
        _whatsAppService = whatsAppService;
        _emailService = emailService;
        _parserService = parserService;
        _matcherService = matcherService;
    }

    /// <summary>
    /// Lista todos os orçamentos
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var quotes = await _quoteService.GetAllAsync(
            establishmentId.Value, status, startDate, endDate);

        return Ok(quotes);
    }

    /// <summary>
    /// Busca orçamento por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var quote = await _quoteService.GetByIdAsync(id, establishmentId.Value);

        if (quote == null)
            return NotFound(new { message = "Orçamento não encontrado" });

        return Ok(quote);
    }

    /// <summary>
    /// Cria orçamento a partir de ingredientes confirmados
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuoteFromOcrDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, quote) = await _quoteService.CreateQuoteAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(nameof(GetById), new { id = quote!.Id }, quote);
    }

    /// <summary>
    /// Processamento rápido: Upload → OCR → Match → Orçamento
    /// </summary>
    [HttpPost("quick-process")]
    public async Task<IActionResult> QuickProcess([FromBody] QuickProcessPrescriptionDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var result = await _workflowService.ProcessPrescriptionAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Envia orçamento por WhatsApp
    /// </summary>
    [HttpPost("{id}/send-whatsapp")]
    public async Task<IActionResult> SendWhatsApp(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message) = await _whatsAppService.SendQuoteAsync(id, establishmentId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    /// <summary>
    /// Envia orçamento por E-mail
    /// </summary>
    [HttpPost("{id}/send-email")]
    public async Task<IActionResult> SendEmail(Guid id, [FromBody] SendQuoteEmailDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message) = await _emailService.SendQuoteEmailAsync(
            id, dto.Email, dto.Message, establishmentId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    /// <summary>
    /// Envia lembrete de orçamento pendente
    /// </summary>
    [HttpPost("{id}/send-reminder")]
    public async Task<IActionResult> SendReminder(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message) = await _whatsAppService.SendReminderAsync(id, establishmentId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    /// <summary>
    /// Atualiza valores do orçamento
    /// </summary>
    [HttpPut("{id}/pricing")]
    public async Task<IActionResult> UpdatePricing(
        Guid id,
        [FromBody] UpdateQuotePricingDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message) = await _quoteService.UpdatePricingAsync(
            id,
            dto.MarkupPercentage,
            dto.DiscountPercentage,
            dto.LaborCost,
            dto.PackagingCost,
            establishmentId.Value,
            employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    /// <summary>
    /// Converte orçamento aprovado em venda e ordem de manipulação
    /// </summary>
    [HttpPost("{id}/convert-to-sale")]
    public async Task<IActionResult> ConvertToSale(Guid id, [FromBody] ConvertQuoteToSaleDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, saleId, manipulationOrderId) = await _workflowService.ConvertQuoteToSaleAsync(
            id,
            dto.PaymentMethod,
            dto.AmountPaid,
            dto.Installments,
            dto.DiscountAmount,
            dto.DiscountReason,
            establishmentId.Value,
            employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        // Enviar confirmação por WhatsApp
        if (manipulationOrderId.HasValue)
        {
            var manipOrder = await _context.Set<Models.Pharmacy.ManipulationOrder>()
                .FirstOrDefaultAsync(m => m.Id == manipulationOrderId.Value);

            if (manipOrder != null)
            {
                await _whatsAppService.SendApprovalConfirmationAsync(
                    id, establishmentId.Value, manipOrder.Code);
            }
        }

        return Ok(new { message, saleId, manipulationOrderId });
    }

    // ===== ENDPOINTS PÚBLICOS (sem autenticação) =====

    /// <summary>
    /// Busca orçamento por token público (para cliente)
    /// </summary>
    [HttpGet("public/{token}")]
    public async Task<IActionResult> GetByToken(string token)
    {
        var quote = await _quoteService.GetByPublicTokenAsync(token);

        if (quote == null)
            return NotFound(new { message = "Orçamento não encontrado" });

        return Ok(quote);
    }

    /// <summary>
    /// Cliente aprova orçamento
    /// </summary>
    [HttpPost("public/{token}/approve")]
    public async Task<IActionResult> ApproveByToken(string token, [FromBody] ApproveQuoteDto dto)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (success, message) = await _quoteService.ApproveByTokenAsync(
            token, dto.CustomerObservations, clientIp);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    /// <summary>
    /// Cliente recusa orçamento
    /// </summary>
    [HttpPost("public/{token}/reject")]
    public async Task<IActionResult> RejectByToken(string token, [FromBody] RejectQuoteDto dto)
    {
        var (success, message) = await _quoteService.RejectByTokenAsync(
            token, dto.Reason, dto.CustomerObservations);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // ===== MÉTODOS AUXILIARES =====

    private decimal ParseQuantity(string quantityText)
    {
        if (string.IsNullOrWhiteSpace(quantityText))
            return 0;

        var numbers = new string(quantityText.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        numbers = numbers.Replace(',', '.');

        if (decimal.TryParse(numbers, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;

        return 0;
    }

    private Guid? GetEmployeeId()
    {
        var sessionToken = Request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = _context.EmployeeSessions
            .FirstOrDefault(s => s.Token == sessionToken &&
                                s.ExpiresAt > DateTime.UtcNow &&
                                s.IsActive);

        return session?.EmployeeId;
    }

    private async Task<Guid?> GetEstablishmentId(Guid employeeId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        return employee?.EstablishmentId;
    }
}

public class UpdateQuotePricingDto
{
    public decimal? MarkupPercentage { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? PackagingCost { get; set; }
}

public class SendQuoteEmailDto
{
    public string Email { get; set; } = string.Empty;
    public string? Message { get; set; }
}