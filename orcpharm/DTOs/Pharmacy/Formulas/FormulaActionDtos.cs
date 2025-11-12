using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.Formulas;

/// <summary>
/// DTO para aprovação de fórmula pelo farmacêutico responsável
/// </summary>
public class ApproveFormulaDto
{
    /// <summary>
    /// ID do farmacêutico aprovador
    /// </summary>
    [Required(ErrorMessage = "O ID do farmacêutico é obrigatório")]
    public Guid PharmacistId { get; set; }

    /// <summary>
    /// Observações da aprovação
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO para clonar uma fórmula (criar nova versão)
/// </summary>
public class CloneFormulaDto
{
    /// <summary>
    /// Novo código para a fórmula clonada (opcional)
    /// </summary>
    [MaxLength(50)]
    public string? NewCode { get; set; }

    /// <summary>
    /// Novo nome para a fórmula clonada (opcional)
    /// </summary>
    [MaxLength(200)]
    public string? NewName { get; set; }

    /// <summary>
    /// Descrição da mudança/motivo do clone
    /// </summary>
    [MaxLength(500)]
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// Indica se deve manter a fórmula original ativa
    /// </summary>
    public bool KeepOriginalActive { get; set; } = false;
}
