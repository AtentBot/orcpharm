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

    [HttpGet("/signup/verify")]
    public IActionResult Verify([FromQuery] string? whatsapp = null)
    {
        ViewBag.WhatsApp = whatsapp;
        return View();
    }

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

    [HttpGet("/signup/complete")]
    public IActionResult Complete([FromQuery] string? session = null)
    {
        ViewBag.SessionId = session;
        return View();
    }
}
