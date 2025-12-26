using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Models.Pharmacy;
using System.ComponentModel.DataAnnotations;

namespace Controllers.Api;

/// <summary>
/// Controller API para gestão de Matérias-Primas
/// Autenticação via EmployeeAuthMiddleware (HttpContext.Items["Employee"])
/// Rotas protegidas - requer X-Session-Token
/// </summary>

[ApiController]
[Route("api/[controller]")]
public class RawMaterialsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<RawMaterialsController> _logger;

    public RawMaterialsController(AppDbContext db, ILogger<RawMaterialsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as matérias-primas do estabelecimento com informações de preço
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? controlType,
        [FromQuery] string? priceSource,      // NOVO: Filtro por origem do preço
        [FromQuery] bool? isVirtual,          // NOVO: Filtro por virtual
        [FromQuery] bool? lowStock,
        [FromQuery] bool? outdatedPrice,      // NOVO: Preço desatualizado (>6 meses)
        [FromQuery] string? category,         // NOVO: Filtro por categoria
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo ou em situação irregular" });

        try
        {
            var query = _db.RawMaterials
                .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive);

            // Filtro de busca
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(rm =>
                    rm.Name.ToLower().Contains(searchLower) ||
                    (rm.DcbCode != null && rm.DcbCode.ToLower().Contains(searchLower)) ||
                    (rm.DciCode != null && rm.DciCode.ToLower().Contains(searchLower)) ||
                    rm.CasNumber.ToLower().Contains(searchLower) ||
                    (rm.Synonyms != null && rm.Synonyms.ToLower().Contains(searchLower)));
            }

            // Filtro por tipo de controle
            if (!string.IsNullOrEmpty(controlType))
                query = query.Where(rm => rm.ControlType == controlType);

            // NOVO: Filtro por origem do preço
            if (!string.IsNullOrEmpty(priceSource))
                query = query.Where(rm => rm.PriceSource == priceSource);

            // NOVO: Filtro por virtual (sem estoque físico)
            if (isVirtual.HasValue)
                query = query.Where(rm => rm.IsVirtual == isVirtual.Value);

            // Filtro por categoria
            if (!string.IsNullOrEmpty(category))
                query = query.Where(rm => rm.Category == category);

            // Filtro de estoque baixo
            if (lowStock.HasValue && lowStock.Value)
                query = query.Where(rm => rm.CurrentStock <= rm.MinimumStock);

            // NOVO: Filtro por preço desatualizado (mais de 6 meses)
            if (outdatedPrice.HasValue && outdatedPrice.Value)
            {
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                query = query.Where(rm => 
                    rm.LastPriceDate == null || 
                    rm.LastPriceDate < sixMonthsAgo);
            }

            var totalRecords = await query.CountAsync();

            var materials = await query
                .OrderBy(rm => rm.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(rm => new RawMaterialListDto
                {
                    Id = rm.Id,
                    Name = rm.Name,
                    DcbCode = rm.DcbCode,
                    DciCode = rm.DciCode,
                    CasNumber = rm.CasNumber,
                    ControlType = rm.ControlType,
                    Category = rm.Category,
                    CurrentStock = rm.CurrentStock,
                    MinimumStock = rm.MinimumStock,
                    MaximumStock = rm.MaximumStock,
                    Unit = rm.Unit,
                    
                    // Informações de preço
                    BasePrice = rm.BasePrice,
                    LastKnownPrice = rm.LastKnownPrice,
                    LastPriceDate = rm.LastPriceDate,
                    IsVirtual = rm.IsVirtual,
                    PriceSource = rm.PriceSource,
                    
                    // Preço atual calculado
                    CurrentPrice = rm.CurrentStock > 0 
                        ? rm.LastKnownPrice ?? rm.BasePrice ?? 0
                        : rm.LastKnownPrice ?? rm.BasePrice ?? 0,
                    
                    // Indicadores
                    StockStatus = rm.CurrentStock <= 0 ? "SEM_ESTOQUE" :
                                  rm.CurrentStock <= rm.MinimumStock ? "BAIXO" :
                                  rm.CurrentStock >= rm.MaximumStock ? "EXCESSO" : "NORMAL",
                    
                    PriceStatus = rm.CurrentStock > 0 ? "ESTOQUE" :
                                  rm.LastKnownPrice.HasValue ? "HISTORICO" : "BASE",
                    
                    IsPriceOutdated = rm.LastPriceDate == null || 
                                      rm.LastPriceDate < DateTime.UtcNow.AddMonths(-6),
                    
                    DaysSinceLastPrice = rm.LastPriceDate.HasValue 
                        ? (int)(DateTime.UtcNow - rm.LastPriceDate.Value).TotalDays 
                        : (int?)null,
                    
                    RequiresSpecialAuthorization = rm.RequiresSpecialAuthorization,
                    RequiresRefrigeration = rm.RequiresRefrigeration,
                    Popularity = rm.Popularity,
                    CreatedAt = rm.CreatedAt,
                    UpdatedAt = rm.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = materials,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalRecords,
                    totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                },
                filters = new { search, controlType, priceSource, isVirtual, lowStock, outdatedPrice, category }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar matérias-primas");
            return StatusCode(500, new { success = false, error = "Erro ao buscar matérias-primas" });
        }
    }

    /// <summary>
    /// Retorna detalhes de uma matéria-prima incluindo informações de preço
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var material = await _db.RawMaterials
            .Include(rm => rm.Batches!.Where(b => b.CurrentQuantity > 0))
            .FirstOrDefaultAsync(rm => 
                rm.Id == id && 
                rm.EstablishmentId == employee.EstablishmentId && 
                rm.IsActive);

        if (material == null)
            return NotFound(new { error = "Matéria-prima não encontrada" });

        // Buscar último lote com preço (lotes com quantidade > 0)
        var lastBatchWithPrice = await _db.Batches
            .Where(b => b.RawMaterialId == id && b.CurrentQuantity > 0)
            .OrderByDescending(b => b.ReceivedDate)
            .Select(b => new { b.UnitCost, b.ReceivedDate })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                material.Id,
                material.Name,
                material.DcbCode,
                material.DciCode,
                material.CasNumber,
                material.Description,
                material.ControlType,
                material.Category,
                material.Synonyms,
                material.Indications,
                material.Unit,
                material.PurityFactor,
                material.EquivalenceFactor,
                material.CurrentStock,
                material.MinimumStock,
                material.MaximumStock,
                
                // Preços
                material.BasePrice,
                material.LastKnownPrice,
                material.LastPriceDate,
                material.IsVirtual,
                material.PriceSource,
                
                // Preço atual calculado
                CurrentPrice = material.CurrentStock > 0 
                    ? lastBatchWithPrice?.UnitCost ?? material.LastKnownPrice ?? material.BasePrice ?? 0
                    : material.LastKnownPrice ?? material.BasePrice ?? 0,
                
                PriceStatus = material.CurrentStock > 0 ? "ESTOQUE" :
                              material.LastKnownPrice.HasValue ? "HISTORICO" : "BASE",
                
                // Info do último lote
                LastBatchPrice = lastBatchWithPrice?.UnitCost,
                LastBatchDate = lastBatchWithPrice?.ReceivedDate,
                
                // Armazenamento
                material.StorageConditions,
                material.RequiresRefrigeration,
                material.LightSensitive,
                material.HumiditySensitive,
                material.RequiresSpecialAuthorization,
                material.Popularity,
                
                // Lotes ativos
                ActiveBatches = material.Batches?.Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    b.CurrentQuantity,
                    b.UnitCost,
                    b.ReceivedDate
                }).ToList(),
                
                material.CreatedAt,
                material.UpdatedAt
            }
        });
    }

    /// <summary>
    /// Atualiza o preço base de uma matéria-prima
    /// </summary>
    [HttpPatch("{id:guid}/base-price")]
    public async Task<IActionResult> UpdateBasePrice(Guid id, [FromBody] UpdateBasePriceDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar preços" });

        var material = await _db.RawMaterials
            .FirstOrDefaultAsync(rm => 
                rm.Id == id && 
                rm.EstablishmentId == employee.EstablishmentId && 
                rm.IsActive);

        if (material == null)
            return NotFound(new { error = "Matéria-prima não encontrada" });

        var oldPrice = material.BasePrice;
        material.BasePrice = dto.BasePrice;
        material.UpdatedAt = DateTime.UtcNow;
        material.UpdatedByEmployeeId = employee.Id;

        // Se não tem LastKnownPrice, atualiza PriceSource
        if (!material.LastKnownPrice.HasValue && material.CurrentStock <= 0)
            material.PriceSource = "BASE";

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Preço base de {MaterialName} alterado de {OldPrice} para {NewPrice} por {EmployeeName}",
            material.Name, oldPrice, dto.BasePrice, employee.FullName);

        return Ok(new
        {
            success = true,
            message = "Preço base atualizado com sucesso",
            data = new
            {
                material.Id,
                material.Name,
                OldBasePrice = oldPrice,
                NewBasePrice = material.BasePrice,
                material.PriceSource
            }
        });
    }

    /// <summary>
    /// Retorna estatísticas de precificação
    /// </summary>
    [HttpGet("pricing-statistics")]
    public async Task<IActionResult> GetPricingStatistics()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var materials = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .ToListAsync();

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

        var stats = new
        {
            totalMaterials = materials.Count,
            
            // Por estoque
            withStock = materials.Count(m => m.CurrentStock > 0),
            withoutStock = materials.Count(m => m.CurrentStock <= 0),
            virtualOnly = materials.Count(m => m.IsVirtual),
            
            // Por origem de preço
            priceFromStock = materials.Count(m => m.PriceSource == "ESTOQUE"),
            priceFromHistory = materials.Count(m => m.PriceSource == "HISTORICO"),
            priceFromBase = materials.Count(m => m.PriceSource == "BASE"),
            
            // Qualidade dos preços
            withBasePrice = materials.Count(m => m.BasePrice.HasValue && m.BasePrice > 0),
            withoutBasePrice = materials.Count(m => !m.BasePrice.HasValue || m.BasePrice <= 0),
            withLastKnownPrice = materials.Count(m => m.LastKnownPrice.HasValue),
            outdatedPrice = materials.Count(m => 
                m.LastPriceDate == null || m.LastPriceDate < sixMonthsAgo),
            
            // Por categoria
            byCategory = materials
                .Where(m => !string.IsNullOrEmpty(m.Category))
                .GroupBy(m => m.Category)
                .Select(g => new { category = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10),
            
            // Mais usados sem estoque
            popularWithoutStock = materials
                .Where(m => m.CurrentStock <= 0)
                .OrderByDescending(m => m.Popularity)
                .Take(10)
                .Select(m => new { m.Id, m.Name, m.Popularity, m.LastKnownPrice, m.BasePrice }),
            
            // Preços desatualizados (mais usados primeiro)
            outdatedPriceList = materials
                .Where(m => m.LastPriceDate == null || m.LastPriceDate < sixMonthsAgo)
                .OrderByDescending(m => m.Popularity)
                .Take(10)
                .Select(m => new 
                { 
                    m.Id, 
                    m.Name, 
                    m.LastPriceDate,
                    DaysSinceUpdate = m.LastPriceDate.HasValue 
                        ? (int)(DateTime.UtcNow - m.LastPriceDate.Value).TotalDays 
                        : (int?)null,
                    m.BasePrice,
                    m.LastKnownPrice
                })
        };

        return Ok(new { success = true, data = stats });
    }

    /// <summary>
    /// Retorna lista de categorias disponíveis
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var categories = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .Where(rm => !string.IsNullOrEmpty(rm.Category))
            .GroupBy(rm => rm.Category)
            .Select(g => new { category = g.Key, count = g.Count() })
            .OrderBy(x => x.category)
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }

    /// <summary>
    /// Cria uma nova matéria-prima
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRawMaterialDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar estoque" });

        if (dto.ControlType != "COMUM" && !await HasControlledSubstancePermission(employee))
            return Forbid();

        // Validar duplicidade
        var existing = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .Where(rm => rm.CasNumber == dto.CasNumber ||
                        (dto.DcbCode != null && rm.DcbCode == dto.DcbCode))
            .FirstOrDefaultAsync();

        if (existing != null)
            return BadRequest(new { error = "Já existe matéria-prima com este CAS ou DCB" });

        var material = new RawMaterial
        {
            Id = Guid.NewGuid(),
            EstablishmentId = employee.EstablishmentId,
            Name = dto.Name.Trim(),
            DcbCode = dto.DcbCode?.Trim(),
            DciCode = dto.DciCode?.Trim(),
            CasNumber = dto.CasNumber.Trim(),
            Description = dto.Description?.Trim(),
            ControlType = dto.ControlType.ToUpper(),
            Category = dto.Category?.Trim(),
            Synonyms = dto.Synonyms?.Trim(),
            Indications = dto.Indications?.Trim(),
            Unit = dto.Unit.ToLower(),
            PurityFactor = dto.PurityFactor > 0 ? dto.PurityFactor : 1.0m,
            MinimumStock = dto.MinimumStock >= 0 ? dto.MinimumStock : 0,
            MaximumStock = dto.MaximumStock >= 0 ? dto.MaximumStock : 0,
            CurrentStock = 0,
            BasePrice = dto.BasePrice,
            IsVirtual = true, // Novo ingrediente começa como virtual
            PriceSource = dto.BasePrice.HasValue ? "BASE" : "BASE",
            StorageConditions = dto.StorageConditions?.Trim(),
            RequiresRefrigeration = dto.RequiresRefrigeration,
            LightSensitive = dto.LightSensitive,
            HumiditySensitive = dto.HumiditySensitive,
            RequiresSpecialAuthorization = dto.ControlType != "COMUM",
            Popularity = dto.Popularity > 0 ? dto.Popularity : 50,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employee.Id,
            UpdatedByEmployeeId = employee.Id
        };

        _db.RawMaterials.Add(material);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Matéria-prima {Name} criada por {Employee}", material.Name, employee.FullName);

        return CreatedAtAction(nameof(GetById), new { id = material.Id }, new
        {
            success = true,
            message = "Matéria-prima criada com sucesso",
            data = new { material.Id, material.Name }
        });
    }

    /// <summary>
    /// Atualiza uma matéria-prima
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRawMaterialDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão" });

        var material = await _db.RawMaterials
            .FirstOrDefaultAsync(rm => 
                rm.Id == id && 
                rm.EstablishmentId == employee.EstablishmentId && 
                rm.IsActive);

        if (material == null)
            return NotFound(new { error = "Matéria-prima não encontrada" });

        // Atualizar campos
        if (!string.IsNullOrEmpty(dto.Name)) material.Name = dto.Name.Trim();
        if (dto.Description != null) material.Description = dto.Description.Trim();
        if (!string.IsNullOrEmpty(dto.ControlType)) material.ControlType = dto.ControlType.ToUpper();
        if (!string.IsNullOrEmpty(dto.Category)) material.Category = dto.Category.Trim();
        if (dto.Synonyms != null) material.Synonyms = dto.Synonyms.Trim();
        if (dto.Indications != null) material.Indications = dto.Indications.Trim();
        if (!string.IsNullOrEmpty(dto.Unit)) material.Unit = dto.Unit.ToLower();
        if (dto.PurityFactor.HasValue) material.PurityFactor = dto.PurityFactor.Value;
        if (dto.MinimumStock.HasValue) material.MinimumStock = dto.MinimumStock.Value;
        if (dto.MaximumStock.HasValue) material.MaximumStock = dto.MaximumStock.Value;
        if (dto.BasePrice.HasValue) material.BasePrice = dto.BasePrice.Value;
        if (dto.Popularity.HasValue) material.Popularity = dto.Popularity.Value;
        if (dto.StorageConditions != null) material.StorageConditions = dto.StorageConditions.Trim();
        if (dto.RequiresRefrigeration.HasValue) material.RequiresRefrigeration = dto.RequiresRefrigeration.Value;
        if (dto.LightSensitive.HasValue) material.LightSensitive = dto.LightSensitive.Value;
        if (dto.HumiditySensitive.HasValue) material.HumiditySensitive = dto.HumiditySensitive.Value;

        material.UpdatedAt = DateTime.UtcNow;
        material.UpdatedByEmployeeId = employee.Id;

        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Matéria-prima atualizada" });
    }

    /// <summary>
    /// Desativa uma matéria-prima
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasStockManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão" });

        var material = await _db.RawMaterials
            .FirstOrDefaultAsync(rm => 
                rm.Id == id && 
                rm.EstablishmentId == employee.EstablishmentId && 
                rm.IsActive);

        if (material == null)
            return NotFound(new { error = "Matéria-prima não encontrada" });

        material.IsActive = false;
        material.UpdatedAt = DateTime.UtcNow;
        material.UpdatedByEmployeeId = employee.Id;

        await _db.SaveChangesAsync();

        _logger.LogWarning("Matéria-prima {Name} desativada por {Employee}", material.Name, employee.FullName);

        return Ok(new { success = true, message = "Matéria-prima desativada" });
    }

    /// <summary>
    /// Retorna estatísticas gerais de estoque
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var materials = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalMaterials = materials.Count,
                controlledSubstances = materials.Count(m => m.ControlType != "COMUM"),
                byControlType = materials.GroupBy(m => m.ControlType)
                    .Select(g => new { controlType = g.Key, count = g.Count() }),
                lowStock = materials.Count(m => m.CurrentStock <= m.MinimumStock),
                excessStock = materials.Count(m => m.CurrentStock >= m.MaximumStock),
                normalStock = materials.Count(m => m.CurrentStock > m.MinimumStock && m.CurrentStock < m.MaximumStock),
                virtualIngredients = materials.Count(m => m.IsVirtual),
                withBasePrice = materials.Count(m => m.BasePrice.HasValue && m.BasePrice > 0)
            }
        });
    }

    // ==================== MÉTODOS AUXILIARES ====================

    /// <summary>
    /// Verifica se o funcionário está ativo (mesmo padrão do EmployeeAuthMiddleware)
    /// </summary>
    private bool IsEmployeeActive(Employee employee)
    {
        // Usar ToUpper() para consistência com o middleware
        if (employee.Status.ToUpper() != "ATIVO") return false;
        if (employee.TerminationDate.HasValue && employee.TerminationDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow)) return false;
        if (employee.LockedUntil.HasValue && employee.LockedUntil.Value > DateTime.UtcNow) return false;
        return true;
    }

    private async Task<bool> HasStockManagementPermission(Employee employee)
    {
        if (employee.JobPosition == null)
            await _db.Entry(employee).Reference(e => e.JobPosition).LoadAsync();

        if (employee.JobPosition == null) return false;

        var allowedCodes = new[] { "pharmacist", "pharmacist_rt", "manager", "admin", "stock_assistant" };
        return allowedCodes.Contains(employee.JobPosition.Code);
    }

    private async Task<bool> HasControlledSubstancePermission(Employee employee)
    {
        if (employee.JobPosition == null)
            await _db.Entry(employee).Reference(e => e.JobPosition).LoadAsync();

        if (employee.JobPosition == null) return false;

        var allowedCodes = new[] { "pharmacist", "pharmacist_rt", "admin" };
        return allowedCodes.Contains(employee.JobPosition.Code);
    }

    // ==================== DTOs ====================

    public class RawMaterialListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? DcbCode { get; set; }
        public string? DciCode { get; set; }
        public string CasNumber { get; set; } = "";
        public string ControlType { get; set; } = "";
        public string? Category { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal MaximumStock { get; set; }
        public string Unit { get; set; } = "";
        
        // Preços
        public decimal? BasePrice { get; set; }
        public decimal? LastKnownPrice { get; set; }
        public DateTime? LastPriceDate { get; set; }
        public bool IsVirtual { get; set; }
        public string PriceSource { get; set; } = "";
        public decimal CurrentPrice { get; set; }
        
        // Status
        public string StockStatus { get; set; } = "";
        public string PriceStatus { get; set; } = "";
        public bool IsPriceOutdated { get; set; }
        public int? DaysSinceLastPrice { get; set; }
        
        public bool RequiresSpecialAuthorization { get; set; }
        public bool RequiresRefrigeration { get; set; }
        public int Popularity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateBasePriceDto
    {
        [Required]
        [Range(0.0001, 999999.99, ErrorMessage = "Preço deve ser maior que zero")]
        public decimal BasePrice { get; set; }
    }

    public class CreateRawMaterialDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(50)]
        public string? DcbCode { get; set; }

        [MaxLength(50)]
        public string? DciCode { get; set; }

        [Required, MaxLength(50)]
        public string CasNumber { get; set; } = "";

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string ControlType { get; set; } = "COMUM";

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Synonyms { get; set; }

        [MaxLength(1000)]
        public string? Indications { get; set; }

        [MaxLength(20)]
        public string Unit { get; set; } = "g";

        public decimal PurityFactor { get; set; } = 1.0m;
        public decimal MinimumStock { get; set; }
        public decimal MaximumStock { get; set; }
        public decimal? BasePrice { get; set; }
        public int Popularity { get; set; } = 50;

        [MaxLength(500)]
        public string? StorageConditions { get; set; }

        public bool RequiresRefrigeration { get; set; }
        public bool LightSensitive { get; set; }
        public bool HumiditySensitive { get; set; }
    }

    public class UpdateRawMaterialDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? ControlType { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Synonyms { get; set; }

        [MaxLength(1000)]
        public string? Indications { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        public decimal? PurityFactor { get; set; }
        public decimal? MinimumStock { get; set; }
        public decimal? MaximumStock { get; set; }
        public decimal? BasePrice { get; set; }
        public int? Popularity { get; set; }

        [MaxLength(500)]
        public string? StorageConditions { get; set; }

        public bool? RequiresRefrigeration { get; set; }
        public bool? LightSensitive { get; set; }
        public bool? HumiditySensitive { get; set; }
    }
}
