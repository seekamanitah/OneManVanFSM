namespace OneManVanFSM.Shared.Models;

public class ServiceHistoryRecord
{
    public int Id { get; set; }
    public string RecordNumber { get; set; } = string.Empty;
    public ServiceHistoryType Type { get; set; } = ServiceHistoryType.NonWarrantyRepair;
    public ServiceHistoryStatus Status { get; set; } = ServiceHistoryStatus.Open;
    public DateTime ServiceDate { get; set; } = DateTime.Now;
    public string? Description { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? Evidence { get; set; } // JSON (photos, receipts, attachments)
    public decimal? Cost { get; set; }
    public decimal? Reimbursement { get; set; }
    public string? VendorName { get; set; }
    public string? IssueType { get; set; } // Leak, Defect, Vibration, etc. - customizable
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Polymorphic relationships
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public int? TechId { get; set; }
    public Employee? Tech { get; set; }

    // Claim action steps
    public ICollection<ClaimAction> ClaimActions { get; set; } = [];
}

public class ClaimAction
{
    public int Id { get; set; }
    public int ServiceHistoryRecordId { get; set; }
    public ServiceHistoryRecord ServiceHistoryRecord { get; set; } = null!;
    public string Action { get; set; } = string.Empty; // Submitted, Response, Appeal, etc.
    public string? Response { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.Now;
    public string? PerformedBy { get; set; }
}

public enum ServiceHistoryType
{
    WarrantyClaim,
    NonWarrantyRepair,
    PreventiveMaintenance
}

public enum ServiceHistoryStatus
{
    Open,
    Submitted,
    InProgress,
    Approved,
    Denied,
    Resolved
}
