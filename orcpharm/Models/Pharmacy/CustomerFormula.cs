using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

[Table("CustomerFormulas")]
public class CustomerFormula
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("Code")]
    public string Code { get; set; } = default!;

    // VÍNCULOS
    [Required]
    [Column("EstablishmentId")]
    public Guid EstablishmentId { get; set; }

    [Column("CustomerId")]
    public Guid? CustomerId { get; set; }

    // Dados do Cliente (mesmo sem cadastro)
    [MaxLength(200)]
    [Column("CustomerName")]
    public string? CustomerName { get; set; }

    [MaxLength(20)]
    [Column("CustomerPhone")]
    public string? CustomerPhone { get; set; }

    [MaxLength(200)]
    [Column("CustomerEmail")]
    public string? CustomerEmail { get; set; }

    // Configuração do Produto
    [Required]
    [Column("ProductTypeId")]
    public Guid ProductTypeId { get; set; }
    
    [ForeignKey("ProductTypeId")]
    public ProductType? ProductType { get; set; }

    [Required]
    [Column("ProductSubTypeId")]
    public Guid ProductSubTypeId { get; set; }
    
    [ForeignKey("ProductSubTypeId")]
    public ProductSubType? ProductSubType { get; set; }

    [Required]
    [Column("Quantity")]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("Unit")]
    public string Unit { get; set; } = default!;

    // Componentes Adicionais (JSON)
    [Column("AdditionalIngredients", TypeName = "jsonb")]
    public string? AdditionalIngredients { get; set; }

    [Column("CustomerNotes")]
    public string? CustomerNotes { get; set; }

    // FLUXO DE APROVAÇÃO
    [Required]
    [MaxLength(30)]
    [Column("Status")]
    public string Status { get; set; } = "AGUARDANDO_COMPRA";

    // Análise Farmacêutica
    [Column("PharmacistId")]
    public Guid? PharmacistId { get; set; }

    [Column("PharmaceuticalAnalysis")]
    public string? PharmaceuticalAnalysis { get; set; }

    [Column("AnalyzedAt")]
    public DateTime? AnalyzedAt { get; set; }

    [Column("ApprovedAt")]
    public DateTime? ApprovedAt { get; set; }

    [Column("RejectedAt")]
    public DateTime? RejectedAt { get; set; }

    [Column("RejectionReason")]
    public string? RejectionReason { get; set; }

    [Column("AdjustmentRequest")]
    public string? AdjustmentRequest { get; set; }

    // Validações Técnicas
    [Column("RequiresPrescription")]
    public bool RequiresPrescription { get; set; } = false;

    [Column("IsControlledSubstance")]
    public bool IsControlledSubstance { get; set; } = false;

    [Column("HasIncompatibilities")]
    public bool HasIncompatibilities { get; set; } = false;

    [Column("IncompatibilityDetails")]
    public string? IncompatibilityDetails { get; set; }

    [Column("EstimatedShelfLifeDays")]
    public int? EstimatedShelfLifeDays { get; set; }

    // Precificação
    [Column("EstimatedPrice")]
    public decimal? EstimatedPrice { get; set; }

    [Column("FinalPrice")]
    public decimal? FinalPrice { get; set; }

    [Column("DiscountApplied")]
    public decimal DiscountApplied { get; set; } = 0;

    // Relacionamentos
    [Column("PrescriptionQuoteId")]
    public Guid? PrescriptionQuoteId { get; set; }

    [Column("ManipulationOrderId")]
    public Guid? ManipulationOrderId { get; set; }

    [Column("OnlineOrderId")]
    public Guid? OnlineOrderId { get; set; }

    // Pagamento
    [Column("PaidAt")]
    public DateTime? PaidAt { get; set; }

    [Column("PaidAmount")]
    public decimal? PaidAmount { get; set; }

    [Column("RequiresRefund")]
    public bool RequiresRefund { get; set; } = false;

    [Column("RefundedAt")]
    public DateTime? RefundedAt { get; set; }

    [Column("RefundAmount")]
    public decimal? RefundAmount { get; set; }

    // Token para Sessão
    [MaxLength(100)]
    [Column("SessionToken")]
    public string? SessionToken { get; set; }

    [Column("SessionExpiresAt")]
    public DateTime? SessionExpiresAt { get; set; }

    // Auditoria
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("CreatedByEmployeeId")]
    public Guid? CreatedByEmployeeId { get; set; }

    // Navigation Properties
    public ICollection<PharmaceuticalAnalysisLog>? AnalysisLogs { get; set; }
    public ICollection<Refund>? Refunds { get; set; }
}
