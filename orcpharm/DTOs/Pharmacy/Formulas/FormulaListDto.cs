using System;

namespace DTOs.Pharmacy.Formulas;

/// <summary>
/// DTO para resposta básica de fórmula (usado em listagens)
/// </summary>
public class FormulaListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Category { get; set; } = default!;
    public string PharmaceuticalForm { get; set; } = default!;
    public decimal StandardYield { get; set; }
    public int ShelfLifeDays { get; set; }
    public bool RequiresSpecialControl { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int ComponentCount { get; set; } // Quantidade de componentes
}
