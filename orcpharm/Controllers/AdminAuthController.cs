using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs;
using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminAuthController> _logger;

    public AdminAuthController(AppDbContext context, ILogger<AdminAuthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
    {
        try
        {
            var admin = await _context.Set<SaasAdmin>()
                .FirstOrDefaultAsync(a => a.Email == dto.Email && a.IsActive);

            if (admin == null)
                return Unauthorized(new { message = "Credenciais inválidas" });

            if (!Argon2.Verify(admin.PasswordHash, dto.Password))
                return Unauthorized(new { message = "Credenciais inválidas" });

            // Criar sessão
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            var session = new SaasAdminSession
            {
                Id = Guid.NewGuid(),
                SaasAdminId = admin.Id,
                Token = token,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(12),
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<SaasAdminSession>().Add(session);

            admin.LastLoginAt = DateTime.UtcNow;
            admin.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Definir cookie
            Response.Cookies.Append("AdminSessionId", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = session.ExpiresAt
            });

            return Ok(new AdminLoginResponseDto
            {
                Token = token,
                ExpiresAt = session.ExpiresAt,
                Admin = new AdminDto
                {
                    Id = admin.Id,
                    FullName = admin.FullName,
                    Email = admin.Email,
                    Role = admin.Role
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer login de admin");
            return StatusCode(500, new { message = "Erro ao processar login" });
        }
    }

    [HttpGet("generate-hash")]
    public IActionResult GenerateHash([FromQuery] string password = "OrcPharm@2024")
    {
        var hash = Argon2.Hash(password);
        return Ok(new { password, hash });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionToken = Request.Cookies["AdminSessionId"];
            if (!string.IsNullOrEmpty(sessionToken))
            {
                var session = await _context.Set<SaasAdminSession>()
                    .FirstOrDefaultAsync(s => s.Token == sessionToken);

                if (session != null)
                {
                    session.IsActive = false;
                    session.ExpiresAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                Response.Cookies.Delete("AdminSessionId");
            }

            return Ok(new { message = "Logout realizado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer logout de admin");
            return StatusCode(500, new { message = "Erro ao processar logout" });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        try
        {
            var sessionToken = Request.Cookies["AdminSessionId"];
            if (string.IsNullOrEmpty(sessionToken))
                return Unauthorized(new { message = "Não autenticado" });

            var session = await _context.Set<SaasAdminSession>()
                .Include(s => s.SaasAdmin)
                .FirstOrDefaultAsync(s => s.Token == sessionToken && 
                                         s.IsActive && 
                                         s.ExpiresAt > DateTime.UtcNow);

            if (session?.SaasAdmin == null)
                return Unauthorized(new { message = "Sessão inválida ou expirada" });

            // Atualizar última atividade
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new AdminDto
            {
                Id = session.SaasAdmin.Id,
                FullName = session.SaasAdmin.FullName,
                Email = session.SaasAdmin.Email,
                Role = session.SaasAdmin.Role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar admin logado");
            return StatusCode(500, new { message = "Erro ao processar requisição" });
        }
    }
}
