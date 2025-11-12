using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Pharmacy;

namespace Models;

[Table("batches")]
public class Batch
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("batch_number")]
    [StringLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    [Required]
    [Column("raw_material_id")]
    public int RawMaterialId { get; set; }

    [ForeignKey("RawMaterialId")]
    public RawMaterial? RawMaterial { get; set; }

    [Column("supplier_id")]
    public int? SupplierId { get; set; }

    [Required]
    [Column("manufacturing_date")]
    public DateTime ManufacturingDate { get; set; }

    [Required]
    [Column("expiration_date")]
    public DateTime ExpirationDate { get; set; }

    [Column("receipt_date")]
    public DateTime ReceiptDate { get; set; }

    [Required]
    [Column("initial_quantity")]
    [Precision(18, 4)]
    public decimal InitialQuantity { get; set; }

    [Required]
    [Column("current_quantity")]
    [Precision(18, 4)]
    public decimal CurrentQuantity { get; set; }

    [Required]
    [Column("unit")]
    [StringLength(10)]
    public string Unit { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "QUARANTINE"; // QUARANTINE, APPROVED, REJECTED, EXPIRED, DEPLETED

    [Column("quarantine_until")]
    public DateTime? QuarantineUntil { get; set; }

    [Column("quality_certificate_number")]
    [StringLength(100)]
    public string? QualityCertificateNumber { get; set; }

    [Column("quality_certificate_date")]
    public DateTime? QualityCertificateDate { get; set; }

    [Column("purity_percentage")]
    [Precision(5, 2)]
    public decimal? PurityPercentage { get; set; }

    [Column("humidity_percentage")]
    [Precision(5, 2)]
    public decimal? HumidityPercentage { get; set; }

    [Column("correction_factor")]
    [Precision(10, 6)]
    public decimal CorrectionFactor { get; set; } = 1.0m;

    [Column("storage_location")]
    [StringLength(50)]
    public string? StorageLocation { get; set; }

    [Column("storage_temperature_min")]
    [Precision(5, 2)]
    public decimal? StorageTemperatureMin { get; set; }

    [Column("storage_temperature_max")]
    [Precision(5, 2)]
    public decimal? StorageTemperatureMax { get; set; }

    [Column("requires_refrigeration")]
    public bool RequiresRefrigeration { get; set; }

    [Column("requires_light_protection")]
    public bool RequiresLightProtection { get; set; }

    [Column("invoice_number")]
    [StringLength(50)]
    public string? InvoiceNumber { get; set; }

    [Column("invoice_date")]
    public DateTime? InvoiceDate { get; set; }

    [Column("unit_cost")]
    [Precision(18, 4)]
    public decimal? UnitCost { get; set; }

    [Column("total_cost")]
    [Precision(18, 2)]
    public decimal? TotalCost { get; set; }

    [Column("approved_by_employee_id")]
    public int? ApprovedByEmployeeId { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("rejected_by_employee_id")]
    public int? RejectedByEmployeeId { get; set; }

    [Column("rejected_at")]
    public DateTime? RejectedAt { get; set; }

    [Column("rejection_reason")]
    [StringLength(500)]
    public string? RejectionReason { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Column("establishment_id")]
    public int EstablishmentId { get; set; }

    [ForeignKey("EstablishmentId")]
    public Establishment? Establishment { get; set; }

    [Required]
    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_by")]
    public int? DeletedBy { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Calculated properties
    [NotMapped]
    public int DaysUntilExpiration => (ExpirationDate.Date - DateTime.UtcNow.Date).Days;

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow.Date >= ExpirationDate.Date;

    [NotMapped]
    public bool IsNearExpiration => DaysUntilExpiration <= 90 && DaysUntilExpiration > 0;

    [NotMapped]
    public decimal UsagePercentage => InitialQuantity > 0 ? ((InitialQuantity - CurrentQuantity) / InitialQuantity) * 100 : 0;

    [NotMapped]
    public bool IsInQuarantine => Status == "QUARANTINE" && (QuarantineUntil == null || DateTime.UtcNow < QuarantineUntil);

    [NotMapped]
    public bool CanBeUsed => Status == "APPROVED" && !IsExpired && CurrentQuantity > 0 && IsActive;
}
