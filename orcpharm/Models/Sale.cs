using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

namespace Models.Pharmacy;

[Index(nameof(SaleNumber), nameof(EstablishmentId), IsUnique = true)]
[Index(nameof(SaleDate))]
[Index(nameof(Status))]
public class Sale
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EstablishmentId { get; set; }
    public Establishment? Establishment { get; set; }

    [Required, MaxLength(50)]
    public string SaleNumber { get; set; } = default!;

    public DateTime SaleDate { get; set; }

    // Cliente
    [Required, MaxLength(200)]
    public string CustomerName { get; set; } = default!;

    [MaxLength(11)]
    public string? CustomerCpf { get; set; }

    [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? CustomerEmail { get; set; }

    // Prescrição (quando aplicável)
    [MaxLength(50)]
    public string? PrescriptionNumber { get; set; }

    [MaxLength(200)]
    public string? PrescriberName { get; set; }

    [MaxLength(50)]
    public string? PrescriberRegistration { get; set; } // CRM/CRO/CRMV

    public DateTime? PrescriptionDate { get; set; }

    // Valores
    [Column(TypeName = "decimal(10,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercent { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    // Pagamento
    [Required, MaxLength(20)]
    public string PaymentMethod { get; set; } = default!;
    // DINHEIRO, CARTAO_CREDITO, CARTAO_DEBITO, PIX, CONVENIO

    [MaxLength(50)]
    public string? PaymentReference { get; set; }

    // Fiscal
    [MaxLength(50)]
    public string? InvoiceNumber { get; set; }

    [MaxLength(100)]
    public string? InvoiceKey { get; set; } // Chave NFe/NFCe

    // Status
    [Required, MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";
    // PENDENTE, PAGO, CANCELADO, DEVOLVIDO

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    // Auditoria
    public Guid SoldByEmployeeId { get; set; }
    public Employee? SoldByEmployee { get; set; }

    public Guid? AuthorizedByPharmacistId { get; set; }
    public Employee? AuthorizedByPharmacist { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CanceledAt { get; set; }

    // Navegação
    public ICollection<SaleItem>? Items { get; set; }
}