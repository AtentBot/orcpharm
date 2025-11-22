using Microsoft.AspNetCore.Mvc;
using Data;
using Models.Employees;
using Microsoft.EntityFrameworkCore;
using ViewModels;

namespace Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(AppDbContext db, ILogger<DashboardController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== DASHBOARD PRINCIPAL ====================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var employee = HttpContext.Items["Employee"] as Employee;

        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Carregar dados completos do funcionário
        var fullEmployee = await _db.Employees
            .Include(e => e.Establishment)
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == employee.Id);

        if (fullEmployee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Preparar dados para o dashboard
        ViewBag.EmployeeName = fullEmployee.FullName;
        ViewBag.JobPosition = fullEmployee.JobPosition?.Name ?? "Sem cargo";
        ViewBag.EstablishmentName = fullEmployee.Establishment?.NomeFantasia ?? "Estabelecimento";
        ViewBag.AccessLevel = fullEmployee.JobPosition?.Code ?? "USER";

        // Estatísticas baseadas no nível de acesso
        var dashboardData = new DashboardViewModel
        {
            Employee = fullEmployee,
            CanViewReports = CanViewReports(fullEmployee),
            CanManageEmployees = CanManageEmployees(fullEmployee),
            CanManageInventory = CanManageInventory(fullEmployee),
            CanManageFormulas = CanManageFormulas(fullEmployee),
            CanManagePurchases = CanManagePurchases(fullEmployee)
        };

        // Buscar estatísticas se tiver permissão
        if (dashboardData.CanViewReports)
        {
            dashboardData.TotalSuppliers = await _db.Suppliers
                .Where(s => s.EstablishmentId == fullEmployee.EstablishmentId && s.IsActive)
                .CountAsync();

            dashboardData.TotalRawMaterials = await _db.RawMaterials
                .Where(rm => rm.EstablishmentId == fullEmployee.EstablishmentId && rm.IsActive)
                .CountAsync();

            dashboardData.LowStockItems = await _db.RawMaterials
                .Where(rm => rm.EstablishmentId == fullEmployee.EstablishmentId
                    && rm.IsActive
                    && rm.CurrentStock <= rm.MinimumStock)
                .CountAsync();

            dashboardData.PendingPurchases = await _db.PurchaseOrders
                .Where(p => p.EstablishmentId == fullEmployee.EstablishmentId
                    && p.Status == "PENDENTE")
                .CountAsync();
        }

        return View(dashboardData);
    }

    // ==================== MÉTODOS AUXILIARES ====================
    private bool CanViewReports(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "MANAGER", "PHARMACIST_RT", "SUPERVISOR" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }

    private bool CanManageEmployees(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "MANAGER" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }

    private bool CanManageInventory(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "MANAGER", "PHARMACIST_RT", "SUPERVISOR", "STOCK_CONTROLLER" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }

    private bool CanManageFormulas(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "MANAGER", "PHARMACIST_RT", "PHARMACIST" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }

    private bool CanManagePurchases(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "MANAGER", "PHARMACIST_RT", "SUPERVISOR", "PURCHASER" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }
}