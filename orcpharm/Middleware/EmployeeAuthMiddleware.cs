using Data;
using Microsoft.EntityFrameworkCore;

namespace Middleware;

public class EmployeeAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EmployeeAuthMiddleware> _logger;

    public EmployeeAuthMiddleware(RequestDelegate next, ILogger<EmployeeAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Rotas públicas - sem necessidade de autenticação
        if (path.Contains("/health") ||
            path.Contains("/swagger") ||
            path.Contains("/login") ||
            path.Contains(".css") ||
            path.Contains(".js") ||
            path == "/" ||
            path.Contains("/home"))
        {
            await _next(context);
            return;
        }

        // Verificar token de sessão para rotas protegidas
        var token = context.Request.Headers["X-SESSION-TOKEN"].FirstOrDefault();

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var session = await db.EmployeeSessions
                    .Include(s => s.Employee)
                    .ThenInclude(e => e!.Establishment)
                    .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

                if (session != null && session.ExpiresAt > DateTime.UtcNow)
                {
                    // Atualizar última atividade
                    session.LastActivityAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();

                    // Adicionar employee ao contexto para uso nos controllers
                    context.Items["Employee"] = session.Employee;
                    context.Items["EstablishmentId"] = session.Employee?.EstablishmentId;
                    context.Items["SessionId"] = session.Id;

                    _logger.LogInformation($"Autenticado: {session.Employee?.FullName}");
                }
                else if (session != null)
                {
                    // Sessão expirada
                    session.IsActive = false;
                    await db.SaveChangesAsync();
                    _logger.LogWarning("Sessão expirada");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar sessão");
            }
        }

        // Continuar - os controllers decidirão se precisam de autenticação
        await _next(context);
    }
}

// Extension method
public static class EmployeeAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseEmployeeAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EmployeeAuthMiddleware>();
    }
}