using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;
using Service;
using Models;

namespace Controllers;

[Route("Cliente")]
[AllowAnonymous]  // Permite acesso sem autenticação do ASP.NET Core
public class ClienteController : Controller
{
    private readonly AppDbContext _context;
    private readonly CustomerAuthService _authService;
    private readonly ILogger<ClienteController> _logger;

    public ClienteController(
        AppDbContext context,
        CustomerAuthService authService,
        ILogger<ClienteController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    // ==================== AUTH VIEWS ====================

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (Request.Cookies.ContainsKey("CustomerSessionId"))
        {
            return RedirectToAction("Estabelecimentos");
        }
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpGet("Cadastro")]
    public IActionResult Cadastro()
    {
        if (Request.Cookies.ContainsKey("CustomerSessionId"))
        {
            return RedirectToAction("Estabelecimentos");
        }
        return View();
    }

    [HttpGet("Verificar")]
    public IActionResult Verificar(string? phone = null)
    {
        ViewBag.Phone = phone;
        return View();
    }

    [HttpGet("EsqueciSenha")]
    public IActionResult EsqueciSenha()
    {
        return View();
    }

    [HttpGet("RedefinirSenha")]
    public IActionResult RedefinirSenha(string? phone = null)
    {
        ViewBag.Phone = phone;
        return View();
    }

    [HttpGet("DefinirSenha")]
    public IActionResult DefinirSenha()
    {
        return View();
    }

    // ==================== ÁREA LOGADA ====================

