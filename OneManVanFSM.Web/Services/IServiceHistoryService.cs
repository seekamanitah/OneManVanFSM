namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IServiceHistoryService
{
    Task<List<ServiceHistoryListItem>> GetRecordsAsync(ServiceHistoryFilter? filter = null);
    Task<ServiceHistoryDetail?> GetRecordAsync(int id);
    Task<ServiceHistoryRecord> CreateRecordAsync(ServiceHistoryEditModel model);
    Task<bool> UpdateRecordAsync(int id, ServiceHistoryEditModel model);
    Task<bool> UpdateStatusAsync(int id, ServiceHistoryStatus newStatus, string? notes = null);
    Task<bool> DeleteRecordAsync(int id);
    Task<bool> AddClaimActionAsync(int recordId, ClaimActionEditModel action);
    Task<ServiceHistoryKpis> GetKpisAsync();
}

public class ServiceHistoryFilter
{
    public string? Search { get; set; }
    public ServiceHistoryType? Type { get; set; }
    public ServiceHistoryStatus? Status { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
    public int? AssetId { get; set; }
    public string? SortBy { get; set; } = "ServiceDate";
    public bool SortDescending { get; set; } = true;
}

public class ServiceHistoryListItem
{
    public int Id { get; set; }
    public string RecordNumber { get; set; } = string.Empty;
    public ServiceHistoryType Type { get; set; }
    public ServiceHistoryStatus Status { get; set; }
    public DateTime ServiceDate { get; set; }
    public string? Description { get; set; }
    public decimal? Cost { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
    public string? AssetName { get; set; }
    public string? TechName { get; set; }
    public string? IssueType { get; set; }
}

public class ServiceHistoryDetail
{
    public int Id { get; set; }
    public string RecordNumber { get; set; } = string.Empty;
    public ServiceHistoryType Type { get; set; }
    public ServiceHistoryStatus Status { get; set; }
    public DateTime ServiceDate { get; set; }
    public string? Description { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? Evidence { get; set; }
    public decimal? Cost { get; set; }
    public decimal? Reimbursement { get; set; }
    public string? VendorName { get; set; }
    public string? IssueType { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public int? JobId { get; set; }
    public string? JobNumber { get; set; }
    public int? TechId { get; set; }
    public string? TechName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ClaimActionItem> ClaimActions { get; set; } = [];
}

public class ClaimActionItem
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime ActionDate { get; set; }
    public string? PerformedBy { get; set; }
}

public class ServiceHistoryEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Record number is required.")]
    public string RecordNumber { get; set; } = string.Empty;
    public ServiceHistoryType Type { get; set; } = ServiceHistoryType.NonWarrantyRepair;
    public ServiceHistoryStatus Status { get; set; } = ServiceHistoryStatus.Open;
    public DateTime ServiceDate { get; set; } = DateTime.Now;
    public string? Description { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? Evidence { get; set; }
    public decimal? Cost { get; set; }
    public decimal? Reimbursement { get; set; }
    public string? VendorName { get; set; }
    public string? IssueType { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public int? AssetId { get; set; }
    public int? JobId { get; set; }
    public int? TechId { get; set; }
}

public class ClaimActionEditModel
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Action { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.Now;
    public string? PerformedBy { get; set; }
}

public class ServiceHistoryKpis
{
    public int TotalRecords { get; set; }
    public int OpenClaims { get; set; }
    public int ResolvedThisMonth { get; set; }
    public int DeniedClaims { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalReimbursed { get; set; }
}
