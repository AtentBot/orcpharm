using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Common;
using DTOs.Cash;
using Models;
using Models.Employees;
using Service;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class CashRegisterController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CashRegisterService _service;

    public CashRegisterController(AppDbContext context)
    {
        _context = context;
        _service = new CashRegisterService(context);
    }

    private Guid GetEstablishmentId()
    {
        var claim = User.FindFirst("EstablishmentId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    private Guid GetEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    [HttpPost("open")]
    public async Task<ActionResult<ApiResponse<CashRegisterDto>>> OpenCashRegister([FromBody] OpenCashRegisterDto dto)
    {
        var establishmentId = GetEstablishmentId();

        var (success, message, cashRegister) = await _service.OpenCashRegisterAsync(
            establishmentId,
            dto.EmployeeId,
            dto.OpeningBalance,
            dto.Observations);

        if (!success)
            return BadRequest(ApiResponse<CashRegisterDto>.ErrorResponse(message));

        var result = await MapToCashRegisterDto(cashRegister!);
        return Ok(ApiResponse<CashRegisterDto>.SuccessResponse(result, message));
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<ApiResponse<object>>> CloseCashRegister(Guid id, [FromBody] CloseCashRegisterDto dto)
    {
        var (success, message) = await _service.CloseCashRegisterAsync(
            id,
            dto.EmployeeId,
            dto.ClosingBalance,
            dto.Observations);

        if (!success)
            return BadRequest(ApiResponse<object>.ErrorResponse(message));

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<CashRegisterDto>>> GetCurrentCashRegister()
    {
        var establishmentId = GetEstablishmentId();

        var cashRegister = await _service.GetOpenCashRegisterAsync(establishmentId);

        if (cashRegister == null)
            return NotFound(ApiResponse<CashRegisterDto>.ErrorResponse("Nenhum caixa aberto"));

        var result = await MapToCashRegisterDto(cashRegister);
        return Ok(ApiResponse<CashRegisterDto>.SuccessResponse(result));
    }

    [HttpPost("{id}/supply")]
    public async Task<ActionResult<ApiResponse<object>>> AddSupply(Guid id, [FromBody] AddCashMovementDto dto)
    {
        var employeeId = GetEmployeeId();

        var (success, message) = await _service.AddSupplyAsync(
            id,
            dto.Amount,
            dto.Description,
            employeeId);

        if (!success)
            return BadRequest(ApiResponse<object>.ErrorResponse(message));

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }

    [HttpPost("{id}/withdrawal")]
    public async Task<ActionResult<ApiResponse<object>>> AddWithdrawal(Guid id, [FromBody] AddCashMovementDto dto)
    {
        var employeeId = GetEmployeeId();

        var (success, message) = await _service.AddWithdrawalAsync(
            id,
            dto.Amount,
            dto.Description,
            employeeId);

        if (!success)
            return BadRequest(ApiResponse<object>.ErrorResponse(message));

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }

    [HttpGet("{id}/report")]
    public async Task<ActionResult<ApiResponse<CashRegisterReportDto>>> GetCashRegisterReport(Guid id)
    {
        var cashRegister = await _context.Set<CashRegister>()
            .Include(c => c.OpenedByEmployee)
            .Include(c => c.ClosedByEmployee)
            .Include(c => c.Movements)
                .ThenInclude(m => m.Employee)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cashRegister == null)
            return NotFound(ApiResponse<CashRegisterReportDto>.ErrorResponse("Caixa não encontrado"));

        var report = new CashRegisterReportDto
        {
            CashRegister = await MapToCashRegisterDto(cashRegister),
            Movements = cashRegister.Movements.Select(m => new CashMovementDto
            {
                Id = m.Id,
                MovementType = m.MovementType,
                Amount = m.Amount,
                PaymentMethod = m.PaymentMethod,
                SaleId = m.SaleId,
                Description = m.Description,
                EmployeeName = m.Employee?.FullName ?? "N/A",
                MovementDate = m.MovementDate
            }).OrderBy(m => m.MovementDate).ToList(),
            ByPaymentMethod = new Dictionary<string, decimal>
            {
                { "DINHEIRO", cashRegister.TotalCash },
                { "CARTAO", cashRegister.TotalCard },
                { "PIX", cashRegister.TotalPix }
            },
            SalesByPaymentMethod = cashRegister.Movements
                .Where(m => m.MovementType == "VENDA" && m.PaymentMethod != null)
                .GroupBy(m => m.PaymentMethod!)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return Ok(ApiResponse<CashRegisterReportDto>.SuccessResponse(report));
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<CashRegisterDto>>>> GetHistory([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var establishmentId = GetEstablishmentId();

        var query = _context.Set<CashRegister>()
            .Include(c => c.OpenedByEmployee)
            .Include(c => c.ClosedByEmployee)
            .Where(c => c.EstablishmentId == establishmentId);

        if (startDate.HasValue)
            query = query.Where(c => c.OpeningDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.OpeningDate <= endDate.Value);

        var cashRegisters = await query
            .OrderByDescending(c => c.OpeningDate)
            .ToListAsync();

        var result = new List<CashRegisterDto>();
        foreach (var cr in cashRegisters)
        {
            result.Add(await MapToCashRegisterDto(cr));
        }

        return Ok(ApiResponse<List<CashRegisterDto>>.SuccessResponse(result));
    }

    private async Task<CashRegisterDto> MapToCashRegisterDto(CashRegister cashRegister)
    {
        return new CashRegisterDto
        {
            Id = cashRegister.Id,
            Code = cashRegister.Code,
            OpeningDate = cashRegister.OpeningDate,
            ClosingDate = cashRegister.ClosingDate,
            OpenedByEmployeeName = cashRegister.OpenedByEmployee?.FullName ??
                (await _context.Set<Models.Employees.Employee>().FindAsync(cashRegister.OpenedByEmployeeId))?.FullName ?? "N/A",
            ClosedByEmployeeName = cashRegister.ClosedByEmployeeId.HasValue ?
                (cashRegister.ClosedByEmployee?.FullName ??
                (await _context.Set<Models.Employees.Employee>().FindAsync(cashRegister.ClosedByEmployeeId.Value))?.FullName) : null,
            OpeningBalance = cashRegister.OpeningBalance,
            ClosingBalance = cashRegister.ClosingBalance,
            ExpectedBalance = cashRegister.ExpectedBalance,
            Difference = cashRegister.Difference,
            TotalSales = cashRegister.TotalSales,
            TotalCash = cashRegister.TotalCash,
            TotalCard = cashRegister.TotalCard,
            TotalPix = cashRegister.TotalPix,
            SalesCount = cashRegister.SalesCount,
            Status = cashRegister.Status,
            Observations = cashRegister.Observations
        };
    }
}

