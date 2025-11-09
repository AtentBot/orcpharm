using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Models.Pharmacy;

[Index(nameof(Cnpj), IsUnique = true)]
[Index(nameof(EstablishmentId), nameof(IsActive))]
[Index(nameof(IsQualified))]
public class Supplier
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EstablishmentId { get; set; }
    public Establishment? Establishment { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(14)]
    public string Cnpj { get; set; } = default!;

    [MaxLength(200)]
    public string? TradeName { get; set; }

    // Documentação ANVISA
    [MaxLength(50)]
    public string? AfeNumber { get; set; } // Autorização de Funcionamento

    public DateTime? AfeExpiryDate { get; set; }

    [MaxLength(50)]
    public string? SpecialAuthorizationNumber { get; set; } // Para controlados

    public DateTime? SpecialAuthorizationExpiry { get; set; }

    // Contato
    [MaxLength(200)]
    public string? ContactName { get; set; }

    [Required, MaxLength(20)]
    public string Phone { get; set; } = default!;

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    // Endereço
    [Required, MaxLength(200)]
    public string Street { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Number { get; set; } = default!;

    [MaxLength(100)]
    public string? Complement { get; set; }

    [Required, MaxLength(100)]
    public string Neighborhood { get; set; } = default!;

    [Required, MaxLength(100)]
    public string City { get; set; } = default!;

    [Required, MaxLength(2)]
    public string State { get; set; } = default!;

    [Required, MaxLength(8)]
    public string PostalCode { get; set; } = default!;

    // Qualificação
    public bool IsQualified { get; set; } = false;
    public DateTime? QualificationDate { get; set; }
    public DateTime? NextQualificationDate { get; set; }

    [MaxLength(1000)]
    public string? QualificationNotes { get; set; }

    // Categorias permitidas
    public bool SuppliesControlled { get; set; } = false;
    public bool SuppliesAntibiotics { get; set; } = false;
    public bool SuppliesHormones { get; set; } = false;

    // Status
    public bool IsActive { get; set; } = true;

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByEmployeeId { get; set; }
    public Guid? UpdatedByEmployeeId { get; set; }

    // Navegação
    public ICollection<Batch>? Batches { get; set; }
}
