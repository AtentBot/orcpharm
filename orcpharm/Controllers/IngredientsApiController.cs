using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using System.Globalization;
using System.Text;

namespace Controllers.Api;

[ApiController]
[Route("api/ingredients")]
[AllowAnonymous]
public class IngredientsApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<IngredientsApiController> _logger;

    public IngredientsApiController(AppDbContext context, ILogger<IngredientsApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Autocomplete de ingredientes ativos
    /// GET /api/ingredients/search?q=vitamina&limit=10
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return Ok(new { success = true, data = Array.Empty<object>() });
        }

        var normalizedQuery = NormalizeString(q.Trim());
        limit = Math.Clamp(limit, 1, 50);

        var ingredients = await _context.Set<ActiveIngredient>()
            .Where(i => i.IsActive && 
                (i.NormalizedName.Contains(normalizedQuery) ||
                 i.Name.Contains(q) ||
                 (i.Synonyms != null && i.Synonyms.ToLower().Contains(q.ToLower()))))
            .OrderByDescending(i => i.NormalizedName.StartsWith(normalizedQuery)) // Prioriza match no início
            .ThenByDescending(i => i.Popularity)
            .ThenBy(i => i.Name)
            .Take(limit)
            .Select(i => new
            {
                id = i.Id,
                name = i.Name,
                category = i.Category,
                defaultUnit = i.DefaultUnit,
                minDosage = i.MinDosage,
                maxDosage = i.MaxDosage,
                requiresPrescription = i.RequiresPrescription,
                isControlled = i.IsControlled,
                indications = i.Indications
            })
            .ToListAsync();

        return Ok(new { success = true, data = ingredients });
    }

    /// <summary>
    /// Busca ingrediente por ID
    /// GET /api/ingredients/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ingredient = await _context.Set<ActiveIngredient>()
            .Where(i => i.Id == id && i.IsActive)
            .Select(i => new
            {
                id = i.Id,
                name = i.Name,
                category = i.Category,
                subcategory = i.Subcategory,
                defaultUnit = i.DefaultUnit,
                minDosage = i.MinDosage,
                maxDosage = i.MaxDosage,
                pricePerUnit = i.PricePerUnit,
                description = i.Description,
                indications = i.Indications,
                requiresPrescription = i.RequiresPrescription,
                isControlled = i.IsControlled,
                synonyms = i.Synonyms
            })
            .FirstOrDefaultAsync();

        if (ingredient == null)
            return NotFound(new { success = false, message = "Ingrediente não encontrado" });

        return Ok(new { success = true, data = ingredient });
    }

    /// <summary>
    /// Lista categorias de ingredientes
    /// GET /api/ingredients/categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Set<ActiveIngredient>()
            .Where(i => i.IsActive && !string.IsNullOrEmpty(i.Category))
            .Select(i => i.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }

    /// <summary>
    /// Lista ingredientes por categoria
    /// GET /api/ingredients/by-category/Vitaminas
    /// </summary>
    [HttpGet("by-category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, [FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 100);

        var ingredients = await _context.Set<ActiveIngredient>()
            .Where(i => i.IsActive && i.Category == category)
            .OrderByDescending(i => i.Popularity)
            .ThenBy(i => i.Name)
            .Take(limit)
            .Select(i => new
            {
                id = i.Id,
                name = i.Name,
                defaultUnit = i.DefaultUnit,
                minDosage = i.MinDosage,
                maxDosage = i.MaxDosage,
                indications = i.Indications
            })
            .ToListAsync();

        return Ok(new { success = true, data = ingredients });
    }

    /// <summary>
    /// Ingredientes mais populares
    /// GET /api/ingredients/popular?limit=20
    /// </summary>
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular([FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 50);

        var ingredients = await _context.Set<ActiveIngredient>()
            .Where(i => i.IsActive)
            .OrderByDescending(i => i.Popularity)
            .Take(limit)
            .Select(i => new
            {
                id = i.Id,
                name = i.Name,
                category = i.Category,
                defaultUnit = i.DefaultUnit,
                indications = i.Indications
            })
            .ToListAsync();

        return Ok(new { success = true, data = ingredients });
    }

    private static string NormalizeString(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
