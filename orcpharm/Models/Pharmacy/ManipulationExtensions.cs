using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Registro de perdas durante a manipulação (RDC 67/2007)
/// </summary>
[Table("manipulation_losses")]
public class ManipulationLoss
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Column("raw_material_id")]
    public Guid? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = default!;

    [Column("reason")]
    [MaxLength(500)]
    public string Reason { get; set; } = default!;

    [Column("loss_type")]
    [MaxLength(50)]
    public string LossType { get; set; } = "PROCESSO"; // PROCESSO, QUEBRA, CONTAMINACAO, VENCIMENTO, OUTRO

    [Column("registered_by_employee_id")]
    public Guid RegisteredByEmployeeId { get; set; }
    public Employee? RegisteredByEmployee { get; set; }

    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; }

    [Column("batch_number")]
    [MaxLength(50)]
    public string? BatchNumber { get; set; }

    [Column("value_lost")]
    public decimal? ValueLost { get; set; }
}

/// <summary>
/// Registro de sobras durante a manipulação
/// </summary>
[Table("manipulation_leftovers")]
public class ManipulationLeftover
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Column("raw_material_id")]
    public Guid? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = default!;

    [Column("destination")]
    [MaxLength(200)]
    public string Destination { get; set; } = default!; // REINTEGRAR_ESTOQUE, DESCARTE, OUTRA_MANIPULACAO

    [Column("destination_details")]
    [MaxLength(500)]
    public string? DestinationDetails { get; set; }

    [Column("registered_by_employee_id")]
    public Guid RegisteredByEmployeeId { get; set; }
    public Employee? RegisteredByEmployee { get; set; }

    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; }

    [Column("batch_number")]
    [MaxLength(50)]
    public string? BatchNumber { get; set; }

    [Column("reintegrated_to_stock")]
    public bool ReintegratedToStock { get; set; } = false;

    [Column("reintegration_date")]
    public DateTime? ReintegrationDate { get; set; }
}

/// <summary>
/// Conferência dupla (requisito RDC 67/2007)
/// </summary>
[Table("dual_verifications")]
public class DualVerification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Column("verification_type")]
    [MaxLength(50)]
    public string VerificationType { get; set; } = default!; // PESAGEM, ROTULAGEM, CONFERENCIA_FINAL

    // Primeira verificação (geralmente o manipulador)
    [Column("first_verifier_id")]
    public Guid FirstVerifierId { get; set; }
    public Employee? FirstVerifier { get; set; }

    [Column("first_verification_at")]
    public DateTime FirstVerificationAt { get; set; }

    [Column("first_verifier_notes")]
    [MaxLength(500)]
    public string? FirstVerifierNotes { get; set; }

    [Column("first_verifier_signature")]
    [MaxLength(500)]
    public string? FirstVerifierSignature { get; set; } // Base64 ou hash

    // Segunda verificação (conferente ou farmacêutico)
    [Column("second_verifier_id")]
    public Guid SecondVerifierId { get; set; }
    public Employee? SecondVerifier { get; set; }

    [Column("second_verification_at")]
    public DateTime? SecondVerificationAt { get; set; }

    [Column("second_verifier_notes")]
    [MaxLength(500)]
    public string? SecondVerifierNotes { get; set; }

    [Column("second_verifier_signature")]
    [MaxLength(500)]
    public string? SecondVerifierSignature { get; set; }

    // Resultado
    [Column("approved")]
    public bool Approved { get; set; }

    [Column("rejection_reason")]
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    // Detalhes da verificação
    [Column("checklist_json")]
    public string? ChecklistJson { get; set; } // JSON com itens verificados

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Registro de produção - quantidade real produzida
/// </summary>
[Table("production_records")]
public class ProductionRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Column("expected_quantity")]
    public decimal ExpectedQuantity { get; set; }

    [Column("actual_quantity")]
    public decimal ActualQuantity { get; set; }

    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = default!;

    [Column("yield_percentage")]
    public decimal YieldPercentage { get; set; }

    [Column("is_yield_acceptable")]
    public bool IsYieldAcceptable { get; set; }

    [Column("yield_deviation_reason")]
    [MaxLength(500)]
    public string? YieldDeviationReason { get; set; }

    [Column("batch_number")]
    [MaxLength(50)]
    public string BatchNumber { get; set; } = default!;

    [Column("expiry_date")]
    public DateTime ExpiryDate { get; set; }

    [Column("production_start")]
    public DateTime ProductionStart { get; set; }

    [Column("production_end")]
    public DateTime ProductionEnd { get; set; }

    [Column("total_production_time_minutes")]
    public int TotalProductionTimeMinutes { get; set; }

    [Column("produced_by_employee_id")]
    public Guid ProducedByEmployeeId { get; set; }
    public Employee? ProducedByEmployee { get; set; }

    [Column("verified_by_employee_id")]
    public Guid? VerifiedByEmployeeId { get; set; }
    public Employee? VerifiedByEmployee { get; set; }

    [Column("approved_by_pharmacist_id")]
    public Guid? ApprovedByPharmacistId { get; set; }
    public Employee? ApprovedByPharmacist { get; set; }

    [Column("quality_notes")]
    [MaxLength(1000)]
    public string? QualityNotes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
