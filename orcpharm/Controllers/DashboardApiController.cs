using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;

namespace Controllers.Api;

/// <summary>
/// API de Dashboard Executivo com KPIs consolidados
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<DashboardApiController> _logger;

    public DashboardApiController(AppDbContext db, ILogger<DashboardApiController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee() => HttpContext.Items["Employee"] as Employee;
    
    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var estId) && estId is Guid id)
            return id;
        return Guid.Parse("e0000000-0000-0000-0000-000000000001");
    }

    /// <summary>
    /// KPIs principais do dashboard
    /// GET /api/dashboardapi/kpis
    /// </summary>
    [HttpGet("kpis")]
    public async Task<IActionResult> GetKPIs()
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var hoje = DateTime.UtcNow.Date;
            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

            // Vendas do dia
            var vendasHoje = await _db.Sales
                .Where(s => s.EstablishmentId == establishmentId && s.CreatedAt.Date == hoje)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            // Vendas do mês
            var vendasMes = await _db.Sales
                .Where(s => s.EstablishmentId == establishmentId && s.CreatedAt >= inicioMes)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            // Ordens em produção
            var ordensProducao = await _db.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId && 
                                o.Status != "CONCLUIDA" && o.Status != "CANCELADA" && o.Status != "ENTREGUE");

            // Ordens concluídas hoje
            var ordensConcluidas = await _db.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId && 
                                o.Status == "CONCLUIDA" && o.UpdatedAt.Date == hoje);

            // Estoque crítico (abaixo do mínimo)
            var estoqueCritico = await _db.RawMaterials
                .CountAsync(r => r.EstablishmentId == establishmentId && 
                                r.IsActive && r.CurrentStock < r.MinimumStock);

            // Lotes vencendo em 30 dias
            var limite30Dias = hoje.AddDays(30);
            var lotesVencendo = await _db.Batches
                .Where(b => b.RawMaterial != null && b.RawMaterial.EstablishmentId == establishmentId)
                .CountAsync(b => b.ExpiryDate <= limite30Dias && b.ExpiryDate > hoje &&
                                b.CurrentQuantity > 0);

            // Clientes ativos (compraram no mês)
            var clientesAtivos = await _db.Sales
                .Where(s => s.EstablishmentId == establishmentId && s.CreatedAt >= inicioMes && s.CustomerId != null)
                .Select(s => s.CustomerId)
                .Distinct()
                .CountAsync();

            // Ticket médio do mês
            var qtdVendasMes = await _db.Sales
                .CountAsync(s => s.EstablishmentId == establishmentId && s.CreatedAt >= inicioMes);
            var ticketMedio = qtdVendasMes > 0 ? vendasMes / qtdVendasMes : 0;

            // Orçamentos pendentes
            var orcamentosPendentes = await _db.PrescriptionQuotes
                .CountAsync(q => q.EstablishmentId == establishmentId && 
                                q.Status == "AGUARDANDO_APROVACAO");

            // Taxa de conversão (orçamentos aprovados / total)
            var totalOrcamentosMes = await _db.PrescriptionQuotes
                .CountAsync(q => q.EstablishmentId == establishmentId && q.CreatedAt >= inicioMes);
            var orcamentosAprovados = await _db.PrescriptionQuotes
                .CountAsync(q => q.EstablishmentId == establishmentId && 
                                q.CreatedAt >= inicioMes && q.Status == "APROVADO");
            var taxaConversao = totalOrcamentosMes > 0 
                ? Math.Round((decimal)orcamentosAprovados / totalOrcamentosMes * 100, 1) 
                : 0;

            return Ok(new
            {
                vendas = new {
                    hoje = vendasHoje,
                    mes = vendasMes,
                    ticketMedio,
                    clientesAtivos
                },
                producao = new {
                    emAndamento = ordensProducao,
                    concluidasHoje = ordensConcluidas
                },
                estoque = new {
                    critico = estoqueCritico,
                    lotesVencendo
                },
                comercial = new {
                    orcamentosPendentes,
                    taxaConversao
                },
                atualizadoEm = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter KPIs");
            return StatusCode(500, new { error = "Erro ao carregar KPIs" });
        }
    }

    /// <summary>
    /// Gráfico de vendas dos últimos 7 dias
    /// GET /api/dashboardapi/vendas-semana
    /// </summary>
    [HttpGet("vendas-semana")]
    public async Task<IActionResult> GetVendasSemana()
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var hoje = DateTime.UtcNow.Date;
            var seteDiasAtras = hoje.AddDays(-6);

            var vendas = await _db.Sales
                .Where(s => s.EstablishmentId == establishmentId && 
                           s.CreatedAt.Date >= seteDiasAtras)
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new { data = g.Key, total = g.Sum(s => s.TotalAmount) })
                .ToListAsync();

            // Preencher dias sem vendas
            var resultado = new List<object>();
            for (int i = 6; i >= 0; i--)
            {
                var dia = hoje.AddDays(-i);
                var venda = vendas.FirstOrDefault(v => v.data == dia);
                resultado.Add(new
                {
                    data = dia.ToString("dd/MM"),
                    diaSemana = dia.ToString("ddd"),
                    total = venda?.total ?? 0
                });
            }

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter vendas da semana");
            return StatusCode(500, new { error = "Erro ao carregar dados" });
        }
    }

    /// <summary>
    /// Top 5 produtos mais vendidos do mês
    /// GET /api/dashboardapi/top-produtos
    /// </summary>
    [HttpGet("top-produtos")]
    public async Task<IActionResult> GetTopProdutos()
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var topProdutos = await _db.ManipulationOrders
                .Include(o => o.Formula)
                .Where(o => o.EstablishmentId == establishmentId && o.CreatedAt >= inicioMes)
                .GroupBy(o => o.Formula != null ? o.Formula.Name : "Não especificado")
                .Select(g => new {
                    produto = g.Key,
                    quantidade = g.Count(),
                    total = g.Sum(o => o.QuantityToProduce)
                })
                .OrderByDescending(x => x.quantidade)
                .Take(5)
                .ToListAsync();

            return Ok(topProdutos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter top produtos");
            return StatusCode(500, new { error = "Erro ao carregar dados" });
        }
    }

    /// <summary>
    /// Alertas ativos do sistema
    /// GET /api/dashboardapi/alertas
    /// </summary>
    [HttpGet("alertas")]
    public async Task<IActionResult> GetAlertas()
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var hoje = DateTime.UtcNow.Date;
            var alertas = new List<object>();

            // Estoque crítico
            var estoqueCritico = await _db.RawMaterials
                .Where(r => r.EstablishmentId == establishmentId && 
                           r.IsActive && r.CurrentStock < r.MinimumStock)
                .Select(r => new { r.Name, r.CurrentStock, r.MinimumStock })
                .Take(5)
                .ToListAsync();

            foreach (var item in estoqueCritico)
            {
                alertas.Add(new
                {
                    tipo = "ESTOQUE_CRITICO",
                    icone = "exclamation-triangle",
                    cor = "danger",
                    titulo = "Estoque Crítico",
                    mensagem = $"{item.Name}: {item.CurrentStock:N2} (mín: {item.MinimumStock:N2})",
                    link = "/Estoque/MateriaPrima"
                });
            }

            // Lotes vencendo
            var limite15Dias = hoje.AddDays(15);
            var lotesVencendo = await _db.Batches
                .Include(b => b.RawMaterial)
                .Where(b => b.RawMaterial != null &&
                           b.RawMaterial.EstablishmentId == establishmentId &&
                           b.ExpiryDate <= limite15Dias && b.ExpiryDate > hoje &&
                           b.CurrentQuantity > 0)
                .Select(b => new { 
                    Material = b.RawMaterial!.Name, 
                    b.BatchNumber, 
                    b.ExpiryDate 
                })
                .Take(5)
                .ToListAsync();

            foreach (var lote in lotesVencendo)
            {
                var diasRestantes = (lote.ExpiryDate - hoje).Days;
                alertas.Add(new
                {
                    tipo = "LOTE_VENCENDO",
                    icone = "calendar-x",
                    cor = diasRestantes <= 7 ? "danger" : "warning",
                    titulo = "Lote Vencendo",
                    mensagem = $"{lote.Material} - Lote {lote.BatchNumber}: vence em {diasRestantes} dias",
                    link = "/Estoque/Lotes"
                });
            }

            // Ordens atrasadas
            var ordensAtrasadas = await _db.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId &&
                               o.Status != "CONCLUIDA" && o.Status != "CANCELADA" && o.Status != "ENTREGUE" &&
                               o.ExpectedDate < hoje);

            if (ordensAtrasadas > 0)
            {
                alertas.Add(new
                {
                    tipo = "ORDENS_ATRASADAS",
                    icone = "clock-history",
                    cor = "danger",
                    titulo = "Ordens Atrasadas",
                    mensagem = $"{ordensAtrasadas} ordem(ns) com prazo vencido",
                    link = "/Manipulacao/Ordens"
                });
            }

            return Ok(new
            {
                total = alertas.Count,
                alertas = alertas.Take(10)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter alertas");
            return StatusCode(500, new { error = "Erro ao carregar alertas" });
        }
    }

    /// <summary>
    /// Status das ordens de produção
    /// GET /api/dashboardapi/producao-status
    /// </summary>
    [HttpGet("producao-status")]
    public async Task<IActionResult> GetProducaoStatus()
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var statusCounts = await _db.ManipulationOrders
                .Where(o => o.EstablishmentId == establishmentId &&
                           o.Status != "CANCELADA")
                .GroupBy(o => o.Status)
                .Select(g => new { status = g.Key, quantidade = g.Count() })
                .ToListAsync();

            return Ok(statusCounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter status de produção");
            return StatusCode(500, new { error = "Erro ao carregar dados" });
        }
    }

    /// <summary>
    /// Resumo financeiro do mês
    /// GET /api/dashboardapi/financeiro
    /// </summary>
    [HttpGet("financeiro")]
    public async Task<IActionResult> GetFinanceiro()
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // Receitas (vendas)
            var receitas = await _db.Sales
                .Where(s => s.EstablishmentId == establishmentId && s.CreatedAt >= inicioMes)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            // Pagamentos recebidos
            var recebido = await _db.SalePayments
                .Where(p => p.Sale != null && p.Sale.EstablishmentId == establishmentId && p.CreatedAt >= inicioMes)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // A receber
            var aReceber = receitas - recebido;

            // Por forma de pagamento
            var porFormaPagamento = await _db.SalePayments
                .Where(p => p.Sale != null && p.Sale.EstablishmentId == establishmentId && p.CreatedAt >= inicioMes)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { forma = g.Key, total = g.Sum(p => p.Amount) })
                .ToListAsync();

            return Ok(new
            {
                receitas,
                recebido,
                aReceber,
                porFormaPagamento
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter dados financeiros");
            return StatusCode(500, new { error = "Erro ao carregar dados" });
        }
    }
}
