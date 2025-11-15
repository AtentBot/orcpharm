using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Auth;

public class TwoFactorAuth
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }  // ? MUDOU DE int PARA Guid

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [Required]
    [StringLength(50)]
    public string Purpose { get; set; } = string.Empty; // LOGIN, CONTROLLED_SUBSTANCE

    [StringLength(100)]
    public string? IpAddress { get; set; }

    [StringLength(200)]
    public string? UserAgent { get; set; }

    public int Attempts { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}