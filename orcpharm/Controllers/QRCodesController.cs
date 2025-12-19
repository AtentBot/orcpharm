using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Employees;

namespace Controllers;

[Route("QRCodes")]
public class QRCodesController : Controller
{
    private readonly AppDbContext _context;

    public QRCodesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /QRCodes
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return RedirectToAction("Login", "Auth");

        var establishmentId = session.Employee.EstablishmentId;

        var qrcodes = await _context.Set<EstablishmentQRCode>()
            .Where(q => q.EstablishmentId == establishmentId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        ViewBag.QRCodes = qrcodes;
        ViewBag.Establishment = session.Employee?.Establishment;

        return View();
    }

    // GET: /QRCodes/Print/{id}
    [HttpGet("Print/{id:guid}")]
    public async Task<IActionResult> Print(Guid id)
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return RedirectToAction("Login", "Auth");

        var establishmentId = session.Employee.EstablishmentId;

        var qrcode = await _context.Set<EstablishmentQRCode>()
            .Include(q => q.Establishment)
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (qrcode == null)
            return NotFound();

        return View(qrcode);
    }
}
