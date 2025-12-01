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
    
    // Helpers para exibição
    public string ValorOrcamentosPendentesFormatado => ValorOrcamentosPendentes.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    public string TaxaConversaoFormatada => $"{TaxaConversaoOrcamentos:0.#}%";
    
    // Alertas
    public bool TemAlertasPrescricoes => PrescriptionsVencendo > 0 || PrescriptionsPendentes > 5;
    public bool TemAlertasOrcamentos => OrcamentosExpirando > 0 || OrcamentosPendentes > 10;
    public bool TemAlertasEstoque => LowStockItems > 0;
    public bool TemAlertasProducao => TotalAtrasados > 0;
}
