using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.CustomerFormulas;
using Models.Pharmacy;
using Service.CustomerFormulas;

namespace Controllers;

[Route("api/customer-portal")]
[ApiController]
public class CustomerPortalController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CustomFormulaService _formulaService;
    private readonly PricingService _pricingService;
    private readonly ILogger<CustomerPortalController> _logger;

    public CustomerPortalController(
        AppDbContext context,
        CustomFormulaService formulaService,
        PricingService pricingService,
        ILogger<CustomerPortalController> logger)
    {
        _context = context;
        _formulaService = formulaService;
        _pricingService = pricingService;
        _logger = logger;
    }

    /// <summary>
    /// GET api/customer-portal/product-types
    /// Listar todos os tipos de produtos disponíveis
    /// </summary>
    [HttpGet("product-types")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<ProductTypeDto>>>> GetProductTypes()
    {
        try
        {
            var types = await _context.ProductTypes
                .Where(pt => pt.IsActive)
                .OrderBy(pt => pt.DisplayOrder)
                .Select(pt => new ProductTypeDto
                {
                    Id = pt.Id,
                    Name = pt.Name,
                    Description = pt.Description,
                    PharmaceuticalForm = pt.PharmaceuticalForm,
                    Category = pt.Category
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ProductTypeDto>>.SuccessResponse(types));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar tipos de produtos");
            return StatusCode(500, ApiResponse<List<ProductTypeDto>>.ErrorResponse("Erro ao buscar tipos de produtos"));
        }
    }

    /// <summary>
    /// GET api/customer-portal/product-types/{id}/subtypes
    /// Listar subtipos de um tipo específico
    /// </summary>
    [HttpGet("product-types/{id}/subtypes")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<ProductSubTypeDto>>>> GetSubTypes(Guid id)
    {
        try
        {
            var subTypes = await _context.ProductSubTypes
                .Where(pst => pst.ProductTypeId == id && pst.IsActive)
                .OrderBy(pst => pst.DisplayOrder)
                .Select(pst => new ProductSubTypeDto
                {
                    Id = pst.Id,
                    ProductTypeId = pst.ProductTypeId,
                    Name = pst.Name,
                    Description = pst.Description,
                    StandardQuantity = pst.StandardQuantity,
                    StandardUnit = pst.StandardUnit,
                    PriceModifier = pst.PriceModifier
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ProductSubTypeDto>>.SuccessResponse(subTypes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar subtipos");
            return StatusCode(500, ApiResponse<List<ProductSubTypeDto>>.ErrorResponse("Erro ao buscar subtipos"));
        }
    }

    /// <summary>
    /// POST api/customer-portal/custom-formula
    /// Cliente cria fórmula personalizada
    /// </summary>
    [HttpPost("custom-formula")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CustomerFormulaDto>>> CreateCustomFormula(
        [FromBody] CreateCustomFormulaDto dto)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            
            // Criar fórmula
            var formula = await _formulaService.CreateFormulaAsync(dto, establishmentId);
            
            // Calcular preço estimado
            var estimatedPrice = await _pricingService.CalculatePriceAsync(
                dto.ProductSubTypeId,
                dto.Quantity,
                establishmentId
            );
            
            formula.EstimatedPrice = estimatedPrice;
            await _context.SaveChangesAsync();

            var result = MapToDto(formula);
            
            return Ok(ApiResponse<CustomerFormulaDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar fórmula personalizada");
            return StatusCode(500, ApiResponse<CustomerFormulaDto>.ErrorResponse("Erro ao criar fórmula"));
        }
    }

    /// <summary>
    /// GET api/customer-portal/custom-formula/{id}
    /// Buscar fórmula por ID
    /// </summary>
    [HttpGet("custom-formula/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CustomerFormulaDto>>> GetCustomFormula(Guid id)
    {
        try
        {
            var formula = await _context.CustomerFormulas
                .Include(cf => cf.ProductType)
                .Include(cf => cf.ProductSubType)
                .FirstOrDefaultAsync(cf => cf.Id == id);

            if (formula == null)
                return NotFound(ApiResponse<CustomerFormulaDto>.ErrorResponse("Fórmula não encontrada"));

            var result = MapToDto(formula);
            
            return Ok(ApiResponse<CustomerFormulaDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar fórmula {Id}", id);
            return StatusCode(500, ApiResponse<CustomerFormulaDto>.ErrorResponse("Erro ao buscar fórmula"));
        }
    }

    // ==================== MÉTODOS AUXILIARES ====================

    private Guid GetEstablishmentId()
    {
        // TODO: Implementar lógica para obter EstablishmentId
        // Por enquanto retorna um GUID fixo
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private CustomerFormulaDto MapToDto(CustomerFormula formula)
    {
        return new CustomerFormulaDto
        {
            Id = formula.Id,
            Code = formula.Code,
            Status = formula.Status,
            ProductTypeName = formula.ProductType?.Name ?? "",
            ProductSubTypeName = formula.ProductSubType?.Name ?? "",
            Quantity = formula.Quantity,
            Unit = formula.Unit,
            CustomerName = formula.CustomerName,
            CustomerPhone = formula.CustomerPhone,
            EstimatedPrice = formula.EstimatedPrice,
            FinalPrice = formula.FinalPrice,
            RequiresPrescription = formula.RequiresPrescription,
            EstimatedShelfLifeDays = formula.EstimatedShelfLifeDays,
            PharmaceuticalAnalysis = formula.PharmaceuticalAnalysis,
            ApprovedAt = formula.ApprovedAt,
            RejectedAt = formula.RejectedAt,
            RejectionReason = formula.RejectionReason,
            CreatedAt = formula.CreatedAt,
            PaidAt = formula.PaidAt,
            PaidAmount = formula.PaidAmount
        };
    }
}

// DTOs auxiliares
public class ProductTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string PharmaceuticalForm { get; set; } = default!;
    public string Category { get; set; } = default!;
}

public class ProductSubTypeDto
{
    public Guid Id { get; set; }
    public Guid ProductTypeId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal? StandardQuantity { get; set; }
    public string? StandardUnit { get; set; }
    public decimal PriceModifier { get; set; }
}
