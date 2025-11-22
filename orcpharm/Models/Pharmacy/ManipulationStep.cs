using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Registra cada etapa do processo de manipulação
/// </summary>
[Index(nameof(ManipulationOrderId), nameof(StepType))]
[Index(nameof(StartedAt))]
[Index(nameof(Status))]
public class ManipulationStep
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    /// <summary>
    /// Tipo da etapa: PESAGEM, MISTURA, ENVASE, ROTULAGEM, CONFERENCIA
    /// </summary>
    [Required, MaxLength(20)]
    public string StepType { get; set; } = default!;

    /// <summary>
    /// Status: PENDENTE, EM_EXECUCAO, CONCLUIDA, REJEITADA
    /// </summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";

    // Responsável pela etapa
    [Required]
    public Guid PerformedByEmployeeId { get; set; }
    public Employee? PerformedByEmployee { get; set; }

    // Datas
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Dados específicos por etapa (armazenados como JSON)
    [Column(TypeName = "jsonb")]
    public string? StepData { get; set; }

    // Observações gerais
    [MaxLength(2000)]
    public string? Observations { get; set; }

    // Controle de qualidade intermediário
    public bool? PassedIntermediateCheck { get; set; }
    public Guid? CheckedByEmployeeId { get; set; }
    public Employee? CheckedByEmployee { get; set; }

    [MaxLength(1000)]
    public string? CheckNotes { get; set; }
    public DateTime? CheckedAt { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navegação
    public ICollection<ManipulationPhoto>? Photos { get; set; }
}
