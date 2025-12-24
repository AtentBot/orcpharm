using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Data;

namespace Service.CustomerFormulas;

public class PricingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PricingService> _logger;

    // Configurações de precificação (podem vir de appsettings.json)
    private const decimal DEFAULT_RAW_MATERIAL_COST_PER_GRAM = 0.50m;
    private const decimal DEFAULT_PROFIT_MARGIN = 0.60m; // 60%
    private const decimal DEFAULT_TAX_RATE = 0.15m; // 15%

    public PricingService(
        AppDbContext context,
        ILogger<PricingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcular preço estimado de uma fórmula
    /// </summary>
    public async Task<decimal> CalculatePriceAsync(
        Guid productSubTypeId,
        decimal quantity,
        Guid establishmentId,
        string unit = "g")
    {
        try
        {
            // 1. Buscar subtipo do produto
            var subType = await _context.ProductSubTypes
                .FirstOrDefaultAsync(pst => pst.Id == productSubTypeId);

            if (subType == null)
                throw new Exception("Subtipo de produto não encontrado");

            // 2. Normalizar quantidade para gramas (unidade base)
            var quantityInGrams = ConvertToGrams(quantity, unit);

            // 3. Calcular custo base de matérias-primas
            var rawMaterialCost = quantityInGrams * DEFAULT_RAW_MATERIAL_COST_PER_GRAM;

            // 4. Custo de manipulação (fixo do subtipo)
            var manipulationCost = subType.ManipulationCostBase;

            // 5. Aplicar modificador de preço do subtipo
            var baseCost = (rawMaterialCost + manipulationCost) * subType.PriceModifier;

            // 6. Adicionar margem de lucro
            var priceWithMargin = baseCost * (1 + DEFAULT_PROFIT_MARGIN);

            // 7. Adicionar impostos
            var finalPrice = priceWithMargin * (1 + DEFAULT_TAX_RATE);

            // 8. Arredondar para 2 casas decimais
            var roundedPrice = Math.Round(finalPrice, 2);

            _logger.LogInformation(
                "Preço calculado para {SubType}: R$ {Price} (Qtd: {Quantity}{Unit})",
                subType.Name, roundedPrice, quantity, unit);

            return roundedPrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular preço");
            throw;
        }
    }

    /// <summary>
    /// Calcular preço detalhado (com breakdown de custos)
    /// </summary>
    public async Task<PriceBreakdown> CalculatePriceDetailedAsync(
        Guid productSubTypeId,
        decimal quantity,
        Guid establishmentId,
        string unit = "g")
    {
        try
        {
            var subType = await _context.ProductSubTypes
                .FirstOrDefaultAsync(pst => pst.Id == productSubTypeId);

            if (subType == null)
                throw new Exception("Subtipo de produto não encontrado");

            var quantityInGrams = ConvertToGrams(quantity, unit);
            var rawMaterialCost = quantityInGrams * DEFAULT_RAW_MATERIAL_COST_PER_GRAM;
            var manipulationCost = subType.ManipulationCostBase;
            var baseCost = (rawMaterialCost + manipulationCost) * subType.PriceModifier;
            var profitAmount = baseCost * DEFAULT_PROFIT_MARGIN;
            var subtotal = baseCost + profitAmount;
            var taxAmount = subtotal * DEFAULT_TAX_RATE;
            var finalPrice = subtotal + taxAmount;

            return new PriceBreakdown
            {
                RawMaterialCost = Math.Round(rawMaterialCost, 2),
                ManipulationCost = Math.Round(manipulationCost, 2),
                BaseCost = Math.Round(baseCost, 2),
                PriceModifier = subType.PriceModifier,
                ProfitMargin = DEFAULT_PROFIT_MARGIN,
                ProfitAmount = Math.Round(profitAmount, 2),
                Subtotal = Math.Round(subtotal, 2),
                TaxRate = DEFAULT_TAX_RATE,
                TaxAmount = Math.Round(taxAmount, 2),
                FinalPrice = Math.Round(finalPrice, 2),
                QuantityInGrams = quantityInGrams
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular preço detalhado");
            throw;
        }
    }

    /// <summary>
    /// Aplicar desconto a um preço
    /// </summary>
    public decimal ApplyDiscount(decimal price, decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 1)
            throw new ArgumentException("Desconto deve estar entre 0 e 1 (0% a 100%)");

        var discountAmount = price * discountPercentage;
        var finalPrice = price - discountAmount;

        return Math.Round(finalPrice, 2);
    }

    /// <summary>
    /// Converter unidades para gramas (unidade base)
    /// </summary>
    private decimal ConvertToGrams(decimal quantity, string unit)
    {
        return unit.ToLower() switch
        {
            "g" => quantity,
            "mg" => quantity / 1000m,
            "kg" => quantity * 1000m,
            "ml" => quantity, // Assumindo densidade ~1g/ml
            "l" => quantity * 1000m,
            "un" => quantity * 10m, // Assumir 10g por unidade
            _ => throw new ArgumentException($"Unidade '{unit}' não suportada")
        };
    }

    /// <summary>
    /// Recalcular preço de uma fórmula existente
    /// </summary>
    public async Task<bool> RecalculateFormulaPriceAsync(Guid formulaId)
    {
        var formula = await _context.CustomerFormulas
            .Include(cf => cf.ProductSubType)
            .FirstOrDefaultAsync(cf => cf.Id == formulaId);

        if (formula == null)
            return false;

        var newPrice = await CalculatePriceAsync(
            formula.ProductSubTypeId,
            formula.Quantity,
            formula.EstablishmentId,
            formula.Unit
        );

        formula.EstimatedPrice = newPrice;
        formula.FinalPrice = newPrice;
        formula.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Preço da fórmula {Code} recalculado: R$ {Price}",
            formula.Code, newPrice);

        return true;
    }
}

/// <summary>
/// Detalhamento de custos e preço final
/// </summary>
public class PriceBreakdown
{
    public decimal RawMaterialCost { get; set; }
    public decimal ManipulationCost { get; set; }
    public decimal BaseCost { get; set; }
    public decimal PriceModifier { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal QuantityInGrams { get; set; }
}