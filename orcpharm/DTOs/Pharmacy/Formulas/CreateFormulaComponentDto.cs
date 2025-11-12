using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.Formulas;

/// <summary>
/// DTO para criação de um componente de fórmula
/// </summary>
public class CreateFormulaComponentDto
{
    /// <summary>
    /// ID da matéria-prima
    /// </summary>
    [Required(ErrorMessage = "O ID da matéria-prima é obrigatório")]
    public Guid RawMaterialId { get; set; }

    /// <summary>
    /// Quantidade da matéria-prima
    /// </summary>
    [Required(ErrorMessage = "A quantidade é obrigatória")]
    [Range(0.000001, 999999.999999, ErrorMessage = "A quantidade deve ser maior que zero")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unidade de medida (g, mg, mL, UI, etc.)
    /// </summary>
    [Required(ErrorMessage = "A unidade é obrigatória")]
    [MaxLength(10)]
    public string Unit { get; set; } = default!;

    /// <summary>
    /// Tipo do componente (ATIVO, EXCIPIENTE, VEICULO, CONSERVANTE)
    /// </summary>
    [Required(ErrorMessage = "O tipo do componente é obrigatório")]
    [MaxLength(20)]
    public string ComponentType { get; set; } = "ATIVO";

    /// <summary>
    /// Ordem de adição no processo de manipulação
    /// </summary>
    [Range(1, 999, ErrorMessage = "A ordem deve ser entre 1 e 999")]
    public int OrderIndex { get; set; } = 1;

    /// <summary>
    /// Instruções especiais para este componente
    /// </summary>
    [MaxLength(500)]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Indica se o componente é opcional
    /// </summary>
    public bool IsOptional { get; set; } = false;
}
