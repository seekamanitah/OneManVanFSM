namespace OneManVanFSM.Shared.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? PartNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public string? Unit { get; set; } // ea, ft, box, roll, etc.
    public string? Description { get; set; }
    public string? ShelfBin { get; set; } // Physical location: "Shelf B3", "Truck Drawer 2"
    public string? PreferredSupplier { get; set; }
    public InventoryLocation Location { get; set; } = InventoryLocation.Warehouse;
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; } // Reorder Point
    public decimal MaxCapacity { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MarkupPercent { get; set; }
    public DateTime? LastRestockedDate { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
}

public enum InventoryLocation
{
    Warehouse,
    Truck,
    Site,
    Other
}
