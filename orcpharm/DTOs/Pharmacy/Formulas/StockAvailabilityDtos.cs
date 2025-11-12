using System;
using System.Collections.Generic;

namespace DTOs.Pharmacy.Formulas;

/// <summary>
/// DTO para verificar disponibilidade de estoque para produção de uma fórmula
/// </summary>
public class CheckStockAvailabilityDto
{
    /// <summary>
    /// ID da fórmula
    /// </summary>
    public Guid FormulaId { get; set; }

    /// <summary>
    /// Quantidade a ser produzida
    /// </summary>
    public decimal QuantityToProduce { get; set; }

    /// <summary>
    /// Multiplicador da fórmula (padrão 1.0)
    /// </summary>
    public decimal Multiplier { get; set; } = 1.0m;
}

/// <summary>
/// DTO de resposta para verificação de estoque
/// </summary>
public class StockAvailabilityResponseDto
{
    /// <summary>
    /// Indica se há estoque suficiente para todos os componentes
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Lista de componentes com suas disponibilidades
    /// </summary>
    public List<ComponentAvailabilityDto> Components { get; set; } = new();

    /// <summary>
    /// Custo estimado total da produção
    /// </summary>
    public decimal EstimatedTotalCost { get; set; }

    /// <summary>
    /// Mensagem geral sobre a disponibilidade
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// DTO para disponibilidade de um componente individual
/// </summary>
public class ComponentAvailabilityDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialCode { get; set; } = default!;
    public string RawMaterialName { get; set; } = default!;
    public decimal RequiredQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string Unit { get; set; } = default!;
    public bool IsAvailable { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public string? Notes { get; set; }
}
