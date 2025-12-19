using Data;
using Models;
using Models.Employees;
using DTOs;
using Microsoft.EntityFrameworkCore;
using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;
using Service.Notifications;

namespace Service;

public class SignupService
{
    private readonly AppDbContext _context;
    private readonly SubscriptionService _subscriptionService;
    private readonly WhatsAppService _whatsAppService;
    private readonly ILogger<SignupService> _logger;

    // IDs padrão para novos cadastros
    private static readonly Guid OwnerAccessLevelId = Guid.Parse("b0000000-0000-0000-0000-000000000001");  // OWNER - Proprietário
    private static readonly Guid DefaultCategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000001");   // Farmácia de Manipulação
    private static readonly Guid OwnerJobPositionId = Guid.Parse("145f5c60-62fa-428d-ab46-56503621a8d6");  // Cargo: Proprietário(a)

    public SignupService(
        AppDbContext context,
        SubscriptionService subscriptionService,
        WhatsAppService whatsAppService,
        ILogger<SignupService> logger)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<(bool success, string message, Guid? establishmentId)> RegisterAsync(SignupRequestDto dto)
    {
        try
        {
            // Limpar CNPJ - remover formatação (pontos, barras, hífens)
            var cnpjLimpo = string.IsNullOrWhiteSpace(dto.Cnpj) 
                ? null 
                : new string(dto.Cnpj.Where(char.IsDigit).ToArray());

            // Limpar WhatsApp - remover formatação (parênteses, espaços, hífens)
            var whatsappLimpo = string.IsNullOrWhiteSpace(dto.WhatsApp)
                ? null
                : new string(dto.WhatsApp.Where(char.IsDigit).ToArray());

            // Validar CNPJ único
            if (!string.IsNullOrWhiteSpace(cnpjLimpo))
            {
                var cnpjExists = await _context.Establishments
                    .AnyAsync(e => e.Cnpj == cnpjLimpo);

                if (cnpjExists)
                    return (false, "CNPJ já cadastrado", null);
            }

            // Validar email único
            var emailExists = await _context.Establishments
                .AnyAsync(e => e.Email == dto.Email.Trim());

            if (emailExists)
                return (false, "Email já cadastrado", null);

            // Validar WhatsApp único
            if (!string.IsNullOrWhiteSpace(whatsappLimpo))
            {
                var whatsappExists = await _context.Establishments
                    .AnyAsync(e => e.WhatsApp == whatsappLimpo);

                if (whatsappExists)
                    return (false, "WhatsApp já cadastrado", null); 
            }

            // Validar plano (opcional - usa plano gratuito/trial se não informado)
            SubscriptionPlan? plan = null;
            if (dto.PlanId.HasValue && dto.PlanId.Value != Guid.Empty)
            {
                plan = await _context.Set<SubscriptionPlan>().FindAsync(dto.PlanId.Value);
                if (plan == null || !plan.IsActive)
                    return (false, "Plano inválido", null);
            }
            else
            {
                // Usar primeiro plano ativo como padrão (geralmente o mais barato/trial)
                plan = await _context.Set<SubscriptionPlan>()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.PriceMonthly)
                    .FirstOrDefaultAsync();

                if (plan == null)
                    return (false, "Nenhum plano disponível", null);
            }

            // Criar establishment
            var establishment = new Establishment
            {
                Id = Guid.NewGuid(),
                NomeFantasia = dto.NomeFantasia.Trim(),
                RazaoSocial = dto.RazaoSocial.Trim(),
                Cnpj = cnpjLimpo,
                WhatsApp = whatsappLimpo,
                Email = dto.Email.Trim(),
                PostalCode = dto.ZipCode?.Trim(),
                Street = dto.Street?.Trim(),
                Number = dto.Number?.Trim(),
                Complement = dto.Complement?.Trim(),
                Neighborhood = dto.Neighborhood?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                AccessLevelId = OwnerAccessLevelId,
                CategoryId = DefaultCategoryId,
                PasswordHash = Argon2.Hash(dto.Password),
                PasswordAlgorithm = "argon2id-v1",
                PasswordCreatedAt = DateTime.UtcNow,
                OnboardingCompleted = false,
                IsActive = false,
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
                WhatsApp = whatsappLimpo,
                Numero = code,
                OnboardingCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ClientOnboardings.Add(onboarding);
            await _context.SaveChangesAsync();

            // Enviar código via WhatsApp
            var message = $"🔐 OrcPharm - Verificação de Cadastro\n\n" +
                          $"Olá {dto.NomeFantasia}!\n\n" +
                          $"Bem-vindo ao OrcPharm! Seu código de verificação é: *{code}*\n\n" +
                          $"Este código expira em 10 minutos.\n" +
                          $"Se você não solicitou este cadastro, ignore esta mensagem.";
            
            var (whatsappSuccess, whatsappMessage) = await _whatsAppService.SendMessageAsync(whatsappLimpo!, message);
            
            if (!whatsappSuccess)
            {
                _logger.LogWarning("Falha ao enviar WhatsApp para {Number}: {Message}", whatsappLimpo, whatsappMessage);
            }

            _logger.LogInformation("Signup iniciado para establishment {EstablishmentId}", establishment.Id);

            return (true, "Cadastro iniciado. Verifique seu WhatsApp.", establishment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar signup");
            return (false, "Erro ao processar cadastro", null);
        }
    }

    /// <summary>
    /// Verifica o código e ativa o Establishment.
    /// NÃO cria o Employee aqui - isso será feito em CompleteOwnerProfileAsync
    /// </summary>
    public async Task<(bool success, string message, Guid? establishmentId)> VerifyCodeAsync(VerifySignupCodeDto dto)
    {
        try
        {
            // Limpar WhatsApp para busca
            var whatsappLimpo = string.IsNullOrWhiteSpace(dto.WhatsApp)
                ? null
                : new string(dto.WhatsApp.Where(char.IsDigit).ToArray());

            var onboarding = await _context.ClientOnboardings
                .Include(o => o.Establishment)
                .Where(o => o.WhatsApp == whatsappLimpo && o.Numero.ToString() == dto.Code)
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

            // Ativa o establishment, mas NÃO cria o Employee ainda
            establishment.IsActive = true;
            establishment.UpdatedAt = DateTime.UtcNow;
            // OnboardingCompleted será true apenas após criar o Employee

            await _context.SaveChangesAsync();

            _logger.LogInformation("Código verificado para establishment {EstablishmentId}. Aguardando perfil do proprietário.", 
                establishment.Id);

            return (true, "Código verificado com sucesso", establishment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar código");
            return (false, "Erro ao verificar código", null);
        }
    }

    /// <summary>
    /// Completa o perfil do proprietário criando o Employee
    /// </summary>
    public async Task<(bool success, string message, Guid? employeeId)> CompleteOwnerProfileAsync(CompleteOwnerProfileDto dto)
    {
        try
        {
            // Buscar o establishment
            var establishment = await _context.Establishments.FindAsync(dto.EstablishmentId);
            if (establishment == null)
                return (false, "Estabelecimento não encontrado", null);

            if (!establishment.IsActive)
                return (false, "Estabelecimento não está ativo. Verifique o código primeiro.", null);

            // Verificar se já existe um proprietário para este establishment
            var existingOwner = await _context.Set<Employee>()
                .AnyAsync(e => e.EstablishmentId == dto.EstablishmentId && e.JobPositionId == OwnerJobPositionId);

            if (existingOwner)
                return (false, "Já existe um proprietário cadastrado para este estabelecimento", null);

            // Limpar CPF
            var cpfLimpo = new string(dto.Cpf.Where(char.IsDigit).ToArray());

            if (cpfLimpo.Length != 11)
                return (false, "CPF inválido. Deve conter 11 dígitos.", null);

            // Verificar se CPF já está em uso
            var cpfExists = await _context.Set<Employee>()
                .AnyAsync(e => e.Cpf == cpfLimpo);

            if (cpfExists)
                return (false, "CPF já cadastrado no sistema", null);

            // Criar o Employee proprietário
            var owner = new Employee
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishment.Id,
                JobPositionId = OwnerJobPositionId,
                FullName = dto.FullName.Trim(),
                Cpf = cpfLimpo,
                Email = establishment.Email!,
                WhatsApp = establishment.WhatsApp,
                Phone = dto.Phone?.Trim(),
                DateOfBirth = dto.DateOfBirth,
                // Copiar senha do establishment
                PasswordHash = establishment.PasswordHash,
                PasswordAlgorithm = establishment.PasswordAlgorithm,
                PasswordCreatedAt = establishment.PasswordCreatedAt,
                RequirePasswordChange = false,
                // Dados de contrato
                ContractType = "Proprietário",
                Status = "Ativo",
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                // Auditoria
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<Employee>().Add(owner);

            // Marcar onboarding como completo
            establishment.OnboardingCompleted = true;
            establishment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Enviar mensagem de boas-vindas
            var welcomeMessage = $"✅ OrcPharm - Cadastro Completo!\n\n" +
                                 $"Parabéns {dto.FullName}!\n\n" +
                                 $"Seu cadastro no {establishment.NomeFantasia} foi finalizado com sucesso.\n\n" +
                                 $"🔑 Acesse o sistema com:\n" +
                                 $"CPF: {FormatCpf(cpfLimpo)}\n" +
                                 $"Senha: a mesma que você cadastrou\n\n" +
                                 $"URL: https://app.orcpharm.com.br/login\n\n" +
                                 $"Boas manipulações! 💊";
            
            await _whatsAppService.SendMessageAsync(establishment.WhatsApp!, welcomeMessage);

            _logger.LogInformation("Proprietário {EmployeeId} criado para establishment {EstablishmentId}", 
                owner.Id, establishment.Id);

            return (true, "Cadastro finalizado com sucesso!", owner.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao completar perfil do proprietário");
            return (false, "Erro ao completar cadastro", null);
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

    public async Task<(bool success, string message)> ResendCodeAsync(string whatsApp)
    {
        try
        {
            var whatsappLimpo = new string(whatsApp.Where(char.IsDigit).ToArray());

            var onboarding = await _context.ClientOnboardings
                .Include(o => o.Establishment)
                .Where(o => o.WhatsApp == whatsappLimpo && !o.OnboardingCompleted)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (onboarding == null)
                return (false, "Cadastro não encontrado");

            // Gerar novo código
            var code = RandomNumberGenerator.GetInt32(100000, 1000000);
            onboarding.Numero = code;
            onboarding.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            // Enviar novo código via WhatsApp
            var message = $"🔐 OrcPharm - Novo Código de Verificação\n\n" +
                          $"Olá {onboarding.Establishment?.NomeFantasia}!\n\n" +
                          $"Seu novo código de verificação é: *{code}*\n\n" +
                          $"Este código expira em 10 minutos.";
            
            var (whatsappSuccess, whatsappMessage) = await _whatsAppService.SendMessageAsync(whatsappLimpo, message);
            
            if (!whatsappSuccess)
            {
                _logger.LogWarning("Falha ao reenviar código WhatsApp para {Number}: {Message}", whatsappLimpo, whatsappMessage);
                return (false, "Falha ao enviar código. Tente novamente.");
            }

            return (true, "Código reenviado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reenviar código");
            return (false, "Erro ao reenviar código");
        }
    }

    /// <summary>
    /// Formata CPF para exibição: 000.000.000-00
    /// </summary>
    private static string FormatCpf(string cpf)
    {
        if (string.IsNullOrEmpty(cpf) || cpf.Length != 11)
            return cpf;
        return $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";
    }
}
