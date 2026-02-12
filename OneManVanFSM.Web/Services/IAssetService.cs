namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IAssetService
{
    Task<List<AssetListItem>> GetAssetsAsync(AssetFilter? filter = null);
    Task<AssetDetail?> GetAssetAsync(int id);
    Task<Asset> CreateAssetAsync(AssetEditModel model);
    Task<Asset> UpdateAssetAsync(int id, AssetEditModel model);
    Task<bool> ArchiveAssetAsync(int id);
    Task<bool> RestoreAssetAsync(int id);
    Task<bool> DeleteAssetPermanentlyAsync(int id);
    Task<int> BulkArchiveAssetsAsync(List<int> ids);
    Task<int> BulkRestoreAssetsAsync(List<int> ids);
    Task<int> BulkDeleteAssetsPermanentlyAsync(List<int> ids);
    Task<List<AssetTimelineEntry>> GetUnifiedTimelineAsync(int assetId);
    // Asset linking (peer-to-peer equipment grouping)
    Task<List<LinkedAssetDto>> GetLinkedAssetsAsync(int assetId);
    Task LinkAssetsAsync(int assetId, int linkedAssetId, string? linkType = null, string? label = null);
    Task UnlinkAssetAsync(int assetId, int linkedAssetId);
    Task<List<AssetOption>> GetAssetOptionsAsync(int? customerId = null, int? siteId = null);
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
    public bool ShowArchived { get; set; }
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
    public bool NoWarranty { get; set; }
    public bool WarrantyActive => !NoWarranty && WarrantyExpiry.HasValue && WarrantyExpiry.Value > DateTime.UtcNow;
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
    public decimal? SEER2 { get; set; }
    public decimal? AFUE { get; set; }
    public decimal? HSPF { get; set; }
    public decimal? HSPF2 { get; set; }
    public decimal? EER { get; set; }
    public string? AssetTag { get; set; }
    public string? Nickname { get; set; }
    public string? Voltage { get; set; }
    public string? Phase { get; set; }
    public string? LocationOnSite { get; set; }
    public int? AmpRating { get; set; }
    public string? PanelType { get; set; }
    public string? PipeMaterial { get; set; }
    public int? GallonCapacity { get; set; }
    public string? RefrigerantType { get; set; }
    public decimal? RefrigerantQuantity { get; set; }
    public string? FilterType { get; set; }
    public int? FilterChangeIntervalMonths { get; set; }
    public DateTime? FilterLastChanged { get; set; }
    public DateTime? FilterNextDue { get; set; }
    public string? ThermostatBrand { get; set; }
    public string? ThermostatModel { get; set; }
    public string? ThermostatType { get; set; }
    public bool ThermostatWiFiEnabled { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime? NextServiceDue { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public int? WarrantyTermYears { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public DateTime? LaborWarrantyExpiry { get; set; }
    public DateTime? PartsWarrantyExpiry { get; set; }
    public DateTime? CompressorWarrantyExpiry { get; set; }
    public int? LaborWarrantyTermYears { get; set; }
    public int? PartsWarrantyTermYears { get; set; }
    public int? CompressorWarrantyTermYears { get; set; }
    public bool RegisteredOnline { get; set; }
    public string? InstalledBy { get; set; }
    public bool WarrantedByCompany { get; set; }
    public bool NoWarranty { get; set; }
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
    public List<AssetTimelineEntry> UnifiedTimeline { get; set; } = [];
    public List<LinkedAssetDto> LinkedAssets { get; set; } = [];
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
    public string? RefrigerantType { get; set; }
    public decimal? RefrigerantAmountAdded { get; set; }
    public decimal? RefrigerantBeforeReading { get; set; }
    public decimal? RefrigerantAfterReading { get; set; }
}

public class AssetTimelineEntry
{
    public DateTime Date { get; set; }
    public string Source { get; set; } = string.Empty; // "Job", "ServiceHistory", "ServiceLog"
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? PerformedBy { get; set; }
    public decimal? Cost { get; set; }
    public int? SourceId { get; set; }
    public string? Badge { get; set; } // CSS class for badge styling
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
    public decimal? SEER2 { get; set; }
    public decimal? AFUE { get; set; }
    public decimal? HSPF { get; set; }
    public decimal? HSPF2 { get; set; }
    public decimal? EER { get; set; }
    public string? AssetTag { get; set; }
    public string? Nickname { get; set; }
    public string? Voltage { get; set; }
    public string? Phase { get; set; }
    public string? LocationOnSite { get; set; }
    public int? AmpRating { get; set; }
    public string? PanelType { get; set; }
    public string? PipeMaterial { get; set; }
    public int? GallonCapacity { get; set; }
    public string? RefrigerantType { get; set; }
    public decimal? RefrigerantQuantity { get; set; }
    public string? FilterType { get; set; }
    public int? FilterChangeIntervalMonths { get; set; }
    public DateTime? FilterLastChanged { get; set; }
    public DateTime? FilterNextDue { get; set; }
    public string? ThermostatBrand { get; set; }
    public string? ThermostatModel { get; set; }
    public string? ThermostatType { get; set; }
    public bool ThermostatWiFiEnabled { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime? NextServiceDue { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public int? WarrantyTermYears { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public int? LaborWarrantyTermYears { get; set; }
    public int? PartsWarrantyTermYears { get; set; }
    public int? CompressorWarrantyTermYears { get; set; }
    public bool RegisteredOnline { get; set; }
    public string? InstalledBy { get; set; }
    public bool WarrantedByCompany { get; set; }
    public bool NoWarranty { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public decimal? Value { get; set; }
    public string? Notes { get; set; }
    public int? ProductId { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
    public List<int> JobIds { get; set; } = [];
}

public class LinkedAssetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetType { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? LocationOnSite { get; set; }
    public string? LinkType { get; set; }
    public string? Label { get; set; }
    public AssetStatus Status { get; set; }
}

public class AssetOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetType { get; set; }
    public string? SerialNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
    public string Display
    {
        get
        {
            var parts = new List<string> { Name };
            if (!string.IsNullOrEmpty(AssetType)) parts.Add(AssetType);
            if (!string.IsNullOrEmpty(SerialNumber)) parts.Add($"SN: {SerialNumber}");
            var context = SiteName ?? CustomerName;
            if (!string.IsNullOrEmpty(context)) parts.Add(context);
            return string.Join(" — ", parts);
        }
    }
}
