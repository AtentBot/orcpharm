using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Common;
using DTOs.Pharmacy.ManipulationOrders;
using Models.Pharmacy;
using Models.Pharmacy.StepData;
using System.Text.Json;
using Service;

namespace Controllers;

/// <summary>
/// Extensão do ManipulationOrdersController com TODOS os endpoints de workflow
/// CONSOLIDAÇÃO: Workflow + Workflow2 + Helpers
/// </summary>
public partial class ManipulationOrdersController
{
    // ===================================================================
    // HELPER METHODS - WORKFLOW
    // ===================================================================

    /// <summary>
    /// Calcula data de validade baseada na fórmula e componentes
    /// </summary>
    [HttpGet("{id}/calculate-expiry")]
    public async Task<ActionResult<ApiResponse<DateTime>>> CalculateExpiryDate(Guid id)
    {
        var service = new ManipulationService(_context);
        var expiryDate = await service.CalculateExpiryDate(id);

        return Ok(ApiResponse<DateTime>.SuccessResponse(expiryDate));
    }

    /// <summary>
    /// Gera número de lote único
    /// </summary>
    [HttpGet("{id}/generate-batch-number")]
    public ActionResult<ApiResponse<string>> GenerateBatchNumber(Guid id)
    {
        var service = new ManipulationService(_context);
        var establishmentId = GetEstablishmentId();
        var batchNumber = service.GenerateBatchNumber(establishmentId);

        return Ok(ApiResponse<string>.SuccessResponse(batchNumber));
    }

