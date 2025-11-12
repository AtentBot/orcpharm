using System;

namespace DTOs.Pharmacy.Formulas;

/// <summary>
/// DTO de resposta para componente de fórmula
/// </summary>
public class FormulaComponentDto
{
    public Guid Id { get; set; }
    public Guid FormulaId { get; set; }
    public Guid RawMaterialId { get; set; }
    
    // Informações da matéria-prima
    public string RawMaterialCode { get; set; } = default!;
    public string RawMaterialName { get; set; } = default!;
    public string RawMaterialUnit { get; set; } = default!;
    public bool RawMaterialIsControlled { get; set; }
    
    // Informações do componente
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public string ComponentType { get; set; } = default!;
    public int OrderIndex { get; set; }
    public string? SpecialInstructions { get; set; }
    public bool IsOptional { get; set; }
    
    // Informações calculadas
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? AvailableStock { get; set; }
    public bool HasSufficientStock { get; set; }
}
