using Data;
using DTOs;
using DTOs.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using Models.Pharmacy;
using Service;
using System.Text.Json;

namespace Service.Prescriptions;

/// <summary>
/// Serviço que orquestra todo o fluxo de prescrição:
/// Upload → OCR → Match → Orçamento → Aprovação → Venda
/// </summary>
public class PrescriptionWorkflowService
{
    private readonly AppDbContext _context;
    private readonly OpenAIPrescriptionParserService _ocrService;
    private readonly Service.IngredientMatcherService _matcherService;
    private readonly PrescriptionQuoteService _quoteService;
    private readonly ILogger<PrescriptionWorkflowService> _logger;

    public PrescriptionWorkflowService(
        AppDbContext context,
        OpenAIPrescriptionParserService ocrService,
        Service.IngredientMatcherService matcherService,
        PrescriptionQuoteService quoteService,
        ILogger<PrescriptionWorkflowService> logger)
    {
        _context = context;
        _ocrService = ocrService;
        _matcherService = matcherService;
        _quoteService = quoteService;
        _logger = logger;
    }

    /// <summary>
    /// Processa receita completa: Upload → OCR → Match → Orçamento
    /// </summary>
    public async Task<QuickProcessResultDto> ProcessPrescriptionAsync(
        QuickProcessPrescriptionDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        var result = new QuickProcessResultDto();
        var warnings = new List<string>();
        var unmatchedIngredients = new List<string>();

        try
        {
            _logger.LogInformation("Iniciando processamento de receita para estabelecimento {EstablishmentId}", establishmentId);

            // 1. Criar prescrição base
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                Code = await GeneratePrescriptionCodeAsync(establishmentId),
                CustomerId = dto.CustomerId ?? Guid.Empty,
                PrescriptionDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                DoctorName = "Aguardando OCR",
                PrescriptionType = "COMUM",
                Status = "PENDENTE",
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();
            result.PrescriptionId = prescription.Id;

            // 2. Salvar arquivo
            var fileBytes = Convert.FromBase64String(dto.ImageBase64);
            var prescriptionFile = new PrescriptionFile
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                FileName = $"receita_{DateTime.Now:yyyyMMddHHmmss}.{GetExtension(dto.ImageType)}",
                FileType = dto.ImageType,
                FileBase64 = dto.ImageBase64,
                FileSizeBytes = fileBytes.Length,
                UploadedAt = DateTime.UtcNow,
                UploadedByEmployeeId = employeeId,
                OcrStatus = "PROCESSING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PrescriptionFiles.Add(prescriptionFile);
            await _context.SaveChangesAsync();
            result.PrescriptionFileId = prescriptionFile.Id;

            // 3. OCR com OpenAI
            _logger.LogInformation("Processando OCR...");
            try
            {
                result.OcrResult = await _ocrService.ParsePrescriptionAsync(dto.ImageBase64, dto.ImageType);

                prescriptionFile.OcrStatus = "COMPLETED";
                prescriptionFile.OcrProcessedAt = DateTime.UtcNow;
                prescriptionFile.OcrResult = JsonSerializer.Serialize(result.OcrResult);
                prescriptionFile.OcrConfidence = (decimal)result.OcrResult.OverallConfidence;

                // Atualizar prescrição com dados do OCR
                if (result.OcrResult.Doctor != null)
                {
                    prescription.DoctorName = result.OcrResult.Doctor.Name;
                    prescription.DoctorCrm = result.OcrResult.Doctor.Crm;
                    prescription.DoctorCrmState = result.OcrResult.Doctor.CrmState;
                }

                if (!string.IsNullOrEmpty(result.OcrResult.PrescriptionDate))
                {
                    if (DateTime.TryParse(result.OcrResult.PrescriptionDate, out var prescDate))
                        prescription.PrescriptionDate = prescDate;
                }

                prescription.Medications = string.Join("; ", result.OcrResult.Items?.Select(i => $"{i.Name} {i.Quantity}{i.Unit}") ?? Array.Empty<string>());
                prescription.Posology = result.OcrResult.Instructions;
                prescription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                if (result.OcrResult.OverallConfidence < 0.5)
                {
                    warnings.Add("Baixa confiança na leitura. Recomenda-se revisão manual.");
                    result.RequiresManualReview = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no OCR");
                prescriptionFile.OcrStatus = "FAILED";
                prescriptionFile.OcrErrorMessage = ex.Message;
                await _context.SaveChangesAsync();

                result.Success = false;
                result.Message = $"Erro no OCR: {ex.Message}";
                return result;
            }

            // 4. Match de ingredientes
            if (result.OcrResult.Items?.Any() == true)
            {
                _logger.LogInformation("Fazendo match de {Count} ingredientes...", result.OcrResult.Items.Count);

                // Converter OcrIngredientDto para OcrItemDto (DTOs)
                var ocrItems = result.OcrResult.Items.Select(i => new DTOs.OcrItemDto
                {
                    Component = i.Name,
                    RawText = i.RawText,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Confidence = (decimal)i.Confidence
                }).ToList();

                // Chamar o matcher existente
                var matchResponse = await _matcherService.FindMatchesAsync(ocrItems);

                // Converter IngredientMatchDto para IngredientMatchResultDto
                result.IngredientMatches = matchResponse.Matches.Select(m => new IngredientMatchResultDto
                {
                    OcrText = m.OcrText,
                    OcrQuantity = m.Quantity ?? "",
                    OcrUnit = m.Unit ?? "",
                    Suggestions = m.Suggestions.Select(s => new OcrRawMaterialMatchDto
                    {
                        RawMaterialId = s.RawMaterialId,
                        Code = s.Code,
                        Name = s.Name,
                        DcbCode = s.DcbCode,
                        Confidence = (double)s.Confidence,
                        InStock = s.InStock,
                        AvailableQuantity = s.AvailableQuantity,
                        Unit = s.Unit,
                        UnitCost = s.UnitCost,
                        ControlType = s.ControlType
                    }).ToList(),
                    BestMatch = m.Suggestions.FirstOrDefault() != null ? new OcrRawMaterialMatchDto
                    {
                        RawMaterialId = m.Suggestions.First().RawMaterialId,
                        Code = m.Suggestions.First().Code,
                        Name = m.Suggestions.First().Name,
                        DcbCode = m.Suggestions.First().DcbCode,
                        Confidence = (double)m.Suggestions.First().Confidence,
                        InStock = m.Suggestions.First().InStock,
                        AvailableQuantity = m.Suggestions.First().AvailableQuantity,
                        Unit = m.Suggestions.First().Unit,
                        UnitCost = m.Suggestions.First().UnitCost,
                        ControlType = m.Suggestions.First().ControlType
                    } : null
                }).ToList();

                foreach (var match in result.IngredientMatches)
                {
                    if (!match.HasMatch)
                    {
                        unmatchedIngredients.Add(match.OcrText);
                        result.RequiresManualReview = true;
                    }
                    else if (!match.BestMatch!.InStock)
                    {
                        warnings.Add($"'{match.BestMatch.Name}' sem estoque disponível.");
                    }
                }

                if (unmatchedIngredients.Any())
                    warnings.Add($"{unmatchedIngredients.Count} ingrediente(s) não encontrado(s) no cadastro.");
            }
            else
            {
                warnings.Add("Nenhum ingrediente identificado na receita.");
                result.RequiresManualReview = true;
            }

            // 5. Criar orçamento automaticamente (se configurado e possível)
            if (dto.AutoCreateQuote && result.IngredientMatches?.Any(m => m.HasMatch) == true)
            {
                _logger.LogInformation("Criando orçamento automaticamente...");

                var confirmedIngredients = result.IngredientMatches
                    .Where(m => m.HasMatch && m.BestMatch != null)
                    .Select(m => new OcrConfirmedItemDto
                    {
                        RawMaterialId = m.BestMatch!.RawMaterialId,
                        RawMaterialName = m.BestMatch.Name,
                        Quantity = ParseQuantity(m.OcrQuantity),
                        Unit = m.OcrUnit,
                        IsQsp = m.OcrQuantity.ToLower().Contains("qsp")
                    })
                    .ToList();

                var quoteDto = new CreateQuoteFromOcrDto
                {
                    PrescriptionId = prescription.Id,
                    CustomerId = dto.CustomerId,
                    CustomerName = dto.CustomerName ?? "Cliente",
                    CustomerPhone = dto.CustomerPhone,
                    CustomerEmail = dto.CustomerEmail,
                    Doctor = result.OcrResult.Doctor ?? new OcrDoctorInfo(),
                    Usage = result.OcrResult.Patient?.Usage ?? "",
                    PharmaceuticalForm = result.OcrResult.PharmaceuticalForm ?? "",
                    TotalQuantity = result.OcrResult.TotalQuantity ?? "",
                    Instructions = result.OcrResult.Instructions ?? "",
                    Ingredients = confirmedIngredients
                };

                var (success, message, quote) = await _quoteService.CreateQuoteAsync(
                    quoteDto, establishmentId, employeeId);

                if (success && quote != null)
                {
                    result.Quote = quote;
                    result.QuoteId = quote.Id;
                    _logger.LogInformation("Orçamento {Code} criado com sucesso", quote.Code);
                }
                else
                {
                    warnings.Add($"Falha ao criar orçamento: {message}");
                }
            }

            result.Success = true;
            result.Message = result.RequiresManualReview
                ? "Processado com pendências de revisão"
                : "Processado com sucesso";
            result.Warnings = warnings;
            result.UnmatchedIngredients = unmatchedIngredients;

            _logger.LogInformation("Processamento concluído. Sucesso: {Success}, Revisão necessária: {Review}",
                result.Success, result.RequiresManualReview);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no processamento de receita");
            result.Success = false;
            result.Message = $"Erro no processamento: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Converte orçamento aprovado em venda e ordem de manipulação
    /// </summary>
    public async Task<(bool Success, string Message, Guid? SaleId, Guid? ManipulationOrderId)> ConvertQuoteToSaleAsync(
        Guid quoteId,
        string paymentMethod,
        decimal amountPaid,
        int installments,
        decimal? discountAmount,
        string? discountReason,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            var quote = await _context.PrescriptionQuotes
                .FirstOrDefaultAsync(q => q.Id == quoteId && q.EstablishmentId == establishmentId);

            if (quote == null)
                return (false, "Orçamento não encontrado", null, null);

            if (quote.Status != "APROVADO")
                return (false, "Apenas orçamentos aprovados podem ser convertidos", null, null);

            // Carregar componentes
            var components = JsonSerializer.Deserialize<List<QuoteComponent>>(quote.ComponentsJson);
            if (components == null || !components.Any())
                return (false, "Orçamento sem componentes", null, null);

            // Calcular valor final com desconto
            var finalValue = quote.FinalPrice;
            if (discountAmount.HasValue && discountAmount.Value > 0)
            {
                finalValue -= discountAmount.Value;
            }

            // 1. Criar venda
            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                Code = await GenerateSaleCodeAsync(establishmentId),
                CustomerId = quote.CustomerId,
                SaleDate = DateTime.UtcNow,
                Subtotal = quote.Subtotal,
                DiscountPercentage = discountAmount.HasValue ? (discountAmount.Value / quote.Subtotal * 100) : 0,
                DiscountAmount = discountAmount ?? 0,
                TotalAmount = finalValue,
                PaymentMethod = paymentMethod,
                PaymentStatus = amountPaid >= finalValue ? "PAGO" : "PARCIAL",
                PaidAmount = amountPaid,
                ChangeAmount = amountPaid > finalValue ? amountPaid - finalValue : 0,
                PaymentDate = DateTime.UtcNow,
                Status = "FINALIZADA",
                Observations = $"Orçamento: {quote.Code}. {discountReason}",
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sales.Add(sale);

            // 2. Criar item da venda
            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                Description = $"Fórmula Magistral - {quote.PharmaceuticalForm} {quote.TotalQuantity}",
                Quantity = 1,
                UnitPrice = finalValue,
                TotalPrice = finalValue,
                CostPrice = quote.MaterialsCost,
                ProfitMargin = finalValue > 0 ? ((finalValue - quote.MaterialsCost) / finalValue * 100) : 0
            };

            _context.SaleItems.Add(saleItem);

            // 3. Buscar nome do cliente
            var customerName = quote.CustomerName;
            var customerPhone = quote.CustomerPhone;
            if (quote.CustomerId.HasValue)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == quote.CustomerId.Value);
                if (customer != null)
                {
                    customerName = customer.FullName;
                    customerPhone = customer.Phone;
                }
            }

            // 4. Criar ordem de manipulação
            var orderNumber = await GenerateManipulationCodeAsync(establishmentId);
            var manipulationOrder = new ManipulationOrder
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                Code = orderNumber,
                OrderNumber = orderNumber,
                PrescriptionNumber = quote.Code,
                PrescriberName = quote.DoctorName,
                PrescriberRegistration = quote.DoctorCrm,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                QuantityToProduce = quote.TotalQuantityNumeric,
                Unit = quote.TotalQuantityUnit ?? "un",
                SpecialInstructions = quote.Instructions,
                Status = "PENDENTE",
                OrderDate = DateTime.UtcNow,
                ExpectedDate = DateTime.UtcNow.AddDays(quote.EstimatedDays),
                RequestedByEmployeeId = employeeId,
                QualityNotes = $"Gerado do orçamento {quote.Code}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ManipulationOrders.Add(manipulationOrder);

            // 5. Criar componentes da ordem
            var orderIndex = 1;
            foreach (var comp in components)
            {
                var orderComponent = new ManipulationOrderComponent
                {
                    Id = Guid.NewGuid(),
                    ManipulationOrderId = manipulationOrder.Id,
                    RawMaterialId = comp.RawMaterialId,
                    RequiredQuantity = comp.Quantity,
                    Unit = comp.Unit,
                    UnitCost = comp.UnitCost,
                    TotalCost = comp.TotalCost,
                    OrderIndex = orderIndex++,
                    Status = "PENDENTE",
                    CreatedAt = DateTime.UtcNow
                };

                _context.ManipulationOrderComponents.Add(orderComponent);
            }

            // 6. Atualizar orçamento
            quote.Status = "CONVERTIDO";
            quote.SaleId = sale.Id;
            quote.ManipulationOrderId = manipulationOrder.Id;
            quote.UpdatedAt = DateTime.UtcNow;
            quote.UpdatedByEmployeeId = employeeId;

            // 7. Atualizar prescrição (se existir)
            if (quote.PrescriptionId.HasValue)
            {
                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.Id == quote.PrescriptionId.Value);

                if (prescription != null)
                {
                    prescription.Status = "VALIDADA";
                    prescription.ManipulationOrderId = manipulationOrder.Id;
                    prescription.ManipulationGeneratedAt = DateTime.UtcNow;
                    prescription.UpdatedAt = DateTime.UtcNow;
                    prescription.UpdatedByEmployeeId = employeeId;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Orçamento {QuoteCode} convertido. Venda: {SaleCode}, OM: {OmCode}",
                quote.Code, sale.Code, manipulationOrder.Code);

            return (true, "Venda e ordem de manipulação criadas com sucesso", sale.Id, manipulationOrder.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao converter orçamento em venda");
            return (false, $"Erro: {ex.Message}", null, null);
        }
    }

