using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Auth;
using Models.Auth;
using Models.Employees;
using Service.Notifications;
using System.Security.Cryptography;

namespace Service.Auth;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly WhatsAppService _whatsAppService;

    public AuthService(AppDbContext context, WhatsAppService whatsAppService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
    }

    public async Task<LoginResponseDto> LoginAsync(EmployeeLoginDto dto, string ipAddress, string? userAgent)
    {
        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .Include(e => e.Establishment)
            .FirstOrDefaultAsync(e =>
                (e.Cpf == dto.Identifier || e.WhatsApp == dto.Identifier) &&
                e.Status.ToUpper() == "ATIVO");

        var attempt = new LoginAttempt
        {
            Identifier = dto.Identifier,
            EmployeeId = employee?.Id,
            Success = false,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AttemptedAt = DateTime.UtcNow
        };

        if (employee == null)
        {
            attempt.FailureReason = "Funcionário não encontrado ou inativo";
            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = false,
                Message = "CPF/WhatsApp ou senha inválidos"
            };
        }

        if (employee.Establishment?.IsActive != true)
        {
            attempt.FailureReason = "Estabelecimento inativo";
            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = false,
                Message = "Estabelecimento inativo"
            };
        }

        if (!VerifyPassword(dto.Password, employee.PasswordHash))
        {
            attempt.FailureReason = "Senha incorreta";
            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = false,
                Message = "CPF/WhatsApp ou senha inválidos"
            };
        }

        var requires2FA = await Check2FARequired(employee.Id);

        if (requires2FA)
        {
            var (success2FA, _) = await Create2FACodeAsync(employee.Id, "LOGIN", ipAddress, userAgent);

            if (!success2FA)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Erro ao gerar código 2FA"
                };
            }

            attempt.Success = true;
            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = true,
                Message = "Código de verificação enviado via WhatsApp",
                Requires2FA = true
            };
        }

        var sessionToken = await CreateSessionAsync(employee.Id, dto.RememberMe, ipAddress, userAgent);

        attempt.Success = true;
        _context.LoginAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return new LoginResponseDto
        {
            Success = true,
            Message = "Login realizado com sucesso",
            Requires2FA = false,
            SessionId = sessionToken,
            Employee = new EmployeeInfoDto
            {
                Id = employee.Id,
                Name = employee.FullName,
                Cpf = employee.Cpf,
                WhatsApp = employee.WhatsApp,
                Email = employee.Email,
                JobPositionName = employee.JobPosition?.Name ?? "",
                EstablishmentName = employee.Establishment?.NomeFantasia ?? ""
            }
        };
    }

    // ✅ MÉTODO CORRIGIDO COM LOGS
    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(RequestPasswordResetDto dto)
    {
        Console.WriteLine($"🔵 [RequestPasswordResetAsync] INICIANDO para: {dto.Identifier}");

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Cpf == dto.Identifier || e.WhatsApp == dto.Identifier);

        if (employee == null)
        {
            Console.WriteLine($"⚠️ [RequestPasswordResetAsync] Employee não encontrado");
            await Task.Delay(Random.Shared.Next(1000, 3000));
            return (true, "Se o CPF/WhatsApp existir, você receberá o código de recuperação");
        }

        Console.WriteLine($"✅ [RequestPasswordResetAsync] Employee: {employee.Id}, WhatsApp: '{employee.WhatsApp}'");

        // ✅ Rate limit FORA do try-catch
        var recentTokens = await _context.PasswordResetTokens
            .Where(t => t.EmployeeId == employee.Id &&
                       t.CreatedAt > DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        Console.WriteLine($"📊 [RequestPasswordResetAsync] Tokens recentes: {recentTokens}");

        if (recentTokens >= 3)
        {
            Console.WriteLine($"❌ [RequestPasswordResetAsync] Rate limit excedido");
            return (false, "Limite de tentativas excedido. Aguarde 1 hora");
        }

        var code = GenerateNumericCode(6);
        var token = GenerateSecureToken();

        Console.WriteLine($"🔑 [RequestPasswordResetAsync] Código gerado: {code}");

        var resetToken = new PasswordResetToken
        {
            EmployeeId = employee.Id,
            Token = token,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Type = dto.Method.ToUpper(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ [RequestPasswordResetAsync] Token salvo no banco");

            // ✅ VERIFICAR CONDIÇÕES
            Console.WriteLine($"🔍 [RequestPasswordResetAsync] Verificando condições:");
            Console.WriteLine($"   - Method: '{dto.Method}' (Upper: '{dto.Method.ToUpper()}')");
            Console.WriteLine($"   - Employee.WhatsApp: '{employee.WhatsApp ?? "NULL"}'");
            Console.WriteLine($"   - IsNullOrEmpty: {string.IsNullOrEmpty(employee.WhatsApp)}");

            if (dto.Method.ToUpper() == "WHATSAPP" && !string.IsNullOrEmpty(employee.WhatsApp))
            {
                Console.WriteLine($"📱 [RequestPasswordResetAsync] CONDIÇÕES OK! Enviando WhatsApp...");
                Console.WriteLine($"   - Telefone: {employee.WhatsApp}");
                Console.WriteLine($"   - Nome: {employee.FullName}");
                Console.WriteLine($"   - Código: {code}");

                var (whatsappSuccess, whatsappMessage) = await _whatsAppService.SendPasswordResetCodeAsync(
                    employee.WhatsApp,
                    code,
                    employee.FullName
                );

                Console.WriteLine($"📱 [RequestPasswordResetAsync] WhatsApp retornou:");
                Console.WriteLine($"   - Success: {whatsappSuccess}");
                Console.WriteLine($"   - Message: {whatsappMessage}");

                if (!whatsappSuccess)
                {
                    Console.WriteLine($"❌ [RequestPasswordResetAsync] WhatsApp FALHOU!");
                    return (false, $"Erro ao enviar código: {whatsappMessage}");
                }

                Console.WriteLine($"✅ [RequestPasswordResetAsync] WhatsApp ENVIADO COM SUCESSO!");
            }
            else
            {
                Console.WriteLine($"⚠️ [RequestPasswordResetAsync] WhatsApp NÃO FOI ENVIADO!");
                Console.WriteLine($"   - Condição falhou!");
            }

            return (true, "Código de recuperação enviado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [RequestPasswordResetAsync] EXCEPTION:");
            Console.WriteLine($"   Type: {ex.GetType().Name}");
            Console.WriteLine($"   Message: {ex.Message}");
            Console.WriteLine($"   StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"   InnerException: {ex.InnerException.Message}");
            }

            return (false, "Erro ao processar recuperação de senha. Tente novamente.");
        }
    }

    public async Task<(bool Success, string Message)> VerifyResetCodeAsync(VerifyResetCodeDto dto)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Cpf == dto.Identifier || e.WhatsApp == dto.Identifier);

        if (employee == null)
            return (false, "Código inválido ou expirado");

        var token = await _context.PasswordResetTokens
            .Where(t => t.EmployeeId == employee.Id &&
                       t.Code == dto.Code &&
                       !t.IsUsed &&
                       t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (token == null)
            return (false, "Código inválido ou expirado");

        return (true, "Código válido");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Cpf == dto.Identifier || e.WhatsApp == dto.Identifier);

        if (employee == null)
            return (false, "Código inválido ou expirado");

        var token = await _context.PasswordResetTokens
            .Where(t => t.EmployeeId == employee.Id &&
                       t.Code == dto.Code &&
                       !t.IsUsed &&
                       t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (token == null)
            return (false, "Código inválido ou expirado");

        employee.PasswordHash = HashPassword(dto.NewPassword);
        employee.UpdatedAt = DateTime.UtcNow;

        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow;

        await _context.EmployeeSessions
            .Where(s => s.EmployeeId == employee.Id && s.IsActive)
            .ForEachAsync(s => s.IsActive = false);

        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(employee.WhatsApp))
        {
            await _whatsAppService.SendPasswordChangedNotificationAsync(employee.WhatsApp, employee.FullName);
        }

        return (true, "Senha alterada com sucesso");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid employeeId, ChangePasswordDto dto)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            return (false, "Funcionário não encontrado");

        if (!VerifyPassword(dto.CurrentPassword, employee.PasswordHash))
            return (false, "Senha atual incorreta");

        employee.PasswordHash = HashPassword(dto.NewPassword);
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.EmployeeSessions
            .Where(s => s.EmployeeId == employee.Id && s.IsActive)
            .ForEachAsync(s => s.IsActive = false);

        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(employee.WhatsApp))
        {
            await _whatsAppService.SendPasswordChangedNotificationAsync(employee.WhatsApp, employee.FullName);
        }

        return (true, "Senha alterada com sucesso");
    }

    public async Task<(bool Success, string Message)> Create2FACodeAsync(Guid employeeId, string purpose, string? ipAddress, string? userAgent)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            return (false, "Funcionário não encontrado");

        if (string.IsNullOrEmpty(employee.WhatsApp))
            return (false, "WhatsApp não cadastrado");

        var code = GenerateNumericCode(6);

        var twoFA = new TwoFactorAuth
        {
            EmployeeId = employeeId,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Purpose = purpose,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        _context.TwoFactorAuths.Add(twoFA);
        await _context.SaveChangesAsync();

        await _whatsAppService.Send2FACodeAsync(employee.WhatsApp, code, purpose);

        return (true, "Código enviado via WhatsApp");
    }

    public async Task<(bool Success, string Message, string? SessionId)> Verify2FAAsync(Guid employeeId, Verify2FADto dto, string ipAddress, string? userAgent)
    {
        var twoFA = await _context.TwoFactorAuths
            .Where(t => t.EmployeeId == employeeId &&
                       t.Code == dto.Code &&
                       t.Purpose == dto.Purpose &&
                       !t.IsVerified &&
                       t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (twoFA == null)
        {
            var existingCode = await _context.TwoFactorAuths
                .Where(t => t.EmployeeId == employeeId &&
                           t.Code == dto.Code &&
                           t.Purpose == dto.Purpose)
                .FirstOrDefaultAsync();

            if (existingCode != null)
            {
                existingCode.Attempts++;
                await _context.SaveChangesAsync();
            }

            return (false, "Código inválido ou expirado", null);
        }

        twoFA.IsVerified = true;
        twoFA.VerifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (dto.Purpose == "LOGIN")
        {
            var sessionToken = await CreateSessionAsync(employeeId, false, ipAddress, userAgent);
            return (true, "Código verificado com sucesso", sessionToken);
        }

        return (true, "Código verificado com sucesso", null);
    }

    private async Task<bool> Check2FARequired(Guid employeeId)
    {
        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee?.JobPosition == null)
            return false;

        var requires2FA = new[] { "FARMACEUTICO_RT", "GERENTE" }
            .Contains(employee.JobPosition.Code, StringComparer.OrdinalIgnoreCase);

        return requires2FA;
    }

    private async Task<string> CreateSessionAsync(Guid employeeId, bool rememberMe, string ipAddress, string? userAgent)
    {
        var sessionToken = GenerateSecureToken();
        var expiresAt = rememberMe
            ? DateTime.UtcNow.AddDays(30)
            : DateTime.UtcNow.AddHours(8);

        var session = new EmployeeSession
        {
            EmployeeId = employeeId,
            Token = sessionToken,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeSessions.Add(session);
        await _context.SaveChangesAsync();

        return sessionToken;
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private string GenerateNumericCode(int length)
    {
        var chars = "0123456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
            .ToArray());
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}