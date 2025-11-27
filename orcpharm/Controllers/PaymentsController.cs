using Data;
using DTOs.Common;
using DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PaymentService _paymentService;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
        _paymentService = new PaymentService(context);
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

    /// <summary>
    /// Adiciona um pagamento a uma venda
    /// </summary>
    [HttpPost("sales/{saleId}")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> AddPayment(
        Guid saleId,
        [FromBody] CreatePaymentDto dto)
    {
        var employeeId = GetEmployeeId();

        var result = await _paymentService.AddPaymentToSaleAsync(saleId, dto, employeeId);

        if (!result.Success || result.Payment == null)
            return BadRequest(ApiResponse<PaymentDto>.ErrorResponse(result.Message));

        var paymentDto = new PaymentDto
        {
            Id = result.Payment.Id,
            PaymentMethod = result.Payment.PaymentMethod,
            Amount = result.Payment.Amount,
            PaymentStatus = result.Payment.PaymentStatus,
            PaymentDate = result.Payment.PaymentDate,
            CashReceived = result.Payment.CashReceived,
            ChangeAmount = result.Payment.ChangeAmount,
            CardBrand = result.Payment.CardBrand,
            CardLastDigits = result.Payment.CardLastDigits,
            Installments = result.Payment.Installments,
            Nsu = result.Payment.Nsu,
            AuthorizationCode = result.Payment.AuthorizationCode,
            PixKey = result.Payment.PixKey,
            PixTransactionId = result.Payment.PixTransactionId,
            BoletoBarcode = result.Payment.BoletoBarcode,
            BoletoDueDate = result.Payment.BoletoDueDate,
            Observations = result.Payment.Observations
        };

        return Ok(ApiResponse<PaymentDto>.SuccessResponse(paymentDto, result.Message));
    }

    /// <summary>
    /// Lista todos os pagamentos de uma venda
    /// </summary>
    [HttpGet("sales/{saleId}")]
    public async Task<ActionResult<ApiResponse<List<PaymentDto>>>> GetSalePayments(Guid saleId)
    {
        var payments = await _paymentService.GetSalePaymentsAsync(saleId);
        return Ok(ApiResponse<List<PaymentDto>>.SuccessResponse(payments));
    }

    /// <summary>
    /// Busca venda com todos os pagamentos
    /// </summary>
    [HttpGet("sales/{saleId}/details")]
    public async Task<ActionResult<ApiResponse<SaleWithPaymentsDto>>> GetSaleWithPayments(Guid saleId)
    {
        var sale = await _paymentService.GetSaleWithPaymentsAsync(saleId);

        if (sale == null)
            return NotFound(ApiResponse<SaleWithPaymentsDto>.ErrorResponse("Venda não encontrada"));

        return Ok(ApiResponse<SaleWithPaymentsDto>.SuccessResponse(sale));
    }

    /// <summary>
    /// Fluxo de caixa diário
    /// </summary>
    [HttpGet("daily-cash-flow")]
    public async Task<ActionResult<ApiResponse<DailyCashFlowDto>>> GetDailyCashFlow(
        [FromQuery] DateTime? date)
    {
        var establishmentId = GetEstablishmentId();
        var targetDate = date ?? DateTime.Today;

        var cashFlow = await _paymentService.GetDailyCashFlowAsync(establishmentId, targetDate);

        return Ok(ApiResponse<DailyCashFlowDto>.SuccessResponse(cashFlow));
    }

    /// <summary>
    /// Cancela/estorna um pagamento
    /// </summary>
    [HttpPut("{paymentId}/cancel")]
    public async Task<ActionResult<ApiResponse<string>>> CancelPayment(Guid paymentId)
    {
        var employeeId = GetEmployeeId();
        var result = await _paymentService.CancelPaymentAsync(paymentId, employeeId);

        if (!result.Success)
            return BadRequest(ApiResponse<string>.ErrorResponse(result.Message));

        return Ok(ApiResponse<string>.SuccessResponse(null, result.Message));
    }
}