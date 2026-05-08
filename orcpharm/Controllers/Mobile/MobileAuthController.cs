using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;
using Models;
using Service.Marketplace;
using Isopoh.Cryptography.Argon2;

namespace Controllers.Mobile;

[ApiController]
[Route("api/mobile/v1/auth")]
public class MobileAuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly ILogger<MobileAuthController> _logger;

    public MobileAuthController(AppDbContext db, JwtTokenService jwt, ILogger<MobileAuthController> logger)
    {
        _db = db;
        _jwt = jwt;
        _logger = logger;
    }

    /// <summary>
    /// Registro de novo cliente via app mobile
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<MobileAuthResponse>> Register([FromBody] MobileRegisterRequest request)
    {
        // Verificar email duplicado
        var existingAuth = await _db.CustomerAuths
            .AnyAsync(a => a.Phone == request.Email || a.Email == request.Email);

        var existingCustomer = await _db.Customers
            .AnyAsync(c => c.Email == request.Email);

        if (existingAuth || existingCustomer)
        {
            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Este email já está cadastrado"
            });
        }

        // Criar customer (sem EstablishmentId — cliente marketplace é global)
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Cpf = request.Cpf,
            LoginProvider = "EMAIL",
            Status = "ATIVO",
            ConsentDataProcessing = true,
            ConsentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Criar auth
        var passwordHash = Argon2.Hash(request.Password);
        var auth = new CustomerAuth
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Phone = request.Phone ?? request.Email, // Phone ou email como fallback
            Email = request.Email,
            Cpf = request.Cpf ?? "",
            PasswordHash = passwordHash,
            PasswordAlgorithm = "argon2id-v1",
            IsVerified = true, // Sem verificação por SMS no mobile inicialmente
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        _db.CustomerAuths.Add(auth);
        await _db.SaveChangesAsync();

        // Gerar tokens
        var accessToken = _jwt.GenerateAccessToken(customer.Id, request.Email, request.FullName);
        var refreshToken = _jwt.GenerateRefreshToken();

        // Salvar refresh token
        var session = new CustomerSession
        {
            Id = Guid.NewGuid(),
            CustomerAuthId = auth.Id,
            SessionToken = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _db.CustomerSessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Novo cliente marketplace registrado: {Email}", request.Email);

        return Ok(new MobileAuthResponse
        {
            Success = true,
            Message = "Conta criada com sucesso",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Customer = new MobileCustomerProfile
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                LoginProvider = "EMAIL"
            }
        });
    }

    /// <summary>
    /// Login de cliente via email/senha
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<MobileAuthResponse>> Login([FromBody] MobileLoginRequest request)
    {
        var auth = await _db.CustomerAuths
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Email == request.Email || a.Phone == request.Email);

        if (auth == null)
        {
            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Email ou senha incorretos"
            });
        }

        if (auth.LockoutEnd.HasValue && auth.LockoutEnd > DateTime.UtcNow)
        {
            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Conta temporariamente bloqueada. Tente novamente mais tarde."
            });
        }

        if (!Argon2.Verify(auth.PasswordHash, request.Password))
        {
            auth.FailedLoginAttempts += 1;
            if (auth.FailedLoginAttempts >= 5)
            {
                auth.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                auth.FailedLoginAttempts = 0;
            }
            await _db.SaveChangesAsync();

            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Email ou senha incorretos"
            });
        }

        // Reset failed attempts
        auth.FailedLoginAttempts = 0;
        auth.LockoutEnd = null;
        auth.LastLoginAt = DateTime.UtcNow;

        var customer = auth.Customer!;
        var accessToken = _jwt.GenerateAccessToken(customer.Id, request.Email, customer.FullName);
        var refreshToken = _jwt.GenerateRefreshToken();

        var session = new CustomerSession
        {
            Id = Guid.NewGuid(),
            CustomerAuthId = auth.Id,
            SessionToken = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _db.CustomerSessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(new MobileAuthResponse
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Customer = new MobileCustomerProfile
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                ProfileImageUrl = customer.ProfileImageUrl,
                LoginProvider = customer.LoginProvider
            }
        });
    }

    /// <summary>
    /// Renovar access token usando refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<MobileAuthResponse>> RefreshToken([FromBody] MobileRefreshTokenRequest request)
    {
        var session = await _db.CustomerSessions
            .Include(s => s.CustomerAuth)
                .ThenInclude(a => a!.Customer)
            .FirstOrDefaultAsync(s => s.SessionToken == request.RefreshToken
                                     && s.IsActive
                                     && s.ExpiresAt > DateTime.UtcNow);

        if (session?.CustomerAuth?.Customer == null)
        {
            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Token inválido ou expirado"
            });
        }

        var customer = session.CustomerAuth.Customer;
        var accessToken = _jwt.GenerateAccessToken(customer.Id, customer.Email ?? "", customer.FullName);

        // Rotacionar refresh token
        session.IsActive = false;
        var newRefreshToken = _jwt.GenerateRefreshToken();

        var newSession = new CustomerSession
        {
            Id = Guid.NewGuid(),
            CustomerAuthId = session.CustomerAuthId,
            SessionToken = newRefreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _db.CustomerSessions.Add(newSession);
        await _db.SaveChangesAsync();

        return Ok(new MobileAuthResponse
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Customer = new MobileCustomerProfile
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                ProfileImageUrl = customer.ProfileImageUrl,
                LoginProvider = customer.LoginProvider
            }
        });
    }

    /// <summary>
    /// Perfil do cliente autenticado
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<MobileCustomerProfile>>> GetProfile()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var customer = await _db.Customers.FindAsync(customerId.Value);
        if (customer == null)
            return NotFound(ApiResponse.ErrorResponse("Cliente não encontrado"));

        return Ok(ApiResponse<MobileCustomerProfile>.SuccessResponse(new MobileCustomerProfile
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            ProfileImageUrl = customer.ProfileImageUrl,
            LoginProvider = customer.LoginProvider
        }));
    }

    private Guid? GetCustomerId()
    {
        if (HttpContext.Items.TryGetValue("MobileCustomerId", out var id) && id is Guid customerId)
            return customerId;
        return null;
    }
}
