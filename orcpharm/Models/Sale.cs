using Models.Pharmacy;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("sales")]
public class Sale
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("sale_date")]
    public DateTime SaleDate { get; set; }

    // Valores
    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("discount_percentage")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    // Pagamento
    [Column("payment_method")]
    [MaxLength(20)]
    public string PaymentMethod { get; set; } = "DINHEIRO";
    // DINHEIRO, CARTAO_CREDITO, CARTAO_DEBITO, PIX, BOLETO

    [Column("payment_status")]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "PAGO";
    // PENDENTE, PAGO, CANCELADO

    [Column("paid_amount")]
    public decimal PaidAmount { get; set; }

    [Column("change_amount")]
    public decimal ChangeAmount { get; set; } = 0;

    [Column("payment_date")]
    public DateTime? PaymentDate { get; set; }

    // Nota Fiscal
    [Column("invoice_number")]
    [MaxLength(50)]
    public string? InvoiceNumber { get; set; }

    [Column("invoice_key")]
    [MaxLength(44)]
    public string? InvoiceKey { get; set; }

    [Column("invoice_status")]
    [MaxLength(20)]
    public string? InvoiceStatus { get; set; }

    // Status
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "FINALIZADA";
    // ORCAMENTO, FINALIZADA, CANCELADA

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("cancelled_by_employee_id")]
    public Guid? CancelledByEmployeeId { get; set; }

    [Column("cancellation_reason")]
    public string? CancellationReason { get; set; }

    // Observações
    [Column("observations")]
    public string? Observations { get; set; }

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }

    // Relacionamentos
    public virtual ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}