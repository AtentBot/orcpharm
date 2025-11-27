using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("sale_items")]  // ← Tabela correta
public class SaleItem  // ← Classe correta
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("sale_id")]
    public Guid SaleId { get; set; }

    [Column("manipulation_order_id")]
    public Guid? ManipulationOrderId { get; set; }

    [Column("prescription_id")]
    public Guid? PrescriptionId { get; set; }

    [Column("formula_id")]
    public Guid? FormulaId { get; set; }

    [Required]
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
    [MaxLength(1000)]
    public string? Observations { get; set; }

    [ForeignKey("SaleId")]
    public virtual Sale Sale { get; set; } = null!;
}