using System;
using System.Collections.Generic;

namespace DTOs.Pharmacy.Formulas;

/// <summary>
/// DTO para detalhes completos de uma fórmula (incluindo componentes)
/// </summary>
public class FormulaDetailsDto
{
    public Guid Id { get; set; }
    public Guid EstablishmentId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Category { get; set; } = default!;
    public string PharmaceuticalForm { get; set; } = default!;
    public decimal StandardYield { get; set; }
    public int ShelfLifeDays { get; set; }
    public string? PreparationInstructions { get; set; }
    public string? StorageInstructions { get; set; }
    public string? UsageInstructions { get; set; }
    public bool RequiresSpecialControl { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public Guid? PreviousVersionId { get; set; }
    
    // Auditoria
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByEmployeeId { get; set; }
    public string CreatedByEmployeeName { get; set; } = default!;
    public Guid? UpdatedByEmployeeId { get; set; }
    public string? UpdatedByEmployeeName { get; set; }
    public Guid? ApprovedByPharmacistId { get; set; }
    public string? ApprovedByPharmacistName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Componentes
    public List<FormulaComponentDto> Components { get; set; } = new();

    // Informações calculadas
    public decimal EstimatedCost { get; set; }
    public bool HasAvailableStock { get; set; }
}
