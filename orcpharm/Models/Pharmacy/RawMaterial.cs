using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Pharmacy;

[Index(nameof(DcbCode), IsUnique = true)]
[Index(nameof(Name))]
[Index(nameof(ControlType))]
[Index(nameof(EstablishmentId), nameof(IsActive))]
public class RawMaterial
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EstablishmentId { get; set; }
    public Establishment? Establishment { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    [MaxLength(50)]
    public string? DcbCode { get; set; } // Denominação Comum Brasileira

    [MaxLength(50)]
    public string? DciCode { get; set; } // Denominação Comum Internacional

    [Required, MaxLength(50)]
    public string CasNumber { get; set; } = default!; // Chemical Abstracts Service

    [MaxLength(500)]
    public string? Description { get; set; }

    // Classificação regulatória
    [Required, MaxLength(20)]
    public string ControlType { get; set; } = "COMUM";
    // COMUM, LISTA_A, LISTA_B, LISTA_C1, LISTA_C2, ANTIMICROBIANO, HORMONIO

    // Unidade de medida
    [Required, MaxLength(10)]
    public string Unit { get; set; } = "g"; // g, mg, mL, UI

    // Fatores de correção
    [Column(TypeName = "decimal(10,4)")]
    public decimal PurityFactor { get; set; } = 1.0m;

    [Column(TypeName = "decimal(10,4)")]
    public decimal EquivalenceFactor { get; set; } = 1.0m;

    // Estoque
    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentStock { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinimumStock { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaximumStock { get; set; } = 0;

    // Condições de armazenamento
    [MaxLength(200)]
    public string? StorageConditions { get; set; }

    public bool RequiresRefrigeration { get; set; } = false;
    public bool LightSensitive { get; set; } = false;
    public bool HumiditySensitive { get; set; } = false;

    // Status e auditoria
    public bool IsActive { get; set; } = true;
    public bool RequiresSpecialAuthorization { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedByEmployeeId { get; set; }
    public Guid? UpdatedByEmployeeId { get; set; }

    // Navegação
    public ICollection<Batch>? Batches { get; set; }
    public ICollection<FormulaComponent>? FormulaComponents { get; set; }
    public ICollection<StockMovement>? StockMovements { get; set; }
}