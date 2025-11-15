using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Auth;
using Service.Auth;
using Validators.Auth;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;

    public AuthController(AppDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] EmployeeLoginDto dto)
    {
        var validator = new EmployeeLoginValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        var result = await _authService.LoginAsync(dto, ipAddress, userAgent);

        if (!result.Success)
            return Unauthorized(result);

        if (result.Requires2FA)
            return Ok(result);

        Response.Cookies.Append("SessionId", result.SessionId!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });

        return Ok(result);
    }

    [HttpPost("verify-2fa")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto dto)
    {
        var validator = new Verify2FAValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeIdFromTempSession();
        if (!employeeId.HasValue)  // ✅ CORRIGIDO
            return Unauthorized(new { message = "Sessão temporária inválida" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        var (success, message, sessionId) = await _authService.Verify2FAAsync(
            employeeId.Value, dto, ipAddress, userAgent);

        if (!success)
            return BadRequest(new { message });

        if (!string.IsNullOrEmpty(sessionId))
        {
            Response.Cookies.Append("SessionId", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            var employee = await _context.Employees
                .Include(e => e.JobPosition)
                .Include(e => e.Establishment)
                .FirstOrDefaultAsync(e => e.Id == employeeId.Value);

            return Ok(new LoginResponseDto
            {
                Success = true,
                Message = message,
                SessionId = sessionId,
                Employee = employee != null ? new EmployeeInfoDto
                {
                    Id = employee.Id,
                    Name = employee.FullName,
                    Cpf = employee.Cpf,
                    WhatsApp = employee.WhatsApp,
                    Email = employee.Email,
                    JobPositionName = employee.JobPosition?.Name ?? "",
                    EstablishmentName = employee.Establishment?.NomeFantasia ?? ""  // ✅ CORRIGIDO
                } : null
            });
        }

        return Ok(new { success = true, message });
    }

    [HttpPost("request-2fa")]
    public async Task<IActionResult> Request2FA([FromBody] Request2FADto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)  // ✅ CORRIGIDO
            return Unauthorized(new { message = "Sessão inválida" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        var (success, message) = await _authService.Create2FACodeAsync(
            employeeId.Value, dto.Purpose, ipAddress, userAgent);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("password/request-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
    {
        var validator = new RequestPasswordResetValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var (success, message) = await _authService.RequestPasswordResetAsync(dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("password/verify-code")]
    public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto dto)
    {
        var validator = new VerifyResetCodeValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var (success, message) = await _authService.VerifyResetCodeAsync(dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var validator = new ResetPasswordValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var (success, message) = await _authService.ResetPasswordAsync(dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var validator = new ChangePasswordValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)  // ✅ CORRIGIDO
            return Unauthorized(new { message = "Sessão inválida" });

        var result = await _authService.ChangePasswordAsync(employeeId.Value, dto);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        Response.Cookies.Delete("SessionId");

        return Ok(new { message = result.Message });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionToken = Request.Cookies["SessionId"];
        if (!string.IsNullOrEmpty(sessionToken))
        {
            var session = await _context.EmployeeSessions
                .FirstOrDefaultAsync(s => s.Token == sessionToken);

            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete("SessionId");
        return Ok(new { message = "Logout realizado com sucesso" });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentEmployee()
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)  // ✅ CORRIGIDO
            return Unauthorized(new { message = "Sessão inválida" });

        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .Include(e => e.Establishment)
            .FirstOrDefaultAsync(e => e.Id == employeeId.Value);

        if (employee == null)
            return NotFound(new { message = "Funcionário não encontrado" });

        return Ok(new EmployeeInfoDto
        {
            Id = employee.Id,
            Name = employee.FullName,
            Cpf = employee.Cpf,
            WhatsApp = employee.WhatsApp,
            Email = employee.Email,
            JobPositionName = employee.JobPosition?.Name ?? "",
            EstablishmentName = employee.Establishment?.NomeFantasia ?? "" 
        });
    }

    private Guid? GetEmployeeId()
    {
        var sessionToken = Request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = _context.EmployeeSessions
            .FirstOrDefault(s => s.Token == sessionToken &&
                                s.ExpiresAt > DateTime.UtcNow &&
                                s.IsActive);

        return session?.EmployeeId;
    }

    private Guid? GetEmployeeIdFromTempSession()
    {
        var identifier = Request.Headers["X-Temp-Identifier"].ToString();
        if (string.IsNullOrEmpty(identifier))
            return null;

        var employee = _context.Employees
            .FirstOrDefault(e => e.Cpf == identifier || e.WhatsApp == identifier);

        return employee?.Id;
    }
}