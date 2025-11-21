using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using Service;
using Validators;
using Models;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabelsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly LabelService _service;

    public LabelsController(AppDbContext context, LabelService service)
    {
        _context = context;
        _service = service;
    }

    // ============================================
    // TEMPLATES
    // ============================================

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] bool? activeOnly = true)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Set<LabelTemplate>()
            .Where(t => t.EstablishmentId == establishmentId.Value);

        if (activeOnly.HasValue && activeOnly.Value)
            query = query.Where(t => t.IsActive);

        var templates = await query
            .OrderBy(t => t.Name)
            .Select(t => new LabelTemplateResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                TemplateType = t.TemplateType,
                PharmaceuticalForm = t.PharmaceuticalForm,
                Width = t.Width,
                Height = t.Height,
                IsActive = t.IsActive,
                IsDefault = t.IsDefault,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return Ok(templates);
    }

    [HttpGet("templates/{id}")]
    public async Task<IActionResult> GetTemplateById(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
            return NotFound(new { message = "Template não encontrado" });

        return Ok(template);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateLabelTemplateDto dto)
    {
        var validator = new CreateLabelTemplateValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, template) = await _service.CreateTemplateAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(
            nameof(GetTemplateById),
            new { id = template!.Id },
            new { message, templateId = template.Id });
    }

    [HttpPut("templates/{id}")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateLabelTemplateDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
            return NotFound(new { message = "Template não encontrado" });

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.HtmlTemplate = dto.HtmlTemplate;
        template.CssStyles = dto.CssStyles;
        template.IsActive = dto.IsActive;
        template.IsDefault = dto.IsDefault;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByEmployeeId = employeeId.Value;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Template atualizado com sucesso" });
    }

    [HttpDelete("templates/{id}")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
            return NotFound(new { message = "Template não encontrado" });

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByEmployeeId = employeeId.Value;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Template desativado com sucesso" });
    }

    // ============================================
    // GERAÇÃO E IMPRESSÃO
    // ============================================

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateLabel([FromBody] GenerateLabelDto dto)
    {
        var validator = new GenerateLabelValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, label) = await _service.GenerateLabelAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, labelId = label!.Id, html = label.GeneratedHtml });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLabel(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var label = await _context.Set<GeneratedLabel>()
            .Where(l => l.Id == id && l.EstablishmentId == establishmentId.Value)
            .Select(l => new GeneratedLabelResponseDto
            {
                Id = l.Id,
                LabelCode = l.LabelCode,
                ManipulationOrderId = l.ManipulationOrderId,
                ManipulationOrderCode = _context.ManipulationOrders
                    .Where(o => o.Id == l.ManipulationOrderId)
                    .Select(o => o.Code)
                    .FirstOrDefault() ?? "",
                PatientName = l.PatientName,
                FormulaName = l.FormulaName,
                ManipulationDate = l.ManipulationDate,
                ExpirationDate = l.ExpirationDate,
                BatchNumber = l.BatchNumber,
                QrCodeData = l.QrCodeData,
                QrCodeImageUrl = l.QrCodeImageUrl,
                GeneratedHtml = l.GeneratedHtml,
                PrintCount = l.PrintCount,
                LastPrintedAt = l.LastPrintedAt,
                Status = l.Status,
                CreatedAt = l.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (label == null)
            return NotFound(new { message = "Rótulo não encontrado" });

        return Ok(label);
    }

    [HttpPost("{id}/print")]
    public async Task<IActionResult> PrintLabel(Guid id, [FromBody] PrintLabelDto dto)
    {
        var validator = new PrintLabelValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message) = await _service.PrintLabelAsync(
            id, dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpGet]
    public async Task<IActionResult> GetLabels(
        [FromQuery] Guid? manipulationOrderId = null,
        [FromQuery] string? status = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Set<GeneratedLabel>()
            .Where(l => l.EstablishmentId == establishmentId.Value);

        if (manipulationOrderId.HasValue)
            query = query.Where(l => l.ManipulationOrderId == manipulationOrderId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(l => l.Status == status.ToUpper());

        var labels = await query
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new GeneratedLabelResponseDto
            {
                Id = l.Id,
                LabelCode = l.LabelCode,
                ManipulationOrderId = l.ManipulationOrderId,
                ManipulationOrderCode = _context.ManipulationOrders
                    .Where(o => o.Id == l.ManipulationOrderId)
                    .Select(o => o.Code)
                    .FirstOrDefault() ?? "",
                PatientName = l.PatientName,
                FormulaName = l.FormulaName,
                ManipulationDate = l.ManipulationDate,
                ExpirationDate = l.ExpirationDate,
                BatchNumber = l.BatchNumber,
                QrCodeData = l.QrCodeData,
                PrintCount = l.PrintCount,
                LastPrintedAt = l.LastPrintedAt,
                Status = l.Status,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return Ok(labels);
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
