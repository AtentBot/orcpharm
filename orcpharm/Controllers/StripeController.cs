using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using DTOs;
using Service;
using Data;
using Microsoft.EntityFrameworkCore;

// Alias para resolver ambiguidade
using AppSubscriptionService = Service.SubscriptionService;
using StripeInvoice = Stripe.Invoice;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly StripeService _stripeService;
    private readonly AppSubscriptionService _subscriptionService;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeController> _logger;

    public StripeController(
        StripeService stripeService,
        AppSubscriptionService subscriptionService,
        AppDbContext context,
        IConfiguration config,
        ILogger<StripeController> logger)
    {
        _stripeService = stripeService;
        _subscriptionService = subscriptionService;
        _context = context;
        _config = config;
        _logger = logger;
    }

    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutSessionDto dto)
    {
        try
        {
            var session = await _stripeService.CreateCheckoutSessionAsync(
                dto.EstablishmentId,
                dto.PlanId,
                dto.BillingCycle,
                dto.SuccessUrl,
                dto.CancelUrl
            );

            var publishableKey = _config["Stripe:PublishableKey"];

            return Ok(new CheckoutSessionResponseDto
            {
                SessionId = session.Id,
                PublishableKey = publishableKey ?? ""
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar checkout session");
            return BadRequest(new { message = "Erro ao criar sessão de pagamento" });
        }
    }

    [HttpPost("create-portal-session")]
    public async Task<IActionResult> CreatePortalSession([FromBody] CreatePortalSessionDto dto)
    {
        try
        {
            var session = await _stripeService.CreatePortalSessionAsync(
                dto.EstablishmentId,
                dto.ReturnUrl
            );

            return Ok(new PortalSessionResponseDto
            {
                Url = session.Url
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar portal session");
            return BadRequest(new { message = "Erro ao criar sessão do portal" });
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var webhookSecret = _config["Stripe:WebhookSecret"];
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret
            );

            _logger.LogInformation("Webhook recebido: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent);
                    break;

                case "customer.subscription.created":
                    await HandleSubscriptionCreated(stripeEvent);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;

                case "invoice.paid":
                    await HandleInvoicePaid(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailed(stripeEvent);
                    break;

                case "customer.subscription.trial_will_end":
                    await HandleTrialWillEnd(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Evento não tratado: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro no webhook do Stripe");
            return BadRequest();
        }
    }

    [HttpGet("success")]
    public IActionResult Success([FromQuery] string session_id)
    {
        return Redirect("/signup/complete?session=" + session_id);
    }

    [HttpGet("cancel")]
    public IActionResult Cancel()
    {
        return Redirect("/signup/payment?canceled=true");
    }

    /// <summary>
    /// Helper para extrair SubscriptionId da Invoice no Stripe.net 49.x
    /// A estrutura mudou e o SubscriptionId agora está em Parent.SubscriptionDetails
    /// </summary>
    private string? GetSubscriptionIdFromInvoice(StripeInvoice invoice)
    {
        // Tenta obter do Parent.SubscriptionDetails (estrutura nova do Stripe.net 49.x)
        var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;

        if (!string.IsNullOrEmpty(subscriptionId))
            return subscriptionId;

        // Fallback: tenta obter das Lines
        var lineItem = invoice.Lines?.Data?.FirstOrDefault();
        if (lineItem?.Parent?.SubscriptionItemDetails != null)
        {
            return lineItem.Parent.SubscriptionItemDetails.Subscription;
        }

        return null;
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        var establishmentId = session.Metadata.GetValueOrDefault("establishment_id");
        var planId = session.Metadata.GetValueOrDefault("plan_id");

        if (string.IsNullOrEmpty(establishmentId) || string.IsNullOrEmpty(planId))
            return;

        _logger.LogInformation("Checkout completo para establishment {EstablishmentId}", establishmentId);
    }

    private async Task HandleSubscriptionCreated(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var establishmentIdStr = stripeSubscription.Metadata.GetValueOrDefault("establishment_id");
        if (string.IsNullOrEmpty(establishmentIdStr) || !Guid.TryParse(establishmentIdStr, out var establishmentId))
            return;

        var subscription = await _context.Set<Models.Subscription>()
            .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);

        if (subscription != null)
        {
            subscription.StripeSubscriptionId = stripeSubscription.Id;
            subscription.StripeCustomerId = stripeSubscription.CustomerId;
            subscription.Status = stripeSubscription.Status.ToUpper();

            // Stripe.net 49.x: CurrentPeriodStart/End estão no SubscriptionItem
            var firstItem = stripeSubscription.Items?.Data?.FirstOrDefault();
            if (firstItem != null)
            {
                subscription.CurrentPeriodStart = firstItem.CurrentPeriodStart;
                subscription.CurrentPeriodEnd = firstItem.CurrentPeriodEnd;
            }

            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Subscription criada: {SubscriptionId} para establishment {EstablishmentId}",
            stripeSubscription.Id, establishmentId);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var subscription = await _context.Set<Models.Subscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription != null)
        {
            // Stripe.net 49.x: CurrentPeriodStart/End estão no SubscriptionItem
            var firstItem = stripeSubscription.Items?.Data?.FirstOrDefault();

            await _subscriptionService.UpdateSubscriptionStatusAsync(
                subscription.Id,
                stripeSubscription.Status.ToUpper(),
                stripeSubscription.Id,
                firstItem?.CurrentPeriodStart ?? DateTime.UtcNow,
                firstItem?.CurrentPeriodEnd ?? DateTime.UtcNow.AddMonths(1)
            );
        }

        _logger.LogInformation("Subscription atualizada: {SubscriptionId}, Status: {Status}",
            stripeSubscription.Id, stripeSubscription.Status);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var subscription = await _context.Set<Models.Subscription>()
            .Include(s => s.Establishment)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription != null)
        {
            subscription.Status = "CANCELED";
            subscription.CanceledAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (subscription.Establishment != null)
            {
                subscription.Establishment.IsActive = false;
                subscription.Establishment.SubscriptionStatus = "CANCELED";
            }

            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Subscription cancelada: {SubscriptionId}", stripeSubscription.Id);
    }

    private async Task HandleInvoicePaid(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as StripeInvoice;
        if (invoice == null) return;

        var subscriptionId = GetSubscriptionIdFromInvoice(invoice);
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var subscription = await _context.Set<Models.Subscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            var invoiceRecord = new Models.Billing.Invoice
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                StripeInvoiceId = invoice.Id,
                Amount = invoice.AmountPaid / 100m,
                Status = "PAID",
                InvoiceUrl = invoice.HostedInvoiceUrl,
                InvoicePdfUrl = invoice.InvoicePdf,
                PaidAt = DateTime.UtcNow,
                DueDate = invoice.DueDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<Models.Billing.Invoice>().Add(invoiceRecord);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Invoice paga: {InvoiceId}", invoice.Id);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as StripeInvoice;
        if (invoice == null) return;

        var subscriptionId = GetSubscriptionIdFromInvoice(invoice);
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var subscription = await _context.Set<Models.Subscription>()
            .Include(s => s.Establishment)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            var invoiceRecord = new Models.Billing.Invoice
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                StripeInvoiceId = invoice.Id,
                Amount = invoice.AmountDue / 100m,
                Status = "FAILED",
                InvoiceUrl = invoice.HostedInvoiceUrl,
                InvoicePdfUrl = invoice.InvoicePdf,
                DueDate = invoice.DueDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<Models.Billing.Invoice>().Add(invoiceRecord);

            subscription.Status = "PAST_DUE";
            subscription.UpdatedAt = DateTime.UtcNow;

            if (subscription.Establishment != null)
            {
                subscription.Establishment.SubscriptionStatus = "PAST_DUE";
            }

            await _context.SaveChangesAsync();
        }

        _logger.LogWarning("Falha no pagamento da invoice: {InvoiceId}", invoice.Id);
    }

    private async Task HandleTrialWillEnd(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var subscription = await _context.Set<Models.Subscription>()
            .Include(s => s.Establishment)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription?.Establishment != null)
        {
            _logger.LogInformation("Trial vai expirar para establishment {EstablishmentId}",
                subscription.EstablishmentId);
        }
    }
}