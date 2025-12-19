using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Employees;
using Service.Notifications;

namespace Controllers.Api;

[ApiController]
[Route("api/online-orders")]
public class OnlineOrdersApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly WhatsAppService _whatsAppService;
    private readonly ILogger<OnlineOrdersApiController> _logger;

    public OnlineOrdersApiController(
        AppDbContext context, 
        WhatsAppService whatsAppService,
        ILogger<OnlineOrdersApiController> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    // GET: api/online-orders
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var query = _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Where(o => o.EstablishmentId == session.Employee!.EstablishmentId);

        if (!string.IsNullOrEmpty(status) && status != "todos")
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(o => 
                o.OrderNumber.Contains(search) || 
                o.Customer!.FullName.Contains(search));

        var total = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Total,
                o.CreatedAt,
                o.PaymentStatus,
                o.DeliveryType,
                CustomerName = o.Customer!.FullName,
                CustomerPhone = o.Customer.Phone,
                ItemCount = o.Items!.Count()
            })
            .ToListAsync();

        return Ok(new { success = true, orders, total, page, pageSize });
    }

    // GET: api/online-orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var order = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Items!)
                .ThenInclude(i => i.Product)
            .Where(o => o.Id == id && o.EstablishmentId == session.Employee!.EstablishmentId)
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
                o.DeliveryAddress,
                o.CustomerNotes,
                o.EstimatedReadyAt,
                o.ReadyAt,
                o.DeliveredAt,
                o.CancelledAt,
                o.CreatedAt,
                Customer = new
                {
                    o.Customer!.Id,
                    o.Customer.FullName,
                    o.Customer.Phone,
                    o.Customer.Email
                },
                Items = o.Items!.Select(i => new
                {
                    i.Id,
                    i.ProductId,
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

    // PUT: api/online-orders/{id}/status
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var order = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Establishment)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == session.Employee!.EstablishmentId);

        if (order == null)
            return NotFound(new { success = false, message = "Pedido não encontrado" });

        var oldStatus = order.Status;
        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        // Atualizar timestamps específicos
        switch (dto.Status)
        {
            case "CONFIRMED":
                order.EstimatedReadyAt = dto.EstimatedReadyAt ?? DateTime.UtcNow.AddHours(2);
                break;
            case "READY":
                order.ReadyAt = DateTime.UtcNow;
                break;
            case "DELIVERED":
                order.DeliveredAt = DateTime.UtcNow;
                break;
            case "CANCELLED":
                order.CancelledAt = DateTime.UtcNow;
                break;
        }

        await _context.SaveChangesAsync();

        // Enviar notificação WhatsApp
        if (dto.NotifyCustomer && order.Customer?.Phone != null)
        {
            var message = GetStatusMessage(order, dto.Status, dto.Notes);
            try
            {
                await _whatsAppService.SendMessageAsync(order.Customer.Phone, message);
                _logger.LogInformation("Notificação enviada para {Phone} - Pedido {OrderNumber}", 
                    order.Customer.Phone, order.OrderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificação WhatsApp");
            }
        }

        _logger.LogInformation("Pedido {OrderNumber} atualizado: {OldStatus} -> {NewStatus} por {Employee}",
            order.OrderNumber, oldStatus, dto.Status, session.Employee?.FullName);

        return Ok(new { 
            success = true, 
            message = $"Status atualizado para {GetStatusDisplayName(dto.Status)}" 
        });
    }

    // PUT: api/online-orders/{id}/confirm
    [HttpPut("{id:guid}/confirm")]
    public async Task<IActionResult> ConfirmOrder(Guid id, [FromBody] ConfirmOrderDto dto)
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var order = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Establishment)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == session.Employee!.EstablishmentId);

        if (order == null)
            return NotFound(new { success = false, message = "Pedido não encontrado" });

        if (order.Status != "PENDING")
            return BadRequest(new { success = false, message = "Pedido não está pendente" });

        order.Status = "CONFIRMED";
        order.EstimatedReadyAt = dto.EstimatedReadyAt ?? DateTime.UtcNow.AddHours(2);
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Notificar cliente
        if (order.Customer?.Phone != null)
        {
            var eta = order.EstimatedReadyAt?.ToString("HH:mm") ?? "em breve";
            var message = $"✅ *Pedido Confirmado!*\n\n" +
                         $"Olá {order.Customer.FullName.Split(' ')[0]}!\n\n" +
                         $"Seu pedido *{order.OrderNumber}* foi confirmado.\n" +
                         $"📍 *{order.Establishment?.NomeFantasia}*\n\n" +
                         $"⏰ Previsão: *{eta}*\n\n" +
                         $"Avisaremos quando estiver pronto! 😊";

            try
            {
                await _whatsAppService.SendMessageAsync(order.Customer.Phone, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificação");
            }
        }

        return Ok(new { success = true, message = "Pedido confirmado!" });
    }

    // PUT: api/online-orders/{id}/ready
    [HttpPut("{id:guid}/ready")]
    public async Task<IActionResult> MarkAsReady(Guid id)
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var order = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Establishment)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == session.Employee!.EstablishmentId);

        if (order == null)
            return NotFound(new { success = false, message = "Pedido não encontrado" });

        if (order.Status != "CONFIRMED" && order.Status != "PREPARING")
            return BadRequest(new { success = false, message = "Pedido não pode ser marcado como pronto" });

        order.Status = "READY";
        order.ReadyAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Notificar cliente
        if (order.Customer?.Phone != null)
        {
            var message = $"🎉 *Pedido Pronto!*\n\n" +
                         $"Olá {order.Customer.FullName.Split(' ')[0]}!\n\n" +
                         $"Seu pedido *{order.OrderNumber}* está pronto para retirada!\n\n" +
                         $"📍 *{order.Establishment?.NomeFantasia}*\n" +
                         $"📞 {order.Establishment?.Phone}\n\n" +
                         $"Aguardamos você! 😊";

            try
            {
                await _whatsAppService.SendMessageAsync(order.Customer.Phone, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificação");
            }
        }

        return Ok(new { success = true, message = "Pedido marcado como pronto!" });
    }

    // PUT: api/online-orders/{id}/deliver
    [HttpPut("{id:guid}/deliver")]
    public async Task<IActionResult> MarkAsDelivered(Guid id)
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var order = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == session.Employee!.EstablishmentId);

        if (order == null)
            return NotFound(new { success = false, message = "Pedido não encontrado" });

        if (order.Status != "READY")
            return BadRequest(new { success = false, message = "Pedido não está pronto para entrega" });

        order.Status = "DELIVERED";
        order.DeliveredAt = DateTime.UtcNow;
        order.PaymentStatus = "PAID"; // Assumindo pagamento na retirada
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pedido {OrderNumber} entregue", order.OrderNumber);

        return Ok(new { success = true, message = "Pedido entregue!" });
    }

    // PUT: api/online-orders/{id}/cancel
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOnlineOrderDto dto)
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var order = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Establishment)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == session.Employee!.EstablishmentId);

        if (order == null)
            return NotFound(new { success = false, message = "Pedido não encontrado" });

        if (order.Status == "DELIVERED" || order.Status == "CANCELLED")
            return BadRequest(new { success = false, message = "Pedido não pode ser cancelado" });

        order.Status = "CANCELLED";
        order.CancelledAt = DateTime.UtcNow;
        order.CustomerNotes = (order.CustomerNotes ?? "") + $"\n[CANCELADO] {dto.Reason}";
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Notificar cliente
        if (dto.NotifyCustomer && order.Customer?.Phone != null)
        {
            var message = $"❌ *Pedido Cancelado*\n\n" +
                         $"Olá {order.Customer.FullName.Split(' ')[0]}!\n\n" +
                         $"Infelizmente seu pedido *{order.OrderNumber}* foi cancelado.\n\n" +
                         $"📝 Motivo: {dto.Reason}\n\n" +
                         $"Entre em contato conosco para mais informações:\n" +
                         $"📞 {order.Establishment?.Phone}";

            try
            {
                await _whatsAppService.SendMessageAsync(order.Customer.Phone, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificação");
            }
        }

        _logger.LogInformation("Pedido {OrderNumber} cancelado: {Reason}", order.OrderNumber, dto.Reason);

        return Ok(new { success = true, message = "Pedido cancelado" });
    }

    // GET: api/online-orders/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var session = HttpContext.Items["Session"] as EmployeeSession;
        if (session?.Employee?.EstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var establishmentId = session.Employee!.EstablishmentId;
        var today = DateTime.UtcNow.Date;

        var stats = new
        {
            pending = await _context.Set<OnlineOrder>()
                .CountAsync(o => o.EstablishmentId == establishmentId && o.Status == "PENDING"),
            confirmed = await _context.Set<OnlineOrder>()
                .CountAsync(o => o.EstablishmentId == establishmentId && o.Status == "CONFIRMED"),
            preparing = await _context.Set<OnlineOrder>()
                .CountAsync(o => o.EstablishmentId == establishmentId && o.Status == "PREPARING"),
            ready = await _context.Set<OnlineOrder>()
                .CountAsync(o => o.EstablishmentId == establishmentId && o.Status == "READY"),
            todayCount = await _context.Set<OnlineOrder>()
                .CountAsync(o => o.EstablishmentId == establishmentId && o.CreatedAt >= today),
            todayTotal = await _context.Set<OnlineOrder>()
                .Where(o => o.EstablishmentId == establishmentId && o.CreatedAt >= today)
                .SumAsync(o => o.Total)
        };

        return Ok(new { success = true, stats });
    }

    // Helpers
    private string GetStatusMessage(OnlineOrder order, string status, string? notes)
    {
        var customerName = order.Customer?.FullName.Split(' ')[0] ?? "Cliente";
        var pharmacy = order.Establishment?.NomeFantasia ?? "Farmácia";

        return status switch
        {
            "CONFIRMED" => $"✅ *Pedido Confirmado!*\n\nOlá {customerName}!\n\nSeu pedido *{order.OrderNumber}* foi confirmado pela *{pharmacy}*.\n\nAvisaremos quando estiver pronto!",
            "PREPARING" => $"👨‍🔬 *Em Preparação*\n\nOlá {customerName}!\n\nSeu pedido *{order.OrderNumber}* está sendo preparado!",
            "READY" => $"🎉 *Pedido Pronto!*\n\nOlá {customerName}!\n\nSeu pedido *{order.OrderNumber}* está pronto para retirada na *{pharmacy}*!",
            "DELIVERED" => $"✅ *Pedido Entregue*\n\nObrigado pela preferência, {customerName}!\n\nVolte sempre! 😊",
            "CANCELLED" => $"❌ *Pedido Cancelado*\n\nOlá {customerName}!\n\nSeu pedido *{order.OrderNumber}* foi cancelado.\n\n{(notes != null ? $"Motivo: {notes}" : "")}",
            _ => $"📋 Atualização do pedido *{order.OrderNumber}*: {status}"
        };
    }

    private string GetStatusDisplayName(string status) => status switch
    {
        "PENDING" => "Pendente",
        "CONFIRMED" => "Confirmado",
        "PREPARING" => "Em Preparação",
        "READY" => "Pronto",
        "DELIVERED" => "Entregue",
        "CANCELLED" => "Cancelado",
        _ => status
    };
}

// DTOs
public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool NotifyCustomer { get; set; } = true;
    public DateTime? EstimatedReadyAt { get; set; }
}

public class ConfirmOrderDto
{
    public DateTime? EstimatedReadyAt { get; set; }
}

public class CancelOnlineOrderDto
{
    public string Reason { get; set; } = string.Empty;
    public bool NotifyCustomer { get; set; } = true;
}
