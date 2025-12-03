using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;

namespace Controllers;

public class SignupMvcController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<SignupMvcController> _logger;

    public SignupMvcController(AppDbContext context, ILogger<SignupMvcController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Passo 1: Formulário de cadastro do estabelecimento
    /// </summary>
    [HttpGet("/signup")]
    public async Task<IActionResult> Index([FromQuery] Guid? planId = null)
    {
        var plans = await _context.Set<SubscriptionPlan>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        ViewBag.Plans = plans;
        ViewBag.SelectedPlanId = planId;

        return View();
    }

    /// <summary>
    /// Passo 2: Verificação do código enviado via WhatsApp
    /// </summary>
    [HttpGet("/signup/verify")]
    public IActionResult Verify([FromQuery] string? whatsapp = null)
    {
        ViewBag.WhatsApp = whatsapp;
        return View();
    }

    /// <summary>
    /// Passo 3: Completar perfil do proprietário (CPF, nome completo)
    /// </summary>
    [HttpGet("/signup/complete-profile")]
    public async Task<IActionResult> CompleteProfile([FromQuery] Guid establishmentId)
    {
        if (establishmentId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Estabelecimento não informado";
            return RedirectToAction("Index");
        }

        var establishment = await _context.Establishments.FindAsync(establishmentId);
        if (establishment == null)
        {
            TempData["ErrorMessage"] = "Estabelecimento não encontrado";
            return RedirectToAction("Index");
        }

        if (!establishment.IsActive)
        {
            TempData["ErrorMessage"] = "Verifique o código primeiro";
            return RedirectToAction("Verify");
        }

        // Verificar se já existe proprietário
        var hasOwner = await _context.Set<Models.Employees.Employee>()
            .AnyAsync(e => e.EstablishmentId == establishmentId);

        if (hasOwner)
        {
            TempData["SuccessMessage"] = "Cadastro já finalizado. Faça login.";
            return Redirect("/login");
        }

        ViewBag.EstablishmentId = establishmentId;
        ViewBag.EstablishmentName = establishment.NomeFantasia;
        ViewBag.Email = establishment.Email;

        return View();
    }

    /// <summary>
    /// Passo 4: Página de pagamento (opcional)
    /// </summary>
    [HttpGet("/signup/payment")]
    public async Task<IActionResult> Payment(
        [FromQuery] Guid establishmentId,
        [FromQuery] Guid planId,
        [FromQuery] bool canceled = false)
    {
        var establishment = await _context.Establishments.FindAsync(establishmentId);
        if (establishment == null)
            return RedirectToAction("Index");

        var plan = await _context.Set<SubscriptionPlan>().FindAsync(planId);
        if (plan == null)
            return RedirectToAction("Index");

        ViewBag.Establishment = establishment;
        ViewBag.Plan = plan;
        ViewBag.Canceled = canceled;

        return View();
    }

    /// <summary>
    /// Passo 5: Página de conclusão (após pagamento)
    /// </summary>
    [HttpGet("/signup/complete")]
    public IActionResult Complete([FromQuery] string? session = null)
    {
        ViewBag.SessionId = session;
        return View();
    }

    /// <summary>
    /// Página de sucesso após finalizar o cadastro
    /// </summary>
    [HttpGet("/signup/success")]
    public IActionResult Success()
    {
        return View();
    }
}
