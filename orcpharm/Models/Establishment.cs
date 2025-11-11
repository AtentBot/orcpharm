using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Core;

namespace Models;

[Index(nameof(Cnpj), IsUnique = true)]
[Index(nameof(City))]
[Index(nameof(State))]
public class Establishment
{
    [Key]
    public Guid Id { get; set; }

    // Chave estrangeira -> categoria
    [Required]
    public Guid CategoryId { get; set; }

    // Razão social / Nome fantasia / CNPJ
    [Required, MaxLength(200)]
    public string RazaoSocial { get; set; } = default!;

    [Required, MaxLength(200)]
    public string NomeFantasia { get; set; } = default!;

    // CNPJ apenas dígitos (formatação fica para o front/DTO)
    [Required, MaxLength(14)]
    public string Cnpj { get; set; } = default!;

    // Endereço (Brasil)
    [Required, MaxLength(200)]
    public string Street { get; set; } = default!;      // Logradouro

    [Required, MaxLength(20)]
    public string Number { get; set; } = default!;

    [MaxLength(120)]
    public string? Complement { get; set; }

    [Required, MaxLength(120)]
    public string Neighborhood { get; set; } = default!; // Bairro

    [Required, MaxLength(120)]
    public string City { get; set; } = default!;

    [Required, MaxLength(2)]
    public string State { get; set; } = default!; // UF (ex.: SP)

    [Required, MaxLength(8)]
    public string PostalCode { get; set; } = default!; // CEP (só dígitos)

    [Required, MaxLength(60)]
    public string Country { get; set; } = "Brasil";

    // Geolocalização (opcional)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Contatos
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [MaxLength(200), EmailAddress]
    public string? Email { get; set; }

    // Redes sociais
    [MaxLength(200)]
    public string? Instagram { get; set; }

    [MaxLength(200)]
    public string? Facebook { get; set; }

    [MaxLength(200)]
    public string? TikTok { get; set; }

    // Segurança de senha (armazenar apenas hash e metadados)
    [Required]
    public string PasswordHash { get; set; } = default!;

    public DateTime PasswordCreatedAt { get; set; }
    public DateTime? PasswordLastRehash { get; set; }

    [Required, MaxLength(40)]
    public string PasswordAlgorithm { get; set; } = "argon2id-v1";

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ==================== NAVEGAÇÃO ====================
    [Required]
    public Guid AccessLevelId { get; set; }

    [ForeignKey(nameof(AccessLevelId))]
    public AccessLevel? AccessLevel { get; set; }

    // 👇 ADICIONADO: Navegação para Category
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    // Status do estabelecimento
    public bool OnboardingCompleted { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public ICollection<Models.Pharmacy.Supplier>? Suppliers { get; set; }
    = new List<Models.Pharmacy.Supplier>();

}