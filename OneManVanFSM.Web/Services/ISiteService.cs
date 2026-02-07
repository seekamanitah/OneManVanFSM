namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface ISiteService
{
    Task<List<SiteListItem>> GetSitesAsync(SiteFilter? filter = null);
    Task<SiteDetail?> GetSiteAsync(int id);
    Task<Site> CreateSiteAsync(SiteEditModel model);
    Task<Site> UpdateSiteAsync(int id, SiteEditModel model);
    Task<bool> ArchiveSiteAsync(int id);
    Task<List<CompanyOption>> GetCompaniesForDropdownAsync();
}

public class SiteFilter
{
    public string? Search { get; set; }
    public PropertyType? PropertyType { get; set; }
    public int? CustomerId { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
}

public class SiteListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public PropertyType PropertyType { get; set; }
    public int? SqFt { get; set; }
    public int? Zones { get; set; }
    public string? OwnerName { get; set; }
    public int AssetCount { get; set; }
    public int OpenJobCount { get; set; }
}

public class SiteDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public PropertyType PropertyType { get; set; }
    public int? SqFt { get; set; }
    public int? Zones { get; set; }
    public int? Stories { get; set; }
    public string? AccessCodes { get; set; }
    public string? Instructions { get; set; }
    public string? Parking { get; set; }
    public string? EquipmentLocation { get; set; }
    public string? Notes { get; set; }
    public string? GasLineLocation { get; set; }
    public string? ElectricalPanelLocation { get; set; }
    public string? WaterShutoffLocation { get; set; }
    public string? HeatingFuelSource { get; set; }
    public int? YearBuilt { get; set; }
    public bool? HasAtticAccess { get; set; }
    public bool? HasCrawlSpace { get; set; }
    public bool? HasBasement { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<AssetSummary> Assets { get; set; } = [];
    public List<JobSummary> RecentJobs { get; set; } = [];
    public List<JobSummary> UpcomingJobs { get; set; } = [];
}

public class SiteEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Site name is required.")]
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
    public string? HeatingFuelSource { get; set; }
    public int? YearBuilt { get; set; }
    public bool? HasAtticAccess { get; set; }
    public bool? HasCrawlSpace { get; set; }
    public bool? HasBasement { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
}

public class CompanyOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
