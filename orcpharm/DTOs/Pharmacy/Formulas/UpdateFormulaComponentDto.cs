using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.Formulas;

/// <summary>
/// DTO para atualização de um componente de fórmula
/// </summary>
public class UpdateFormulaComponentDto
{
    /// <summary>
    /// Quantidade da matéria-prima
    /// </summary>
    [Required(ErrorMessage = "A quantidade é obrigatória")]
    [Range(0.000001, 999999.999999, ErrorMessage = "A quantidade deve ser maior que zero")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unidade de medida
    /// </summary>
    [Required(ErrorMessage = "A unidade é obrigatória")]
    [MaxLength(10)]
    public string Unit { get; set; } = default!;

    /// <summary>
    /// Tipo do componente
    /// </summary>
    [Required(ErrorMessage = "O tipo do componente é obrigatório")]
    [MaxLength(20)]
    public string ComponentType { get; set; } = default!;

    /// <summary>
    /// Ordem de adição
    /// </summary>
    [Range(1, 999, ErrorMessage = "A ordem deve ser entre 1 e 999")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// Instruções especiais
    /// </summary>
    [MaxLength(500)]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Componente opcional
    /// </summary>
    public bool IsOptional { get; set; }
}
