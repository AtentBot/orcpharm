using System.Text;
using Data;
using Microsoft.EntityFrameworkCore;
using Models.Pharmacy;

namespace Service;

/// <summary>
/// Serviço para geração de fichas de manipulação em PDF
/// </summary>
public class WeighingSheetService
{
    private readonly AppDbContext _context;

    public WeighingSheetService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gera HTML da ficha de pesagem (que pode ser convertido para PDF)
    /// </summary>
    public async Task<string> GenerateWeighingSheetHtml(Guid manipulationOrderId)
    {
        var order = await _context.ManipulationOrders
            .Include(o => o.Establishment)
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                .ThenInclude(c => c.RawMaterial)
            .Include(o => o.RequestedByEmployee)
            .FirstOrDefaultAsync(o => o.Id == manipulationOrderId);

        if (order == null)
            throw new Exception("Ordem de manipulação não encontrada");

        if (order.Formula == null || !order.Formula.Components.Any())
            throw new Exception("Fórmula não possui componentes");

        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='utf-8'>");
        html.AppendLine("<title>Ficha de Pesagem</title>");
        html.AppendLine("<style>");
        html.AppendLine(@"
            body {
                font-family: Arial, sans-serif;
                margin: 20px;
                font-size: 12px;
            }
            .header {
                text-align: center;
                border-bottom: 2px solid #333;
                padding-bottom: 10px;
                margin-bottom: 20px;
            }
            .header h1 {
                margin: 0;
                font-size: 18px;
            }
            .header p {
                margin: 5px 0;
                font-size: 10px;
            }
            .section {
                margin-bottom: 20px;
            }
            .section-title {
                background-color: #f0f0f0;
                padding: 5px;
                font-weight: bold;
                border: 1px solid #ccc;
                margin-bottom: 10px;
            }
            .info-grid {
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 10px;
                margin-bottom: 15px;
            }
            .info-item {
                display: flex;
            }
            .info-label {
                font-weight: bold;
                min-width: 120px;
            }
            table {
                width: 100%;
                border-collapse: collapse;
                margin-bottom: 20px;
            }
            th, td {
                border: 1px solid #333;
                padding: 8px;
                text-align: left;
            }
            th {
                background-color: #e0e0e0;
                font-weight: bold;
            }
            .signature-box {
                margin-top: 40px;
                page-break-inside: avoid;
            }
            .signature-line {
                border-top: 1px solid #333;
                margin-top: 50px;
                padding-top: 5px;
                text-align: center;
            }
            .footer {
                margin-top: 30px;
                padding-top: 10px;
                border-top: 1px solid #ccc;
                font-size: 10px;
                text-align: center;
            }
            .warning {
                background-color: #fff3cd;
                border: 1px solid #ffc107;
                padding: 10px;
                margin: 10px 0;
                border-radius: 4px;
            }
            @media print {
                body {
                    margin: 0;
                }
                .signature-box {
                    page-break-inside: avoid;
                }
            }
        ");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // Cabeçalho
        html.AppendLine("<div class='header'>");
        html.AppendLine($"<h1>{order.Establishment?.NomeFantasia ?? "Farmácia de Manipulação"}</h1>");
        html.AppendLine($"<p>{order.Establishment?.Street}, {order.Establishment?.Number} - {order.Establishment?.Neighborhood}, {order.Establishment?.City} - {order.Establishment?.State}</p>");
        html.AppendLine($"<p>CNPJ: {order.Establishment?.Cnpj} | Telefone: {order.Establishment?.Phone}</p>");
        html.AppendLine("<h2 style='margin-top: 10px;'>FICHA DE PESAGEM</h2>");
        html.AppendLine("</div>");

        // Informações da Ordem
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>INFORMAÇÕES DA ORDEM</div>");
        html.AppendLine("<div class='info-grid'>");
        html.AppendLine($"<div class='info-item'><span class='info-label'>Nº da Ordem:</span><span>{order.OrderNumber}</span></div>");
        html.AppendLine($"<div class='info-item'><span class='info-label'>Data:</span><span>{order.OrderDate:dd/MM/yyyy}</span></div>");
        html.AppendLine($"<div class='info-item'><span class='info-label'>Cliente:</span><span>{order.CustomerName}</span></div>");
        html.AppendLine($"<div class='info-item'><span class='info-label'>Fórmula:</span><span>{order.Formula?.Name ?? "N/A"}</span></div>");
        html.AppendLine($"<div class='info-item'><span class='info-label'>Quantidade:</span><span>{order.QuantityToProduce} {order.Unit}</span></div>");
        html.AppendLine($"<div class='info-item'><span class='info-label'>Solicitante:</span><span>{order.RequestedByEmployee?.FullName ?? "N/A"}</span></div>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        // Prescrição (se houver)
        if (!string.IsNullOrEmpty(order.PrescriptionNumber))
        {
            html.AppendLine("<div class='section'>");
            html.AppendLine("<div class='section-title'>PRESCRIÇÃO</div>");
            html.AppendLine("<div class='info-grid'>");
            html.AppendLine($"<div class='info-item'><span class='info-label'>Nº Prescrição:</span><span>{order.PrescriptionNumber}</span></div>");
            html.AppendLine($"<div class='info-item'><span class='info-label'>Prescritor:</span><span>{order.PrescriberName ?? "N/A"}</span></div>");
            html.AppendLine($"<div class='info-item'><span class='info-label'>CRM/Registro:</span><span>{order.PrescriberRegistration ?? "N/A"}</span></div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }

        // Instruções Especiais
        if (!string.IsNullOrEmpty(order.SpecialInstructions))
        {
            html.AppendLine("<div class='warning'>");
            html.AppendLine($"<strong>INSTRUÇÕES ESPECIAIS:</strong> {order.SpecialInstructions}");
            html.AppendLine("</div>");
        }

        // Tabela de Componentes
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>COMPONENTES A PESAR</div>");
        html.AppendLine("<table>");
        html.AppendLine("<thead>");
        html.AppendLine("<tr>");
        html.AppendLine("<th style='width: 5%;'>Ordem</th>");
        html.AppendLine("<th style='width: 30%;'>Componente</th>");
        html.AppendLine("<th style='width: 10%;'>Qtd. Teórica</th>");
        html.AppendLine("<th style='width: 10%;'>Unidade</th>");
        html.AppendLine("<th style='width: 15%;'>Lote Utilizado</th>");
        html.AppendLine("<th style='width: 10%;'>Qtd. Real</th>");
        html.AppendLine("<th style='width: 20%;'>Assinatura</th>");
        html.AppendLine("</tr>");
        html.AppendLine("</thead>");
        html.AppendLine("<tbody>");

        var components = order.Formula.Components.OrderBy(c => c.OrderIndex).ToList();
        foreach (var component in components)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td style='text-align: center;'>{component.OrderIndex}</td>");
            html.AppendLine($"<td>{component.RawMaterial?.Name ?? "N/A"}</td>");
            html.AppendLine($"<td style='text-align: right;'>{component.Quantity:F3}</td>");
            html.AppendLine($"<td>{component.Unit}</td>");
            html.AppendLine($"<td style='height: 30px;'></td>"); // Espaço para preencher lote
            html.AppendLine($"<td style='height: 30px;'></td>"); // Espaço para preencher quantidade real
            html.AppendLine($"<td style='height: 30px;'></td>"); // Espaço para assinatura
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody>");
        html.AppendLine("</table>");
        html.AppendLine("</div>");

        // Observações
        html.AppendLine("<div class='section'>");
        html.AppendLine("<div class='section-title'>OBSERVAÇÕES</div>");
        html.AppendLine("<div style='border: 1px solid #ccc; min-height: 80px; padding: 10px;'>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        // Assinaturas
        html.AppendLine("<div class='signature-box'>");
        html.AppendLine("<div style='display: grid; grid-template-columns: 1fr 1fr; gap: 40px;'>");

        html.AppendLine("<div>");
        html.AppendLine("<div class='signature-line'>Manipulador</div>");
        html.AppendLine("<p style='text-align: center; margin-top: 5px; font-size: 10px;'>Nome: __________________ Data: ___/___/___</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div>");
        html.AppendLine("<div class='signature-line'>Conferente</div>");
        html.AppendLine("<p style='text-align: center; margin-top: 5px; font-size: 10px;'>Nome: __________________ Data: ___/___/___</p>");
        html.AppendLine("</div>");

        html.AppendLine("</div>");
        html.AppendLine("</div>");

        // Rodapé
        html.AppendLine("<div class='footer'>");
        html.AppendLine($"Documento gerado em: {DateTime.Now:dd/MM/yyyy HH:mm} | Sistema OrcPharm");
        html.AppendLine("</div>");

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    /// <summary>
    /// Calcula desvio percentual entre quantidade teórica e real
    /// </summary>
    public static decimal CalculateDeviation(decimal theoretical, decimal actual)
    {
        if (theoretical == 0)
            return 0;

        return Math.Round(((actual - theoretical) / theoretical) * 100, 2);
    }

    /// <summary>
    /// Verifica se o desvio está dentro do limite aceitável (padrão: ±5%)
    /// </summary>
    public static bool IsDeviationAcceptable(decimal deviationPercentage, decimal maxDeviation = 5.0m)
    {
        return Math.Abs(deviationPercentage) <= maxDeviation;
    }
}