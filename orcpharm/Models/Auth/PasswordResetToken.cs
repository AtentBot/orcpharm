using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Auth;

public class PasswordResetToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }  // ? MUDOU DE int PARA Guid

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Required]
    [StringLength(100)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    [Required]
    [StringLength(20)]
    public string Type { get; set; } = "WHATSAPP"; // WHATSAPP, EMAIL

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}