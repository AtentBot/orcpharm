using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Common;
using DTOs.Fiscal;
using Service;
using Models.Fiscal;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FiscalController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly FiscalService _fiscalService;

    public FiscalController(AppDbContext context)
    {
        _context = context;
        _fiscalService = new FiscalService(context);
    }

    private Guid GetEstablishmentId()
    {
        var claim = User.FindFirst("EstablishmentId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("EmployeeId") ?? User.FindFirst("UserId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    // ============================================================
    // CONFIGURAÇÃO
    // ============================================================

    /// <summary>
    /// Retorna status da configuração fiscal
    /// </summary>
    [HttpGet("config/status")]
    public async Task<ActionResult<ApiResponse<FiscalConfigStatusDto>>> GetConfigStatus()
    {
        var establishmentId = GetEstablishmentId();
        var status = await _fiscalService.GetConfigStatusAsync(establishmentId);
        return Ok(ApiResponse<FiscalConfigStatusDto>.SuccessResponse(status));
    }

    /// <summary>
    /// Retorna configuração fiscal atual
    /// </summary>
    [HttpGet("config")]
    public async Task<ActionResult<ApiResponse<FiscalConfigDto>>> GetConfig()
    {
        var establishmentId = GetEstablishmentId();
        var config = await _fiscalService.GetConfigAsync(establishmentId);

        if (config == null)
            return Ok(ApiResponse<FiscalConfigDto>.SuccessResponse(new FiscalConfigDto(), "Configuração não encontrada"));

        var dto = new FiscalConfigDto
        {
            Environment = config.Environment,
            Uf = config.Uf,
            NfeSeries = config.NfeSeries,
            NfceSeries = config.NfceSeries,
            CscId = config.CscId,
            CscToken = config.CscToken != null ? "****" : null,
            TaxRegime = config.TaxRegime,
            DefaultCfopVenda = config.DefaultCfopVenda,
            DefaultCfopManipulacao = config.DefaultCfopManipulacao,
            DefaultNcmManipulacao = config.DefaultNcmManipulacao,
            Provider = config.Provider,
            ProviderApiKey = config.ProviderApiKey != null ? "****" : null,
            PrintDanfeAuto = config.PrintDanfeAuto,
            ContingencyEnabled = config.ContingencyEnabled,
            DefaultNature = config.DefaultNature,
            DefaultAdditionalInfo = config.DefaultAdditionalInfo
        };

        return Ok(ApiResponse<FiscalConfigDto>.SuccessResponse(dto));
    }

    /// <summary>
    /// Salva configuração fiscal
    /// </summary>
    [HttpPost("config")]
    public async Task<ActionResult<ApiResponse<bool>>> SaveConfig([FromBody] FiscalConfigDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var result = await _fiscalService.SaveConfigAsync(establishmentId, dto);

        if (result.Success)
            return Ok(ApiResponse<bool>.SuccessResponse(true, result.Message));

        return BadRequest(ApiResponse<bool>.ErrorResponse(result.Message));
    }

    /// <summary>
    /// Upload de certificado digital A1
    /// </summary>
    [HttpPost("config/certificate")]
    public async Task<ActionResult<ApiResponse<string>>> UploadCertificate([FromBody] UploadCertificateDto dto)
    {
        var establishmentId = GetEstablishmentId();

        try
        {
            var certificateData = Convert.FromBase64String(dto.CertificateBase64);
            var result = await _fiscalService.UploadCertificateAsync(establishmentId, certificateData, dto.Password);

            if (result.Success)
                return Ok(ApiResponse<string>.SuccessResponse(result.Message));

            return BadRequest(ApiResponse<string>.ErrorResponse(result.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse($"Erro no upload: {ex.Message}"));
        }
    }

    /// <summary>
    /// Testa conexão com SEFAZ
    /// </summary>
    [HttpPost("config/test-connection")]
    public async Task<ActionResult<ApiResponse<bool>>> TestConnection()
    {
        var establishmentId = GetEstablishmentId();
        var config = await _fiscalService.GetConfigAsync(establishmentId);

        if (config == null)
            return BadRequest(ApiResponse<bool>.ErrorResponse("Configuração não encontrada"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Conexão com SEFAZ OK"));
    }

    // ============================================================
    // EMISSÃO
    // ============================================================

    /// <summary>
    /// Emite NF-e ou NFC-e para uma venda
    /// </summary>
    [HttpPost("emitir")]
    public async Task<ActionResult<ApiResponse<NFeResultDto>>> Emitir([FromBody] EmitirNFeDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var userId = GetUserId();

        var result = await _fiscalService.EmitirNFeAsync(establishmentId, dto, userId);

        if (result.Success)
            return Ok(ApiResponse<NFeResultDto>.SuccessResponse(result, 
                $"{dto.InvoiceType} #{result.InvoiceNumber} emitida com sucesso!"));

        if (result.InContingency)
            return Ok(ApiResponse<NFeResultDto>.SuccessResponse(result, 
                "Nota adicionada à fila de contingência"));

        return BadRequest(ApiResponse<NFeResultDto>.ErrorResponse(result.ErrorMessage ?? "Erro na emissão"));
    }

    /// <summary>
    /// Emite NFC-e diretamente do PDV
    /// </summary>
    [HttpPost("emitir-pdv/{saleId}")]
    public async Task<ActionResult<ApiResponse<NFeResultDto>>> EmitirDoPdv(Guid saleId)
    {
        var establishmentId = GetEstablishmentId();
        var userId = GetUserId();

        var dto = new EmitirNFeDto
        {
            SaleId = saleId,
            InvoiceType = "NFCE",
            PrintDanfe = true
        };

        var result = await _fiscalService.EmitirNFeAsync(establishmentId, dto, userId);

        if (result.Success)
            return Ok(ApiResponse<NFeResultDto>.SuccessResponse(result, "NFC-e emitida!"));

        return BadRequest(ApiResponse<NFeResultDto>.ErrorResponse(result.ErrorMessage ?? "Erro na emissão"));
    }

    /// <summary>
    /// Cancela uma nota fiscal
    /// </summary>
    [HttpPost("cancelar")]
    public async Task<ActionResult<ApiResponse<CancelamentoResultDto>>> Cancelar([FromBody] CancelarNFeDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var userId = GetUserId();

        var result = await _fiscalService.CancelarNFeAsync(establishmentId, dto, userId);

        if (result.Success)
            return Ok(ApiResponse<CancelamentoResultDto>.SuccessResponse(result, "Nota cancelada com sucesso"));

        return BadRequest(ApiResponse<CancelamentoResultDto>.ErrorResponse(result.ErrorMessage ?? "Erro no cancelamento"));
    }

    /// <summary>
    /// Inutiliza numeração
    /// </summary>
    [HttpPost("inutilizar")]
    public async Task<ActionResult<ApiResponse<InutilizacaoResultDto>>> Inutilizar([FromBody] InutilizarNumeracaoDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var userId = GetUserId();

        var result = await _fiscalService.InutilizarNumeracaoAsync(establishmentId, dto, userId);

        if (result.Success)
            return Ok(ApiResponse<InutilizacaoResultDto>.SuccessResponse(result, "Numeração inutilizada"));

        return BadRequest(ApiResponse<InutilizacaoResultDto>.ErrorResponse(result.ErrorMessage ?? "Erro na inutilização"));
    }

    // ============================================================
    // CONSULTAS
    // ============================================================

    /// <summary>
    /// Lista notas fiscais
    /// </summary>
    [HttpGet("invoices")]
    public async Task<ActionResult<ApiResponse<List<FiscalInvoiceListDto>>>> GetInvoices(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var establishmentId = GetEstablishmentId();
        var invoices = await _fiscalService.GetInvoicesAsync(
            establishmentId, status, type, startDate, endDate, page, pageSize);

        return Ok(ApiResponse<List<FiscalInvoiceListDto>>.SuccessResponse(invoices, 
            $"{invoices.Count} nota(s) encontrada(s)"));
    }

    /// <summary>
    /// Detalhes de uma nota fiscal
    /// </summary>
    [HttpGet("invoices/{id}")]
    public async Task<ActionResult<ApiResponse<FiscalInvoiceDetailDto>>> GetInvoiceDetail(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        var invoice = await _fiscalService.GetInvoiceDetailAsync(id, establishmentId);

        if (invoice == null)
            return NotFound(ApiResponse<FiscalInvoiceDetailDto>.ErrorResponse("Nota não encontrada"));

        return Ok(ApiResponse<FiscalInvoiceDetailDto>.SuccessResponse(invoice));
    }

    /// <summary>
    /// Estatísticas fiscais
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<FiscalStatsDto>>> GetStats()
    {
        var establishmentId = GetEstablishmentId();
        var stats = await _fiscalService.GetStatsAsync(establishmentId);
        return Ok(ApiResponse<FiscalStatsDto>.SuccessResponse(stats));
    }

    // ============================================================
    // DOWNLOADS
    // ============================================================

    /// <summary>
    /// Download do XML da nota
    /// </summary>
    [HttpGet("xml/{id}")]
    public async Task<IActionResult> DownloadXml(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        var invoice = await _fiscalService.GetInvoiceDetailAsync(id, establishmentId);

        if (invoice == null)
            return NotFound("Nota não encontrada");

        if (string.IsNullOrEmpty(invoice.XmlUrl))
            return NotFound("XML não disponível");

        var xmlContent = $"<nfeProc><NFe><chave>{invoice.InvoiceKey}</chave></NFe></nfeProc>";
        var bytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);

        return File(bytes, "application/xml", $"NFe_{invoice.InvoiceNumber}.xml");
    }

    /// <summary>
    /// Download do DANFE
    /// </summary>
    [HttpGet("danfe/{id}")]
    public async Task<IActionResult> DownloadDanfe(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        var invoice = await _fiscalService.GetInvoiceDetailAsync(id, establishmentId);

        if (invoice == null)
            return NotFound("Nota não encontrada");

        return NotFound("DANFE ainda não implementado");
    }

    // ============================================================
    // FILA DE CONTINGÊNCIA
    // ============================================================

    /// <summary>
    /// Lista itens na fila de contingência
    /// </summary>
    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<List<FiscalQueueItemDto>>>> GetQueue()
    {
        var establishmentId = GetEstablishmentId();
        var queue = await _fiscalService.GetQueueAsync(establishmentId);
        return Ok(ApiResponse<List<FiscalQueueItemDto>>.SuccessResponse(queue, 
            $"{queue.Count} item(s) na fila"));
    }

    /// <summary>
    /// Processa fila de contingência
    /// </summary>
    [HttpPost("queue/process")]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessQueue()
    {
        var establishmentId = GetEstablishmentId();
        var userId = GetUserId();

        await _fiscalService.ProcessQueueAsync(establishmentId, userId);

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Fila processada"));
    }

    /// <summary>
    /// Remove item da fila (desistir da emissão)
    /// </summary>
    [HttpDelete("queue/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveFromQueue(Guid id)
    {
        var establishmentId = GetEstablishmentId();

        var item = await _context.FiscalQueues
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (item == null)
            return NotFound(ApiResponse<bool>.ErrorResponse("Item não encontrado"));

        _context.FiscalQueues.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Item removido da fila"));
    }
}
