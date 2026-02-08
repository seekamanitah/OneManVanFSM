namespace OneManVanFSM.Shared.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; } // Manufacturer: Carrier, Honeywell, etc.
    public string? ModelNumber { get; set; }
    public string? PartNumber { get; set; } // Manufacturer part number / SKU
    public string? ProductNumber { get; set; } // Auto-generated if blank
    public string? Barcode { get; set; } // For mobile scanning
    public string? Category { get; set; } // Ductwork, Equipment, Sealing, etc.
    public string? EquipmentType { get; set; }
    public string? FuelType { get; set; }
    public string? Description { get; set; }
    public decimal Cost { get; set; } // Wholesale Cost
    public decimal Price { get; set; } // Suggested Sell Price
    public decimal MSRP { get; set; }
    public decimal MarkupPercent { get; set; }
    public string? Unit { get; set; } // Count, Boxes, Ft, Rolls, etc.
    public string? Specs { get; set; } // JSON for flexible specs
    public string? SupplierName { get; set; }

    // Specifications
    public string? Tonnage { get; set; }
    public string? RefrigerantType { get; set; }
    public string? SEERRating { get; set; }
    public string? AFUERating { get; set; }
    public string? Voltage { get; set; }

    // Warranty
    public int LaborWarrantyYears { get; set; }
    public int PartsWarrantyYears { get; set; }
    public int CompressorWarrantyYears { get; set; }
    public bool RegistrationRequired { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public bool IsDiscontinued { get; set; }
    public bool IsTemplate { get; set; } = true;
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
    public ICollection<Asset> Assets { get; set; } = [];
}
