using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Pharmacy;

[Index(nameof(DcbCode), IsUnique = true)]
[Index(nameof(Name))]
[Index(nameof(ControlType))]
[Index(nameof(EstablishmentId), nameof(IsActive))]
[Index(nameof(Category))]
[Index(nameof(IsVirtual))]

[Table("RawMaterials")]
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

    // ════════════════════════════════════════════════════════════════════════
    // PRECIFICAÇÃO INTELIGENTE
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Preço base de referência de mercado (R$/unidade)
    /// Usado como fallback quando não há estoque nem histórico
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? BasePrice { get; set; }

    /// <summary>
    /// Último preço efetivamente pago (R$/unidade)
    /// Atualizado automaticamente quando batch é aprovado
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? LastKnownPrice { get; set; }

    /// <summary>
    /// Data do último preço pago
    /// </summary>
    public DateTime? LastPriceDate { get; set; }

    /// <summary>
    /// TRUE = ingrediente virtual, nunca teve em estoque físico
    /// Usado para ingredientes importados do catálogo base
    /// </summary>
    public bool IsVirtual { get; set; } = false;

    /// <summary>
    /// Origem do preço atual: ESTOQUE, HISTORICO, BASE
    /// </summary>
    [MaxLength(20)]
    public string PriceSource { get; set; } = "BASE";

    // ════════════════════════════════════════════════════════════════════════
    // CATEGORIZAÇÃO E BUSCA
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Categoria do ingrediente (Vitaminas, Minerais, Aminoácidos, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Nomes alternativos separados por vírgula
    /// Ex: "Vitamina D, Colecalciferol, D3"
    /// </summary>
    [MaxLength(500)]
    public string? Synonyms { get; set; }

    /// <summary>
    /// Indicações terapêuticas principais
    /// Ex: "Ossos, imunidade, absorção de cálcio"
    /// </summary>
    [MaxLength(1000)]
    public string? Indications { get; set; }

    /// <summary>
    /// Popularidade de 1-100 para ordenação em buscas
    /// Ingredientes mais usados = maior popularidade
    /// </summary>
    public int Popularity { get; set; } = 50;

    // ════════════════════════════════════════════════════════════════════════
    // STATUS E AUDITORIA
    // ════════════════════════════════════════════════════════════════════════

    public bool IsActive { get; set; } = true;
    public bool RequiresSpecialAuthorization { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedByEmployeeId { get; set; }
    public Guid? UpdatedByEmployeeId { get; set; }

    // ════════════════════════════════════════════════════════════════════════
    // NAVEGAÇÃO
    // ════════════════════════════════════════════════════════════════════════

    public ICollection<Batch>? Batches { get; set; }
    public ICollection<FormulaComponent>? FormulaComponents { get; set; }
    public ICollection<StockMovement>? StockMovements { get; set; }
}
