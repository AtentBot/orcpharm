using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Pharmacy;

[Index(nameof(SaleId))]
public class SaleItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid? ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Required, MaxLength(200)]
    public string Description { get; set; } = default!;

    [Column(TypeName = "decimal(10,4)")]
    public decimal Quantity { get; set; }

    [Required, MaxLength(10)]
    public string Unit { get; set; } = default!;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }

    // Para produtos manipulados
    [MaxLength(50)]
    public string? BatchNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }
}