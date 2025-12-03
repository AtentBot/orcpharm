using Microsoft.AspNetCore.Mvc;
using DTOs;
using Service;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SignupController : ControllerBase
{
    private readonly SignupService _signupService;
    private readonly ILogger<SignupController> _logger;

    public SignupController(SignupService signupService, ILogger<SignupController> logger)
    {
        _signupService = signupService;
        _logger = logger;
    }

    /// <summary>
    /// Passo 1: Registra o estabelecimento e envia código de verificação via WhatsApp
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SignupRequestDto dto)
    {
        if (!dto.AcceptTerms)
            return BadRequest(new { message = "Você deve aceitar os termos de uso" });

        var (success, message, establishmentId) = await _signupService.RegisterAsync(dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new SignupResponseDto
        {
            EstablishmentId = establishmentId ?? Guid.Empty,
            Message = message,
            RequiresVerification = true
        });
    }

    /// <summary>
    /// Passo 2: Verifica o código enviado via WhatsApp
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifySignupCodeDto dto)
    {
        var (success, message, establishmentId) = await _signupService.VerifyCodeAsync(dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new VerifyCodeResponseDto
        { 
            EstablishmentId = establishmentId ?? Guid.Empty,
            Message = message, 
            RedirectTo = $"/signup/complete-profile?establishmentId={establishmentId}"
        });
    }

    /// <summary>
    /// Passo 3: Completa o perfil do proprietário (CPF, nome completo)
    /// </summary>
    [HttpPost("complete-profile")]
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteOwnerProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Nome completo é obrigatório" });

        if (string.IsNullOrWhiteSpace(dto.Cpf))
            return BadRequest(new { message = "CPF é obrigatório" });

        var (success, message, employeeId) = await _signupService.CompleteOwnerProfileAsync(dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new CompleteOwnerProfileResponseDto
        {
            EmployeeId = employeeId ?? Guid.Empty,
            Message = message,
            RedirectTo = "/login"
        });
    }

    /// <summary>
    /// Passo 4 (opcional): Finaliza após pagamento do Stripe
    /// </summary>
    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteSignupDto dto)
    {
        _logger.LogInformation("Signup completo para establishment {EstablishmentId}", dto.EstablishmentId);

        return Ok(new { message = "Cadastro finalizado com sucesso" });
    }

    /// <summary>
    /// Reenvia o código de verificação
    /// </summary>
    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode([FromBody] ResendCodeDto dto)
    {
        var (success, message) = await _signupService.ResendCodeAsync(dto.WhatsApp);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }
}

public class ResendCodeDto
{
    public string WhatsApp { get; set; } = string.Empty;
}