    // Helpers
    private async Task<string> GeneratePrescriptionCodeAsync(Guid establishmentId)
    {
        var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var tomorrowUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);
        var count = await _context.Prescriptions
            .CountAsync(p => p.EstablishmentId == establishmentId &&
                            p.CreatedAt >= todayUtc &&
                            p.CreatedAt < tomorrowUtc);
        return $"RX{todayUtc:yyyyMMdd}{(count + 1):D4}";
    }

    private async Task<string> GenerateSaleCodeAsync(Guid establishmentId)
    {
        var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var tomorrowUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);
        var count = await _context.Sales
            .CountAsync(s => s.EstablishmentId == establishmentId &&
                            s.CreatedAt >= todayUtc &&
                            s.CreatedAt < tomorrowUtc);
        return $"VND{todayUtc:yyyyMMdd}{(count + 1):D4}";
    }

    private async Task<string> GenerateManipulationCodeAsync(Guid establishmentId)
    {
        var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var tomorrowUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);
        var count = await _context.ManipulationOrders
            .CountAsync(m => m.EstablishmentId == establishmentId &&
                            m.CreatedAt >= todayUtc &&
                            m.CreatedAt < tomorrowUtc);
        return $"OM{todayUtc:yyyyMMdd}{(count + 1):D4}";
    }

    private string GetExtension(string mimeType) => mimeType.ToLower() switch
    {
        "image/jpeg" or "image/jpg" => "jpg",
        "image/png" => "png",
        "application/pdf" => "pdf",
        _ => "jpg"
    };

    private decimal ParseQuantity(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var numbers = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        numbers = numbers.Replace(',', '.');
        return decimal.TryParse(numbers, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}