using Data;
using Microsoft.EntityFrameworkCore;

namespace Service;

public class ManipulationService
{
    private readonly AppDbContext _context;

    public ManipulationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DateTime> CalculateExpiryDate(Guid manipulationOrderId)
    {
        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == manipulationOrderId);

        if (order?.Formula?.Components == null || !order.Formula.Components.Any())
            return DateTime.UtcNow.AddMonths(6);

        var pesagemStep = await _context.ManipulationSteps
            .FirstOrDefaultAsync(s => s.ManipulationOrderId == manipulationOrderId
                && s.StepType == "PESAGEM"
                && s.Status == "CONCLUIDA");

        if (pesagemStep?.StepData == null)
            return DateTime.UtcNow.AddMonths(6);

        var pesagemData = System.Text.Json.JsonSerializer.Deserialize<Models.Pharmacy.StepData.PesagemStepData>(pesagemStep.StepData);

        if (pesagemData?.Components == null || !pesagemData.Components.Any())
            return DateTime.UtcNow.AddMonths(6);

        DateTime? earliestExpiry = null;

        foreach (var comp in pesagemData.Components)
        {
            var batch = await _context.Batches
                .FirstOrDefaultAsync(b => b.Id == comp.BatchId);

            if (batch?.ExpiryDate != null)
            {
                if (earliestExpiry == null || batch.ExpiryDate < earliestExpiry)
                    earliestExpiry = batch.ExpiryDate;
            }
        }

        if (earliestExpiry.HasValue)
        {
            var manipulatedProductExpiry = earliestExpiry.Value.AddMonths(-3);

            if (manipulatedProductExpiry <= DateTime.UtcNow)
                manipulatedProductExpiry = DateTime.UtcNow.AddMonths(1);

            return manipulatedProductExpiry;
        }

        return DateTime.UtcNow.AddMonths(6);
    }

    public string GenerateBatchNumber(Guid establishmentId)
    {
        var date = DateTime.Now;
        var random = new Random();
        var seq = random.Next(1000, 9999);

        return $"LOT-{date:yyyyMM}-{seq}";
    }

    public async Task<decimal> CalculateYieldPercentage(Guid manipulationOrderId, decimal actualQuantity)
    {
        var order = await _context.ManipulationOrders
            .FirstOrDefaultAsync(o => o.Id == manipulationOrderId);

        if (order == null || order.QuantityToProduce == 0)
            return 0;

        return Math.Round((actualQuantity / order.QuantityToProduce) * 100, 2);
    }

    public async Task<bool> AllStepsCompleted(Guid manipulationOrderId)
    {
        var requiredSteps = new[] { "PESAGEM", "MISTURA", "ENVASE", "ROTULAGEM", "CONFERENCIA" };

        var completedSteps = await _context.ManipulationSteps
            .Where(s => s.ManipulationOrderId == manipulationOrderId && s.Status == "CONCLUIDA")
            .Select(s => s.StepType)
            .Distinct()
            .ToListAsync();

        return requiredSteps.All(step => completedSteps.Contains(step));
    }

    public async Task<bool> CanStartStep(Guid manipulationOrderId, string stepType)
    {
        var order = await _context.ManipulationOrders
            .FirstOrDefaultAsync(o => o.Id == manipulationOrderId);

        if (order == null)
            return false;

        var previousSteps = stepType switch
        {
            "PESAGEM" => new[] { "EM_PRODUCAO" },
            "MISTURA" => new[] { "PESAGEM" },
            "ENVASE" => new[] { "MISTURA" },
            "ROTULAGEM" => new[] { "ENVASE" },
            "CONFERENCIA" => new[] { "ROTULAGEM" },
            _ => Array.Empty<string>()
        };

        if (!previousSteps.Any())
            return false;

        if (stepType == "PESAGEM")
            return order.Status == "EM_PRODUCAO" || order.Status == "PESAGEM";

        var previousStep = previousSteps[0];
        var prevStepCompleted = await _context.ManipulationSteps
            .AnyAsync(s => s.ManipulationOrderId == manipulationOrderId
                && s.StepType == previousStep
                && s.Status == "CONCLUIDA"
                && s.PassedIntermediateCheck != false);

        return prevStepCompleted;
    }
}