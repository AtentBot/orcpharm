using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Models.Pharmacy;
using System.ComponentModel.DataAnnotations;

namespace Controllers;

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
    /// Lista todas as matérias-primas do estabelecimento
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? controlType,
        [FromQuery] bool? lowStock,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Funcionário não autenticado" });
        }

        // Validar status do funcionário
        if (!IsEmployeeActive(employee))
        {
            return Unauthorized(new { error = "Funcionário inativo ou em situação irregular" });
        }

        try
        {
            var query = _db.RawMaterials
                .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive);

            // Filtro de busca
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(rm =>
                    rm.Name.Contains(search) ||
                    (rm.DcbCode != null && rm.DcbCode.Contains(search)) ||
                    (rm.DciCode != null && rm.DciCode.Contains(search)) ||
                    rm.CasNumber.Contains(search));
            }

            // Filtro por tipo de controle
            if (!string.IsNullOrEmpty(controlType))
            {
                query = query.Where(rm => rm.ControlType == controlType);
            }

            // Filtro de estoque baixo
            if (lowStock.HasValue && lowStock.Value)
            {
                query = query.Where(rm => rm.CurrentStock <= rm.MinimumStock);
            }

            // Total de registros (antes da paginação)
            var totalRecords = await query.CountAsync();

            // Aplicar paginação
            var materials = await query
                .OrderBy(rm => rm.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(rm => new
                {
                    rm.Id,
                    rm.Name,
                    rm.DcbCode,
                    rm.DciCode,
                    rm.CasNumber,
                    rm.ControlType,
                    rm.CurrentStock,
                    rm.MinimumStock,
                    rm.MaximumStock,
                    rm.Unit,
                    StockStatus = rm.CurrentStock <= rm.MinimumStock ? "BAIXO" :
                                  rm.CurrentStock >= rm.MaximumStock ? "EXCESSO" : "NORMAL",
                    StockPercentage = rm.MaximumStock > 0
                        ? (rm.CurrentStock / rm.MaximumStock * 100)
                        : 0,
                    rm.RequiresSpecialAuthorization,
                    rm.RequiresRefrigeration,
                    rm.LightSensitive,
                    rm.HumiditySensitive,
                    rm.PurityFactor,
                    rm.CreatedAt,
                    rm.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                data = materials,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalRecords,
                    totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                },
                filters = new
                {
                    search,
                    controlType,
                    lowStock
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar matérias-primas para Employee {EmployeeId}", employee.Id);
            return StatusCode(500, new { error = "Erro ao buscar matérias-primas" });
        }
    }

    /// <summary>
    /// Cria uma nova matéria-prima
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRawMaterialDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Funcionário não autenticado" });
        }

        // Validar status e permissões
        if (!IsEmployeeActive(employee))
        {
            return Unauthorized(new { error = "Funcionário inativo ou em situação irregular" });
        }

        if (!await HasStockManagementPermission(employee))
        {
            return Forbid();
        }

        // Validação adicional para substâncias controladas
        if (dto.ControlType != "COMUM" && !await HasControlledSubstancePermission(employee))
        {
            return Forbid();
        }

        // Validar se já existe matéria-prima com mesmo CAS ou DCB
        var existingMaterial = await _db.RawMaterials
            .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
            .Where(rm => rm.CasNumber == dto.CasNumber ||
                        (dto.DcbCode != null && rm.DcbCode == dto.DcbCode))
            .FirstOrDefaultAsync();

        if (existingMaterial != null)
        {
            return BadRequest(new
            {
                error = "Já existe uma matéria-prima cadastrada com este CAS ou DCB",
                existingMaterial = new { existingMaterial.Id, existingMaterial.Name }
            });
        }

        try
        {
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
                Unit = dto.Unit.ToLower(),
                PurityFactor = dto.PurityFactor > 0 ? dto.PurityFactor : 1.0m,
                MinimumStock = dto.MinimumStock >= 0 ? dto.MinimumStock : 0,
                MaximumStock = dto.MaximumStock >= 0 ? dto.MaximumStock : 0,
                CurrentStock = 0, // Estoque inicial zerado
                StorageConditions = dto.StorageConditions?.Trim(),
                RequiresRefrigeration = dto.RequiresRefrigeration,
                LightSensitive = dto.LightSensitive,
                HumiditySensitive = dto.HumiditySensitive,
                RequiresSpecialAuthorization = dto.ControlType != "COMUM",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employee.Id,
                UpdatedByEmployeeId = employee.Id
            };

            _db.RawMaterials.Add(material);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Matéria-prima {MaterialName} criada por {EmployeeName} (ID: {EmployeeId})",
                material.Name, employee.FullName, employee.Id);

            return CreatedAtAction(nameof(GetById), new { id = material.Id }, new
            {
                material.Id,
                material.Name,
                material.DcbCode,
                material.DciCode,
                material.CasNumber,
                material.ControlType,
                material.Unit,
                material.PurityFactor,
                material.CurrentStock,
                material.MinimumStock,
                material.MaximumStock,
                material.RequiresSpecialAuthorization,
                material.CreatedAt,
                CreatedBy = new
                {
                    employee.Id,
                    employee.FullName,
                    JobPosition = employee.JobPosition?.Name
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar matéria-prima por Employee {EmployeeId}", employee.Id);
            return StatusCode(500, new { error = "Erro ao criar matéria-prima" });
        }
    }

    /// <summary>
    /// Busca uma matéria-prima específica
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Funcionário não autenticado" });
        }

        if (!IsEmployeeActive(employee))
        {
            return Unauthorized(new { error = "Funcionário inativo ou em situação irregular" });
        }

        try
        {
            var material = await _db.RawMaterials
                .Include(rm => rm.Batches!.Where(b => b.Status == "APROVADO"))
                    .ThenInclude(b => b.Supplier)
                .FirstOrDefaultAsync(rm =>
                    rm.Id == id &&
                    rm.EstablishmentId == employee.EstablishmentId &&
                    rm.IsActive);

            if (material == null)
            {
                return NotFound(new { error = "Matéria-prima não encontrada" });
            }

            // Buscar informações dos funcionários separadamente (se necessário)
            Employee? createdByEmp = null;
            Employee? updatedByEmp = null;

            if (material.CreatedByEmployeeId.HasValue)
            {
                createdByEmp = await _db.Employees
                    .Include(e => e.JobPosition)
                    .FirstOrDefaultAsync(e => e.Id == material.CreatedByEmployeeId.Value);
            }

            if (material.UpdatedByEmployeeId.HasValue)
            {
                updatedByEmp = await _db.Employees
                    .Include(e => e.JobPosition)
                    .FirstOrDefaultAsync(e => e.Id == material.UpdatedByEmployeeId.Value);
            }

            // Calcular estatísticas dos lotes
            var batchStats = material.Batches?.Any() == true ? new
            {
                totalBatches = material.Batches.Count,
                totalQuantity = material.Batches.Sum(b => b.CurrentQuantity),
                totalReceived = material.Batches.Sum(b => b.ReceivedQuantity),
                oldestExpiry = material.Batches.Min(b => b.ExpiryDate),
                averageCost = material.Batches.Average(b => b.UnitCost)
            } : null;

            return Ok(new
            {
                material.Id,
                material.Name,
                material.DcbCode,
                material.DciCode,
                material.CasNumber,
                material.Description,
                material.ControlType,
                material.Unit,
                material.PurityFactor,
                material.CurrentStock,
                material.MinimumStock,
                material.MaximumStock,
                material.StorageConditions,
                material.RequiresRefrigeration,
                material.LightSensitive,
                material.HumiditySensitive,
                material.RequiresSpecialAuthorization,
                material.CreatedAt,
                material.UpdatedAt,
                createdBy = createdByEmp != null ? new
                {
                    createdByEmp.Id,
                    createdByEmp.FullName,
                    JobPosition = createdByEmp.JobPosition?.Name
                } : null,
                updatedBy = updatedByEmp != null ? new
                {
                    updatedByEmp.Id,
                    updatedByEmp.FullName,
                    JobPosition = updatedByEmp.JobPosition?.Name
                } : null,
                batches = material.Batches?.Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    b.InvoiceNumber,
                    ReceivedQuantity = b.ReceivedQuantity,
                    CurrentQuantity = b.CurrentQuantity,
                    b.UnitCost,
                    b.ExpiryDate,
                    b.ManufactureDate,
                    b.ReceivedDate,
                    b.Status,
                    b.CertificateNumber,
                    b.ApprovalDate,
                    DaysUntilExpiry = (b.ExpiryDate - DateTime.UtcNow).Days,
                    supplier = b.Supplier != null ? new
                    {
                        b.Supplier.Id,
                        b.Supplier.TradeName
                    } : null
                }).OrderBy(b => b.ExpiryDate),
                batchStatistics = batchStats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar matéria-prima {MaterialId} por Employee {EmployeeId}",
                id, employee.Id);
            return StatusCode(500, new { error = "Erro ao buscar matéria-prima" });
        }
    }

    /// <summary>
    /// Atualiza uma matéria-prima existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRawMaterialDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Funcionário não autenticado" });
        }

        if (!IsEmployeeActive(employee))
        {
            return Unauthorized(new { error = "Funcionário inativo ou em situação irregular" });
        }

        if (!await HasStockManagementPermission(employee))
        {
            return Forbid();
        }

        try
        {
            var material = await _db.RawMaterials
                .FirstOrDefaultAsync(rm =>
                    rm.Id == id &&
                    rm.EstablishmentId == employee.EstablishmentId &&
                    rm.IsActive);

            if (material == null)
            {
                return NotFound(new { error = "Matéria-prima não encontrada" });
            }

            // Validação adicional para mudança de tipo de controle
            if (material.ControlType == "COMUM" && dto.ControlType != "COMUM")
            {
                if (!await HasControlledSubstancePermission(employee))
                {
                    return Forbid();
                }
            }

            // Atualizar campos
            material.Name = dto.Name?.Trim() ?? material.Name;
            material.Description = dto.Description?.Trim();
            material.ControlType = dto.ControlType?.ToUpper() ?? material.ControlType;
            material.Unit = dto.Unit?.ToLower() ?? material.Unit;
            material.PurityFactor = dto.PurityFactor ?? material.PurityFactor;
            material.MinimumStock = dto.MinimumStock ?? material.MinimumStock;
            material.MaximumStock = dto.MaximumStock ?? material.MaximumStock;
            material.StorageConditions = dto.StorageConditions?.Trim();
            material.RequiresRefrigeration = dto.RequiresRefrigeration ?? material.RequiresRefrigeration;
            material.LightSensitive = dto.LightSensitive ?? material.LightSensitive;
            material.HumiditySensitive = dto.HumiditySensitive ?? material.HumiditySensitive;
            material.RequiresSpecialAuthorization = material.ControlType != "COMUM";
            material.UpdatedAt = DateTime.UtcNow;
            material.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Matéria-prima {MaterialName} atualizada por {EmployeeName} (ID: {EmployeeId})",
                material.Name, employee.FullName, employee.Id);

            return Ok(new
            {
                message = "Matéria-prima atualizada com sucesso",
                material = new
                {
                    material.Id,
                    material.Name,
                    material.ControlType,
                    material.UpdatedAt,
                    updatedBy = new
                    {
                        employee.Id,
                        employee.FullName,
                        JobPosition = employee.JobPosition?.Name
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar matéria-prima {MaterialId} por Employee {EmployeeId}",
                id, employee.Id);
            return StatusCode(500, new { error = "Erro ao atualizar matéria-prima" });
        }
    }

    /// <summary>
    /// Desativa uma matéria-prima (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Funcionário não autenticado" });
        }

        if (!IsEmployeeActive(employee))
        {
            return Unauthorized(new { error = "Funcionário inativo ou em situação irregular" });
        }

        if (!await HasStockManagementPermission(employee))
        {
            return Forbid();
        }

        try
        {
            var material = await _db.RawMaterials
                .Include(rm => rm.Batches)
                .FirstOrDefaultAsync(rm =>
                    rm.Id == id &&
                    rm.EstablishmentId == employee.EstablishmentId &&
                    rm.IsActive);

            if (material == null)
            {
                return NotFound(new { error = "Matéria-prima não encontrada" });
            }

            // Validar se existem lotes ativos
            var activeBatches = material.Batches?.Count(b => b.Status == "APROVADO") ?? 0;
            if (activeBatches > 0)
            {
                return BadRequest(new
                {
                    error = "Não é possível desativar matéria-prima com lotes ativos",
                    activeBatchesCount = activeBatches
                });
            }

            // Soft delete
            material.IsActive = false;
            material.UpdatedAt = DateTime.UtcNow;
            material.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "Matéria-prima {MaterialName} desativada por {EmployeeName} (ID: {EmployeeId})",
                material.Name, employee.FullName, employee.Id);

            return Ok(new
            {
                message = "Matéria-prima desativada com sucesso",
                materialId = material.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desativar matéria-prima {MaterialId} por Employee {EmployeeId}",
                id, employee.Id);
            return StatusCode(500, new { error = "Erro ao desativar matéria-prima" });
        }
    }

    /// <summary>
    /// Retorna estatísticas de estoque de matérias-primas
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return Unauthorized(new { error = "Funcionário não autenticado" });
        }

        if (!IsEmployeeActive(employee))
        {
            return Unauthorized(new { error = "Funcionário inativo ou em situação irregular" });
        }

        try
        {
            var materials = await _db.RawMaterials
                .Where(rm => rm.EstablishmentId == employee.EstablishmentId && rm.IsActive)
                .ToListAsync();

            var stats = new
            {
                totalMaterials = materials.Count,
                controlledSubstances = materials.Count(m => m.ControlType != "COMUM"),
                byControlType = materials.GroupBy(m => m.ControlType)
                    .Select(g => new { controlType = g.Key, count = g.Count() }),
                lowStock = materials.Count(m => m.CurrentStock <= m.MinimumStock),
                excessStock = materials.Count(m => m.CurrentStock >= m.MaximumStock),
                normalStock = materials.Count(m => m.CurrentStock > m.MinimumStock && m.CurrentStock < m.MaximumStock),
                requiresRefrigeration = materials.Count(m => m.RequiresRefrigeration),
                lightSensitive = materials.Count(m => m.LightSensitive),
                humiditySensitive = materials.Count(m => m.HumiditySensitive),
                totalStockValue = materials.Sum(m => m.CurrentStock) // Poderia multiplicar por preço unitário
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas por Employee {EmployeeId}", employee.Id);
            return StatusCode(500, new { error = "Erro ao buscar estatísticas" });
        }
    }

    // ==================== MÉTODOS AUXILIARES ====================

    /// <summary>
    /// Verifica se o funcionário está ativo e pode operar no sistema
    /// </summary>
    private bool IsEmployeeActive(Employee employee)
    {
        if (employee.Status != "Ativo")
            return false;

        if (employee.TerminationDate.HasValue && employee.TerminationDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            return false;

        if (employee.LockedUntil.HasValue && employee.LockedUntil.Value > DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Verifica se o funcionário tem permissão para gerenciar estoque
    /// </summary>
    private async Task<bool> HasStockManagementPermission(Employee employee)
    {
        // Carregar o cargo se não estiver carregado
        if (employee.JobPosition == null)
        {
            await _db.Entry(employee)
                .Reference(e => e.JobPosition)
                .LoadAsync();
        }

        if (employee.JobPosition == null)
            return false;

        // Lista de cargos que podem gerenciar estoque
        var allowedPositions = new[]
        {
            "FARMACÊUTICO",
            "FARMACÊUTICO RESPONSÁVEL",
            "GERENTE",
            "ADMINISTRADOR",
            "AUXILIAR DE ESTOQUE"
        };

        return allowedPositions.Contains(employee.JobPosition.Name.ToUpper());
    }

    /// <summary>
    /// Verifica se o funcionário tem permissão para manipular substâncias controladas
    /// </summary>
    private async Task<bool> HasControlledSubstancePermission(Employee employee)
    {
        // Carregar o cargo se não estiver carregado
        if (employee.JobPosition == null)
        {
            await _db.Entry(employee)
                .Reference(e => e.JobPosition)
                .LoadAsync();
        }

        if (employee.JobPosition == null)
            return false;

        // Apenas farmacêuticos podem lidar com substâncias controladas
        var allowedPositions = new[]
        {
            "FARMACÊUTICO",
            "FARMACÊUTICO RESPONSÁVEL",
            "ADMINISTRADOR"
        };

        return allowedPositions.Contains(employee.JobPosition.Name.ToUpper());
    }

    // ==================== DTOs ====================

    public class CreateRawMaterialDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(50)]
        public string? DcbCode { get; set; }

        [MaxLength(50)]
        public string? DciCode { get; set; }

        [Required(ErrorMessage = "Número CAS é obrigatório")]
        [MaxLength(50)]
        public string CasNumber { get; set; } = "";

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Tipo de controle é obrigatório")]
        [MaxLength(50)]
        public string ControlType { get; set; } = "COMUM";

        [Required(ErrorMessage = "Unidade de medida é obrigatória")]
        [MaxLength(20)]
        public string Unit { get; set; } = "g";

        [Range(0.01, 100, ErrorMessage = "Fator de pureza deve estar entre 0.01 e 100")]
        public decimal PurityFactor { get; set; } = 1.0m;

        [Range(0, double.MaxValue, ErrorMessage = "Estoque mínimo não pode ser negativo")]
        public decimal MinimumStock { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Estoque máximo não pode ser negativo")]
        public decimal MaximumStock { get; set; }

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

        [MaxLength(20)]
        public string? Unit { get; set; }

        [Range(0.01, 100)]
        public decimal? PurityFactor { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinimumStock { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaximumStock { get; set; }

        [MaxLength(500)]
        public string? StorageConditions { get; set; }

        public bool? RequiresRefrigeration { get; set; }
        public bool? LightSensitive { get; set; }
        public bool? HumiditySensitive { get; set; }
    }
}