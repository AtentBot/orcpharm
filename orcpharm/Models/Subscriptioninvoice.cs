using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Billing;

/// <summary>
/// Representa uma fatura de cobrança da assinatura SaaS
/// (A farmácia PAGA ao OrcPharm para usar o sistema)
/// </summary>
[Table("subscription_invoices")]
public class SubscriptionInvoice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("subscription_id")]
    [Required]
    public Guid SubscriptionId { get; set; }

    [Column("stripe_invoice_id")]
    [MaxLength(255)]
    public string? StripeInvoiceId { get; set; }

    [Column("amount")]
    [Required]
    public decimal Amount { get; set; }

    [Column("status")]
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "PENDING"; // PAID, PENDING, FAILED

    [Column("invoice_url")]
    [MaxLength(500)]
    public string? InvoiceUrl { get; set; }

    [Column("invoice_pdf_url")]
    [MaxLength(500)]
    public string? InvoicePdfUrl { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("SubscriptionId")]
    public Subscription? Subscription { get; set; }
}