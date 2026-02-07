namespace OneManVanFSM.Shared.Models;

public class Job
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Lead;
    public JobPriority Priority { get; set; } = JobPriority.Standard;
    public string? TradeType { get; set; } // HVAC, Plumbing, Electrical, General
    public string? JobType { get; set; } // Install, Repair, Maintenance, Diagnostic, etc.
    public string? SystemType { get; set; } // Spider, Trunk, etc.
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public decimal? EstimatedDuration { get; set; } // in hours
    public decimal? EstimatedTotal { get; set; }
    public decimal? ActualDuration { get; set; } // actual hours spent
    public decimal? ActualTotal { get; set; } // actual cost
    public bool? PermitRequired { get; set; }
    public string? PermitNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedDate { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    public int? EstimateId { get; set; }
    public Estimate? Estimate { get; set; }
    public int? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public int? MaterialListId { get; set; }
    public MaterialList? MaterialList { get; set; }
    public ICollection<JobEmployee> JobEmployees { get; set; } = [];
    public ICollection<TimeEntry> TimeEntries { get; set; } = [];
    public ICollection<QuickNote> QuickNotes { get; set; } = [];
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<JobAsset> JobAssets { get; set; } = [];
}

public enum JobStatus
{
    Lead,
    Quoted,
    Approved,
    Scheduled,
    EnRoute,
    OnSite,
    InProgress,
    Paused,
    Completed,
    Closed,
    Cancelled
}

public enum JobPriority
{
    Low,
    Standard,
    High,
    Emergency
}
