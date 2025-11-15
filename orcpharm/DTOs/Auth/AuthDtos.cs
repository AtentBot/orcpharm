namespace DTOs.Auth;

public class EmployeeLoginDto
{
    public string Identifier { get; set; } = string.Empty; // CPF ou WhatsApp
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class RequestPasswordResetDto
{
    public string Identifier { get; set; } = string.Empty; // CPF ou WhatsApp
    public string Method { get; set; } = "WHATSAPP"; // WHATSAPP ou EMAIL
}

public class VerifyResetCodeDto
{
    public string Identifier { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string Identifier { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class Request2FADto
{
    public string Purpose { get; set; } = "LOGIN"; // LOGIN ou CONTROLLED_SUBSTANCE
}

public class Verify2FADto
{
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = "LOGIN";
}

public class LoginResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Requires2FA { get; set; }
    public string? SessionId { get; set; }
    public EmployeeInfoDto? Employee { get; set; }
}

public class EmployeeInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string JobPositionName { get; set; } = string.Empty;
    public string EstablishmentName { get; set; } = string.Empty;
}
