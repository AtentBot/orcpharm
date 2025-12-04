using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using Models.Pharmacy;
using Models.Employees;
using Data;
using DTOs.Prescriptions;

namespace Service.Prescriptions;

/// <summary>
/// Serviço responsável pelo workflow de conversão de orçamento em venda
/// Fluxo: Orçamento Aprovado → Ordem de Manipulação → Venda → Pagamento → Caixa
/// </summary>
public class PrescriptionWorkflowService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PrescriptionWorkflowService> _logger;

    public PrescriptionWorkflowService(AppDbContext context, ILogger<PrescriptionWorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Processamento rápido de prescrição: Upload → OCR → Match → Orçamento
    /// Este método é chamado pelo endpoint quick-process
    /// </summary>
    public async Task<QuickProcessResultDto> ProcessPrescriptionAsync(
        QuickProcessPrescriptionDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        // TODO: Implementar lógica de processamento rápido
        // Por enquanto retorna erro indicando que não está implementado
        return new QuickProcessResultDto
        {
            Success = false,
            Message = "Processamento rápido não implementado. Use a tela de processamento manual."
        };
    }

    /// <summary>
    /// Converte um orçamento aprovado em venda completa
    /// </summary>
    public async Task<(bool Success, string Message, Guid? SaleId, Guid? ManipulationOrderId)> ConvertQuoteToSaleAsync(
        Guid quoteId,
        string paymentMethod,
        decimal amountPaid,
        int installments,
        decimal discountAmount,
        string? discountReason,
        Guid establishmentId,
        Guid employeeId,
        string? cardBrand = null,
        string? cardLastDigits = null,
        string? nsu = null,
        string? authorizationCode = null,
        string? pixTransactionId = null,
        string? observations = null)
    {
        try
        {
            // 1. Buscar e validar orçamento
            var quote = await _context.PrescriptionQuotes
                .FirstOrDefaultAsync(q => q.Id == quoteId && q.EstablishmentId == establishmentId);

            if (quote == null)
                return (false, "Orçamento não encontrado", null, null);

            if (quote.Status != "APROVADO" && quote.Status != "PENDENTE")
                return (false, $"Orçamento não pode ser convertido. Status atual: {quote.Status}", null, null);

            // Verificar se já foi convertido
            if (quote.SaleId.HasValue)
                return (false, "Este orçamento já foi convertido em venda.", null, null);

            // 2. Verificar se há caixa aberto
            var openCashRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId &&
                                         c.Status == "ABERTO" &&
                                         c.ClosingDate == null);

            if (openCashRegister == null)
                return (false, "Não há caixa aberto. Abra um caixa antes de realizar vendas.", null, null);

            // 3. Calcular valores finais
            var finalPrice = quote.FinalPrice - discountAmount;
            if (finalPrice < 0) finalPrice = 0;

            var changeAmount = 0m;
            if (paymentMethod.ToUpper() == "DINHEIRO" && amountPaid > finalPrice)
            {
                changeAmount = amountPaid - finalPrice;
            }

            // 4. Criar Ordem de Manipulação (apenas adiciona ao context, não salva)
            var order = await CreateManipulationOrderAsync(quote, employeeId, establishmentId);

            // 5. Criar Venda
            var sale = await CreateSaleAsync(quote, order, employeeId, establishmentId,
                finalPrice, discountAmount, discountReason, paymentMethod,
                amountPaid, changeAmount, observations);

            // 6. Criar Item da Venda
            await CreateSaleItemAsync(sale, order, quote);

            // 7. Criar Pagamento
            await CreateSalePaymentAsync(sale, paymentMethod, amountPaid, changeAmount,
                installments, cardBrand, cardLastDigits, nsu, authorizationCode, pixTransactionId, employeeId);

            // 8. Registrar no Caixa
            var cashMovement = new CashMovement
            {
                Id = Guid.NewGuid(),
                CashRegisterId = openCashRegister.Id,
                SaleId = sale.Id,
                MovementType = "ENTRADA",
                PaymentMethod = paymentMethod.ToUpper(),
                Amount = paymentMethod.ToUpper() == "DINHEIRO" ? (amountPaid - changeAmount) : amountPaid,
                Description = $"Venda {sale.Code} - Orçamento {quote.Code}",
                EmployeeId = employeeId,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashMovements.Add(cashMovement);

            // Atualizar totais do caixa
            UpdateCashRegisterTotals(openCashRegister, paymentMethod, amountPaid, changeAmount, finalPrice);

            // 9. Atualizar status do orçamento
            quote.Status = "CONVERTIDO";
            quote.SaleId = sale.Id;
            quote.ManipulationOrderId = order.Id;
            quote.UpdatedAt = DateTime.UtcNow;

            // 10. Atualizar status da prescrição (se existir)
            if (quote.PrescriptionId.HasValue)
            {
                var prescription = await _context.Prescriptions.FindAsync(quote.PrescriptionId.Value);
                if (prescription != null)
                {
                    prescription.Status = "EM_MANIPULACAO";
                    prescription.UpdatedAt = DateTime.UtcNow;
                }
            }

            // 11. Salvar TUDO de uma vez (atômico)
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Orçamento {QuoteId} convertido em Venda {SaleId} e OM {OrderId}",
                quoteId, sale.Id, order.Id);

            return (true, "Venda realizada com sucesso!", sale.Id, order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao converter orçamento {QuoteId} em venda", quoteId);
            return (false, $"Erro ao processar venda: {ex.Message}", null, null);
        }
    }

    /// <summary>
    /// Cria a ordem de manipulação a partir do orçamento
    /// </summary>
    private async Task<ManipulationOrder> CreateManipulationOrderAsync(
        PrescriptionQuote quote,
        Guid employeeId,
        Guid establishmentId)
    {
        var orderCode = await GenerateManipulationOrderCodeAsync(establishmentId);

        // Extrair quantidade
        decimal quantity = 1;
        if (!string.IsNullOrEmpty(quote.TotalQuantity))
        {
            var numericPart = new string(quote.TotalQuantity.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (!string.IsNullOrEmpty(numericPart))
            {
                decimal.TryParse(numericPart.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out quantity);
            }
            if (quantity <= 0) quantity = 1;
        }

        // Buscar código da prescrição se existir
        string? prescriptionCode = null;
        if (quote.PrescriptionId.HasValue)
        {
            var prescription = await _context.Prescriptions.FindAsync(quote.PrescriptionId.Value);
            prescriptionCode = prescription?.Code;
        }

        var order = new ManipulationOrder
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            OrderNumber = orderCode,
            Code = orderCode,
            PrescriptionNumber = prescriptionCode,
            PrescriberName = quote.DoctorName,
            PrescriberRegistration = quote.DoctorCrm,
            CustomerName = quote.CustomerName ?? "Cliente",
            CustomerPhone = quote.CustomerPhone,
            QuantityToProduce = quantity,
            Unit = quote.TotalQuantityUnit ?? "un",
            SpecialInstructions = quote.Instructions,
            Status = "AGUARDANDO_PRODUCAO",
            OrderDate = DateTime.UtcNow,
            ExpectedDate = DateTime.UtcNow.AddDays(quote.EstimatedDays),
            RequestedByEmployeeId = employeeId,
            PassedQualityControl = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationOrders.Add(order);
        // NÃO fazer SaveChangesAsync aqui - será feito no método principal após toda a transação

        // Criar componentes da ordem a partir dos componentes do orçamento
        if (!string.IsNullOrEmpty(quote.ComponentsJson))
        {
            try
            {
                var components = System.Text.Json.JsonSerializer.Deserialize<List<QuoteComponentData>>(
                    quote.ComponentsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (components != null)
                {
                    int index = 0;
                    foreach (var comp in components)
                    {
                        var orderComponent = new ManipulationOrderComponent
                        {
                            Id = Guid.NewGuid(),
                            ManipulationOrderId = order.Id,
                            RawMaterialId = comp.RawMaterialId,
                            RequiredQuantity = comp.Quantity,
                            Unit = comp.Unit ?? "g",
                            UnitCost = comp.UnitCost,
                            TotalCost = comp.TotalCost,
                            OrderIndex = index++,
                            Status = "PENDENTE",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.ManipulationOrderComponents.Add(orderComponent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao parsear componentes do orçamento {QuoteId}", quote.Id);
            }
        }

        return order;
    }

    /// <summary>
    /// Cria a venda vinculada ao orçamento e ordem de manipulação
    /// </summary>
    private async Task<Sale> CreateSaleAsync(
        PrescriptionQuote quote,
        ManipulationOrder order,
        Guid employeeId,
        Guid establishmentId,
        decimal finalPrice,
        decimal discountAmount,
        string? discountReason,
        string paymentMethod,
        decimal amountPaid,
        decimal changeAmount,
        string? observations)
    {
        var saleCode = await GenerateSaleCodeAsync(establishmentId);

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            Code = saleCode,
            CustomerId = quote.CustomerId,
            SaleDate = DateTime.UtcNow,
            Subtotal = quote.FinalPrice,
            DiscountAmount = discountAmount,
            TotalAmount = finalPrice,
            PaymentMethod = paymentMethod.ToUpper(),
            PaymentStatus = amountPaid >= finalPrice ? "PAGO" : "PAGAMENTO_PARCIAL",
            PaidAmount = amountPaid,
            ChangeAmount = changeAmount,
            Status = "FINALIZADA",
            Observations = string.IsNullOrEmpty(observations)
                ? $"Venda gerada do orçamento {quote.Code} - OM: {order.OrderNumber}"
                : $"{observations} | Orçamento: {quote.Code}",
            CreatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employeeId,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Sales.Add(sale);
        return sale;
    }

    /// <summary>
    /// Cria o item da venda
    /// </summary>
    private async Task CreateSaleItemAsync(Sale sale, ManipulationOrder order, PrescriptionQuote quote)
    {
        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ManipulationOrderId = order.Id,
            Description = $"{quote.PharmaceuticalForm ?? "Manipulado"} {quote.TotalQuantity} - {quote.CustomerName}",
            Quantity = 1,
            UnitPrice = quote.FinalPrice,
            TotalPrice = quote.FinalPrice,
            CostPrice = quote.MaterialsCost + quote.LaborCost + quote.PackagingCost,
            ProfitMargin = quote.FinalPrice > 0
                ? ((quote.FinalPrice - (quote.MaterialsCost + quote.LaborCost + quote.PackagingCost)) / quote.FinalPrice) * 100
                : 0
        };

        _context.SaleItems.Add(saleItem);
    }

    /// <summary>
    /// Cria o registro de pagamento
    /// </summary>
    private async Task CreateSalePaymentAsync(
        Sale sale,
        string paymentMethod,
        decimal amountPaid,
        decimal changeAmount,
        int installments,
        string? cardBrand,
        string? cardLastDigits,
        string? nsu,
        string? authorizationCode,
        string? pixTransactionId,
        Guid employeeId)
    {
        var payment = new SalePayment
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            PaymentMethod = paymentMethod.ToUpper(),
            Amount = amountPaid,
            PaymentStatus = "APPROVED",
            PaymentDate = DateTime.UtcNow,
            ProcessedByEmployeeId = employeeId,
            CreatedAt = DateTime.UtcNow
        };

        // Campos específicos por método de pagamento
        switch (paymentMethod.ToUpper())
        {
            case "DINHEIRO":
                payment.CashReceived = amountPaid;
                payment.ChangeAmount = changeAmount;
                break;

            case "CARTAO_CREDITO":
            case "CARTAO_DEBITO":
                payment.CardBrand = cardBrand;
                payment.CardLastDigits = cardLastDigits;
                payment.Nsu = nsu;
                payment.AuthorizationCode = authorizationCode;
                payment.Installments = installments;
                break;

            case "PIX":
                payment.PixTransactionId = pixTransactionId;
                break;
        }

        _context.SalePayments.Add(payment);
    }

    /// <summary>
    /// Atualiza os totais do caixa
    /// </summary>
    private void UpdateCashRegisterTotals(
        CashRegister cashRegister,
        string paymentMethod,
        decimal amountPaid,
        decimal changeAmount,
        decimal saleTotal)
    {
        switch (paymentMethod.ToUpper())
        {
            case "DINHEIRO":
                cashRegister.TotalCash += amountPaid - changeAmount;
                break;
            case "CARTAO_CREDITO":
                cashRegister.TotalCredit += amountPaid;
                cashRegister.TotalCard += amountPaid;
                break;
            case "CARTAO_DEBITO":
                cashRegister.TotalDebit += amountPaid;
                cashRegister.TotalCard += amountPaid;
                break;
            case "PIX":
                cashRegister.TotalPix += amountPaid;
                break;
            case "BOLETO":
                cashRegister.TotalBoleto += amountPaid;
                break;
            default:
                cashRegister.TotalOther += amountPaid;
                break;
        }

        cashRegister.TotalSales += saleTotal;
        cashRegister.SalesCount++;
        cashRegister.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gera código único para ordem de manipulação
    /// </summary>
    private async Task<string> GenerateManipulationOrderCodeAsync(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var dateStr = today.ToString("yyyyMMdd");

        var count = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId &&
                       o.CreatedAt.Date == today.Date)
            .CountAsync();

        return $"OM{dateStr}{(count + 1):D4}";
    }

    /// <summary>
    /// Gera código único para venda
    /// </summary>
    private async Task<string> GenerateSaleCodeAsync(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var monthStr = today.ToString("yyyyMM");

        var count = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId &&
                       s.CreatedAt.Year == today.Year &&
                       s.CreatedAt.Month == today.Month)
            .CountAsync();

        return $"VD{monthStr}{(count + 1):D4}";
    }
}

/// <summary>
/// Classe auxiliar para deserializar componentes do JSON
/// </summary>
internal class QuoteComponentData
{
    public Guid RawMaterialId { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}