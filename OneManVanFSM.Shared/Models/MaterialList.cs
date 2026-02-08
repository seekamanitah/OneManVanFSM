namespace OneManVanFSM.Shared.Models;

public class MaterialList
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsTemplate { get; set; }
    public MaterialListStatus Status { get; set; } = MaterialListStatus.Draft;
    public string? TradeType { get; set; } = "HVAC"; // HVAC, Plumbing, Electrical, General
    public PricingMethod PricingMethod { get; set; } = PricingMethod.TimeBased;
    public decimal Subtotal { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? ExternalNotes { get; set; }
    public string? PONumber { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    public ICollection<MaterialListItem> Items { get; set; } = [];
}

public class MaterialListItem
{
    public int Id { get; set; }
    public int MaterialListId { get; set; }
    public MaterialList MaterialList { get; set; } = null!;
    public string Section { get; set; } = string.Empty; // Ductwork, Grills, Sealing, etc.
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal BaseCost { get; set; }
    public decimal? LaborHours { get; set; }
    public decimal? FlatPrice { get; set; }
    public decimal MarkupPercent { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public int? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
}

public enum MaterialListStatus
{
    Draft,
    Approved,
    Ordered
}
