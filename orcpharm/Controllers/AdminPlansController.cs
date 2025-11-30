using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using System.Text.Json;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/plans")]
public class AdminPlansController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminPlansController> _logger;

    public AdminPlansController(AppDbContext context, ILogger<AdminPlansController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Nome do plano é obrigatório" });

            var plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                PriceMonthly = dto.PriceMonthly,
                PriceYearly = dto.PriceYearly,
                MaxEmployees = dto.MaxEmployees,
                MaxMonthlyOrders = dto.MaxMonthlyOrders,
                Features = dto.Features != null ? JsonSerializer.Serialize(dto.Features) : null,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<SubscriptionPlan>().Add(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Plano criado: {Name} (ID: {Id})", plan.Name, plan.Id);

            return Ok(new { message = "Plano criado com sucesso", id = plan.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar plano");
            return StatusCode(500, new { message = "Erro ao criar plano" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePlanDto dto)
    {
        try
        {
            var plan = await _context.Set<SubscriptionPlan>().FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Nome do plano é obrigatório" });

            plan.Name = dto.Name;
            plan.Description = dto.Description;
            plan.PriceMonthly = dto.PriceMonthly;
            plan.PriceYearly = dto.PriceYearly;
            plan.MaxEmployees = dto.MaxEmployees;
            plan.MaxMonthlyOrders = dto.MaxMonthlyOrders;
            plan.IsActive = dto.IsActive;
            plan.UpdatedAt = DateTime.UtcNow;

            if (dto.Features != null)
            {
                plan.Features = JsonSerializer.Serialize(dto.Features);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Plano atualizado: {Name} (ID: {Id})", plan.Name, plan.Id);

            return Ok(new { message = "Plano atualizado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar plano {Id}", id);
            return StatusCode(500, new { message = "Erro ao atualizar plano" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var plan = await _context.Set<SubscriptionPlan>().FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            // Verificar se há assinaturas usando este plano
            var hasSubscriptions = await _context.Subscriptions
                .AnyAsync(s => s.SubscriptionPlanId == id);

            if (hasSubscriptions)
            {
                // Soft delete - apenas desativa
                plan.IsActive = false;
                plan.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Plano {Id} desativado (possui assinaturas vinculadas)", id);

                return Ok(new { message = "Plano desativado (possui assinaturas vinculadas)" });
            }

            // Hard delete - remove do banco
            _context.Set<SubscriptionPlan>().Remove(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Plano excluído: {Name} (ID: {Id})", plan.Name, id);

            return Ok(new { message = "Plano excluído com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir plano {Id}", id);
            return StatusCode(500, new { message = "Erro ao excluir plano" });
        }
    }

    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        try
        {
            var plan = await _context.Set<SubscriptionPlan>().FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            plan.IsActive = !plan.IsActive;
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var status = plan.IsActive ? "ativado" : "desativado";
            _logger.LogInformation("Plano {Id} {Status}", id, status);

            return Ok(new { message = $"Plano {status} com sucesso", isActive = plan.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alternar status do plano {Id}", id);
            return StatusCode(500, new { message = "Erro ao atualizar plano" });
        }
    }
}

public class CreatePlanDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int? MaxEmployees { get; set; }
    public int? MaxMonthlyOrders { get; set; }
    public Dictionary<string, bool>? Features { get; set; }
    public bool IsActive { get; set; } = true;
}
