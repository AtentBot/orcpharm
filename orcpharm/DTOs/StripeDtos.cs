namespace DTOs;

public class SubscriptionPlanDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int? MaxEmployees { get; set; }
    public int? MaxMonthlyOrders { get; set; }
    public Dictionary<string, bool> Features { get; set; } = new();
    public bool IsActive { get; set; }
}

public class CreatePlanDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int MaxEmployees { get; set; }
    public int MaxMonthlyOrders { get; set; }
    public Dictionary<string, bool> Features { get; set; } = new();
}

public class CreateCheckoutSessionDto
{
    public Guid EstablishmentId { get; set; }
    public Guid PlanId { get; set; }
    public string BillingCycle { get; set; } = "MONTHLY";
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class CheckoutSessionResponseDto
{
    public string SessionId { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
}

public class CreatePortalSessionDto
{
    public Guid EstablishmentId { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
}

public class PortalSessionResponseDto
{
    public string Url { get; set; } = string.Empty;
}

public class InvoiceDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? InvoiceUrl { get; set; }
    public string? InvoicePdfUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
