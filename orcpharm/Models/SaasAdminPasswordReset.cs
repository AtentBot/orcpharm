using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("saas_admin_password_resets")]
public class SaasAdminPasswordReset
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("saas_admin_id")]
    public Guid SaasAdminId { get; set; }

    [Required]
    [Column("token")]
    [StringLength(128)]
    public string Token { get; set; } = string.Empty;

    [Column("ip_address")]
    [StringLength(45)]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    [StringLength(500)]
    public string? UserAgent { get; set; }

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [Column("is_used")]
    public bool IsUsed { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegação
    [ForeignKey("SaasAdminId")]
    public virtual SaasAdmin? SaasAdmin { get; set; }
}
