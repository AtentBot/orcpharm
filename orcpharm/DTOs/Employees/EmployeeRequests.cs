using System.ComponentModel.DataAnnotations;

namespace DTOs.Employees;

/// <summary>
/// DTO para criação de novo funcionário
/// </summary>
public record CreateEmployeeRequest
{
    [Required(ErrorMessage = "O ID do estabelecimento é obrigatório")]
    public Guid EstablishmentId { get; init; }

    [Required(ErrorMessage = "O ID do cargo é obrigatório")]
    public Guid JobPositionId { get; init; }

    // Dados Pessoais
    [Required(ErrorMessage = "O nome completo é obrigatório")]
    [MaxLength(200)]
    public string FullName { get; init; } = default!;

    [MaxLength(100)]
    public string? SocialName { get; init; }

    [Required(ErrorMessage = "O CPF é obrigatório")]
    [MaxLength(14)] // Com formatação
    public string Cpf { get; init; } = default!;

    [MaxLength(20)]
    public string? Rg { get; init; }

    [MaxLength(30)]
    public string? RgIssuer { get; init; }

    public DateOnly? RgIssueDate { get; init; }

    [Required(ErrorMessage = "A data de nascimento é obrigatória")]
    public DateOnly DateOfBirth { get; init; }

    [MaxLength(20)]
    public string? Gender { get; init; }

    // Documentos Trabalhistas
    [MaxLength(20)]
    public string? Ctps { get; init; }

    [MaxLength(10)]
    public string? CtpsSeries { get; init; }

    [MaxLength(5)]
    public string? CtpsUf { get; init; }

    public DateOnly? CtpsIssueDate { get; init; }

    [MaxLength(20)]
    public string? PisPasep { get; init; }

    // Contatos
    [MaxLength(20)]
    public string? Phone { get; init; }

    [MaxLength(20)]
    public string? WhatsApp { get; init; }

    [Required(ErrorMessage = "O e-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [MaxLength(200)]
    public string Email { get; init; } = default!;

    // Endereço
    [Required(ErrorMessage = "O logradouro é obrigatório")]
    [MaxLength(200)]
    public string Street { get; init; } = default!;

    [Required(ErrorMessage = "O número é obrigatório")]
    [MaxLength(20)]
    public string Number { get; init; } = default!;

    [MaxLength(120)]
    public string? Complement { get; init; }

    [Required(ErrorMessage = "O bairro é obrigatório")]
    [MaxLength(120)]
    public string Neighborhood { get; init; } = default!;

    [Required(ErrorMessage = "A cidade é obrigatória")]
    [MaxLength(120)]
    public string City { get; init; } = default!;

    [Required(ErrorMessage = "O estado é obrigatório")]
    [MaxLength(2)]
    public string State { get; init; } = default!;

    [Required(ErrorMessage = "O CEP é obrigatório")]
    [MaxLength(9)] // Com formatação
    public string PostalCode { get; init; } = default!;

    // Dados Trabalhistas
    [Required(ErrorMessage = "A data de admissão é obrigatória")]
    public DateOnly HireDate { get; init; }

    [Required(ErrorMessage = "O tipo de contrato é obrigatório")]
    [MaxLength(50)]
    public string ContractType { get; init; } = "CLT";

    [MaxLength(50)]
    public string? WorkShift { get; init; }

    [Required(ErrorMessage = "O salário é obrigatório")]
    [Range(0.01, 999999.99, ErrorMessage = "Salário inválido")]
    public decimal Salary { get; init; }

    [MaxLength(50)]
    public string? Department { get; init; }

    // Dados Bancários
    [MaxLength(10)]
    public string? BankCode { get; init; }

    [MaxLength(100)]
    public string? BankName { get; init; }

    [MaxLength(20)]
    public string? BankBranch { get; init; }

    [MaxLength(30)]
    public string? BankAccount { get; init; }

    [MaxLength(10)]
    public string? BankAccountType { get; init; }

    // Contato de Emergência
    [MaxLength(200)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(50)]
    public string? EmergencyContactRelationship { get; init; }

    [MaxLength(20)]
    public string? EmergencyContactPhone { get; init; }

    // Dependentes
    public int DependentsCount { get; init; } = 0;

    // Período de Experiência (padrão: 90 dias)
    public int ProbationDays { get; init; } = 90;
}

/// <summary>
/// DTO para atualização de funcionário
/// </summary>
public record UpdateEmployeeRequest
{
    [Required]
    public Guid Id { get; init; }

    public Guid? JobPositionId { get; init; }

    [MaxLength(200)]
    public string? FullName { get; init; }

    [MaxLength(100)]
    public string? SocialName { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [MaxLength(20)]
    public string? WhatsApp { get; init; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; init; }

    // Endereço
    [MaxLength(200)]
    public string? Street { get; init; }

    [MaxLength(20)]
    public string? Number { get; init; }

    [MaxLength(120)]
    public string? Complement { get; init; }

    [MaxLength(120)]
    public string? Neighborhood { get; init; }

    [MaxLength(120)]
    public string? City { get; init; }

    [MaxLength(2)]
    public string? State { get; init; }

    [MaxLength(9)]
    public string? PostalCode { get; init; }

    // Dados Bancários
    [MaxLength(10)]
    public string? BankCode { get; init; }

    [MaxLength(100)]
    public string? BankName { get; init; }

    [MaxLength(20)]
    public string? BankBranch { get; init; }

    [MaxLength(30)]
    public string? BankAccount { get; init; }

    [MaxLength(10)]
    public string? BankAccountType { get; init; }

    public decimal? Salary { get; init; }

    [MaxLength(50)]
    public string? Department { get; init; }

    [MaxLength(30)]
    public string? Status { get; init; }

    // Contato de Emergência
    [MaxLength(200)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(50)]
    public string? EmergencyContactRelationship { get; init; }

    [MaxLength(20)]
    public string? EmergencyContactPhone { get; init; }

    public int? DependentsCount { get; init; }
}

/// <summary>
/// DTO para resposta com dados do funcionário
/// </summary>
public record EmployeeResponse
{
    public Guid Id { get; init; }
    public Guid EstablishmentId { get; init; }
    public string EstablishmentName { get; init; } = default!;
    public Guid JobPositionId { get; init; }
    public string JobPositionName { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public string? SocialName { get; init; }
    public string Cpf { get; init; } = default!;
    public string? Phone { get; init; }
    public string? WhatsApp { get; init; }
    public string Email { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateOnly HireDate { get; init; }
    public DateOnly? TerminationDate { get; init; }
    public decimal Salary { get; init; }
    public bool IsInProbation { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO para mudança de cargo
/// </summary>
public record ChangeJobPositionRequest
{
    [Required]
    public Guid EmployeeId { get; init; }

    [Required]
    public Guid NewJobPositionId { get; init; }

    [Required]
    public DateOnly EffectiveDate { get; init; }

    [MaxLength(50)]
    public string? ChangeReason { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    public decimal? NewSalary { get; init; }
}

/// <summary>
/// DTO para demissão de funcionário
/// </summary>
public record TerminateEmployeeRequest
{
    [Required]
    public Guid EmployeeId { get; init; }

    [Required]
    public DateOnly TerminationDate { get; init; }

    [Required]
    [MaxLength(50)]
    public string TerminationType { get; init; } = default!; 
    // ComJustaCausa, SemJustaCausa, Pedido, Acordo, FimContrato

    [MaxLength(1000)]
    public string? TerminationReason { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    public bool RevokeAccessImmediately { get; init; } = true;
}
