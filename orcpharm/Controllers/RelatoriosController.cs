using Microsoft.AspNetCore.Mvc;
using Data;

namespace Controllers;

[Route("[controller]")]
[Route("Reports")]
public class RelatoriosController : Controller
{
    private readonly AppDbContext _context;

    public RelatoriosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    [HttpGet("Ordens")]
    public IActionResult Ordens()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    [HttpGet("Rendimento")]
    public IActionResult Rendimento()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    [HttpGet("Perdas")]
    public IActionResult Perdas()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    [HttpGet("Custos")]
    public IActionResult Custos()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    [HttpGet("Produtividade")]
    public IActionResult Produtividade()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    [HttpGet("Dashboard")]
    public IActionResult Dashboard()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Items["Employee"] != null;
    }
}
