using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using System.Text;

namespace Controllers.Api;

/// <summary>
/// API de Geração de Cupom Não-Fiscal
/// Prepara dados para impressão térmica ou PDF
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CupomController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CupomController> _logger;

    public CupomController(AppDbContext db, ILogger<CupomController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var estId) && estId is Guid id)
            return id;
        return Guid.Parse("e0000000-0000-0000-0000-000000000001");
    }

    /// <summary>
    /// Gera cupom não-fiscal para uma venda
    /// GET /api/cupom/venda/{saleId}
    /// </summary>
    [HttpGet("venda/{saleId:guid}")]
    public async Task<IActionResult> GerarCupomVenda(Guid saleId)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var venda = await _db.Sales
                .Include(s => s.Customer)
                .Include(s => s.Payments)
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == saleId && s.EstablishmentId == establishmentId);

            if (venda == null)
                return NotFound(new { error = "Venda não encontrada" });

            var estabelecimento = await _db.Establishments.FindAsync(establishmentId);

            // Montar endereço completo
            var endereco = estabelecimento != null 
                ? $"{estabelecimento.Street}, {estabelecimento.Number} - {estabelecimento.Neighborhood}, {estabelecimento.City}/{estabelecimento.State}"
                : null;

            var cupom = new CupomDto
            {
                Tipo = "VENDA",
                Numero = venda.Code ?? venda.Id.ToString()[..8].ToUpper(),
                Data = venda.CreatedAt,
                
                Estabelecimento = new EstabelecimentoCupomDto
                {
                    Nome = estabelecimento?.NomeFantasia ?? estabelecimento?.RazaoSocial ?? "OrcPharm",
                    RazaoSocial = estabelecimento?.RazaoSocial,
                    CNPJ = estabelecimento?.Cnpj,
                    Endereco = endereco,
                    Telefone = estabelecimento?.Phone
                },
                
                Cliente = venda.Customer != null ? new ClienteCupomDto
                {
                    Nome = venda.Customer.FullName ?? "Cliente",
                    CPF = venda.Customer.Cpf,
                    Telefone = venda.Customer.Phone ?? venda.Customer.WhatsApp
                } : null,
                
                Itens = venda.Items?.Select(i => new ItemCupomDto
                {
                    Descricao = i.Description ?? "Item",
                    Quantidade = i.Quantity,
                    Unidade = "UN",
                    ValorUnitario = i.UnitPrice,
                    ValorTotal = i.TotalPrice
                }).ToList() ?? new List<ItemCupomDto>(),
                
                Subtotal = venda.Subtotal,
                Desconto = venda.DiscountAmount,  // CORRIGIDO: DiscountAmount ao invés de DiscountValue
                Total = venda.TotalAmount,
                
                Pagamentos = venda.Payments?.Select(p => new PagamentoCupomDto
                {
                    Forma = FormatarFormaPagamento(p.PaymentMethod),
                    Valor = p.Amount
                }).ToList() ?? new List<PagamentoCupomDto>(),
                
                Observacoes = venda.Observations,  // CORRIGIDO: Observations ao invés de Notes
                Rodape = "CUPOM NÃO FISCAL - NÃO TEM VALOR FISCAL\n" +
                         "Obrigado pela preferência!"
            };

            return Ok(cupom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar cupom para venda {SaleId}", saleId);
            return StatusCode(500, new { error = "Erro ao gerar cupom" });
        }
    }

    /// <summary>
    /// Gera cupom para ordem de manipulação
    /// GET /api/cupom/ordem/{orderId}
    /// </summary>
    [HttpGet("ordem/{orderId:guid}")]
    public async Task<IActionResult> GerarCupomOrdem(Guid orderId)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            // ManipulationOrder NÃO tem CustomerId - usa CustomerName diretamente
            var ordem = await _db.ManipulationOrders
                .Include(o => o.Formula)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

            if (ordem == null)
                return NotFound(new { error = "Ordem não encontrada" });

            var estabelecimento = await _db.Establishments.FindAsync(establishmentId);
            
            // Buscar preço do orçamento aprovado (ManipulationOrder não tem FinalPrice/EstimatedPrice)
            var quote = await _db.PrescriptionQuotes
                .Where(q => q.ManipulationOrderId == orderId && 
                           (q.Status == "APROVADO" || q.Status == "CONVERTIDO"))
                .FirstOrDefaultAsync();
            
            var valorTotal = quote?.FinalPrice ?? 0;

            // Montar endereço completo
            var endereco = estabelecimento != null 
                ? $"{estabelecimento.Street}, {estabelecimento.Number} - {estabelecimento.Neighborhood}, {estabelecimento.City}/{estabelecimento.State}"
                : null;

            var cupom = new CupomDto
            {
                Tipo = "ORDEM_MANIPULACAO",
                Numero = ordem.OrderNumber,
                Data = ordem.CreatedAt,
                
                Estabelecimento = new EstabelecimentoCupomDto
                {
                    // CORRIGIDO: NomeFantasia e RazaoSocial (não TradeName/LegalName)
                    Nome = estabelecimento?.NomeFantasia ?? estabelecimento?.RazaoSocial ?? "OrcPharm",
                    RazaoSocial = estabelecimento?.RazaoSocial,
                    CNPJ = estabelecimento?.Cnpj,
                    Endereco = endereco,
                    Telefone = estabelecimento?.Phone
                },
                
                // CORRIGIDO: ManipulationOrder tem CustomerName diretamente (não CustomerId)
                Cliente = new ClienteCupomDto 
                { 
                    Nome = ordem.CustomerName ?? "Cliente",
                    Telefone = ordem.CustomerPhone
                },
                
                Itens = new List<ItemCupomDto>
                {
                    new ItemCupomDto
                    {
                        // CORRIGIDO: Usa Formula.Name (não ProductDescription)
                        Descricao = ordem.Formula?.Name ?? "Fórmula Manipulada",
                        // CORRIGIDO: QuantityToProduce (não Quantity)
                        Quantidade = ordem.QuantityToProduce,
                        Unidade = ordem.Unit ?? "UN",
                        ValorUnitario = ordem.QuantityToProduce > 0 ? valorTotal / ordem.QuantityToProduce : valorTotal,
                        ValorTotal = valorTotal
                    }
                },
                
                Total = valorTotal,
                
                DadosAdicionais = new Dictionary<string, string>
                {
                    { "Status", ordem.Status },
                    // CORRIGIDO: ExpectedDate (não DueDate)
                    { "Previsão de Entrega", ordem.ExpectedDate.ToString("dd/MM/yyyy") }
                },
                
                // CORRIGIDO: SpecialInstructions (não Notes)
                Observacoes = ordem.SpecialInstructions,
                Rodape = "COMPROVANTE DE PEDIDO - NÃO TEM VALOR FISCAL\n" +
                         $"Previsão de entrega: {ordem.ExpectedDate:dd/MM/yyyy}"
            };

            return Ok(cupom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar cupom para ordem {OrderId}", orderId);
            return StatusCode(500, new { error = "Erro ao gerar cupom" });
        }
    }

    /// <summary>
    /// Gera texto formatado para impressora térmica (80mm)
    /// GET /api/cupom/venda/{saleId}/texto
    /// </summary>
    [HttpGet("venda/{saleId:guid}/texto")]
    public async Task<IActionResult> GerarTextoTermico(Guid saleId)
    {
        try
        {
            var result = await GerarCupomVenda(saleId);
            if (result is NotFoundObjectResult)
                return result;

            var okResult = result as OkObjectResult;
            var cupom = okResult?.Value as CupomDto;
            if (cupom == null)
                return StatusCode(500, new { error = "Erro ao processar cupom" });

            var texto = GerarTextoTermico80mm(cupom);
            return Content(texto, "text/plain", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar texto térmico");
            return StatusCode(500, new { error = "Erro ao gerar texto" });
        }
    }

    #region Helpers

    private string FormatarFormaPagamento(string? metodo)
    {
        return metodo?.ToUpper() switch
        {
            "DINHEIRO" => "Dinheiro",
            "CARTAO_CREDITO" => "Cartão de Crédito",
            "CARTAO_DEBITO" => "Cartão de Débito",
            "PIX" => "PIX",
            "BOLETO" => "Boleto",
            "TRANSFERENCIA" => "Transferência",
            _ => metodo ?? "Outros"
        };
    }

    private string GerarTextoTermico80mm(CupomDto cupom)
    {
        const int LARGURA = 48;
        var sb = new StringBuilder();

        sb.AppendLine(Centralizar(cupom.Estabelecimento.Nome?.ToUpper() ?? "ORCPHARM", LARGURA));
        if (!string.IsNullOrEmpty(cupom.Estabelecimento.CNPJ))
            sb.AppendLine(Centralizar($"CNPJ: {cupom.Estabelecimento.CNPJ}", LARGURA));
        
        sb.AppendLine(new string('-', LARGURA));
        sb.AppendLine(Centralizar("CUPOM NÃO FISCAL", LARGURA));
        sb.AppendLine(new string('-', LARGURA));

        sb.AppendLine($"Nº: {cupom.Numero}");
        sb.AppendLine($"Data: {cupom.Data:dd/MM/yyyy HH:mm}");
        
        if (cupom.Cliente != null)
            sb.AppendLine($"Cliente: {cupom.Cliente.Nome}");

        sb.AppendLine(new string('-', LARGURA));

        foreach (var item in cupom.Itens)
        {
            sb.AppendLine(item.Descricao);
            sb.AppendLine($"  {item.Quantidade:N2} x R$ {item.ValorUnitario:N2} = R$ {item.ValorTotal:N2}");
        }

        sb.AppendLine(new string('-', LARGURA));
        sb.AppendLine(AlinharDireita($"TOTAL: R$ {cupom.Total:N2}", LARGURA));

        if (cupom.Pagamentos.Any())
        {
            sb.AppendLine(new string('-', LARGURA));
            foreach (var pag in cupom.Pagamentos)
                sb.AppendLine($"  {pag.Forma}: R$ {pag.Valor:N2}");
        }

        sb.AppendLine(new string('-', LARGURA));
        sb.AppendLine(Centralizar(cupom.Rodape ?? "", LARGURA));

        return sb.ToString();
    }

    private string Centralizar(string texto, int largura)
    {
        if (texto.Length >= largura) return texto[..largura];
        var espacos = (largura - texto.Length) / 2;
        return new string(' ', espacos) + texto;
    }

    private string AlinharDireita(string texto, int largura)
    {
        if (texto.Length >= largura) return texto[..largura];
        return new string(' ', largura - texto.Length) + texto;
    }

    #endregion
}

// DTOs
public class CupomDto
{
    public string Tipo { get; set; } = "";
    public string Numero { get; set; } = "";
    public DateTime Data { get; set; }
    public EstabelecimentoCupomDto Estabelecimento { get; set; } = new();
    public ClienteCupomDto? Cliente { get; set; }
    public List<ItemCupomDto> Itens { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public List<PagamentoCupomDto> Pagamentos { get; set; } = new();
    public Dictionary<string, string>? DadosAdicionais { get; set; }
    public string? Observacoes { get; set; }
    public string? Rodape { get; set; }
}

public class EstabelecimentoCupomDto
{
    public string? Nome { get; set; }
    public string? RazaoSocial { get; set; }
    public string? CNPJ { get; set; }
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }
}

public class ClienteCupomDto
{
    public string? Nome { get; set; }
    public string? CPF { get; set; }
    public string? Telefone { get; set; }
}

public class ItemCupomDto
{
    public string Descricao { get; set; } = "";
    public decimal Quantidade { get; set; }
    public string Unidade { get; set; } = "UN";
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}

public class PagamentoCupomDto
{
    public string Forma { get; set; } = "";
    public decimal Valor { get; set; }
}
