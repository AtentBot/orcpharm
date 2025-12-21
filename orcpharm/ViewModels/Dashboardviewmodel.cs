using Models.Employees;

namespace ViewModels;

public class DashboardViewModel
{
    public Employee Employee { get; set; } = null!;

    // Permissões
    public bool CanViewReports { get; set; }
    public bool CanManageEmployees { get; set; }
    public bool CanManageInventory { get; set; }
    public bool CanManageFormulas { get; set; }
    public bool CanManagePurchases { get; set; }

    // Estatísticas existentes
    public int TotalSuppliers { get; set; }
    public int TotalRawMaterials { get; set; }
    public int LowStockItems { get; set; }
    public int PendingPurchases { get; set; }
    public int TotalAtrasados { get; set; }

    // ========== FUNCIONÁRIOS ==========
    public int TotalEmployees { get; set; }
    public int TotalEmployeesActive { get; set; }
    public int TotalEmployeesInactive { get; set; }

    // ========== PRESCRIÇÕES ==========
    public int PrescriptionsPendentes { get; set; }
    public int PrescriptionsValidadas { get; set; }
    public int PrescriptionsHoje { get; set; }
    public int PrescriptionsVencendo { get; set; }

    // ========== ORÇAMENTOS ==========
    public int OrcamentosPendentes { get; set; }
    public int OrcamentosAprovados { get; set; }
    public int OrcamentosHoje { get; set; }
    public int OrcamentosExpirando { get; set; }
    public decimal ValorOrcamentosPendentes { get; set; }
    public decimal TaxaConversaoOrcamentos { get; set; }

    // ========== PEDIDOS ONLINE ==========

    /// <summary>
    /// Pedidos online pendentes (PENDING ou CONFIRMED)
    /// </summary>
    public int PedidosPendentes { get; set; }

    /// <summary>
    /// Pedidos online prontos para retirada (READY)
    /// </summary>
    public int PedidosProntos { get; set; }

    // ========== FISCAL (NF-e/NFC-e) ==========

    /// <summary>
    /// Notas fiscais pendentes na fila de contingência
    /// </summary>
    public int NotasFiscaisPendentes { get; set; }

    /// <summary>
    /// NF-e emitidas hoje
    /// </summary>
    public int NFesHoje { get; set; }

    /// <summary>
    /// NFC-e emitidas hoje
    /// </summary>
    public int NFCesHoje { get; set; }

    /// <summary>
    /// Faturamento fiscal do dia (notas autorizadas)
    /// </summary>
    public decimal FaturamentoFiscalHoje { get; set; }

    /// <summary>
    /// Notas fiscais com erro (rejeitadas nos últimos 7 dias)
    /// </summary>
    public int NotasFiscaisComErro { get; set; }

    /// <summary>
    /// Faturamento fiscal do mês
    /// </summary>
    public decimal FaturamentoFiscalMes { get; set; }

    /// <summary>
    /// Total de notas emitidas no mês
    /// </summary>
    public int TotalNotasMes { get; set; }

    // ========== HELPERS PARA EXIBIÇÃO ==========

    // Orçamentos
    public string ValorOrcamentosPendentesFormatado => ValorOrcamentosPendentes.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    public string TaxaConversaoFormatada => $"{TaxaConversaoOrcamentos:0.#}%";

    // Fiscal
    public string FaturamentoFiscalHojeFormatado => FaturamentoFiscalHoje.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    public string FaturamentoFiscalMesFormatado => FaturamentoFiscalMes.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    public int TotalNotasHoje => NFesHoje + NFCesHoje;

    // ========== ALERTAS ==========

    public bool TemAlertasPrescricoes => PrescriptionsVencendo > 0 || PrescriptionsPendentes > 5;
    public bool TemAlertasOrcamentos => OrcamentosExpirando > 0 || OrcamentosPendentes > 10;
    public bool TemAlertasEstoque => LowStockItems > 0;
    public bool TemAlertasProducao => TotalAtrasados > 0;
    public bool TemAlertasPedidos => PedidosPendentes > 0 || PedidosProntos > 0;
    public bool TemAlertasFiscal => NotasFiscaisPendentes > 0 || NotasFiscaisComErro > 0;
}