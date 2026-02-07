using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileAssetService
{
    Task<MobileAssetDetail?> GetAssetDetailAsync(int assetId);
    Task<List<MobileServiceLogItem>> GetServiceLogsAsync(int assetId);
    Task<AssetServiceLog> AddServiceLogAsync(MobileServiceLogCreate model);
}

public class MobileAssetDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetType { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public AssetStatus Status { get; set; }

    // HVAC specs
    public decimal? Tonnage { get; set; }
    public decimal? SEER { get; set; }
    public decimal? AFUE { get; set; }
    public decimal? HSPF { get; set; }
    public int? BTURating { get; set; }
    public string? RefrigerantType { get; set; }
    public decimal? RefrigerantQuantity { get; set; }
    public string? FilterSize { get; set; }
    public string? FuelType { get; set; }
    public string? UnitConfiguration { get; set; }
    public string? Voltage { get; set; }

    // Multi-trade specs
    public int? AmpRating { get; set; }
    public string? PipeMaterial { get; set; }
    public int? GallonCapacity { get; set; }

    // Location & dates
    public string? LocationOnSite { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime? NextServiceDue { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public int? WarrantyTermYears { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public DateTime? LaborWarrantyExpiry { get; set; }
    public DateTime? PartsWarrantyExpiry { get; set; }
    public DateTime? CompressorWarrantyExpiry { get; set; }
    public decimal? Value { get; set; }
    public string? Notes { get; set; }

    // Owner context
    public string? SiteName { get; set; }
    public string? SiteAddress { get; set; }
    public int? SiteId { get; set; }
    public string? CustomerName { get; set; }
    public int? CustomerId { get; set; }

    // Related data
    public List<MobileAssetJob> LinkedJobs { get; set; } = [];
    public List<MobileServiceLogItem> ServiceLogs { get; set; } = [];
    public List<MobileAssetDocument> Documents { get; set; } = [];
}

public class MobileAssetJob
{
    public int JobId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public JobStatus Status { get; set; }
    public string? Role { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public class MobileServiceLogItem
{
    public int Id { get; set; }
    public string ServiceType { get; set; } = string.Empty;
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

public class MobileAssetDocument
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public DocumentCategory Category { get; set; }
    public DateTime UploadDate { get; set; }
}

public class MobileServiceLogCreate
{
    public int AssetId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;
    public string? PerformedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime? NextDueDate { get; set; }
    public decimal? Cost { get; set; }
    public string? RefrigerantType { get; set; }
    public decimal? RefrigerantAmountAdded { get; set; }
    public decimal? RefrigerantBeforeReading { get; set; }
    public decimal? RefrigerantAfterReading { get; set; }
}
