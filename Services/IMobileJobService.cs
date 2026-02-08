using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileJobService
{
    Task<List<MobileJobCard>> GetAssignedJobsAsync(int employeeId, MobileJobFilter? filter = null);
    Task<MobileJobDetail?> GetJobDetailAsync(int id);
    Task<bool> UpdateJobStatusAsync(int id, JobStatus status);
    Task<MobileSiteDetail?> GetSiteDetailAsync(int siteId);
    Task<MobileMaterialItem> AddMaterialItemAsync(MobileMaterialCreate model);
    Task<int> CreateJobAsync(MobileJobCreate model);
    Task<List<MobileJobCard>> GetAllJobCardsAsync();
    Task<List<MobileCustomerOption>> GetCustomerOptionsAsync();
    Task<List<MobileSiteOption>> GetSiteOptionsAsync(int? customerId);
}

public class MobileJobCreate
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public JobPriority Priority { get; set; } = JobPriority.Standard;
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public decimal? EstimatedDuration { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
    public int? AssignedEmployeeId { get; set; }
}

public class MobileCustomerOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MobileSiteOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}

public class MobileJobFilter
{
    public string? Search { get; set; }
    public JobStatus? Status { get; set; }
    public JobPriority? Priority { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class MobileJobDetail
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public string? SystemType { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public decimal? EstimatedDuration { get; set; }
    public decimal? EstimatedTotal { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Customer / Site
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? SiteName { get; set; }
    public string? SiteAddress { get; set; }
    public int? SiteId { get; set; }
    public string? AccessCodes { get; set; }
    public string? EquipmentLocation { get; set; }

    // Technician
    public string? TechnicianName { get; set; }

    // Assets at site
    public List<MobileAssetSummary> Assets { get; set; } = [];

    // Time entries
    public List<MobileTimeEntrySummary> TimeEntries { get; set; } = [];

    // Material list items
    public List<MobileMaterialItem> Materials { get; set; } = [];

    // Documents attached to job
    public List<MobileJobDocument> Documents { get; set; } = [];
}

public class MobileAssetSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetType { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public AssetStatus Status { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
}

public class MobileTimeEntrySummary
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal Hours { get; set; }
    public string? Notes { get; set; }
}

public class MobileMaterialItem
{
    public int Id { get; set; }
    public string Section { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal BaseCost { get; set; }
    public string? Notes { get; set; }
}

public class MobileJobDocument
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public DocumentCategory Category { get; set; }
    public DateTime UploadDate { get; set; }
}

public class MobileSiteDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public PropertyType PropertyType { get; set; }
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
    public string? CustomerName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public bool IsNewConstruction { get; set; }
    public List<MobileAssetSummary> Assets { get; set; } = [];
    public List<MobileSiteJob> RecentJobs { get; set; } = [];
    public List<MobileSiteAgreement> Agreements { get; set; } = [];
}

public class MobileSiteAgreement
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public CoverageLevel CoverageLevel { get; set; }
    public AgreementStatus Status { get; set; }
    public DateTime EndDate { get; set; }
    public int VisitsIncluded { get; set; }
    public int VisitsUsed { get; set; }
    public int VisitsRemaining => VisitsIncluded - VisitsUsed;
}

public class MobileSiteJob
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public JobStatus Status { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public class MobileMaterialCreate
{
    public int JobId { get; set; }
    public string Section { get; set; } = "General";
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string? Unit { get; set; } = "ea";
    public decimal BaseCost { get; set; }
    public string? Notes { get; set; }
}
