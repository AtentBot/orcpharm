using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;

namespace Controllers.Api;

[ApiController]
[Route("api/cliente/cart")]
[AllowAnonymous]
public class ClienteCartApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClienteCartApiController> _logger;

    public ClienteCartApiController(AppDbContext context, ILogger<ClienteCartApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null || session?.CurrentEstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var establishmentId = session.CurrentEstablishmentId.Value;

        // Buscar produto
        var product = await _context.Set<CatalogProduct>()
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId &&
                                      p.EstablishmentId == establishmentId &&
                                      p.IsActive);

        if (product == null)
            return BadRequest(new { success = false, message = "Produto não encontrado" });

        if (product.StockQuantity < dto.Quantity)
            return BadRequest(new { success = false, message = "Quantidade indisponível em estoque" });

        // Buscar ou criar carrinho
        var cart = await _context.Set<CustomerCart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id &&
                                      c.EstablishmentId == establishmentId);

        if (cart == null)
        {
            cart = new CustomerCart
            {
                CustomerId = customer.Id,
                EstablishmentId = establishmentId
            };
            _context.Set<CustomerCart>().Add(cart);
            await _context.SaveChangesAsync();
        }

        // Verificar se produto já está no carrinho
        var existingItem = cart.Items?.FirstOrDefault(i => i.ProductId == dto.ProductId);

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
            existingItem.UnitPrice = product.CurrentPrice;
        }
        else
        {
            var newItem = new CustomerCartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = dto.Quantity,
                UnitPrice = product.CurrentPrice
            };
            _context.Set<CustomerCartItem>().Add(newItem);
        }

        await _context.SaveChangesAsync();

        var totalItems = await _context.Set<CustomerCartItem>()
            .Where(i => i.CartId == cart.Id)
            .SumAsync(i => i.Quantity);

        return Ok(new { success = true, message = "Produto adicionado", cartItems = totalItems });
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartDto dto)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var item = await _context.Set<CustomerCartItem>()
            .Include(i => i.Cart)
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId && i.Cart!.CustomerId == customer.Id);

        if (item == null)
            return BadRequest(new { success = false, message = "Item não encontrado" });

        item.Quantity += dto.Delta;

        if (item.Quantity <= 0)
        {
            _context.Set<CustomerCartItem>().Remove(item);
        }
        else if (item.Product != null && item.Quantity > item.Product.StockQuantity)
        {
            return BadRequest(new { success = false, message = "Quantidade indisponível" });
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("remove")]
    public async Task<IActionResult> RemoveItem([FromBody] RemoveCartDto dto)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var item = await _context.Set<CustomerCartItem>()
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId && i.Cart!.CustomerId == customer.Id);

        if (item == null)
            return BadRequest(new { success = false, message = "Item não encontrado" });

        _context.Set<CustomerCartItem>().Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null || session?.CurrentEstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var cart = await _context.Set<CustomerCart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id &&
                                      c.EstablishmentId == session.CurrentEstablishmentId);

        if (cart != null)
        {
            _context.Set<CustomerCartItem>().RemoveRange(cart.Items!);
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true });
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCartCount()
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null || session?.CurrentEstablishmentId == null)
            return Ok(new { count = 0 });

        var count = await _context.Set<CustomerCartItem>()
            .Where(i => i.Cart!.CustomerId == customer.Id &&
                        i.Cart.EstablishmentId == session.CurrentEstablishmentId)
            .SumAsync(i => i.Quantity);

        return Ok(new { count });
    }
}

[ApiController]
[Route("api/cliente/orders")]
[AllowAnonymous]
public class ClienteOrdersApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClienteOrdersApiController> _logger;

    public ClienteOrdersApiController(AppDbContext context, ILogger<ClienteOrdersApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null || session?.CurrentEstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var establishmentId = session.CurrentEstablishmentId.Value;

        // Buscar carrinho
        var cart = await _context.Set<CustomerCart>()
            .Include(c => c.Items!)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id &&
                                      c.EstablishmentId == establishmentId);

        if (cart == null || !cart.Items!.Any())
            return BadRequest(new { success = false, message = "Carrinho vazio" });

        // Gerar número do pedido único globalmente
        // Formato: P{YYYYMMDD}{HHMMSS}{4 random} = P + 8 + 6 + 4 = 19 chars
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        var orderNumber = $"P{timestamp}{random}";

        // Calcular totais
        var subtotal = cart.Items!.Sum(i => i.UnitPrice * i.Quantity);

        // Criar pedido
        var order = new OnlineOrder
        {
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            EstablishmentId = establishmentId,
            Status = "PENDING",
            Subtotal = subtotal,
            Total = subtotal,
            CustomerNotes = dto.Notes,
            DeliveryType = "PICKUP"
        };

        _context.Set<OnlineOrder>().Add(order);
        await _context.SaveChangesAsync();

        // Criar itens do pedido
        foreach (var item in cart.Items!)
        {
            var orderItem = new OnlineOrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? "Produto",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.UnitPrice * item.Quantity
            };
            _context.Set<OnlineOrderItem>().Add(orderItem);
        }

        // Limpar carrinho
        _context.Set<CustomerCartItem>().RemoveRange(cart.Items!);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pedido {OrderNumber} criado pelo cliente {CustomerId}", orderNumber, customer.Id);

        return Ok(new
        {
            success = true,
            orderId = order.Id,
            orderNumber = order.OrderNumber,
            message = "Pedido criado com sucesso!"
        });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var orders = await _context.Set<OnlineOrder>()
            .Include(o => o.Establishment)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                StatusDisplay = o.Status == "PENDING" ? "Aguardando" :
                               o.Status == "CONFIRMED" ? "Confirmado" :
                               o.Status == "PREPARING" ? "Preparando" :
                               o.Status == "READY" ? "Pronto" :
                               o.Status == "DELIVERED" ? "Entregue" :
                               o.Status == "CANCELLED" ? "Cancelado" : o.Status,
                o.Total,
                o.CreatedAt,
                EstablishmentName = o.Establishment!.NomeFantasia,
                ItemCount = o.Items!.Count
            })
            .ToListAsync();

        return Ok(new { success = true, orders });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var order = await _context.Set<OnlineOrder>()
            .Where(o => o.Id == id && o.CustomerId == customer.Id)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Subtotal,
                o.Discount,
                o.DeliveryFee,
                o.Total,
                o.PaymentMethod,
                o.PaymentStatus,
                o.DeliveryType,
                o.CustomerNotes,
                o.CreatedAt,
                EstablishmentName = o.Establishment!.NomeFantasia,
                Items = o.Items!.Select(i => new
                {
                    i.Id,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice,
                    i.TotalPrice,
                    i.Notes
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(new { success = false, message = "Pedido não encontrado" });

        return Ok(new { success = true, order });
    }
}

// DTOs
public class AddToCartDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartDto
{
    public Guid ItemId { get; set; }
    public int Delta { get; set; }
}

public class RemoveCartDto
{
    public Guid ItemId { get; set; }
}

public class CreateOrderDto
{
    public string? Notes { get; set; }
}