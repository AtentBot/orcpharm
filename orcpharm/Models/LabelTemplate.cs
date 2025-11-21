using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("label_templates")]
public class LabelTemplate
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("template_type")]
    [MaxLength(50)]
    public string TemplateType { get; set; } = "PADRAO";
    // PADRAO, CONTROLADO, TARJA_PRETA, TARJA_VERMELHA, REFRIGERADO

    [Column("pharmaceutical_form")]
    [MaxLength(50)]
    public string? PharmaceuticalForm { get; set; }

    // Dimensões (em mm)
    [Column("width")]
    public decimal Width { get; set; } = 100;

    [Column("height")]
    public decimal Height { get; set; } = 50;

    // Layout HTML/CSS
    [Column("html_template")]
    public string HtmlTemplate { get; set; } = string.Empty;

    [Column("css_styles")]
    public string? CssStyles { get; set; }

    // Campos obrigatórios RDC 67/2007
    [Column("include_establishment_name")]
    public bool IncludeEstablishmentName { get; set; } = true;

    [Column("include_pharmacist_name")]
    public bool IncludePharmacistName { get; set; } = true;

    [Column("include_formula_name")]
    public bool IncludeFormulaName { get; set; } = true;

    [Column("include_composition")]
    public bool IncludeComposition { get; set; } = true;

    [Column("include_posology")]
    public bool IncludePosology { get; set; } = true;

    [Column("include_validity")]
    public bool IncludeValidity { get; set; } = true;

    [Column("include_batch_number")]
    public bool IncludeBatchNumber { get; set; } = true;

    [Column("include_manipulation_date")]
    public bool IncludeManipulationDate { get; set; } = true;

    [Column("include_patient_name")]
    public bool IncludePatientName { get; set; } = true;

    [Column("include_qr_code")]
    public bool IncludeQrCode { get; set; } = true;

    [Column("include_warnings")]
    public bool IncludeWarnings { get; set; } = true;

    // Status
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }
}
