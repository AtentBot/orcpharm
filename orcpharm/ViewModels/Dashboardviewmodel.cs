using Models.Employees;

namespace ViewModels;

public class DashboardViewModel
{
    public Employee Employee { get; set; } = null!;
    public bool CanViewReports { get; set; }
    public bool CanManageEmployees { get; set; }
    public bool CanManageInventory { get; set; }
    public bool CanManageFormulas { get; set; }
    public bool CanManagePurchases { get; set; }
    public int TotalSuppliers { get; set; }
    public int TotalRawMaterials { get; set; }
    public int LowStockItems { get; set; }
    public int PendingPurchases { get; set; }
}