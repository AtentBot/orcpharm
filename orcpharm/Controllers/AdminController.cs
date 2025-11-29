using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/admin/login")]
    public IActionResult Login()
    {
        // Se já estiver logado, redirecionar para dashboard
        if (HttpContext.Items["SaasAdmin"] != null)
        {
            return RedirectToAction("Index", "AdminDashboard");
        }

        return View();
    }

    [HttpGet("/admin/dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet("/admin/establishments")]
    public IActionResult Establishments()
    {
        return View();
    }

    [HttpGet("/admin/establishments/{id}")]
    public IActionResult EstablishmentDetails(Guid id)
    {
        ViewBag.EstablishmentId = id;
        return View();
    }

    [HttpGet("/admin/subscriptions")]
    public IActionResult Subscriptions()
    {
        return View();
    }

    [HttpGet("/admin/plans")]
    public IActionResult Plans()
    {
        return View();
    }

    [HttpGet("/admin/reports")]
    public IActionResult Reports()
    {
        return View();
    }

    [HttpGet("/admin/settings")]
    public IActionResult Settings()
    {
        return View();
    }
}
