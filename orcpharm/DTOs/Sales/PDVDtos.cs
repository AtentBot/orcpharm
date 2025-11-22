
using System.ComponentModel.DataAnnotations;

namespace DTOs.Sales;

public class QuickSaleDto
{
    public Guid? CustomerId { get; set; }

    [Required]
    public List<QuickSaleItemDto> Items { get; set; } = new();

    [Required]
    [MaxLength(20)]
    public string PaymentMethod { get; set; } = "DINHEIRO";

    [Required]
    [Range(0, 999999)]
    public decimal PaidAmount { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    [MaxLength(500)]
    public string? Observations { get; set; }
}

public class QuickSaleItemDto
{
    public Guid? ManipulationOrderId { get; set; }
    public Guid? ProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = default!;

    [Required]
    [Range(1, 9999)]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, 999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;
}

public class SaleReceiptDto
{
    public string Code { get; set; } = default!;
    public DateTime SaleDate { get; set; }
    public string? CustomerName { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string EstablishmentName { get; set; } = default!;
    public string EstablishmentAddress { get; set; } = default!;
    public string EstablishmentCnpj { get; set; } = default!;
}

public class ReceiptItemDto
{
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class DailySalesDto
{
    public DateTime Date { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalPix { get; set; }
    public Dictionary<string, int> SalesByPaymentMethod { get; set; } = new();
    public List<SaleListDto> Sales { get; set; } = new();
}

public class SaleListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string? CustomerName { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public string Status { get; set; } = default!;
}