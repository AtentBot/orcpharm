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
public class PrescriptionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PrescriptionService _service;

    public PrescriptionsController(AppDbContext context, PrescriptionService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] bool? includeExpired = false)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Set<Prescription>()
            .Where(p => p.EstablishmentId == establishmentId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status.ToUpper() == status.ToUpper());

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);

        if (!includeExpired.Value)
            query = query.Where(p => p.Status != "EXPIRADA");

        var prescriptions = await query
            .OrderByDescending(p => p.PrescriptionDate)
            .Select(p => new PrescriptionListDto
            {
                Id = p.Id,
                Code = p.Code,
                CustomerName = _context.Set<Customer>()
                    .Where(c => c.Id == p.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? "",
                PrescriptionDate = p.PrescriptionDate,
                ExpirationDate = p.ExpirationDate,
                DoctorName = p.DoctorName,
                PrescriptionType = p.PrescriptionType,
                Status = p.Status,
                IsExpired = p.ExpirationDate < DateTime.Today,
                DaysUntilExpiration = (int)(p.ExpirationDate - DateTime.Today).TotalDays
            })
            .ToListAsync();

        return Ok(prescriptions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var prescription = await _context.Set<Prescription>()
            .Where(p => p.Id == id && p.EstablishmentId == establishmentId.Value)
            .Select(p => new PrescriptionResponseDto
            {
                Id = p.Id,
                Code = p.Code,
                CustomerId = p.CustomerId,
                CustomerName = _context.Set<Customer>()
                    .Where(c => c.Id == p.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? "",
                CustomerCpf = _context.Set<Customer>()
                    .Where(c => c.Id == p.CustomerId)
                    .Select(c => c.Cpf)
                    .FirstOrDefault(),
                PrescriptionDate = p.PrescriptionDate,
                ExpirationDate = p.ExpirationDate,
                DaysUntilExpiration = (int)(p.ExpirationDate - DateTime.Today).TotalDays,
                IsExpired = p.ExpirationDate < DateTime.Today,
                DoctorName = p.DoctorName,
                DoctorCrm = p.DoctorCrm,
                DoctorCrmState = p.DoctorCrmState,
                PrescriptionType = p.PrescriptionType,
                ControlledType = p.ControlledType,
                PrescriptionColor = p.PrescriptionColor,
                Medications = p.Medications,
                Posology = p.Posology,
                Observations = p.Observations,
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                ValidatedAt = p.ValidatedAt,
                ValidatedByEmployeeName = "",
                ValidationNotes = p.ValidationNotes,
                ManipulationOrderId = p.ManipulationOrderId,
                ManipulationOrderCode = null,
                ManipulationGeneratedAt = p.ManipulationGeneratedAt,
                CancelledAt = p.CancelledAt,
                CancelledByEmployeeName = "",
                CancellationReason = p.CancellationReason,
                CreatedAt = p.CreatedAt,
                CreatedByEmployeeName = "",
                UpdatedAt = p.UpdatedAt,
                UpdatedByEmployeeName = ""
            })
            .FirstOrDefaultAsync();

        if (prescription == null)
            return NotFound(new { message = "Prescrição não encontrada" });

        return Ok(prescription);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
    {
        var validator = new CreatePrescriptionValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, prescription) = await _service.CreatePrescriptionAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(
            nameof(GetById),
            new { id = prescription!.Id },
            new { message, prescriptionId = prescription.Id });
    }

    [HttpPut("{id}/validate")]
    public async Task<IActionResult> Validate(Guid id, [FromBody] ValidatePrescriptionDto dto)
    {
        var validator = new ValidatePrescriptionValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "FARMACEUTICO" });
        if (!hasPermission)
            return Forbid();

        var (success, message) = await _service.ValidatePrescriptionAsync(
            id, dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPrescriptionDto dto)
    {
        var validator = new CancelPrescriptionValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message) = await _service.CancelPrescriptionAsync(
            id, dto.Reason, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] int days = 7)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var expiringDate = DateTime.Today.AddDays(days);

        var prescriptions = await _context.Set<Prescription>()
            .Where(p => p.EstablishmentId == establishmentId.Value &&
                       p.Status == "VALIDADA" &&
                       p.ExpirationDate <= expiringDate &&
                       p.ExpirationDate >= DateTime.Today)
            .OrderBy(p => p.ExpirationDate)
            .Select(p => new PrescriptionListDto
            {
                Id = p.Id,
                Code = p.Code,
                CustomerName = _context.Set<Customer>()
                    .Where(c => c.Id == p.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? "",
                PrescriptionDate = p.PrescriptionDate,
                ExpirationDate = p.ExpirationDate,
                DoctorName = p.DoctorName,
                PrescriptionType = p.PrescriptionType,
                Status = p.Status,
                IsExpired = false,
                DaysUntilExpiration = (int)(p.ExpirationDate - DateTime.Today).TotalDays
            })
            .ToListAsync();

        return Ok(prescriptions);
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

    private async Task<bool> HasPermission(Guid employeeId, string[] allowedPositions)
    {
        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee?.JobPosition == null)
            return false;

        return allowedPositions.Contains(employee.JobPosition.Code, StringComparer.OrdinalIgnoreCase);
    }
}