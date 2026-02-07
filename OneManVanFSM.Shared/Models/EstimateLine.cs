namespace OneManVanFSM.Shared.Models;

public class EstimateLine
{
    public int Id { get; set; }
    public int EstimateId { get; set; }
    public Estimate? Estimate { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; } // Labor, Material, Equipment, Fee, Discount
    public string? Unit { get; set; } // Each, Hour, Ft, Box, Roll
    public string? Section { get; set; } // Grouping within estimate
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
}
