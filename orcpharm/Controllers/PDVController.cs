using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Common;
using DTOs.Sales;
using Models;
using Service;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class PDVController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SaleService _saleService;
    private readonly CashRegisterService _cashService;
    private readonly StockService _stockService;

    public PDVController(AppDbContext context)
    {
        _context = context;
        _saleService = new SaleService(context);
        _cashService = new CashRegisterService(context);
        _stockService = new StockService(context);
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

    [HttpPost("quick-sale")]
    public async Task<ActionResult<ApiResponse<SaleReceiptDto>>> QuickSale([FromBody] QuickSaleDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        if (openCashRegister == null)
            return BadRequest(ApiResponse<SaleReceiptDto>.ErrorResponse("Nenhum caixa aberto. Abra o caixa antes de realizar vendas."));

        var saleDto = new CreateSaleDto
        {
            CustomerId = dto.CustomerId,
            Items = dto.Items.Select(i => new SaleItemDto
            {
                ManipulationOrderId = i.ManipulationOrderId,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountPercentage = i.DiscountPercentage
            }).ToList(),
            SaleDate = DateTime.UtcNow,
            PaymentMethod = dto.PaymentMethod,
            PaidAmount = dto.PaidAmount,
            DiscountPercentage = dto.DiscountPercentage,
            Observations = dto.Observations
        };

        var result = await _saleService.CreateSaleAsync(saleDto, establishmentId, employeeId);
        var success = result.Item1;
        var message = result.Item2;
        var sale = result.Item3;

        if (!success || sale == null)
            return BadRequest(ApiResponse<SaleReceiptDto>.ErrorResponse(message));

        var stockResult = await _stockService.DeductStockForSaleAsync(sale.Id, establishmentId, employeeId);

        if (!stockResult.Success)
        {
            Console.WriteLine($"Aviso de estoque: {stockResult.Message}");
        }

        await _cashService.RegisterSaleInCashRegisterAsync(
            openCashRegister.Id,
            sale.Id,
            sale.TotalAmount,
            sale.PaymentMethod,
            employeeId);

        var receipt = await GenerateReceipt(sale.Id);

        var responseMessage = "Venda realizada com sucesso!";
        if (!stockResult.Success)
            responseMessage += $" ATENÇÃO: {stockResult.Message}";

        return Ok(ApiResponse<SaleReceiptDto>.SuccessResponse(receipt, responseMessage));
    }

    [HttpGet("low-stock-alerts")]
    public async Task<ActionResult<ApiResponse<List<LowStockAlert>>>> GetLowStockAlerts()
    {
        var establishmentId = GetEstablishmentId();
        var lowStockItems = await _stockService.GetLowStockItemsAsync(establishmentId);

        return Ok(ApiResponse<List<LowStockAlert>>.SuccessResponse(
            lowStockItems,
            $"{lowStockItems.Count} item(ns) com estoque baixo"));
    }

    [HttpGet("check-stock/{rawMaterialId}")]
    public async Task<ActionResult<ApiResponse<object>>> CheckStock(Guid rawMaterialId, [FromQuery] Guid? batchId = null)
    {
        var establishmentId = GetEstablishmentId();

        var currentStock = await _stockService.GetCurrentStockAsync(rawMaterialId, batchId, establishmentId);

        var rawMaterial = await _context.RawMaterials
            .FirstOrDefaultAsync(r => r.Id == rawMaterialId);

        if (rawMaterial == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Matéria-prima não encontrada"));

        var stockInfo = new
        {
            RawMaterialId = rawMaterialId,
            Name = rawMaterial.Name,
            CurrentStock = currentStock,
            Unit = rawMaterial.Unit,
            MinimumStock = rawMaterial.MinimumStock,
            IsLowStock = currentStock <= rawMaterial.MinimumStock,
            IsOutOfStock = currentStock <= 0,
            Status = currentStock <= 0 ? "SEM_ESTOQUE" :
                     currentStock <= rawMaterial.MinimumStock ? "ESTOQUE_BAIXO" : "OK"
        };

        return Ok(ApiResponse<object>.SuccessResponse(stockInfo));
    }

    [HttpGet("receipt/{saleId}")]
    public async Task<ActionResult<ApiResponse<SaleReceiptDto>>> GetReceipt(Guid saleId)
    {
        var receipt = await GenerateReceipt(saleId);
        if (receipt == null)
            return NotFound(ApiResponse<SaleReceiptDto>.ErrorResponse("Venda não encontrada"));

        return Ok(ApiResponse<SaleReceiptDto>.SuccessResponse(receipt));
    }

    [HttpGet("daily-sales")]
    public async Task<ActionResult<ApiResponse<DailySalesDto>>> GetDailySales([FromQuery] DateTime? date)
    {
        var establishmentId = GetEstablishmentId();
        var targetDate = date ?? DateTime.Today;

        var sales = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId &&
                       s.SaleDate.Date == targetDate.Date &&
                       s.Status == "FINALIZADA")
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        var result = new DailySalesDto
        {
            Date = targetDate,
            TotalSales = sales.Count,
            TotalAmount = sales.Sum(s => s.TotalAmount),
            TotalCash = sales.Where(s => s.PaymentMethod == "DINHEIRO").Sum(s => s.TotalAmount),
            TotalCard = sales.Where(s => s.PaymentMethod.Contains("CARTAO")).Sum(s => s.TotalAmount),
            TotalPix = sales.Where(s => s.PaymentMethod == "PIX").Sum(s => s.TotalAmount),
            SalesByPaymentMethod = sales.GroupBy(s => s.PaymentMethod).ToDictionary(g => g.Key, g => g.Count()),
            Sales = sales.Select(s => new DTOs.Sales.SaleListDto
            {
                Id = s.Id,
                Code = s.Code,
                CustomerName = s.CustomerId.HasValue ?
                    _context.Customers.Where(c => c.Id == s.CustomerId).Select(c => c.FullName).FirstOrDefault() :
                    "CLIENTE NÃO IDENTIFICADO",
                SaleDate = s.SaleDate,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod,
                Status = s.Status
            }).ToList()
        };

        return Ok(ApiResponse<DailySalesDto>.SuccessResponse(result));
    }

    [HttpGet("pending-orders")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetPendingOrders()
    {
        var establishmentId = GetEstablishmentId();

        var orders = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId &&
                       o.Status == "FINALIZADO" &&
                       !_context.SaleItems.Any(si => si.ManipulationOrderId == o.Id))
            .Include(o => o.Formula)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                FormulaName = o.Formula != null ? o.Formula.Name : "N/A",
                o.QuantityToProduce,
                o.Unit,
                o.CompletionDate,
                SuggestedPrice = 0m
            })
            .OrderBy(o => o.CompletionDate)
            .ToListAsync();

        return Ok(ApiResponse<List<object>>.SuccessResponse(orders.Cast<object>().ToList()));
    }

    private async Task<SaleReceiptDto?> GenerateReceipt(Guid saleId)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale == null)
            return null;

        var establishment = await _context.Establishments
            .FirstOrDefaultAsync(e => e.Id == sale.EstablishmentId);

        var customer = sale.CustomerId.HasValue ?
            await _context.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId) : null;

        return new SaleReceiptDto
        {
            Code = sale.Code,
            SaleDate = sale.SaleDate,
            CustomerName = customer?.FullName,
            Items = sale.Items.Select(i => new ReceiptItemDto
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),
            Subtotal = sale.Subtotal,
            DiscountAmount = sale.DiscountAmount,
            TotalAmount = sale.TotalAmount,
            PaymentMethod = sale.PaymentMethod,
            PaidAmount = sale.PaidAmount ?? 0,
            ChangeAmount = sale.ChangeAmount ?? 0,
            EstablishmentName = establishment?.NomeFantasia ?? "OrcPharm",
            EstablishmentAddress = establishment != null ?
    $"{establishment.Street}, {establishment.Number} - {establishment.Neighborhood}, {establishment.City}/{establishment.State}" : "",
            EstablishmentCnpj = establishment?.Cnpj ?? ""

        };
    }
}