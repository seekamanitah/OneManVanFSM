namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IAssetService
{
    Task<List<AssetListItem>> GetAssetsAsync(AssetFilter? filter = null);
    Task<AssetDetail?> GetAssetAsync(int id);
    Task<Asset> CreateAssetAsync(AssetEditModel model);
    Task<Asset> UpdateAssetAsync(int id, AssetEditModel model);
    Task<bool> ArchiveAssetAsync(int id);
}

public class AssetFilter
{
    public string? Search { get; set; }
    public AssetStatus? Status { get; set; }
    public string? AssetType { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
}

public class AssetListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetType { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public AssetStatus Status { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public bool WarrantyActive => WarrantyExpiry.HasValue && WarrantyExpiry.Value > DateTime.UtcNow;
}

public class AssetDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetType { get; set; }
    public string? Brand { get; set; }
    public string? FuelType { get; set; }
    public string? UnitConfiguration { get; set; }
    public int? BTURating { get; set; }
    public string? FilterSize { get; set; }
    public decimal? Tonnage { get; set; }
    public decimal? SEER { get; set; }
    public decimal? AFUE { get; set; }
    public decimal? HSPF { get; set; }
    public string? Voltage { get; set; }
    public string? Phase { get; set; }
    public string? LocationOnSite { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public int? AmpRating { get; set; }
    public string? PanelType { get; set; }
    public string? PipeMaterial { get; set; }
    public int? GallonCapacity { get; set; }
    public string? RefrigerantType { get; set; }
    public decimal? RefrigerantQuantity { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime? NextServiceDue { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public int? WarrantyTermYears { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public AssetStatus Status { get; set; }
    public decimal? Value { get; set; }
    public string? Notes { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public string? SiteAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Linked data
    public List<AssetLinkedJob> LinkedJobs { get; set; } = [];
    public List<AssetServiceLogItem> ServiceHistory { get; set; } = [];
}

public class AssetLinkedJob
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public JobStatus Status { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? Role { get; set; }
}

public class AssetServiceLogItem
{
    public int Id { get; set; }
    public string? ServiceType { get; set; }
    public DateTime ServiceDate { get; set; }
    public string? PerformedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime? NextDueDate { get; set; }
    public decimal? Cost { get; set; }
}

public class AssetEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Asset name is required.")]
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetType { get; set; }
    public string? Brand { get; set; }
    public string? FuelType { get; set; }
    public string? UnitConfiguration { get; set; }
    public int? BTURating { get; set; }
    public string? FilterSize { get; set; }
    public decimal? Tonnage { get; set; }
    public decimal? SEER { get; set; }
    public decimal? AFUE { get; set; }
    public decimal? HSPF { get; set; }
    public string? Voltage { get; set; }
    public string? Phase { get; set; }
    public string? LocationOnSite { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public int? AmpRating { get; set; }
    public string? PanelType { get; set; }
    public string? PipeMaterial { get; set; }
    public int? GallonCapacity { get; set; }
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
    public int? ProductId { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
}
