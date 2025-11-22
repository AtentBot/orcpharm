using Data;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

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

        // ==================== 1. ROTAS PÚBLICAS (sem autenticação) ====================
        if (IsPublicPath(path))
        {
            _logger.LogDebug("Rota pública acessada: {Path}", path);
            await _next(context);
            return;
        }

        // ==================== 2. ROTAS COM API KEY (sem necessidade de sessão) ====================
        if (IsApiKeyOnlyPath(path))
        {
            _logger.LogDebug("Rota protegida apenas com API Key: {Path}", path);
            await _next(context);
            return;
        }

        // ==================== 3. ROTAS PROTEGIDAS (requerem X-SESSION-TOKEN) ====================
        var token = GetSessionToken(context);

        if (string.IsNullOrEmpty(token))
        {
            await HandleMissingToken(context, path);
            return;
        }

        // Validar sessão
        var validationResult = await ValidateSession(db, token, path);

        if (!validationResult.IsValid)
        {
            await HandleInvalidSession(context, path, validationResult.ErrorMessage);
            return;
        }

        // Sessão válida - adicionar informações no contexto
        AddSessionToContext(context, validationResult.Session!);

        _logger.LogDebug("Autenticação bem-sucedida: {EmployeeName} (ID: {EmployeeId}) acessando {Path}",
            validationResult.Session!.Employee.FullName,
            validationResult.Session.Employee.Id,
            path);

        await _next(context);
    }

    // ==================== MÉTODOS AUXILIARES ====================

    /// <summary>
    /// Verifica se o path é de uma rota pública (sem autenticação)
    /// </summary>
    private static bool IsPublicPath(string path)
    {
        // ===== ROTAS EXATAS =====
        var exactPublicPaths = new[]
        {
            "/",
            "/health",
            "/home",
            "/home/index",
            "/home/error",
            "/home/privacy"
        };

        if (exactPublicPaths.Contains(path))
            return true;

        // ===== ROTAS DE AUTENTICAÇÃO (Views MVC) =====
        var accountPaths = new[]
        {
            "/account/login",
            "/account/register",
            "/account/forgotpassword",
            "/account/resetpassword",
            "/account/validatecode"
        };

        if (accountPaths.Any(p => path.StartsWith(p)))
            return true;

        // ===== ROTAS DE API PÚBLICAS =====
        var publicApiPrefixes = new[]
        {
            "/swagger",
            "/api/auth/login",
            "/api/employees/login",
            "/api/employees/generate-hash",
            "/api/establishment/login"
        };

        if (publicApiPrefixes.Any(prefix => path.StartsWith(prefix)))
            return true;

        // ===== ARQUIVOS ESTÁTICOS =====
        var staticExtensions = new[]
        {
            ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico",
            ".svg", ".woff", ".woff2", ".ttf", ".eot", ".map"
        };

        if (staticExtensions.Any(ext => path.EndsWith(ext)))
            return true;

        return false;
    }

    /// <summary>
    /// Verifica se o path é de uma rota protegida apenas com API Key (sem sessão)
    /// </summary>
    private static bool IsApiKeyOnlyPath(string path)
    {
        var apiKeyOnlyPaths = new[]
        {
            "/api/auth/password/request-reset",
            "/api/auth/password/verify-code",
            "/api/auth/password/reset"
        };

        return apiKeyOnlyPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Obtém o token de sessão do Cookie ou Header
    /// </summary>
    private static string? GetSessionToken(HttpContext context)
    {
        // Primeiro tenta obter do header (APIs, mobile)
        var headerToken = context.Request.Headers["X-Session-Token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerToken))
            return headerToken;

        // Se não tem no header, tenta obter do cookie (web)
        var cookieToken = context.Request.Cookies["SessionId"];
        return cookieToken;
    }

    /// <summary>
    /// Valida a sessão no banco de dados
    /// </summary>
    private async Task<SessionValidationResult> ValidateSession(AppDbContext db, string token, string path)
    {
        try
        {
            // Buscar sessão com todas as relações necessárias
            var session = await db.EmployeeSessions
                .Include(s => s.Employee)
                    .ThenInclude(e => e.JobPosition)
                .Include(s => s.Employee)
                    .ThenInclude(e => e.Establishment)
                .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

            if (session == null)
            {
                _logger.LogWarning("Token inválido ou sessão não encontrada: {TokenPrefix}...",
                    token.Substring(0, Math.Min(10, token.Length)));
                return SessionValidationResult.Fail("Token inválido ou sessão não encontrada");
            }

            // Verificar expiração
            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Sessão expirada: Employee {EmployeeId}, Expirou em {ExpiresAt}",
                    session.EmployeeId, session.ExpiresAt);

                session.IsActive = false;
                await db.SaveChangesAsync();

                return SessionValidationResult.Fail("Sessão expirada. Faça login novamente.");
            }

            // Verificar se employee existe e está ativo
            if (session.Employee == null)
            {
                _logger.LogWarning("Sessão sem Employee associado: SessionId {SessionId}", session.Id);
                return SessionValidationResult.Fail("Funcionário não encontrado");
            }

            if (session.Employee.Status.ToUpper() != "ATIVO")
            {
                _logger.LogWarning("Tentativa de acesso com funcionário inativo: {EmployeeId}, Status: {Status}",
                    session.EmployeeId, session.Employee.Status);
                return SessionValidationResult.Fail("Funcionário inativo");
            }

            // Verificar se estabelecimento está ativo
            if (session.Employee.Establishment == null || !session.Employee.Establishment.IsActive)
            {
                _logger.LogWarning("Tentativa de acesso com estabelecimento inativo: {EstablishmentId}",
                    session.Employee.EstablishmentId);
                return SessionValidationResult.Fail("Estabelecimento inativo");
            }

            // Atualizar última atividade
            session.LastActivityAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return SessionValidationResult.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar sessão: {Path}", path);
            return SessionValidationResult.Fail("Erro ao validar autenticação");
        }
    }

    /// <summary>
    /// Adiciona informações da sessão no HttpContext para uso nos controllers
    /// </summary>
    private static void AddSessionToContext(HttpContext context, EmployeeSession session)
    {
        context.Items["EmployeeId"] = session.EmployeeId;
        context.Items["Employee"] = session.Employee;
        context.Items["EmployeeName"] = session.Employee.FullName;
        context.Items["EmployeeJobPositionCode"] = session.Employee.JobPosition?.Code;
        context.Items["EstablishmentId"] = session.Employee.EstablishmentId;
        context.Items["SessionId"] = session.Id;
    }

    /// <summary>
    /// Trata requisições sem token
    /// </summary>
    private async Task HandleMissingToken(HttpContext context, string path)
    {
        _logger.LogWarning("Tentativa de acesso sem token: {Path}", path);

        if (path.StartsWith("/api/"))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Token de autenticação não fornecido",
                message = "Envie o token no header X-Session-Token ou no cookie SessionId"
            });
        }
        else
        {
            // Views web - redirecionar para login
            context.Response.Redirect("/Account/Login");
        }
    }

    /// <summary>
    /// Trata requisições com sessão inválida
    /// </summary>
    private async Task HandleInvalidSession(HttpContext context, string path, string errorMessage)
    {
        if (path.StartsWith("/api/"))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = errorMessage });
        }
        else
        {
            // Views web - redirecionar para login
            context.Response.Redirect("/Account/Login");
        }
    }
}

// ==================== CLASSES AUXILIARES ====================

/// <summary>
/// Resultado da validação de sessão
/// </summary>
internal class SessionValidationResult
{
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public EmployeeSession? Session { get; private set; }

    private SessionValidationResult() { }

    public static SessionValidationResult Success(EmployeeSession session)
    {
        return new SessionValidationResult
        {
            IsValid = true,
            Session = session
        };
    }

    public static SessionValidationResult Fail(string errorMessage)
    {
        return new SessionValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}

// ==================== EXTENSION METHOD ====================

public static class EmployeeAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseEmployeeAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EmployeeAuthMiddleware>();
    }
}