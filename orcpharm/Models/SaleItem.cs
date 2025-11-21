using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("sale_items")]
public class SaleItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("sale_id")]
    public Guid SaleId { get; set; }

    [Column("manipulation_order_id")]
    public Guid? ManipulationOrderId { get; set; }

    [Column("prescription_id")]
    public Guid? PrescriptionId { get; set; }

    [Column("formula_id")]
    public Guid? FormulaId { get; set; }

    [Column("description")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("discount_percentage")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [Column("cost_price")]
    public decimal CostPrice { get; set; } = 0;

    [Column("profit_margin")]
    public decimal ProfitMargin { get; set; } = 0;

    [Column("observations")]
    public string? Observations { get; set; }

    // Relacionamento
    [ForeignKey("SaleId")]
    public virtual Sale? Sale { get; set; }
}