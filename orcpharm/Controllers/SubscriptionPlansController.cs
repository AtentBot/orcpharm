using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using Models;
using System.Text.Json;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionPlansController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubscriptionPlansController> _logger;

    public SubscriptionPlansController(AppDbContext context, ILogger<SubscriptionPlansController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        var query = _context.Set<SubscriptionPlan>().AsQueryable();

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        var plans = await query
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        var result = plans.Select(p => new SubscriptionPlanDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            PriceMonthly = p.PriceMonthly,
            PriceYearly = p.PriceYearly,
            MaxEmployees = p.MaxEmployees,
            MaxMonthlyOrders = p.MaxMonthlyOrders,
            Features = string.IsNullOrEmpty(p.Features)
                ? new Dictionary<string, bool>()
                : JsonSerializer.Deserialize<Dictionary<string, bool>>(p.Features) ?? new(),
            IsActive = p.IsActive
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _context.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
            return NotFound(new { message = "Plano não encontrado" });

        var result = new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            PriceMonthly = plan.PriceMonthly,
            PriceYearly = plan.PriceYearly,
            MaxEmployees = plan.MaxEmployees,
            MaxMonthlyOrders = plan.MaxMonthlyOrders,
            Features = string.IsNullOrEmpty(plan.Features)
                ? new Dictionary<string, bool>()
                : JsonSerializer.Deserialize<Dictionary<string, bool>>(plan.Features) ?? new(),
            IsActive = plan.IsActive
        };

        return Ok(result);
    }
}
