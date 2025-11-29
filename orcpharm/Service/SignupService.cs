using Data;
using Models;
using DTOs;
using Microsoft.EntityFrameworkCore;
using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;

namespace Service;

public class SignupService
{
    private readonly AppDbContext _context;
    private readonly SubscriptionService _subscriptionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<SignupService> _logger;

    public SignupService(
        AppDbContext context,
        SubscriptionService subscriptionService,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<SignupService> logger)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool success, string message, Guid? establishmentId)> RegisterAsync(SignupRequestDto dto)
    {
        try
        {
            // Validar CNPJ único
            if (!string.IsNullOrWhiteSpace(dto.Cnpj))
            {
                var cnpjExists = await _context.Establishments
                    .AnyAsync(e => e.Cnpj == dto.Cnpj.Trim());

                if (cnpjExists)
                    return (false, "CNPJ já cadastrado", null);
            }

            // Validar email único
            var emailExists = await _context.Establishments
                .AnyAsync(e => e.Email == dto.Email.Trim());

            if (emailExists)
                return (false, "Email já cadastrado", null);

            // Validar WhatsApp único
            var whatsappExists = await _context.Establishments
                .AnyAsync(e => e.WhatsApp == dto.WhatsApp.Trim());

            if (whatsappExists)
                return (false, "WhatsApp já cadastrado", null);

            // Validar plano
            var plan = await _context.Set<SubscriptionPlan>().FindAsync(dto.PlanId);
            if (plan == null || !plan.IsActive)
                return (false, "Plano inválido", null);

            // Criar establishment
            var establishment = new Establishment
            {
                Id = Guid.NewGuid(),
                NomeFantasia = dto.NomeFantasia.Trim(),
                RazaoSocial = dto.RazaoSocial.Trim(),
                Cnpj = dto.Cnpj?.Trim(),
                WhatsApp = dto.WhatsApp.Trim(),
                Email = dto.Email.Trim(),
                PostalCode = dto.ZipCode?.Trim(),
                Street = dto.Street?.Trim(),
                Number = dto.Number?.Trim(),
                Complement = dto.Complement?.Trim(),
                Neighborhood = dto.Neighborhood?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                PasswordHash = Argon2.Hash(dto.Password),
                PasswordAlgorithm = "argon2id-v1",
                PasswordCreatedAt = DateTime.UtcNow,
                OnboardingCompleted = false,
                IsActive = false, // Será ativado após verificação
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Establishments.Add(establishment);

            // Gerar código de 6 dígitos
            var code = RandomNumberGenerator.GetInt32(100000, 1000000);

            // Criar registro de onboarding
            var onboarding = new ClientOnboarding
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishment.Id,
                WhatsApp = dto.WhatsApp.Trim(),
                Numero = code,
                OnboardingCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ClientOnboardings.Add(onboarding);
            await _context.SaveChangesAsync();

            // Enviar código via WhatsApp
            var message = $"Olá {dto.NomeFantasia}! Bem-vindo ao OrcPharm. Seu código de verificação é: {code}";
            await SendWhatsAppAsync(dto.WhatsApp, message);

            _logger.LogInformation("Signup iniciado para establishment {EstablishmentId}", establishment.Id);

            return (true, "Cadastro iniciado. Verifique seu WhatsApp.", establishment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar signup");
            return (false, "Erro ao processar cadastro", null);
        }
    }

    public async Task<(bool success, string message, Guid? establishmentId)> VerifyCodeAsync(VerifySignupCodeDto dto)
    {
        try
        {
            var onboarding = await _context.ClientOnboardings
                .Include(o => o.Establishment)  // Agora funciona com navigation property
                .Where(o => o.WhatsApp == dto.WhatsApp && o.Numero.ToString() == dto.Code)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (onboarding == null)
                return (false, "Código inválido", null);

            if ((DateTime.UtcNow - onboarding.CreatedAt).TotalMinutes > 10)
                return (false, "Código expirado", null);

            if (onboarding.OnboardingCompleted)
                return (false, "Código já utilizado", null);

            onboarding.OnboardingCompleted = true;
            onboarding.UpdatedAt = DateTime.UtcNow;

            var establishment = onboarding.Establishment;
            if (establishment == null)
                return (false, "Establishment não encontrado", null);

            establishment.OnboardingCompleted = true;
            establishment.IsActive = true;
            establishment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Código verificado para establishment {EstablishmentId}", establishment.Id);

            return (true, "Código verificado com sucesso", establishment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar código");
            return (false, "Erro ao verificar código", null);
        }
    }

    public async Task<(bool success, string message)> CompleteSignupAsync(
        Guid establishmentId,
        Guid planId,
        string stripeSessionId)
    {
        try
        {
            var establishment = await _context.Establishments.FindAsync(establishmentId);
            if (establishment == null)
                return (false, "Establishment não encontrado");

            // Criar trial subscription
            var (success, message, subscription) = await _subscriptionService
                .CreateTrialSubscriptionAsync(establishmentId, planId);

            if (!success)
                return (false, message);

            _logger.LogInformation("Signup completo para establishment {EstablishmentId}", establishmentId);

            return (true, "Cadastro finalizado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao completar signup para establishment {EstablishmentId}", establishmentId);
            return (false, "Erro ao completar cadastro");
        }
    }

    private async Task<bool> SendWhatsAppAsync(string number, string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(number)) return false;

            var apiKey = _config["AtentBot:ApiKey"];
            var baseUrl = _config["AtentBot:BaseUrl"] ?? "https://api.atentbot.com";
            
            if (string.IsNullOrWhiteSpace(apiKey)) return false;

            var client = _httpClientFactory.CreateClient();
            var url = $"{baseUrl.TrimEnd('/')}/message/sendText/crescer";
            var payload = new { number, text };

            using var http = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = System.Net.Http.Json.JsonContent.Create(payload)
            };
            http.Headers.Add("apikey", apiKey);

            var resp = await client.SendAsync(http);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar WhatsApp para {Number}", number);
            return false;
        }
    }
}
