using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para Views do módulo Fiscal
/// Rota: /Fiscal
/// </summary>
[Authorize]
public class FiscalController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<FiscalController> _logger;

    public FiscalController(AppDbContext db, ILogger<FiscalController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Página principal de Notas Fiscais
    /// GET /Fiscal
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return RedirectToAction("Login", "Account");

        // Carregar estatísticas básicas para a view
        var establishmentId = employee.EstablishmentId;
        var today = DateTime.UtcNow.Date;
        var primeiroDiaMes = new DateTime(today.Year, today.Month, 1);

        // Estatísticas do dia
        ViewBag.NFesHoje = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate.Date == today
                && f.InvoiceType == "NFE"
                && f.Status == "AUTORIZADO")
            .CountAsync();

        ViewBag.NFCesHoje = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate.Date == today
                && f.InvoiceType == "NFCE"
                && f.Status == "AUTORIZADO")
            .CountAsync();

        ViewBag.FaturamentoHoje = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate.Date == today
                && f.Status == "AUTORIZADO")
            .SumAsync(f => (decimal?)f.TotalAmount) ?? 0;

        // Estatísticas do mês
        ViewBag.TotalNotasMes = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate >= primeiroDiaMes
                && f.Status == "AUTORIZADO")
            .CountAsync();

        ViewBag.FaturamentoMes = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate >= primeiroDiaMes
                && f.Status == "AUTORIZADO")
            .SumAsync(f => (decimal?)f.TotalAmount) ?? 0;

        // Fila de contingência
        ViewBag.NotasPendentes = await _db.FiscalQueues
            .Where(q => q.EstablishmentId == establishmentId && q.Status == "PENDENTE")
            .CountAsync();

        // Notas com erro
        ViewBag.NotasComErro = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.Status == "REJEITADO"
                && f.IssueDate >= today.AddDays(-7))
            .CountAsync();

        // Verificar configuração fiscal
        var config = await _db.FiscalConfigs
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId && c.IsActive);

        ViewBag.ConfiguracaoOk = config != null;
        ViewBag.Ambiente = config?.Environment ?? "NÃO CONFIGURADO";
        ViewBag.CertificadoValido = config?.CertificateExpiry > DateTime.UtcNow;
        ViewBag.CertificadoExpira = config?.CertificateExpiry;

        return View();
    }

    /// <summary>
    /// Página de configurações fiscais
    /// GET /Fiscal/Config
    /// </summary>
    [HttpGet]
    public IActionResult Config()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Página de detalhes de uma nota fiscal
    /// GET /Fiscal/Details/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var invoice = await _db.FiscalInvoices
            .Include(f => f.Items)
            .Include(f => f.Sale)
            .FirstOrDefaultAsync(f => f.Id == id && f.EstablishmentId == employee.EstablishmentId);

        if (invoice == null)
            return NotFound();

        return View(invoice);
    }
}
