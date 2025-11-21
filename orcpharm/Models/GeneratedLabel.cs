using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("generated_labels")]
public class GeneratedLabel
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }

    [Column("template_id")]
    public Guid TemplateId { get; set; }

    [Column("label_code")]
    [MaxLength(50)]
    public string LabelCode { get; set; } = string.Empty;

    // Dados do rótulo
    [Column("patient_name")]
    [MaxLength(200)]
    public string PatientName { get; set; } = string.Empty;

    [Column("formula_name")]
    [MaxLength(200)]
    public string FormulaName { get; set; } = string.Empty;

    [Column("composition")]
    public string Composition { get; set; } = string.Empty;

    [Column("posology")]
    public string Posology { get; set; } = string.Empty;

    [Column("manipulation_date")]
    public DateTime ManipulationDate { get; set; }

    [Column("expiration_date")]
    public DateTime ExpirationDate { get; set; }

    [Column("batch_number")]
    [MaxLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    [Column("pharmacist_name")]
    [MaxLength(200)]
    public string PharmacistName { get; set; } = string.Empty;

    [Column("pharmacist_crm")]
    [MaxLength(20)]
    public string PharmacistCrm { get; set; } = string.Empty;

    [Column("warnings")]
    public string? Warnings { get; set; }

    [Column("storage_instructions")]
    public string? StorageInstructions { get; set; }

    // QR Code
    [Column("qr_code_data")]
    public string QrCodeData { get; set; } = string.Empty;

    [Column("qr_code_image_url")]
    public string? QrCodeImageUrl { get; set; }

    // HTML gerado
    [Column("generated_html")]
    public string GeneratedHtml { get; set; } = string.Empty;

    // Impressão
    [Column("print_count")]
    public int PrintCount { get; set; } = 0;

    [Column("last_printed_at")]
    public DateTime? LastPrintedAt { get; set; }

    [Column("last_printed_by_employee_id")]
    public Guid? LastPrintedByEmployeeId { get; set; }

    // Status
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "GERADO";
    // GERADO, IMPRESSO, CANCELADO

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }
}