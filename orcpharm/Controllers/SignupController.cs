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

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifySignupCodeDto dto)
    {
        var (success, message, establishmentId) = await _signupService.VerifyCodeAsync(dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new 
        { 
            message, 
            establishmentId,
            redirectTo = "/signup/payment"
        });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteSignupDto dto)
    {
        // Aqui seria processado após o checkout do Stripe
        // Por enquanto, apenas registrar
        
        _logger.LogInformation("Signup completo para establishment {EstablishmentId}", dto.EstablishmentId);

        return Ok(new { message = "Cadastro finalizado com sucesso" });
    }

    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode([FromBody] VerifySignupCodeDto dto)
    {
        // TODO: Implementar reenvio de código
        return Ok(new { message = "Código reenviado" });
    }
}
