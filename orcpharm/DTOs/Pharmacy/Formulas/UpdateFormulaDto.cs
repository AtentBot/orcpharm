using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.Formulas;

/// <summary>
/// DTO para atualização de uma fórmula existente
/// </summary>
public class UpdateFormulaDto
{
    /// <summary>
    /// Nome da fórmula
    /// </summary>
    [Required(ErrorMessage = "O nome da fórmula é obrigatório")]
    [MaxLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Descrição detalhada da fórmula
    /// </summary>
    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
    public string? Description { get; set; }

    /// <summary>
    /// Categoria da fórmula
    /// </summary>
    [Required(ErrorMessage = "A categoria é obrigatória")]
    [MaxLength(50)]
    public string Category { get; set; } = default!;

    /// <summary>
    /// Forma farmacêutica
    /// </summary>
    [Required(ErrorMessage = "A forma farmacêutica é obrigatória")]
    [MaxLength(50)]
    public string PharmaceuticalForm { get; set; } = default!;

    /// <summary>
    /// Rendimento padrão da fórmula
    /// </summary>
    [Required(ErrorMessage = "O rendimento padrão é obrigatório")]
    [Range(0.01, 999999.99, ErrorMessage = "O rendimento deve ser maior que zero")]
    public decimal StandardYield { get; set; }

    /// <summary>
    /// Prazo de validade em dias
    /// </summary>
    [Required(ErrorMessage = "O prazo de validade é obrigatório")]
    [Range(1, 3650, ErrorMessage = "O prazo de validade deve estar entre 1 e 3650 dias")]
    public int ShelfLifeDays { get; set; }

    /// <summary>
    /// Instruções de preparação
    /// </summary>
    [MaxLength(2000)]
    public string? PreparationInstructions { get; set; }

    /// <summary>
    /// Instruções de armazenamento
    /// </summary>
    [MaxLength(1000)]
    public string? StorageInstructions { get; set; }

    /// <summary>
    /// Instruções de uso
    /// </summary>
    [MaxLength(1000)]
    public string? UsageInstructions { get; set; }

    /// <summary>
    /// Requer controle especial
    /// </summary>
    public bool RequiresSpecialControl { get; set; }

    /// <summary>
    /// Requer prescrição médica
    /// </summary>
    public bool RequiresPrescription { get; set; }

    /// <summary>
    /// Status ativo/inativo
    /// </summary>
    public bool IsActive { get; set; }
}
