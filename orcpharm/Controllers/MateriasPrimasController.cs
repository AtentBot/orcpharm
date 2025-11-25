using Microsoft.AspNetCore.Mvc;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Controllers;

/// <summary>
/// MVC Controller para Views de Matérias-Primas
/// Serve páginas HTML para gerenciamento de matérias-primas
/// </summary>
[Route("MateriasPrimas")]
public class MateriasPrimasController : Controller
{
    private readonly AppDbContext _context;

    public MateriasPrimasController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Listagem de matérias-primas
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Formulário de criação
    /// </summary>
    [HttpGet("Criar")]
    public IActionResult Criar()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View("Form");
    }

    /// <summary>
    /// Formulário de edição
    /// </summary>
    [HttpGet("Editar/{id}")]
    public async Task<IActionResult> Editar(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var material = await _context.RawMaterials
            .FirstOrDefaultAsync(rm => rm.Id == id && rm.IsActive);

        if (material == null)
            return NotFound();

        return View("Form", material);
    }

    /// <summary>
    /// Detalhes da matéria-prima
    /// </summary>
    [HttpGet("Detalhes/{id}")]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var material = await _context.RawMaterials
            .Include(rm => rm.Batches)
            .FirstOrDefaultAsync(rm => rm.Id == id && rm.IsActive);

        if (material == null)
            return NotFound();

        return View(material);
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Items["Employee"] != null;
    }

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        return employee?.EstablishmentId ?? Guid.Empty;
    }
}
