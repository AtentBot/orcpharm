using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Registro fotográfico das etapas de manipulação
/// </summary>
[Index(nameof(ManipulationOrderId))]
[Index(nameof(ManipulationStepId))]
[Index(nameof(CapturedAt))]
public class ManipulationPhoto
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    public Guid? ManipulationStepId { get; set; }
    public ManipulationStep? ManipulationStep { get; set; }

    /// <summary>
    /// Tipo da etapa fotografada: PESAGEM, MISTURA, ENVASE, ROTULAGEM, PRODUTO_FINAL
    /// </summary>
    [Required, MaxLength(20)]
    public string StepType { get; set; } = default!;

    /// <summary>
    /// URL ou caminho da foto (pode ser base64 ou path no storage)
    /// </summary>
    [Required]
    public string PhotoUrl { get; set; } = default!;

    /// <summary>
    /// Descrição/observação sobre a foto
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Thumbnail (miniatura) para listagens
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Tamanho do arquivo em bytes
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Tipo MIME (image/jpeg, image/png, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? ContentType { get; set; }

    // Quem capturou
    [Required]
    public Guid CapturedByEmployeeId { get; set; }
    public Employee? CapturedByEmployee { get; set; }

    public DateTime CapturedAt { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }
}

