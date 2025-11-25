using Models.Pharmacy;

namespace ViewModels.Manipulation;

public class WeighingViewModel
{
    public Guid ManipulationOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string FormulaName { get; set; } = string.Empty;
    public decimal QuantityToProduce { get; set; }
    public string Unit { get; set; } = string.Empty;
    public List<ComponentWeighingItem> Components { get; set; } = new();
}

public class ComponentWeighingItem
{
    public Guid ComponentId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string DcbCode { get; set; } = string.Empty;
    public decimal UnitQuantity { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsControlled { get; set; }
    public List<AvailableBatchItem> AvailableBatches { get; set; } = new();
}

public class AvailableBatchItem
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsExpiringSoon { get; set; }
}
