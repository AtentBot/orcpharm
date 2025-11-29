using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingInvoice = Models.Billing.Invoice;
using Data;
using DTOs;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(AppDbContext context, ILogger<AdminDashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var totalEstablishments = await _context.Establishments.CountAsync();
            var activeEstablishments = await _context.Establishments.CountAsync(e => e.IsActive);
            var inactiveEstablishments = totalEstablishments - activeEstablishments;

            var subscriptions = await _context.Set<Models.Subscription>()
                .Include(s => s.SubscriptionPlan)
                .ToListAsync();

            var trialingCount = subscriptions.Count(s => s.Status == "TRIALING");
            var pastDueCount = subscriptions.Count(s => s.Status == "PAST_DUE");

            var monthlyRevenue = subscriptions
                .Where(s => s.Status == "ACTIVE" && s.BillingCycle == "MONTHLY")
                .Sum(s => s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceMonthly : 0);

            var yearlyRevenue = subscriptions
                .Where(s => s.Status == "ACTIVE" && s.BillingCycle == "YEARLY")
                .Sum(s => s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceYearly / 12 : 0);

            var mrr = monthlyRevenue + yearlyRevenue;
            var arr = mrr * 12;

            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var newSubscriptionsThisMonth = await _context.Set<Models.Subscription>()
                .CountAsync(s => s.CreatedAt >= firstDayOfMonth);

            var subscriptionsByPlan = subscriptions
                .Where(s => s.Status == "ACTIVE")
                .GroupBy(s => s.SubscriptionPlan != null ? s.SubscriptionPlan.Name : "Sem Plano")
                .ToDictionary(g => g.Key, g => g.Count());

            // Cálculo simples de churn (cancelamentos no mês / total ativo no início do mês)
            var canceledThisMonth = await _context.Set<Models.Subscription>()
                .CountAsync(s => s.CanceledAt.HasValue && s.CanceledAt.Value >= firstDayOfMonth);
            
            var activeAtStartOfMonth = activeEstablishments + canceledThisMonth;
            var churnRate = activeAtStartOfMonth > 0 
                ? (decimal)canceledThisMonth / activeAtStartOfMonth * 100 
                : 0;

            var metrics = new DashboardMetricsDto
            {
                TotalEstablishments = totalEstablishments,
                ActiveEstablishments = activeEstablishments,
                InactiveEstablishments = inactiveEstablishments,
                TrialingEstablishments = trialingCount,
                PastDueEstablishments = pastDueCount,
                MonthlyRecurringRevenue = mrr,
                AnnualRecurringRevenue = arr,
                NewSubscriptionsThisMonth = newSubscriptionsThisMonth,
                ChurnRate = churnRate,
                SubscriptionsByPlan = subscriptionsByPlan
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar métricas do dashboard");
            return StatusCode(500, new { message = "Erro ao buscar métricas" });
        }
    }

    [HttpGet("recent-signups")]
    public async Task<IActionResult> GetRecentSignups([FromQuery] int limit = 10)
    {
        try
        {
            var recentEstablishments = await _context.Establishments
                .OrderByDescending(e => e.CreatedAt)
                .Take(limit)
                .Select(e => new
                {
                    e.Id,
                    e.NomeFantasia,
                    e.Email,
                    e.City,
                    e.State,
                    e.CreatedAt,
                    e.IsActive,
                    e.OnboardingCompleted,
                    e.SubscriptionStatus
                })
                .ToListAsync();

            return Ok(recentEstablishments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar cadastros recentes");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpGet("revenue-chart")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int months = 6)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            var invoices = await _context.Set<BillingInvoice>()
                .Where(i => i.Status == "PAID" && i.PaidAt >= startDate)
                .GroupBy(i => new { 
                    Year = i.PaidAt!.Value.Year, 
                    Month = i.PaidAt.Value.Month 
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(i => i.Amount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var chartData = invoices.Select(i => new
            {
                Label = $"{i.Month:D2}/{i.Year}",
                Value = i.Revenue
            });

            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dados do gráfico de receita");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }
}
