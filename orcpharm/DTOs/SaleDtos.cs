namespace DTOs;

public class CreateSaleDto
{
    public Guid? CustomerId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal DiscountPercentage { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public string PaymentMethod { get; set; } = "DINHEIRO";
    public decimal PaidAmount { get; set; }
    public string? Observations { get; set; }
    public List<CreateSaleItemDto> Items { get; set; } = new();
}

public class CreateSaleItemDto
{
    public Guid? ManipulationOrderId { get; set; }
    public Guid? PrescriptionId { get; set; }
    public Guid? FormulaId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; } = 0;
}

public class SaleResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerCpf { get; set; }
    public DateTime SaleDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public DateTime? PaymentDate { get; set; }

    public string? InvoiceNumber { get; set; }
    public string? InvoiceKey { get; set; }
    public string? InvoiceStatus { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByEmployeeName { get; set; }
    public string? CancellationReason { get; set; }

    public string? Observations { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedByEmployeeName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedByEmployeeName { get; set; }

    public List<SaleItemResponseDto> Items { get; set; } = new();
}

public class SaleItemResponseDto
{
    public Guid Id { get; set; }
    public Guid? ManipulationOrderId { get; set; }
    public string? ManipulationOrderCode { get; set; }
    public Guid? PrescriptionId { get; set; }
    public string? PrescriptionCode { get; set; }
    public Guid? FormulaId { get; set; }
    public string? FormulaName { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal ProfitMargin { get; set; }
    public string? Observations { get; set; }
}

public class SaleListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
}

public class DailySalesReportDto
{
    public DateTime Date { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal AverageTicket { get; set; }
    public Dictionary<string, int> SalesByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> AmountByPaymentMethod { get; set; } = new();
}

public class CancelSaleDto
{
    public string Reason { get; set; } = string.Empty;
}

public class CreateQuotationDto
{
    public Guid? CustomerId { get; set; }
    public List<CreateSaleItemDto> Items { get; set; } = new();
    public string? Observations { get; set; }
}