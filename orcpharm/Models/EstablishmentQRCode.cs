using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models;

[Table("\"EstablishmentQRCodes\"")]
public class EstablishmentQRCode
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("EstablishmentId")]
    public Guid EstablishmentId { get; set; }

    [Required]
    [Column("Code")]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [Column("Name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Column("Description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; } = true;

    [Column("ScanCount")]
    public int ScanCount { get; set; } = 0;

    [Column("LastScannedAt")]
    public DateTime? LastScannedAt { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("CreatedByEmployeeId")]
    public Guid? CreatedByEmployeeId { get; set; }

    // Navigation
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey("CreatedByEmployeeId")]
    public virtual Employee? CreatedByEmployee { get; set; }
}
