using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Common;
using DTOs.Pharmacy.Formulas;
using Models.Pharmacy;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormulasController : ControllerBase
{
    private readonly AppDbContext _context;

    public FormulasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<FormulaListDto>>> GetAll([FromQuery] FormulaFilterDto filter)
    {
        var query = _context.Formulas
            .Include(f => f.Components)
            .Where(f => f.EstablishmentId == GetEstablishmentId());

        if (!string.IsNullOrEmpty(filter.Code))
            query = query.Where(f => f.Code.Contains(filter.Code));

        if (!string.IsNullOrEmpty(filter.Name))
            query = query.Where(f => f.Name.Contains(filter.Name));

        if (!string.IsNullOrEmpty(filter.Category))
            query = query.Where(f => f.Category == filter.Category);

        if (!string.IsNullOrEmpty(filter.PharmaceuticalForm))
            query = query.Where(f => f.PharmaceuticalForm == filter.PharmaceuticalForm);

        if (filter.IsActive.HasValue)
            query = query.Where(f => f.IsActive == filter.IsActive.Value);

        if (filter.RequiresSpecialControl.HasValue)
            query = query.Where(f => f.RequiresSpecialControl == filter.RequiresSpecialControl.Value);

        if (filter.RequiresPrescription.HasValue)
            query = query.Where(f => f.RequiresPrescription == filter.RequiresPrescription.Value);

        if (filter.OnlyApproved.HasValue && filter.OnlyApproved.Value)
            query = query.Where(f => f.ApprovedByPharmacistId != null);

        if (filter.ContainsRawMaterialId.HasValue)
            query = query.Where(f => f.Components!.Any(c => c.RawMaterialId == filter.ContainsRawMaterialId.Value));

        if (filter.CreatedFrom.HasValue)
            query = query.Where(f => f.CreatedAt >= filter.CreatedFrom.Value);

        if (filter.CreatedTo.HasValue)
            query = query.Where(f => f.CreatedAt <= filter.CreatedTo.Value);

        var totalItems = await query.CountAsync();

        query = filter.SortBy?.ToLower() switch
        {
            "code" => filter.Ascending ? query.OrderBy(f => f.Code) : query.OrderByDescending(f => f.Code),
            "category" => filter.Ascending ? query.OrderBy(f => f.Category) : query.OrderByDescending(f => f.Category),
            "createdat" => filter.Ascending ? query.OrderBy(f => f.CreatedAt) : query.OrderByDescending(f => f.CreatedAt),
            _ => filter.Ascending ? query.OrderBy(f => f.Name) : query.OrderByDescending(f => f.Name)
        };

        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(f => new FormulaListDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                Description = f.Description,
                Category = f.Category,
                PharmaceuticalForm = f.PharmaceuticalForm,
                StandardYield = f.StandardYield,
                ShelfLifeDays = f.ShelfLifeDays,
                RequiresSpecialControl = f.RequiresSpecialControl,
                RequiresPrescription = f.RequiresPrescription,
                IsActive = f.IsActive,
                Version = f.Version,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                ApprovedAt = f.ApprovedAt,
                ComponentCount = f.Components!.Count
            })
            .ToListAsync();

        return Ok(new PagedResultDto<FormulaListDto>(items, totalItems, filter.PageNumber, filter.PageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FormulaDetailsDto>>> GetById(Guid id)
    {
        var formula = await _context.Formulas
            .Include(f => f.Components)
                .ThenInclude(c => c.RawMaterial)
            .Where(f => f.Id == id && f.EstablishmentId == GetEstablishmentId())
            .FirstOrDefaultAsync();

        if (formula == null)
            return NotFound(ApiResponse<FormulaDetailsDto>.ErrorResponse("Fórmula não encontrada"));

        var dto = new FormulaDetailsDto
        {
            Id = formula.Id,
            EstablishmentId = formula.EstablishmentId,
            Code = formula.Code,
            Name = formula.Name,
            Description = formula.Description,
            Category = formula.Category,
            PharmaceuticalForm = formula.PharmaceuticalForm,
            StandardYield = formula.StandardYield,
            ShelfLifeDays = formula.ShelfLifeDays,
            PreparationInstructions = formula.PreparationInstructions,
            StorageInstructions = formula.StorageInstructions,
            UsageInstructions = formula.UsageInstructions,
            RequiresSpecialControl = formula.RequiresSpecialControl,
            RequiresPrescription = formula.RequiresPrescription,
            IsActive = formula.IsActive,
            Version = formula.Version,
            PreviousVersionId = formula.PreviousVersionId,
            CreatedAt = formula.CreatedAt,
            UpdatedAt = formula.UpdatedAt,
            CreatedByEmployeeId = formula.CreatedByEmployeeId,
            CreatedByEmployeeName = "N/A",
            UpdatedByEmployeeId = formula.UpdatedByEmployeeId,
            UpdatedByEmployeeName = null,
            ApprovedByPharmacistId = formula.ApprovedByPharmacistId,
            ApprovedByPharmacistName = null,
            ApprovedAt = formula.ApprovedAt,
            Components = formula.Components?.Select(c => new FormulaComponentDto
            {
                Id = c.Id,
                FormulaId = c.FormulaId,
                RawMaterialId = c.RawMaterialId,
                RawMaterialCode = c.RawMaterial?.DcbCode ?? c.RawMaterial?.CasNumber ?? "",
                RawMaterialName = c.RawMaterial?.Name ?? "",
                RawMaterialUnit = c.RawMaterial?.Unit ?? "",
                RawMaterialIsControlled = c.RawMaterial?.ControlType != "COMUM",
                Quantity = c.Quantity,
                Unit = c.Unit,
                ComponentType = c.ComponentType,
                OrderIndex = c.OrderIndex,
                SpecialInstructions = c.SpecialInstructions,
                IsOptional = c.IsOptional
            }).ToList() ?? new List<FormulaComponentDto>()
        };

        return Ok(ApiResponse<FormulaDetailsDto>.SuccessResponse(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FormulaDetailsDto>>> Create([FromBody] CreateFormulaDto dto)
    {
        var establishmentId = GetEstablishmentId();

        if (string.IsNullOrEmpty(dto.Code))
            dto.Code = await GenerateFormulaCode();

        if (await _context.Formulas.AnyAsync(f => f.EstablishmentId == establishmentId && f.Code == dto.Code))
            return BadRequest(ApiResponse<FormulaDetailsDto>.ErrorResponse("Código já existe"));

        var formula = new Formula
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            PharmaceuticalForm = dto.PharmaceuticalForm,
            StandardYield = dto.StandardYield,
            ShelfLifeDays = dto.ShelfLifeDays,
            PreparationInstructions = dto.PreparationInstructions,
            StorageInstructions = dto.StorageInstructions,
            UsageInstructions = dto.UsageInstructions,
            RequiresSpecialControl = dto.RequiresSpecialControl,
            RequiresPrescription = dto.RequiresPrescription,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = GetEmployeeId()
        };

        _context.Formulas.Add(formula);

        if (dto.Components != null && dto.Components.Any())
        {
            foreach (var compDto in dto.Components)
            {
                var component = new FormulaComponent
                {
                    Id = Guid.NewGuid(),
                    FormulaId = formula.Id,
                    RawMaterialId = compDto.RawMaterialId,
                    Quantity = compDto.Quantity,
                    Unit = compDto.Unit,
                    ComponentType = compDto.ComponentType,
                    OrderIndex = compDto.OrderIndex,
                    SpecialInstructions = compDto.SpecialInstructions,
                    IsOptional = compDto.IsOptional
                };
                _context.FormulaComponents.Add(component);
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = formula.Id }, 
            ApiResponse<FormulaDetailsDto>.SuccessResponse(null!, "Fórmula criada com sucesso"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdateFormulaDto dto)
    {
        var formula = await _context.Formulas
            .Where(f => f.Id == id && f.EstablishmentId == GetEstablishmentId())
            .FirstOrDefaultAsync();

        if (formula == null)
            return NotFound(ApiResponse.ErrorResponse("Fórmula não encontrada"));

        formula.Name = dto.Name;
        formula.Description = dto.Description;
        formula.Category = dto.Category;
        formula.PharmaceuticalForm = dto.PharmaceuticalForm;
        formula.StandardYield = dto.StandardYield;
        formula.ShelfLifeDays = dto.ShelfLifeDays;
        formula.PreparationInstructions = dto.PreparationInstructions;
        formula.StorageInstructions = dto.StorageInstructions;
        formula.UsageInstructions = dto.UsageInstructions;
        formula.RequiresSpecialControl = dto.RequiresSpecialControl;
        formula.RequiresPrescription = dto.RequiresPrescription;
        formula.IsActive = dto.IsActive;
        formula.UpdatedAt = DateTime.UtcNow;
        formula.UpdatedByEmployeeId = GetEmployeeId();

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Fórmula atualizada com sucesso"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var formula = await _context.Formulas
            .Where(f => f.Id == id && f.EstablishmentId == GetEstablishmentId())
            .FirstOrDefaultAsync();

        if (formula == null)
            return NotFound(ApiResponse.ErrorResponse("Fórmula não encontrada"));

        formula.IsActive = false;
        formula.UpdatedAt = DateTime.UtcNow;
        formula.UpdatedByEmployeeId = GetEmployeeId();

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Fórmula desativada com sucesso"));
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> Approve(Guid id, [FromBody] ApproveFormulaDto dto)
    {
        var formula = await _context.Formulas
            .Where(f => f.Id == id && f.EstablishmentId == GetEstablishmentId())
            .FirstOrDefaultAsync();

        if (formula == null)
            return NotFound(ApiResponse.ErrorResponse("Fórmula não encontrada"));

        formula.ApprovedByPharmacistId = dto.PharmacistId;
        formula.ApprovedAt = DateTime.UtcNow;
        formula.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Fórmula aprovada com sucesso"));
    }

    [HttpPost("{id}/clone")]
    public async Task<ActionResult<ApiResponse<FormulaDetailsDto>>> Clone(Guid id, [FromBody] CloneFormulaDto dto)
    {
        var original = await _context.Formulas
            .Include(f => f.Components)
            .Where(f => f.Id == id && f.EstablishmentId == GetEstablishmentId())
            .FirstOrDefaultAsync();

        if (original == null)
            return NotFound(ApiResponse<FormulaDetailsDto>.ErrorResponse("Fórmula não encontrada"));

        var newCode = string.IsNullOrEmpty(dto.NewCode) ? await GenerateFormulaCode() : dto.NewCode;
        var newName = string.IsNullOrEmpty(dto.NewName) ? $"{original.Name} (Cópia)" : dto.NewName;

        var clone = new Formula
        {
            Id = Guid.NewGuid(),
            EstablishmentId = original.EstablishmentId,
            Code = newCode,
            Name = newName,
            Description = original.Description,
            Category = original.Category,
            PharmaceuticalForm = original.PharmaceuticalForm,
            StandardYield = original.StandardYield,
            ShelfLifeDays = original.ShelfLifeDays,
            PreparationInstructions = original.PreparationInstructions,
            StorageInstructions = original.StorageInstructions,
            UsageInstructions = original.UsageInstructions,
            RequiresSpecialControl = original.RequiresSpecialControl,
            RequiresPrescription = original.RequiresPrescription,
            IsActive = true,
            Version = original.Version + 1,
            PreviousVersionId = original.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = GetEmployeeId()
        };

        _context.Formulas.Add(clone);

        if (original.Components != null)
        {
            foreach (var comp in original.Components)
            {
                _context.FormulaComponents.Add(new FormulaComponent
                {
                    Id = Guid.NewGuid(),
                    FormulaId = clone.Id,
                    RawMaterialId = comp.RawMaterialId,
                    Quantity = comp.Quantity,
                    Unit = comp.Unit,
                    ComponentType = comp.ComponentType,
                    OrderIndex = comp.OrderIndex,
                    SpecialInstructions = comp.SpecialInstructions,
                    IsOptional = comp.IsOptional
                });
            }
        }

        if (!dto.KeepOriginalActive)
        {
            original.IsActive = false;
            original.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<FormulaDetailsDto>.SuccessResponse(null!, "Fórmula clonada com sucesso"));
    }

    private Guid GetEstablishmentId()
    {
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private Guid GetEmployeeId()
    {
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private async Task<string> GenerateFormulaCode()
    {
        var count = await _context.Formulas.CountAsync(f => f.EstablishmentId == GetEstablishmentId());
        return $"FORM-{(count + 1):D4}";
    }
}
