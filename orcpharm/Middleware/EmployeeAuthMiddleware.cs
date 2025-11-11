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

        // 🔍 LOG TEMPORÁRIO - Pode remover depois
        _logger.LogWarning("🔍 MIDDLEWARE EXECUTANDO: Path={Path}", path);

        // Rotas públicas - sem necessidade de autenticação
        var publicPaths = new[]
        {
            "/health",
            "/swagger",
            "/login",
            "/api/employees/login",
            "/api/employees/generate-hash",
            "/api/establishment/login",
            ".css",
            ".js",
            ".png",
            ".jpg",
            ".ico",
            "/home"
        };

        bool isPublicPath = publicPaths.Any(p => path.Contains(p));

        if (isPublicPath)
        {
            _logger.LogDebug("Rota pública, pulando autenticação: {Path}", path);
            await _next(context);
            return;
        }

        // Verificar token de sessão para rotas protegidas
        var token = context.Request.Headers["X-SESSION-TOKEN"].FirstOrDefault();

        // 🔍 LOG TEMPORÁRIO - Pode remover depois
        _logger.LogWarning("🔍 MIDDLEWARE: Token recebido={Token}",
            string.IsNullOrEmpty(token) ? "NULL/VAZIO" : token.Substring(0, Math.Min(10, token.Length)) + "...");

        if (string.IsNullOrEmpty(token))
        {
            // Para rotas /api/* retornar 401, para outras permitir (views)
            if (path.StartsWith("/api/"))
            {
                _logger.LogWarning("Tentativa de acesso à API sem token: {Path}", path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Token de autenticação não fornecido" });
                return;
            }
            else
            {
                // Views web podem continuar (controllers decidem)
                await _next(context);
                return;
            }
        }

        try
        {
            // 🔧 CORREÇÃO: Removido .AsNoTracking() para permitir rastreamento
            var session = await db.EmployeeSessions
                .Include(s => s.Employee)
                    .ThenInclude(e => e!.JobPosition)
                .Include(s => s.Employee)
                    .ThenInclude(e => e!.Establishment)
                .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

            if (session == null)
            {
                _logger.LogWarning("Token inválido ou sessão não encontrada: {Token}", token.Substring(0, 10) + "...");

                if (path.StartsWith("/api/"))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { error = "Token inválido ou sessão não encontrada" });
                    return;
                }
                else
                {
                    await _next(context);
                    return;
                }
            }

            // 🔍 LOG TEMPORÁRIO - Pode remover depois
            _logger.LogWarning("🔍 MIDDLEWARE: Sessão encontrada! EmployeeId={EmployeeId}", session.EmployeeId);

            // Verificar expiração
            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Token expirado para Employee {EmployeeId}", session.EmployeeId);

                // Desativar sessão expirada
                session.IsActive = false;
                await db.SaveChangesAsync();

                if (path.StartsWith("/api/"))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { error = "Sessão expirada. Faça login novamente." });
                    return;
                }
                else
                {
                    await _next(context);
                    return;
                }
            }

            // Verificar se employee está ativo
            if (session.Employee == null || session.Employee.Status != "Ativo")
            {
                _logger.LogWarning("Tentativa de acesso com funcionário inativo: {EmployeeId}", session.EmployeeId);

                if (path.StartsWith("/api/"))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { error = "Funcionário inativo" });
                    return;
                }
                else
                {
                    await _next(context);
                    return;
                }
            }

            // 🔍 LOG TEMPORÁRIO - Pode remover depois
            _logger.LogWarning("🔍 MIDDLEWARE: Employee ativo! Nome={Nome}, Status={Status}",
                session.Employee.FullName, session.Employee.Status);

            // Verificar se estabelecimento está ativo
            if (session.Employee.Establishment == null || !session.Employee.Establishment.IsActive)
            {
                _logger.LogWarning("Tentativa de acesso com estabelecimento inativo: {EstablishmentId}",
                    session.Employee.EstablishmentId);

                if (path.StartsWith("/api/"))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { error = "Estabelecimento inativo" });
                    return;
                }
                else
                {
                    await _next(context);
                    return;
                }
            }

            // 🔧 CORREÇÃO: Simplificado - usar a mesma session já carregada
            session.LastActivityAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Adicionar employee ao contexto para uso nos controllers
            context.Items["Employee"] = session.Employee;
            context.Items["EstablishmentId"] = session.Employee.EstablishmentId;
            context.Items["SessionId"] = session.Id;

            // 🔍 LOG TEMPORÁRIO - Pode remover depois
            _logger.LogWarning("✅ MIDDLEWARE: Employee INJETADO no contexto! Nome={Nome}, Cargo={Cargo}, CargoCode={Code}",
                session.Employee.FullName,
                session.Employee.JobPosition?.Name ?? "NULL",
                session.Employee.JobPosition?.Code ?? "NULL");

            _logger.LogDebug("Autenticado com sucesso: {EmployeeName} (ID: {EmployeeId})",
                session.Employee.FullName, session.Employee.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar sessão: {Path}", path);

            if (path.StartsWith("/api/"))
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "Erro ao validar autenticação" });
                return;
            }
        }

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