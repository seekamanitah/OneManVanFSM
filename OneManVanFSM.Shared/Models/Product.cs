namespace OneManVanFSM.Shared.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; } // Carrier, Honeywell, etc.
    public string? PartNumber { get; set; } // Manufacturer part number / SKU
    public string? Barcode { get; set; } // For mobile scanning
    public string? Category { get; set; } // Ductwork, Equipment, Sealing, etc.
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MarkupPercent { get; set; }
    public string? Unit { get; set; } // Count, Boxes, Ft, Rolls, etc.
    public string? Specs { get; set; } // JSON for flexible specs
    public string? SupplierName { get; set; }
    public int LaborWarrantyYears { get; set; } = 1;
    public int PartsWarrantyYears { get; set; } = 10;
    public int CompressorWarrantyYears { get; set; } = 10;
    public bool IsTemplate { get; set; } = true;
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    public ICollection<Asset> Assets { get; set; } = [];
}
