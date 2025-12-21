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
            var establishmentId = fullEmployee.EstablishmentId;
            var today = DateTime.UtcNow.Date;

            // Estatísticas existentes
            dashboardData.TotalSuppliers = await _db.Suppliers
                .Where(s => s.EstablishmentId == establishmentId && s.IsActive)
                .CountAsync();

            dashboardData.TotalRawMaterials = await _db.RawMaterials
                .Where(rm => rm.EstablishmentId == establishmentId && rm.IsActive)
                .CountAsync();

            dashboardData.LowStockItems = await _db.RawMaterials
                .Where(rm => rm.EstablishmentId == establishmentId
                    && rm.IsActive
                    && rm.CurrentStock <= rm.MinimumStock)
                .CountAsync();

            dashboardData.PendingPurchases = await _db.PurchaseOrders
                .Where(p => p.EstablishmentId == establishmentId
                    && p.Status == "PENDENTE")
                .CountAsync();

            // Manipulações atrasadas
            dashboardData.TotalAtrasados = await _db.ManipulationOrders
                .Where(mo => mo.EstablishmentId == establishmentId
                    && mo.Status != "FINALIZADO"
                    && mo.Status != "ENTREGUE"
                    && mo.Status != "CANCELADO"
                    && mo.ExpectedDate.Date < today)
                .CountAsync();

            // ========== ESTATÍSTICAS DE FUNCIONÁRIOS ==========
            dashboardData.TotalEmployees = await _db.Employees
                .Where(e => e.EstablishmentId == establishmentId)
                .CountAsync();

            dashboardData.TotalEmployeesActive = await _db.Employees
                .Where(e => e.EstablishmentId == establishmentId)
                .CountAsync();

            dashboardData.TotalEmployeesInactive = await _db.Employees
                .Where(e => e.EstablishmentId == establishmentId)
                .CountAsync();

            // ========== ESTATÍSTICAS DE PRESCRIÇÕES ==========
            dashboardData.PrescriptionsPendentes = await _db.Prescriptions
                .Where(p => p.EstablishmentId == establishmentId && p.Status == "PENDENTE")
                .CountAsync();

            dashboardData.PrescriptionsValidadas = await _db.Prescriptions
                .Where(p => p.EstablishmentId == establishmentId && p.Status == "VALIDADA")
                .CountAsync();

            dashboardData.PrescriptionsHoje = await _db.Prescriptions
                .Where(p => p.EstablishmentId == establishmentId && p.CreatedAt.Date == today)
                .CountAsync();

            // Prescrições vencendo em 7 dias
            var seteDias = today.AddDays(7);
            dashboardData.PrescriptionsVencendo = await _db.Prescriptions
                .Where(p => p.EstablishmentId == establishmentId
                    && p.Status != "CANCELADA"
                    && p.Status != "CONCLUIDA"
                    && p.ExpirationDate.Date <= seteDias
                    && p.ExpirationDate.Date >= today)
                .CountAsync();

            // ========== ESTATÍSTICAS DE ORÇAMENTOS ==========
            dashboardData.OrcamentosPendentes = await _db.PrescriptionQuotes
                .Where(q => q.EstablishmentId == establishmentId && q.Status == "PENDENTE")
                .CountAsync();

            dashboardData.OrcamentosAprovados = await _db.PrescriptionQuotes
                .Where(q => q.EstablishmentId == establishmentId && q.Status == "APROVADO")
                .CountAsync();

            dashboardData.OrcamentosHoje = await _db.PrescriptionQuotes
                .Where(q => q.EstablishmentId == establishmentId && q.CreatedAt.Date == today)
                .CountAsync();

            // Orçamentos expirando hoje
            dashboardData.OrcamentosExpirando = await _db.PrescriptionQuotes
                .Where(q => q.EstablishmentId == establishmentId
                    && q.Status == "PENDENTE"
                    && q.ValidUntil.Date <= today.AddDays(1))
                .CountAsync();

            // Valor total de orçamentos pendentes
            dashboardData.ValorOrcamentosPendentes = await _db.PrescriptionQuotes
                .Where(q => q.EstablishmentId == establishmentId && q.Status == "PENDENTE")
                .SumAsync(q => (decimal?)q.FinalPrice) ?? 0;

            // Taxa de conversão (últimos 30 dias)
            var trintaDiasAtras = today.AddDays(-30);
            var totalOrcamentos30Dias = await _db.PrescriptionQuotes
                .Where(q => q.EstablishmentId == establishmentId && q.CreatedAt >= trintaDiasAtras)
                .CountAsync();

            var aprovados30Dias = await _db.PrescriptionQuotes
                .Where(q => q.EstablishmentId == establishmentId
                    && q.CreatedAt >= trintaDiasAtras
                    && (q.Status == "APROVADO" || q.Status == "CONVERTIDO"))
                .CountAsync();

            dashboardData.TaxaConversaoOrcamentos = totalOrcamentos30Dias > 0
                ? Math.Round((decimal)aprovados30Dias / totalOrcamentos30Dias * 100, 1)
                : 0;

            // ========== ESTATÍSTICAS DE PEDIDOS ONLINE ==========
            dashboardData.PedidosPendentes = await _db.OnlineOrders
                .Where(o => o.EstablishmentId == establishmentId 
                    && (o.Status == "PENDING" || o.Status == "CONFIRMED"))
                .CountAsync();

            dashboardData.PedidosProntos = await _db.OnlineOrders
                .Where(o => o.EstablishmentId == establishmentId && o.Status == "READY")
                .CountAsync();

            // ========== ESTATÍSTICAS FISCAIS (NF-e/NFC-e) ==========
            
            // Notas fiscais pendentes na fila de contingência
            dashboardData.NotasFiscaisPendentes = await _db.FiscalQueues
                .Where(q => q.EstablishmentId == establishmentId && q.Status == "PENDENTE")
                .CountAsync();

            // NF-e emitidas hoje
            dashboardData.NFesHoje = await _db.FiscalInvoices
                .Where(f => f.EstablishmentId == establishmentId 
                    && f.IssueDate.Date == today
                    && f.InvoiceType == "NFE"
                    && f.Status == "AUTORIZADO")
                .CountAsync();

            // NFC-e emitidas hoje
            dashboardData.NFCesHoje = await _db.FiscalInvoices
                .Where(f => f.EstablishmentId == establishmentId 
                    && f.IssueDate.Date == today
                    && f.InvoiceType == "NFCE"
                    && f.Status == "AUTORIZADO")
                .CountAsync();

            // Faturamento fiscal do dia
            dashboardData.FaturamentoFiscalHoje = await _db.FiscalInvoices
                .Where(f => f.EstablishmentId == establishmentId 
                    && f.IssueDate.Date == today
                    && f.Status == "AUTORIZADO")
                .SumAsync(f => (decimal?)f.TotalAmount) ?? 0;

            // Notas com erro (rejeitadas)
            dashboardData.NotasFiscaisComErro = await _db.FiscalInvoices
                .Where(f => f.EstablishmentId == establishmentId 
                    && f.Status == "REJEITADO"
                    && f.IssueDate.Date >= today.AddDays(-7))
                .CountAsync();

            // Faturamento fiscal do mês
            var primeiroDiaMes = new DateTime(today.Year, today.Month, 1);
            dashboardData.FaturamentoFiscalMes = await _db.FiscalInvoices
                .Where(f => f.EstablishmentId == establishmentId 
                    && f.IssueDate >= primeiroDiaMes
                    && f.Status == "AUTORIZADO")
                .SumAsync(f => (decimal?)f.TotalAmount) ?? 0;

            // Total de notas no mês
            dashboardData.TotalNotasMes = await _db.FiscalInvoices
                .Where(f => f.EstablishmentId == establishmentId 
                    && f.IssueDate >= primeiroDiaMes
                    && f.Status == "AUTORIZADO")
                .CountAsync();

            // Passar para ViewBag também (para compatibilidade com cards do Dashboard)
            ViewBag.PedidosPendentes = dashboardData.PedidosPendentes;
            ViewBag.PedidosProntos = dashboardData.PedidosProntos;
            ViewBag.NotasPendentes = dashboardData.NotasFiscaisPendentes;
            ViewBag.NFesHoje = dashboardData.NFesHoje;
            ViewBag.NFCesHoje = dashboardData.NFCesHoje;
            ViewBag.FaturamentoFiscalHoje = dashboardData.FaturamentoFiscalHoje;
        }

        return View(dashboardData);
    }

    // ==================== MÉTODOS AUXILIARES ====================
    private bool CanViewReports(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER", "PHARMACIST_RT", "SUPERVISOR" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }

    private bool CanManageEmployees(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }

    private bool CanManageInventory(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER", "PHARMACIST_RT", "SUPERVISOR", "STOCK_CONTROLLER" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }

    private bool CanManageFormulas(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER", "PHARMACIST_RT", "PHARMACIST" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }

    private bool CanManagePurchases(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER", "PHARMACIST_RT", "SUPERVISOR", "PURCHASER" };
        return allowedCodes.Contains(employee.JobPosition?.Code ?? "");
    }
}
