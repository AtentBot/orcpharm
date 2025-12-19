using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs.Cliente;
using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;
using Service.Notifications;

namespace Service;

public class CustomerAuthService
{
    private readonly AppDbContext _context;
    private readonly WhatsAppService _whatsAppService;
    private readonly ILogger<CustomerAuthService> _logger;
    private const int SESSION_DURATION_DAYS = 30;
    private const int VERIFICATION_CODE_EXPIRY_MINUTES = 10;
    private const int MAX_VERIFICATION_ATTEMPTS = 5;
    private const int MAX_LOGIN_ATTEMPTS = 5;
    private const int LOCKOUT_MINUTES = 30;

    public CustomerAuthService(
        AppDbContext context,
        WhatsAppService whatsAppService,
        ILogger<CustomerAuthService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    // ==================== LOGIN ====================

    /// <summary>
    /// Login simples: telefone + senha (sem verificação de código)
    /// Verificação de código só é necessária no cadastro e recuperação de senha
    /// </summary>
    public async Task<CustomerLoginResponseDto> LoginAsync(CustomerLoginDto dto, string ipAddress, string userAgent)
    {
        try
        {
            var phone = NormalizePhone(dto.Phone);

            if (string.IsNullOrEmpty(phone) || phone.Length < 10)
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Telefone inválido"
                };
            }

            if (string.IsNullOrEmpty(dto.Password))
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Senha é obrigatória"
                };
            }

            var auth = await _context.CustomerAuths
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Phone == phone);

            if (auth == null)
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Telefone não cadastrado. Crie sua conta primeiro."
                };
            }

            // Verificar lockout
            if (auth.LockoutEnd.HasValue && auth.LockoutEnd > DateTime.UtcNow)
            {
                var minutes = (int)(auth.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = $"Conta bloqueada. Tente novamente em {minutes} minutos."
                };
            }

            // Verificar se conta está verificada
            if (!auth.IsVerified)
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Conta não verificada. Complete seu cadastro."
                };
            }

            // Verificar se tem senha definida
            if (string.IsNullOrEmpty(auth.PasswordHash))
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Senha não definida. Use 'Esqueci minha senha' para criar uma."
                };
            }

            // Validar senha
            if (!VerifyPassword(dto.Password, auth.PasswordHash))
            {
                auth.FailedLoginAttempts++;
                if (auth.FailedLoginAttempts >= MAX_LOGIN_ATTEMPTS)
                {
                    auth.LockoutEnd = DateTime.UtcNow.AddMinutes(LOCKOUT_MINUTES);
                    auth.FailedLoginAttempts = 0;
                }
                await _context.SaveChangesAsync();

                _logger.LogWarning("Tentativa de login com senha inválida para telefone {Phone}", phone);
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Senha incorreta"
                };
            }

            // Login com sucesso - criar sessão
            var session = await CreateSessionAsync(auth, ipAddress, userAgent);

            auth.FailedLoginAttempts = 0;
            auth.LastLoginAt = DateTime.UtcNow;
            auth.LastLoginIp = ipAddress;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Login bem-sucedido para cliente {CustomerId}", auth.CustomerId);

            return new CustomerLoginResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso!",
                SessionToken = session.SessionToken,
                Customer = MapToCustomerInfo(auth)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no login do cliente");
            return new CustomerLoginResponseDto
            {
                Success = false,
                Message = "Erro ao processar login. Tente novamente."
            };
        }
    }

    // ==================== REGISTRO ====================

    public async Task<CustomerRegisterResponseDto> RegisterAsync(CustomerRegisterDto dto)
    {
        try
        {
            var phone = NormalizePhone(dto.Phone);
            var cpf = NormalizeCpf(dto.Cpf);

            // Validar CPF
            if (!ValidateCpf(cpf))
            {
                return new CustomerRegisterResponseDto
                {
                    Success = false,
                    Message = "CPF inválido."
                };
            }

            // Verificar se já existe
            var existingAuth = await _context.CustomerAuths
                .FirstOrDefaultAsync(a => a.Phone == phone || a.Cpf == cpf);

            if (existingAuth != null)
            {
                if (existingAuth.Phone == phone)
                    return new CustomerRegisterResponseDto { Success = false, Message = "Telefone já cadastrado." };
                else
                    return new CustomerRegisterResponseDto { Success = false, Message = "CPF já cadastrado." };
            }

            // Buscar primeiro estabelecimento ativo
            var defaultEstablishment = await _context.Establishments
                .Where(e => e.IsActive == true)
                .FirstOrDefaultAsync();

            if (defaultEstablishment == null)
            {
                return new CustomerRegisterResponseDto
                {
                    Success = false,
                    Message = "Nenhum estabelecimento disponível."
                };
            }

            // Gerar código do cliente
            var lastCustomer = await _context.Customers
                .Where(c => c.EstablishmentId == defaultEstablishment.Id)
                .OrderByDescending(c => c.Code)
                .FirstOrDefaultAsync();

            var nextCode = 1;
            if (lastCustomer != null && int.TryParse(lastCustomer.Code, out var lastCode))
            {
                nextCode = lastCode + 1;
            }

            // Criar Customer com campos corretos do modelo
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                EstablishmentId = defaultEstablishment.Id,
                Code = nextCode.ToString("D6"),
                FullName = dto.FullName.Trim(),
                Cpf = cpf,
                Phone = phone,
                WhatsApp = phone,
                Email = dto.Email?.Trim(),
                BirthDate = !string.IsNullOrEmpty(dto.BirthDate) ? DateTime.Parse(dto.BirthDate) : null,
                Gender = dto.Gender,
                ZipCode = dto.ZipCode,
                Street = dto.Street,
                Number = dto.Number,
                Complement = dto.Complement,
                Neighborhood = dto.Neighborhood,
                City = dto.City,
                State = dto.State,
                ConsentDataProcessing = dto.ConsentDataProcessing,
                ConsentDate = dto.ConsentDataProcessing ? DateTime.UtcNow : null,
                Status = "ATIVO",
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = Guid.Empty, // Auto-cadastro pelo portal
                UpdatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);

            // Criar CustomerAuth
            var auth = new CustomerAuth
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Phone = phone,
                Cpf = cpf,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomerAuths.Add(auth);
            await _context.SaveChangesAsync();

            // Enviar código de verificação
            await SendVerificationCodeAsync(auth);

            return new CustomerRegisterResponseDto
            {
                Success = true,
                Message = "Cadastro iniciado! Verifique o código enviado por WhatsApp.",
                CustomerId = customer.Id,
                RequiresVerification = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no registro do cliente");
            return new CustomerRegisterResponseDto
            {
                Success = false,
                Message = "Erro ao processar cadastro. Tente novamente."
            };
        }
    }

    // ==================== VERIFICAÇÃO ====================

    public async Task<CustomerVerifyResponseDto> VerifyCodeAsync(CustomerVerifyCodeDto dto, string ipAddress, string userAgent)
    {
        try
        {
            var phone = NormalizePhone(dto.Phone);

            var auth = await _context.CustomerAuths
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Phone == phone);

            if (auth == null)
            {
                return new CustomerVerifyResponseDto
                {
                    Success = false,
                    Message = "Telefone não encontrado."
                };
            }

            // Verificar tentativas
            if (auth.VerificationAttempts >= MAX_VERIFICATION_ATTEMPTS)
            {
                return new CustomerVerifyResponseDto
                {
                    Success = false,
                    Message = "Muitas tentativas. Solicite um novo código."
                };
            }

            // Verificar expiração
            if (auth.VerificationCodeExpiresAt < DateTime.UtcNow)
            {
                return new CustomerVerifyResponseDto
                {
                    Success = false,
                    Message = "Código expirado. Solicite um novo."
                };
            }

            // Verificar código
            if (auth.VerificationCode != dto.Code)
            {
                auth.VerificationAttempts++;
                await _context.SaveChangesAsync();

                return new CustomerVerifyResponseDto
                {
                    Success = false,
                    Message = "Código incorreto."
                };
            }

            // Código válido - marcar como verificado
            auth.IsVerified = true;
            auth.VerificationCode = null;
            auth.VerificationCodeExpiresAt = null;
            auth.VerificationAttempts = 0;

            // Criar sessão
            var session = await CreateSessionAsync(auth, ipAddress, userAgent);

            auth.LastLoginAt = DateTime.UtcNow;
            auth.LastLoginIp = ipAddress;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cliente {CustomerId} verificado com sucesso", auth.CustomerId);

            return new CustomerVerifyResponseDto
            {
                Success = true,
                Message = "Conta verificada com sucesso!",
                SessionToken = session.SessionToken,
                Customer = MapToCustomerInfo(auth)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na verificação do código");
            return new CustomerVerifyResponseDto
            {
                Success = false,
                Message = "Erro ao verificar código. Tente novamente."
            };
        }
    }

    public async Task<(bool Success, string Message)> ResendCodeAsync(string phone)
    {
        try
        {
            phone = NormalizePhone(phone);

            var auth = await _context.CustomerAuths
                .FirstOrDefaultAsync(a => a.Phone == phone);

            if (auth == null)
                return (false, "Telefone não encontrado.");

            // Rate limiting - máximo 1 código por minuto
            if (auth.LastVerificationSentAt.HasValue &&
                auth.LastVerificationSentAt.Value.AddMinutes(1) > DateTime.UtcNow)
            {
                return (false, "Aguarde 1 minuto para solicitar novo código.");
            }

            await SendVerificationCodeAsync(auth);
            return (true, "Novo código enviado para seu WhatsApp.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reenviar código");
            return (false, "Erro ao enviar código.");
        }
    }

    // ==================== SESSÃO ====================

    public async Task<CustomerSession?> ValidateSessionAsync(string sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = await _context.CustomerSessions
            .Include(s => s.Customer)
            .Include(s => s.CurrentEstablishment)
            .FirstOrDefaultAsync(s =>
                s.SessionToken == sessionToken &&
                s.IsActive &&
                s.ExpiresAt > DateTime.UtcNow);

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return session;
    }

    public async Task<bool> LogoutAsync(string sessionToken)
    {
        var session = await _context.CustomerSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session == null)
            return false;

        session.IsActive = false;
        session.LogoutAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SetCurrentEstablishmentAsync(string sessionToken, Guid establishmentId)
    {
        var session = await _context.CustomerSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

        if (session == null)
            return false;

        var establishment = await _context.Establishments
            .FirstOrDefaultAsync(e => e.Id == establishmentId && e.IsActive == true);

        if (establishment == null)
            return false;

        session.CurrentEstablishmentId = establishmentId;
        await _context.SaveChangesAsync();

        return true;
    }

    // ==================== SENHA ====================

    public async Task<(bool Success, string Message)> SetPasswordAsync(string sessionToken, CustomerSetPasswordDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            return (false, "Senhas não conferem.");

        if (dto.Password.Length < 6)
            return (false, "Senha deve ter no mínimo 6 caracteres.");

        var session = await _context.CustomerSessions
            .Include(s => s.CustomerAuth)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

        if (session?.CustomerAuth == null)
            return (false, "Sessão inválida.");

        session.CustomerAuth.PasswordHash = HashPassword(dto.Password);
        session.CustomerAuth.PasswordCreatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Senha definida com sucesso!");
    }

    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(string phone)
    {
        phone = NormalizePhone(phone);

        var auth = await _context.CustomerAuths
            .FirstOrDefaultAsync(a => a.Phone == phone);

        if (auth == null)
            return (false, "Telefone não encontrado.");

        await SendVerificationCodeAsync(auth);
        return (true, "Código de recuperação enviado para seu WhatsApp.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(CustomerResetPasswordConfirmDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return (false, "Senhas não conferem.");

        if (dto.NewPassword.Length < 6)
            return (false, "Senha deve ter no mínimo 6 caracteres.");

        var phone = NormalizePhone(dto.Phone);

        var auth = await _context.CustomerAuths
            .FirstOrDefaultAsync(a => a.Phone == phone);

        if (auth == null)
            return (false, "Telefone não encontrado.");

        if (auth.VerificationCode != dto.Code || auth.VerificationCodeExpiresAt < DateTime.UtcNow)
            return (false, "Código inválido ou expirado.");

        auth.PasswordHash = HashPassword(dto.NewPassword);
        auth.PasswordCreatedAt = DateTime.UtcNow;
        auth.VerificationCode = null;
        auth.VerificationCodeExpiresAt = null;
        auth.IsVerified = true;
        await _context.SaveChangesAsync();

        return (true, "Senha redefinida com sucesso!");
    }

    // ==================== HELPERS ====================

    private async Task SendVerificationCodeAsync(CustomerAuth auth)
    {
        var code = GenerateVerificationCode();

        auth.VerificationCode = code;
        auth.VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(VERIFICATION_CODE_EXPIRY_MINUTES);
        auth.VerificationAttempts = 0;
        auth.LastVerificationSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var message = $"🔐 *OrcPharm*\n\nSeu código de verificação é: *{code}*\n\nVálido por {VERIFICATION_CODE_EXPIRY_MINUTES} minutos.";
        await _whatsAppService.SendMessageAsync(auth.Phone, message);

        _logger.LogInformation("Código de verificação enviado para {Phone}", auth.Phone);
    }

    private async Task<CustomerSession> CreateSessionAsync(CustomerAuth auth, string ipAddress, string userAgent)
    {
        var session = new CustomerSession
        {
            Id = Guid.NewGuid(),
            CustomerAuthId = auth.Id,
            CustomerId = auth.CustomerId,
            SessionToken = GenerateSessionToken(),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceType = DetectDeviceType(userAgent),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(SESSION_DURATION_DAYS),
            IsActive = true
        };

        _context.CustomerSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    private static string GenerateVerificationCode() => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

    private static string GenerateSessionToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string HashPassword(string password) => Argon2.Hash(password);

    private static bool VerifyPassword(string password, string hash) => Argon2.Verify(hash, password);

    private static string NormalizePhone(string phone) => new string(phone.Where(char.IsDigit).ToArray());

    private static string NormalizeCpf(string cpf) => new string(cpf.Where(char.IsDigit).ToArray());

    private static bool ValidateCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;
        var digits = cpf.Select(c => int.Parse(c.ToString())).ToArray();
        var sum = 0;
        for (int i = 0; i < 9; i++) sum += digits[i] * (10 - i);
        var d1 = sum % 11 < 2 ? 0 : 11 - sum % 11;
        if (digits[9] != d1) return false;
        sum = 0;
        for (int i = 0; i < 10; i++) sum += digits[i] * (11 - i);
        var d2 = sum % 11 < 2 ? 0 : 11 - sum % 11;
        return digits[10] == d2;
    }

    private static string DetectDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        userAgent = userAgent.ToLower();
        if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone")) return "Mobile";
        if (userAgent.Contains("tablet") || userAgent.Contains("ipad")) return "Tablet";
        return "Desktop";
    }

    private static CustomerInfoDto MapToCustomerInfo(CustomerAuth auth) => new()
    {
        Id = auth.CustomerId,
        FullName = auth.Customer?.FullName ?? "",
        Phone = auth.Phone,
        Email = auth.Customer?.Email,
        IsVerified = auth.IsVerified
    };
}