namespace OneManVanFSM.Shared.Models;

public class Site
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public PropertyType PropertyType { get; set; } = PropertyType.Residential;
    public int? SqFt { get; set; }
    public int? Zones { get; set; }
    public int? Stories { get; set; }
    public string? AccessCodes { get; set; }
    public string? Instructions { get; set; }
    public string? Parking { get; set; }
    public string? EquipmentLocation { get; set; }
    public string? GasLineLocation { get; set; }
    public string? ElectricalPanelLocation { get; set; }
    public string? WaterShutoffLocation { get; set; }
    public string? HeatingFuelSource { get; set; } // Natural Gas, Propane, Electric, Oil, Geothermal, Solar
    public int? YearBuilt { get; set; }
    public bool? HasAtticAccess { get; set; }
    public bool? HasCrawlSpace { get; set; }
    public bool? HasBasement { get; set; }
    public bool IsNewConstruction { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public ICollection<Asset> Assets { get; set; } = [];
    public ICollection<Job> Jobs { get; set; } = [];
    public ICollection<Estimate> Estimates { get; set; } = [];
}

public enum PropertyType
{
    Residential,
    Commercial,
    Industrial
}
