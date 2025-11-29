using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingInvoice = Models.Billing.Invoice;
using Data;
using DTOs;
using Models;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/establishments")]
public class AdminEstablishmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminEstablishmentsController> _logger;

    public AdminEstablishmentsController(AppDbContext context, ILogger<AdminEstablishmentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var query = _context.Establishments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToUpper() == "ACTIVE")
                    query = query.Where(e => e.IsActive);
                else if (status.ToUpper() == "INACTIVE")
                    query = query.Where(e => !e.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchUpper = search.ToUpper();
                query = query.Where(e => 
                    e.NomeFantasia.Contains(searchUpper) ||
                    (e.Cnpj != null && e.Cnpj.Contains(searchUpper)) ||
                    e.Email.Contains(searchUpper));
            }

            var total = await query.CountAsync();

            var establishments = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip(skip)
                .Take(Math.Min(take, 100))
                .Select(e => new
                {
                    e.Id,
                    e.NomeFantasia,
                    e.RazaoSocial,
                    e.Cnpj,
                    e.Email,
                    e.WhatsApp,
                    e.City,
                    e.State,
                    e.IsActive,
                    e.OnboardingCompleted,
                    e.SubscriptionStatus,
                    e.TrialEndsAt,
                    e.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, establishments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar establishments");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var establishment = await _context.Establishments.FindAsync(id);
            if (establishment == null)
                return NotFound(new { message = "Establishment não encontrado" });

            var subscription = await _context.Set<Subscription>()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.EstablishmentId == id);

            var invoices = await _context.Set<BillingInvoice>()
                .Where(i => i.SubscriptionId == subscription.Id)
                .OrderByDescending(i => i.CreatedAt)
                .Take(10)
                .ToListAsync();

            var employeesCount = await _context.Employees
                .CountAsync(e => e.EstablishmentId == id);

            var result = new EstablishmentAdminDto
            {
                Id = establishment.Id,
                NomeFantasia = establishment.NomeFantasia,
                RazaoSocial = establishment.RazaoSocial ?? "",
                Cnpj = establishment.Cnpj ?? "",
                Email = establishment.Email,
                WhatsApp = establishment.WhatsApp ?? "",
                City = establishment.City,
                State = establishment.State,
                IsActive = establishment.IsActive,
                OnboardingCompleted = establishment.OnboardingCompleted,
                CreatedAt = establishment.CreatedAt,
                Subscription = subscription != null ? new SubscriptionResponseDto
                {
                    Id = subscription.Id,
                    EstablishmentId = subscription.EstablishmentId,
                    EstablishmentName = establishment.NomeFantasia,
                    SubscriptionPlanId = subscription.SubscriptionPlanId,
                    PlanName = subscription.SubscriptionPlan?.Name ?? "",
                    Status = subscription.Status,
                    BillingCycle = subscription.BillingCycle,
                    Amount = subscription.BillingCycle == "YEARLY"
                        ? (subscription.SubscriptionPlan?.PriceYearly ?? 0)
                        : (subscription.SubscriptionPlan?.PriceMonthly ?? 0),
                    CurrentPeriodStart = subscription.CurrentPeriodStart,
                    CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                    TrialEnd = subscription.TrialEnd,
                    CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                    CreatedAt = subscription.CreatedAt
                } : null
            };

            return Ok(new
            {
                establishment = result,
                employeesCount,
                recentInvoices = invoices
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar establishment {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockEstablishmentDto dto)
    {
        try
        {
            var establishment = await _context.Establishments.FindAsync(id);
            if (establishment == null)
                return NotFound(new { message = "Establishment não encontrado" });

            establishment.IsActive = false;
            establishment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning("Establishment {Id} bloqueado. Motivo: {Reason}", id, dto.Reason);

            return Ok(new { message = "Establishment bloqueado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao bloquear establishment {Id}", id);
            return StatusCode(500, new { message = "Erro ao bloquear" });
        }
    }

    [HttpPost("{id}/unblock")]
    public async Task<IActionResult> Unblock(Guid id)
    {
        try
        {
            var establishment = await _context.Establishments.FindAsync(id);
            if (establishment == null)
                return NotFound(new { message = "Establishment não encontrado" });

            // Verificar se tem subscription ativa
            var subscription = await _context.Set<Subscription>()
                .FirstOrDefaultAsync(s => s.EstablishmentId == id);

            if (subscription == null || subscription.Status == "CANCELED")
                return BadRequest(new { message = "Não é possível desbloquear sem subscription ativa" });

            establishment.IsActive = true;
            establishment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Establishment {Id} desbloqueado", id);

            return Ok(new { message = "Establishment desbloqueado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desbloquear establishment {Id}", id);
            return StatusCode(500, new { message = "Erro ao desbloquear" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var establishment = await _context.Establishments.FindAsync(id);
            if (establishment == null)
                return NotFound(new { message = "Establishment não encontrado" });

            // Soft delete
            establishment.IsActive = false;
            establishment.UpdatedAt = DateTime.UtcNow;

            // Cancelar subscription se existir
            var subscription = await _context.Set<Subscription>()
                .FirstOrDefaultAsync(s => s.EstablishmentId == id);

            if (subscription != null)
            {
                subscription.Status = "CANCELED";
                subscription.CanceledAt = DateTime.UtcNow;
                subscription.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning("Establishment {Id} deletado (soft delete)", id);

            return Ok(new { message = "Establishment removido com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar establishment {Id}", id);
            return StatusCode(500, new { message = "Erro ao remover" });
        }
    }
}
