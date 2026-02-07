namespace OneManVanFSM.Shared.Models;

public class Asset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetType { get; set; } // Furnace, AC Unit, etc. - customizable
    public string? Brand { get; set; } // Carrier, Lennox, Trane, etc.
    public string? FuelType { get; set; } // Natural Gas, Propane, Electric, Oil, Dual Fuel
    public string? UnitConfiguration { get; set; } // Split, Packaged, Mini-Split, Heat Pump, etc.
    public int? BTURating { get; set; } // BTU capacity (24000, 48000, 80000)
    public string? FilterSize { get; set; } // 16x25x1, 20x20x4, etc.
    public decimal? Tonnage { get; set; }
    public decimal? SEER { get; set; }
    public decimal? AFUE { get; set; } // Annual Fuel Utilization Efficiency (%) — furnaces
    public decimal? HSPF { get; set; } // Heating Seasonal Performance Factor — heat pumps
    public string? Voltage { get; set; } // 120V, 208V, 240V, 480V
    public string? Phase { get; set; } // Single Phase, Three Phase
    public string? LocationOnSite { get; set; } // Basement, Attic, Roof, Side Yard, etc.
    public DateTime? ManufactureDate { get; set; }
    public int? AmpRating { get; set; } // Electrical — panel/circuit amp rating
    public string? PanelType { get; set; } // Electrical — Main Breaker, Main Lug, Sub-Panel
    public string? PipeMaterial { get; set; } // Plumbing — Copper, PEX, PVC, Cast Iron
    public int? GallonCapacity { get; set; } // Plumbing — water heater tank size
    public string? RefrigerantType { get; set; }
    public decimal? RefrigerantQuantity { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime? NextServiceDue { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public int? WarrantyTermYears { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public decimal? Value { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    public ICollection<JobAsset> JobAssets { get; set; } = [];
    public ICollection<ServiceAgreementAsset> ServiceAgreementAssets { get; set; } = [];
    public ICollection<AssetServiceLog> ServiceLogs { get; set; } = [];
}

public enum AssetStatus
{
    Active,
    MaintenanceNeeded,
    Retired,
    Decommissioned
}