    [HttpGet("Estabelecimentos")]
    public async Task<IActionResult> Estabelecimentos(string? returnUrl = null)
    {
        var establishments = await _context.Establishments
            .Where(e => e.IsActive == true)
            .OrderBy(e => e.NomeFantasia)
            .Select(e => new
            {
                e.Id,
                e.NomeFantasia,
                e.RazaoSocial,
                e.Phone,
                e.WhatsApp,
                e.City,
                e.State,
                e.Neighborhood
            })
            .ToListAsync();

        ViewBag.Establishments = establishments;
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpGet("LerQRCode")]
    public IActionResult LerQRCode(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpGet("Inicio")]
    public async Task<IActionResult> Inicio()
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null)
        {
            return RedirectToAction("Login");
        }

        // Buscar pedidos recentes de TODAS as farmácias
        var recentOrders = await _context.Set<OnlineOrder>()
            .Include(o => o.Establishment)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        // Buscar farmácias onde o cliente já comprou
        var customerEstablishments = await _context.Set<OnlineOrder>()
            .Where(o => o.CustomerId == customer.Id)
            .Select(o => o.EstablishmentId)
            .Distinct()
            .CountAsync();

        // Contar pedidos pendentes
        var pendingOrdersCount = await _context.Set<OnlineOrder>()
            .Where(o => o.CustomerId == customer.Id && 
                       (o.Status == "PENDING" || o.Status == "CONFIRMED" || o.Status == "PREPARING"))
            .CountAsync();

        // Contar pedidos prontos para retirada
        var readyOrdersCount = await _context.Set<OnlineOrder>()
            .Where(o => o.CustomerId == customer.Id && o.Status == "READY")
            .CountAsync();

        ViewBag.Customer = customer;
        ViewBag.Establishment = session?.CurrentEstablishment;
        ViewBag.RecentOrders = recentOrders;
        ViewBag.CustomerEstablishmentsCount = customerEstablishments;
        ViewBag.PendingOrdersCount = pendingOrdersCount;
        ViewBag.ReadyOrdersCount = readyOrdersCount;
        
        return View();
    }

    [HttpGet("MeusPedidos")]
    public async Task<IActionResult> MeusPedidos(string? status = null, Guid? farmacia = null)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
        {
            return RedirectToAction("Login");
        }
        
        // Buscar pedidos de TODAS as farmácias
        var ordersQuery = _context.Set<OnlineOrder>()
            .Include(o => o.Establishment)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customer.Id);
        
        // Filtro por farmácia (opcional)
        if (farmacia.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.EstablishmentId == farmacia.Value);
        }
        
        // Filtro por status
        if (!string.IsNullOrEmpty(status))
        {
            ordersQuery = status switch
            {
                "pendentes" => ordersQuery.Where(o => o.Status == "PENDING" || o.Status == "CONFIRMED"),
                "producao" => ordersQuery.Where(o => o.Status == "PREPARING"),
                "prontos" => ordersQuery.Where(o => o.Status == "READY" || o.Status == "DELIVERED"),
                _ => ordersQuery
            };
        }
        
        var orders = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        // Buscar farmácias para filtro
        var establishments = await _context.Set<OnlineOrder>()
            .Include(o => o.Establishment)
            .Where(o => o.CustomerId == customer.Id)
            .Select(o => new { o.EstablishmentId, o.Establishment!.NomeFantasia })
            .Distinct()
            .ToListAsync();
        
        ViewBag.Orders = orders;
        ViewBag.SelectedStatus = status;
        ViewBag.SelectedFarmacia = farmacia;
        ViewBag.Establishments = establishments;
        ViewBag.Customer = customer;
        
        return View();
    }

    [HttpGet("Catalogo")]
    public async Task<IActionResult> Catalogo(string? categoria = null)
    {
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;
        if (session?.CurrentEstablishmentId == null)
        {
            return RedirectToAction("Estabelecimentos");
        }
        
        var establishmentId = session.CurrentEstablishmentId.Value;
        
        // Carregar categorias
        var categories = await _context.Set<CatalogCategory>()
            .Where(c => c.EstablishmentId == establishmentId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
        
        // Carregar produtos
        var productsQuery = _context.Set<CatalogProduct>()
            .Include(p => p.Category)
            .Where(p => p.EstablishmentId == establishmentId && p.IsActive);
        
        if (!string.IsNullOrEmpty(categoria))
        {
            productsQuery = productsQuery.Where(p => p.Category != null && p.Category.Slug == categoria);
        }
        
        var products = await productsQuery
            .OrderByDescending(p => p.IsHighlight)
            .ThenByDescending(p => p.IsBestSeller)
            .ThenBy(p => p.Name)
            .ToListAsync();
        
        // Carregar carrinho
        var customer = HttpContext.Items["Customer"] as Customer;
        var cartItemCount = 0;
        if (customer != null)
        {
            cartItemCount = await _context.Set<CustomerCartItem>()
                .Where(i => i.Cart!.CustomerId == customer.Id && i.Cart.EstablishmentId == establishmentId)
                .SumAsync(i => i.Quantity);
        }
        
        ViewBag.Establishment = session.CurrentEstablishment;
        ViewBag.Categories = categories;
        ViewBag.Products = products;
        ViewBag.SelectedCategory = categoria;
        ViewBag.CartItemCount = cartItemCount;
        
        return View();
    }
    
    [HttpGet("Produto/{id}")]
    public async Task<IActionResult> Produto(Guid id)
    {
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;
        if (session?.CurrentEstablishmentId == null)
        {
            return RedirectToAction("Estabelecimentos");
        }
        
        var product = await _context.Set<CatalogProduct>()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.EstablishmentId == session.CurrentEstablishmentId);
        
        if (product == null)
        {
            return RedirectToAction("Catalogo");
        }
        
        ViewBag.Establishment = session.CurrentEstablishment;
        return View(product);
    }
    
    [HttpGet("Carrinho")]
    public async Task<IActionResult> Carrinho()
    {
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;
        var customer = HttpContext.Items["Customer"] as Customer;
        
        if (session?.CurrentEstablishmentId == null || customer == null)
        {
            return RedirectToAction("Estabelecimentos");
        }
        
        var cart = await _context.Set<CustomerCart>()
            .Include(c => c.Items!)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && 
                                      c.EstablishmentId == session.CurrentEstablishmentId);
        
        ViewBag.Establishment = session.CurrentEstablishment;
        ViewBag.Cart = cart;
        
        return View();
    }

    [HttpGet("EnviarReceita")]
    public IActionResult EnviarReceita()
    {
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;
        if (session?.CurrentEstablishmentId == null)
        {
            return RedirectToAction("Estabelecimentos");
        }
        ViewBag.Establishment = session.CurrentEstablishment;
        return View();
    }

    [HttpGet("MeusDados")]
    public IActionResult MeusDados()
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        return View(customer);
    }

    [HttpGet("Logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionToken = Request.Cookies["CustomerSessionId"];
        if (!string.IsNullOrEmpty(sessionToken))
        {
            await _authService.LogoutAsync(sessionToken);
            Response.Cookies.Delete("CustomerSessionId");
        }
        return RedirectToAction("Login");
    }
}

// Controller para QR Code redirect
[Route("c")]
[AllowAnonymous]  // Permite acesso sem autenticação
public class QRCodeRedirectController : Controller
{
    private readonly AppDbContext _context;

    public QRCodeRedirectController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> Redirect(string code)
    {
        var qrCode = await _context.EstablishmentQRCodes
            .Include(q => q.Establishment)
            .FirstOrDefaultAsync(q => q.Code == code && q.IsActive);

        if (qrCode == null)
        {
            return RedirectToAction("Login", "Cliente");
        }

        qrCode.ScanCount++;
        qrCode.LastScannedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (Request.Cookies.ContainsKey("CustomerSessionId"))
        {
            return RedirectToAction("Inicio", "Cliente", new { establishment = qrCode.EstablishmentId });
        }

        return RedirectToAction("Login", "Cliente", new { establishment = qrCode.EstablishmentId });
    }
}
