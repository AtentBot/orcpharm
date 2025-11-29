using Data;
using Microsoft.EntityFrameworkCore;

namespace Middleware;

public class SubscriptionLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionLimitMiddleware> _logger;

    public SubscriptionLimitMiddleware(RequestDelegate next, ILogger<SubscriptionLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;

        // Verificar apenas em rotas que criam recursos
        if (method == "POST" && ShouldCheckLimit(path))
        {
            var establishmentId = GetEstablishmentId(context);
            
            if (establishmentId.HasValue)
            {
                var establishment = await db.Establishments
                    .FirstOrDefaultAsync(e => e.Id == establishmentId.Value);

                if (establishment != null)
                {
                    // Verificar limite de funcionários
                    if (path.Contains("/api/employees"))
                    {
                        var employeeCount = await db.Employees
                            .CountAsync(e => e.EstablishmentId == establishmentId.Value);

                        if (employeeCount >= establishment.MaxEmployeesLimit)
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                message = $"Limite de funcionários atingido. Seu plano permite até {establishment.MaxEmployeesLimit} funcionários.",
                                limit = establishment.MaxEmployeesLimit,
                                current = employeeCount,
                                upgradeRequired = true
                            });
                            return;
                        }
                    }

                    // Verificar limite de ordens/mês
                    if (path.Contains("/api/manipulationorders"))
                    {
                        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                        
                        var ordersThisMonth = await db.ManipulationOrders
                            .CountAsync(o => o.EstablishmentId == establishmentId.Value &&
                                           o.CreatedAt >= firstDayOfMonth);

                        if (ordersThisMonth >= establishment.MaxOrdersLimit)
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                message = $"Limite de ordens de manipulação atingido este mês. Seu plano permite até {establishment.MaxOrdersLimit} ordens/mês.",
                                limit = establishment.MaxOrdersLimit,
                                current = ordersThisMonth,
                                upgradeRequired = true
                            });
                            return;
                        }
                    }

                    // Verificar se subscription está ativa
                    var subscription = await db.Set<Models.Subscription>()
                        .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId.Value);

                    if (subscription != null)
                    {
                        // Se PAST_DUE por mais de 7 dias, bloquear
                        if (subscription.Status == "PAST_DUE" && 
                            subscription.CurrentPeriodEnd.HasValue &&
                            (DateTime.UtcNow - subscription.CurrentPeriodEnd.Value).Days > 7)
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                message = "Sua assinatura está com pagamento pendente há mais de 7 dias. Por favor, regularize o pagamento para continuar usando o sistema.",
                                subscriptionStatus = "BLOCKED"
                            });
                            return;
                        }
                    }
                }
            }
        }

        await _next(context);
    }

    private bool ShouldCheckLimit(string path)
    {
        var pathsToCheck = new[]
        {
            "/api/employees",
            "/api/manipulationorders"
        };

        return pathsToCheck.Any(p => path.Contains(p));
    }

    private Guid? GetEstablishmentId(HttpContext context)
    {
        if (context.Items.TryGetValue("EstablishmentId", out var value) && value is Guid id)
            return id;

        return null;
    }
}

public static class SubscriptionLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseSubscriptionLimits(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SubscriptionLimitMiddleware>();
    }
}
