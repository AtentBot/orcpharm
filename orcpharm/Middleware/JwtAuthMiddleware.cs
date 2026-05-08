using Service.Marketplace;

namespace Middleware;

/// <summary>
/// Middleware para autenticação JWT em rotas mobile /api/mobile/
/// </summary>
public class JwtAuthMiddleware
{
    private readonly RequestDelegate _next;

    public JwtAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, JwtTokenService jwtService)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Aplicar apenas em rotas mobile autenticadas
        if (!path.StartsWith("/api/mobile/") || IsPublicMobileRoute(path))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Token não fornecido" });
            return;
        }

        var token = authHeader["Bearer ".Length..];
        var principal = jwtService.ValidateToken(token);

        if (principal == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Token inválido ou expirado" });
            return;
        }

        var customerId = jwtService.GetCustomerIdFromToken(principal);
        if (customerId == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Token inválido" });
            return;
        }

        context.Items["MobileCustomerId"] = customerId.Value;
        context.Items["MobileCustomerPrincipal"] = principal;

        await _next(context);
    }

    private static bool IsPublicMobileRoute(string path)
    {
        return path.StartsWith("/api/mobile/v1/auth/")
            || path.StartsWith("/api/mobile/v1/pharmacies/nearby")
            || path.StartsWith("/api/mobile/v1/pharmacies/") && !path.Contains("/orders")
            || path.StartsWith("/api/mobile/v1/search")
            || path.StartsWith("/api/mobile/v1/categories");
    }
}
