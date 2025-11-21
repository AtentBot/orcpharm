using Models.Employees;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("customers")]
public class Customer
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("full_name")]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Column("cpf")]
    [MaxLength(11)]
    public string? Cpf { get; set; }

    [Column("rg")]
    [MaxLength(20)]
    public string? Rg { get; set; }

    [Column("birth_date")]
    public DateTime? BirthDate { get; set; }

    [Column("gender")]
    [MaxLength(1)]
    public string? Gender { get; set; } // M, F, O

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("whatsapp")]
    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [Column("email")]
    [MaxLength(200)]
    public string? Email { get; set; }

    // Endereço
    [Column("zip_code")]
    [MaxLength(8)]
    public string? ZipCode { get; set; }

    [Column("street")]
    [MaxLength(200)]
    public string? Street { get; set; }

    [Column("number")]
    [MaxLength(20)]
    public string? Number { get; set; }

    [Column("complement")]
    [MaxLength(100)]
    public string? Complement { get; set; }

    [Column("neighborhood")]
    [MaxLength(100)]
    public string? Neighborhood { get; set; }

    [Column("city")]
    [MaxLength(100)]
    public string? City { get; set; }

    [Column("state")]
    [MaxLength(2)]
    public string? State { get; set; }

    // Informações médicas
    [Column("allergies")]
    public string? Allergies { get; set; }

    [Column("medical_conditions")]
    public string? MedicalConditions { get; set; }

    [Column("observations")]
    public string? Observations { get; set; }

    // LGPD
    [Column("consent_data_processing")]
    public bool ConsentDataProcessing { get; set; } = false;

    [Column("consent_date")]
    public DateTime? ConsentDate { get; set; }

    // Status
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ATIVO"; // ATIVO, INATIVO, BLOQUEADO

    [Column("block_reason")]
    public string? BlockReason { get; set; }

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }

    // Navegação
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey("CreatedByEmployeeId")]
    public virtual Employee? CreatedByEmployee { get; set; }

    [ForeignKey("UpdatedByEmployeeId")]
    public virtual Employee? UpdatedByEmployee { get; set; }
}
