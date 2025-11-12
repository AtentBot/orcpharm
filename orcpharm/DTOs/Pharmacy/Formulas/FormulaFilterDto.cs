using System;

namespace DTOs.Pharmacy.Formulas;

/// <summary>
/// DTO para filtros de consulta de fórmulas
/// </summary>
public class FormulaFilterDto
{
    /// <summary>
    /// Filtrar por código (busca parcial)
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Filtrar por nome (busca parcial)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filtrar por categoria
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filtrar por forma farmacêutica
    /// </summary>
    public string? PharmaceuticalForm { get; set; }

    /// <summary>
    /// Filtrar apenas fórmulas ativas
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filtrar apenas fórmulas que requerem controle especial
    /// </summary>
    public bool? RequiresSpecialControl { get; set; }

    /// <summary>
    /// Filtrar apenas fórmulas que requerem prescrição
    /// </summary>
    public bool? RequiresPrescription { get; set; }

    /// <summary>
    /// Filtrar apenas fórmulas aprovadas
    /// </summary>
    public bool? OnlyApproved { get; set; }

    /// <summary>
    /// Filtrar por ID da matéria-prima (fórmulas que contêm esta matéria-prima)
    /// </summary>
    public Guid? ContainsRawMaterialId { get; set; }

    /// <summary>
    /// Filtrar por data de criação inicial
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filtrar por data de criação final
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Número da página (para paginação)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Tamanho da página (para paginação)
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Campo para ordenação
    /// </summary>
    public string? SortBy { get; set; } = "Name";

    /// <summary>
    /// Ordem crescente ou decrescente
    /// </summary>
    public bool Ascending { get; set; } = true;
}