    /// <summary>
    /// Retorna status consolidado do workflow
    /// </summary>
    [HttpGet("{id}/workflow-status")]
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflowStatus(Guid id)
    {
        var service = new ManipulationService(_context);

        var steps = await _context.ManipulationSteps
            .Where(s => s.ManipulationOrderId == id)
            .GroupBy(s => s.StepType)
            .Select(g => new
            {
                StepType = g.Key,
                Status = g.OrderByDescending(s => s.CreatedAt).First().Status,
                CompletedAt = g.OrderByDescending(s => s.CreatedAt).First().CompletedAt,
                PassedCheck = g.OrderByDescending(s => s.CreatedAt).First().PassedIntermediateCheck
            })
            .ToListAsync();

        var allCompleted = await service.AllStepsCompleted(id);

        var result = new
        {
            Steps = steps,
            AllStepsCompleted = allCompleted,
            CanProceed = steps.Any() &&
                        steps.All(s => s.Status == "CONCLUIDA" && s.PassedCheck != false)
        };

        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    // ===================================================================
    // WORKFLOW - GERAL
    // ===================================================================

    /// <summary>
    /// Gera ficha de pesagem para impressão
    /// </summary>
    [HttpGet("{id}/weighing-sheet")]
    public async Task<ActionResult> GetWeighingSheet(Guid id)
    {
        try
        {
            var service = new WeighingSheetService(_context);
            var html = await service.GenerateWeighingSheetHtml(id);

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                $"Erro ao gerar ficha: {ex.Message}"));
        }
    }

    /// <summary>
    /// Lista todas as etapas de uma ordem de manipulação
    /// </summary>
    [HttpGet("{id}/steps")]
    public async Task<ActionResult<ApiResponse<List<ManipulationStepDto>>>> GetOrderSteps(
        Guid id)
    {
        var steps = await _context.ManipulationSteps
            .Include(s => s.PerformedByEmployee)
            .Include(s => s.CheckedByEmployee)
            .Include(s => s.Photos)
                .ThenInclude(p => p.CapturedByEmployee)
            .Where(s => s.ManipulationOrderId == id)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new ManipulationStepDto
            {
                Id = s.Id,
                ManipulationOrderId = s.ManipulationOrderId,
                StepType = s.StepType,
                Status = s.Status,
                PerformedByEmployeeId = s.PerformedByEmployeeId,
                PerformedByEmployeeName = s.PerformedByEmployee!.FullName,
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt,
                StepData = s.StepData, // Retorna como JSON string
                Observations = s.Observations,
                PassedIntermediateCheck = s.PassedIntermediateCheck,
                CheckedByEmployeeId = s.CheckedByEmployeeId,
                CheckedByEmployeeName = s.CheckedByEmployee != null ?
                                       s.CheckedByEmployee.FullName : null,
                CheckNotes = s.CheckNotes,
                CheckedAt = s.CheckedAt,
                Photos = s.Photos!.Select(p => new PhotoDto
                {
                    Id = p.Id,
                    StepType = p.StepType,
                    PhotoUrl = p.PhotoUrl,
                    ThumbnailUrl = p.ThumbnailUrl,
                    Description = p.Description,
                    CapturedByEmployeeName = p.CapturedByEmployee!.FullName,
                    CapturedAt = p.CapturedAt
                }).ToList(),
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ManipulationStepDto>>.SuccessResponse(steps));
    }

    // ===================================================================
    // ETAPA 1: PESAGEM
    // ===================================================================

    [HttpPost("{id}/steps/pesagem/start")]
    public async Task<ActionResult<ApiResponse<ManipulationStepDto>>> StartPesagem(
        Guid id, [FromBody] StartPesagemDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(ApiResponse<ManipulationStepDto>.ErrorResponse(
                "Ordem não encontrada"));

        if (order.Status != "PENDENTE" && order.Status != "EM_PRODUCAO")
            return BadRequest(ApiResponse<ManipulationStepDto>.ErrorResponse(
                "Ordem deve estar PENDENTE ou EM_PRODUCAO"));

        var stepData = new PesagemStepData
        {
            Components = dto.Components.Select(c => new ComponentPesado
            {
                RawMaterialId = c.RawMaterialId,
                BatchId = c.BatchId,
                QuantidadeEsperada = c.ExpectedWeight,
                QuantidadePesada = c.ActualWeight,
                Unidade = c.Unit,
                LoteInsumo = c.BatchNumber
            }).ToList(),
            BalancaId = dto.ScaleId,
            BalancaNome = dto.ScaleName,
            AmbienteTemperatura = dto.EnvironmentTemperature,
            AmbienteUmidade = dto.EnvironmentHumidity
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "PESAGEM",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        order.Status = "PESAGEM";
        if (!order.StartDate.HasValue)
            order.StartDate = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ManipulationStepDto>.SuccessResponse(
            null!, "Pesagem concluída com sucesso"));
    }

    [HttpPost("{id}/steps/pesagem/check")]
    public async Task<ActionResult<ApiResponse>> CheckPesagem(
        Guid id, [FromBody] CheckPesagemDto dto)
    {
        var step = await _context.ManipulationSteps
            .FirstOrDefaultAsync(s => s.ManipulationOrderId == id &&
                                     s.StepType == "PESAGEM");

        if (step == null)
            return NotFound(ApiResponse.ErrorResponse("Etapa de pesagem não encontrada"));

        step.PassedIntermediateCheck = dto.Passed;
        step.CheckedByEmployeeId = dto.CheckedByEmployeeId;
        step.CheckNotes = dto.CheckNotes;
        step.CheckedAt = DateTime.UtcNow;
        step.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse(
            dto.Passed ? "Pesagem aprovada" : "Pesagem reprovada"));
    }

    // ===================================================================
    // ETAPA 2: MISTURA
    // ===================================================================

    [HttpPost("{id}/steps/mistura/start")]
    public async Task<ActionResult<ApiResponse>> StartMistura(
        Guid id, [FromBody] StartMisturaDto dto)
    {
        var employeeId = GetEmployeeId();

        var stepData = new MisturaStepData
        {
            MetodoMistura = dto.MixingMethod,
            EquipamentoUtilizado = dto.Equipment,
            TempoMistura = dto.MixingDuration,
            VelocidadeMistura = dto.MixingSpeed,
            Observacoes = dto.Observations
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "MISTURA",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        var order = await _context.ManipulationOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = "MISTURA";
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Mistura concluída com sucesso"));
    }

    // ===================================================================
    // ETAPA 3: ENVASE
    // ===================================================================

    [HttpPost("{id}/steps/envase/start")]
    public async Task<ActionResult<ApiResponse>> StartEnvase(
        Guid id, [FromBody] StartEnvaseDto dto)
    {
        var employeeId = GetEmployeeId();

        var stepData = new EnvaseStepData
        {
            TipoEmbalagem = dto.PackagingType,
            QuantidadeEnvasada = dto.PackagedQuantity,
            NumeroLote = dto.BatchNumber,
            DataFabricacao = dto.ManufacturingDate,
            DataValidade = dto.ExpiryDate,
            Observacoes = dto.Observations
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "ENVASE",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        var order = await _context.ManipulationOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = "ENVASE";
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Envase concluído com sucesso"));
    }

    // ===================================================================
    // ETAPA 4: ROTULAGEM
    // ===================================================================

    [HttpPost("{id}/steps/rotulagem/start")]
    public async Task<ActionResult<ApiResponse>> StartRotulagem(
        Guid id, [FromBody] StartRotulagemDto dto)
    {
        var employeeId = GetEmployeeId();

        var stepData = new RotulagemStepData
        {
            NumeroLote = dto.BatchNumber,
            DataFabricacao = dto.ManufacturingDate,
            DataValidade = dto.ExpiryDate,
            InformacoesAdicionais = dto.AdditionalInfo,
            CodigoBarras = dto.Barcode
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "ROTULAGEM",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        var order = await _context.ManipulationOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = "ROTULAGEM";
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Rotulagem concluída com sucesso"));
    }

    // ===================================================================
    // ETAPA 5: CONFERÊNCIA FINAL
    // ===================================================================

    [HttpPost("{id}/steps/conferencia/start")]
    public async Task<ActionResult<ApiResponse>> StartConferencia(
        Guid id, [FromBody] StartConferenciaDto dto)
    {
        var employeeId = GetEmployeeId();

        var stepData = new ConferenciaStepData
        {
            AspectosVisuais = dto.VisualAspects,
            EmbalagemIntegra = dto.PackagingIntact,
            RotuloCorreto = dto.LabelCorrect,
            QuantidadeCorreta = dto.QuantityCorrect,
            DocumentacaoCompleta = dto.DocumentationComplete,
            Observacoes = dto.Observations,
            AprovadoPorFarmaceutico = dto.ApprovedByPharmacist,
            FarmaceuticoResponsavel = dto.PharmacistName,
            CRF = dto.PharmacistCRF
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "CONFERENCIA",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            PassedIntermediateCheck = dto.ApprovedByPharmacist,
            CheckedByEmployeeId = dto.PharmacistEmployeeId,
            CheckedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        var order = await _context.ManipulationOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = dto.ApprovedByPharmacist ? "FINALIZADO" : "CONFERENCIA";
            order.PassedQualityControl = dto.ApprovedByPharmacist;
            order.ApprovedByPharmacistId = dto.PharmacistEmployeeId;

            if (dto.ApprovedByPharmacist && !order.CompletionDate.HasValue)
                order.CompletionDate = DateTime.UtcNow;

            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse(
            dto.ApprovedByPharmacist ?
            "Conferência aprovada - Ordem finalizada" :
            "Conferência registrada - Aguardando aprovação"));
    }

    // ===================================================================
    // FOTOS
    // ===================================================================

    [HttpPost("{id}/steps/{stepType}/photos")]
    public async Task<ActionResult<ApiResponse>> AddPhoto(
        Guid id, string stepType, [FromBody] AddPhotoDto dto)
    {
        var employeeId = GetEmployeeId();

        var photo = new ManipulationPhoto
        {
            ManipulationOrderId = id,
            StepType = stepType.ToUpper(),
            PhotoUrl = dto.PhotoUrl,
            ThumbnailUrl = dto.ThumbnailUrl,
            Description = dto.Description,
            CapturedByEmployeeId = employeeId,
            CapturedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.ManipulationPhotos.Add(photo);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Foto adicionada com sucesso"));
    }

    [HttpDelete("photos/{photoId}")]
    public async Task<ActionResult<ApiResponse>> DeletePhoto(Guid photoId)
    {
        var photo = await _context.ManipulationPhotos.FindAsync(photoId);

        if (photo == null)
            return NotFound(ApiResponse.ErrorResponse("Foto não encontrada"));

        _context.ManipulationPhotos.Remove(photo);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Foto removida com sucesso"));
    }
}