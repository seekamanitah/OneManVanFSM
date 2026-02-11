using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileSiteService
{
    Task<List<MobileSiteCard>> GetSitesAsync(MobileSiteFilter? filter = null);
    Task<MobileSiteStats> GetStatsAsync();
    Task<Site> QuickCreateAsync(MobileSiteQuickCreate model);
}

public class MobileSiteCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public PropertyType PropertyType { get; set; }
    public bool IsNewConstruction { get; set; }
    public string? CustomerName { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int AssetCount { get; set; }
    public int JobCount { get; set; }
    public DateTime? LastJobDate { get; set; }
}

public class MobileSiteFilter
{
    public string? Search { get; set; }
    public PropertyType? PropertyType { get; set; }
}

public class MobileSiteStats
{
    public int TotalSites { get; set; }
    public int ResidentialCount { get; set; }
    public int CommercialCount { get; set; }
    public int TotalAssets { get; set; }
}

public class MobileSiteQuickCreate
{
    public string Name { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public PropertyType PropertyType { get; set; } = PropertyType.Residential;
    public string? Notes { get; set; }
}
