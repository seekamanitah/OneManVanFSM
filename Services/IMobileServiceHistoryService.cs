using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileServiceHistoryService
{
    Task<List<MobileServiceHistoryCard>> GetRecordsAsync(MobileServiceHistoryFilter? filter = null);
    Task<MobileServiceHistoryDetail?> GetRecordDetailAsync(int id);
    Task<int> CreateRecordAsync(MobileServiceHistoryCreate model);
    Task<bool> UpdateRecordAsync(int id, MobileServiceHistoryUpdate model);
    Task<bool> UpdateStatusAsync(int id, ServiceHistoryStatus status);
    Task<bool> DeleteRecordAsync(int id);
    Task<bool> AddClaimActionAsync(int recordId, MobileClaimActionCreate action);
    Task<MobileServiceHistoryStats> GetStatsAsync();
}

public class MobileServiceHistoryFilter
{
    public string? Search { get; set; }
    public ServiceHistoryType? Type { get; set; }
    public ServiceHistoryStatus? Status { get; set; }
}

public class MobileServiceHistoryCard
{
    public int Id { get; set; }
    public string RecordNumber { get; set; } = string.Empty;
    public ServiceHistoryType Type { get; set; }
    public ServiceHistoryStatus Status { get; set; }
    public DateTime ServiceDate { get; set; }
    public string? Description { get; set; }
    public decimal? Cost { get; set; }
    public string? CustomerName { get; set; }
    public string? AssetName { get; set; }
    public string? TechName { get; set; }
    public string? IssueType { get; set; }
}

public class MobileServiceHistoryDetail
{
    public int Id { get; set; }
    public string RecordNumber { get; set; } = string.Empty;
    public ServiceHistoryType Type { get; set; }
    public ServiceHistoryStatus Status { get; set; }
    public DateTime ServiceDate { get; set; }
    public string? Description { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? Cost { get; set; }
    public decimal? Reimbursement { get; set; }
    public string? VendorName { get; set; }
    public string? IssueType { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public int? JobId { get; set; }
    public string? JobNumber { get; set; }
    public int? TechId { get; set; }
    public string? TechName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MobileClaimActionItem> ClaimActions { get; set; } = [];
}

public class MobileClaimActionItem
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime ActionDate { get; set; }
    public string? PerformedBy { get; set; }
}

public class MobileServiceHistoryCreate
{
    public ServiceHistoryType Type { get; set; } = ServiceHistoryType.NonWarrantyRepair;
    public DateTime ServiceDate { get; set; } = DateTime.Now;
    public string? Description { get; set; }
    public decimal? Cost { get; set; }
    public string? VendorName { get; set; }
    public string? IssueType { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
    public int? AssetId { get; set; }
    public int? JobId { get; set; }
    public int? TechId { get; set; }
}

public class MobileServiceHistoryUpdate
{
    public ServiceHistoryType Type { get; set; }
    public ServiceHistoryStatus Status { get; set; }
    public DateTime ServiceDate { get; set; }
    public string? Description { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? Cost { get; set; }
    public decimal? Reimbursement { get; set; }
    public string? VendorName { get; set; }
    public string? IssueType { get; set; }
    public string? Notes { get; set; }
}

public class MobileClaimActionCreate
{
    public string Action { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.Now;
    public string? PerformedBy { get; set; }
}

public class MobileServiceHistoryStats
{
    public int TotalRecords { get; set; }
    public int OpenClaims { get; set; }
    public int ResolvedThisMonth { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalReimbursed { get; set; }
}
