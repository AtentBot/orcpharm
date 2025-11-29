namespace DTOs;

public class SignupRequestDto
{
    public string NomeFantasia { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string BillingCycle { get; set; } = "MONTHLY";
    public string Password { get; set; } = string.Empty;
    public bool AcceptTerms { get; set; }
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}

public class VerifySignupCodeDto
{
    public string Code { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
}

public class SignupResponseDto
{
    public Guid EstablishmentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool RequiresVerification { get; set; }
}

public class CompleteSignupDto
{
    public Guid EstablishmentId { get; set; }
    public string StripeSessionId { get; set; } = string.Empty;
}
