using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Fiscal;

[Table("invoices")]
public class Invoice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("sale_id")]
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    [Column("invoice_number")]
    public int InvoiceNumber { get; set; }

    [Column("series")]
    public int Series { get; set; }

    [Column("invoice_key")]
    [MaxLength(44)]
    public string? InvoiceKey { get; set; }

    [Column("protocol")]
    [MaxLength(50)]
    public string? Protocol { get; set; }

    [Column("invoice_type")]
    [MaxLength(20)]
    public string InvoiceType { get; set; } = "NFCE";

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("issue_date")]
    public DateTime IssueDate { get; set; }

    [Column("authorization_date")]
    public DateTime? AuthorizationDate { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";

    [Column("cancellation_date")]
    public DateTime? CancellationDate { get; set; }

    [Column("cancellation_reason")]
    public string? CancellationReason { get; set; }

    [Column("xml_path")]
    public string? XmlPath { get; set; }

    [Column("pdf_path")]
    public string? PdfPath { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

