namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IJobService
{
    Task<List<JobListItem>> GetJobsAsync(JobFilter? filter = null);
    Task<JobDetail?> GetJobAsync(int id);
    Task<Job> CreateJobAsync(JobEditModel model);
    Task<Job> UpdateJobAsync(int id, JobEditModel model);
    Task<bool> UpdateStatusAsync(int id, JobStatus status);
    Task<bool> ArchiveJobAsync(int id);
    Task<List<EmployeeOption>> GetTechniciansAsync();
    Task<List<JobOption>> GetJobOptionsAsync(int? customerId = null, int? siteId = null);
    // Multi-employee
    Task AddEmployeeToJobAsync(int jobId, int employeeId, string? role, JobEmployeePayType payType, decimal? flatRate);
    Task RemoveEmployeeFromJobAsync(int jobId, int employeeId);
    Task<List<JobEmployeeDto>> GetJobEmployeesAsync(int jobId);
}

public class JobFilter
{
    public string? Search { get; set; }
    public JobStatus? Status { get; set; }
    public JobPriority? Priority { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
    public string? SortBy { get; set; } = "ScheduledDate";
    public bool SortDescending { get; set; } = true;
}

public class JobListItem
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? TechnicianName { get; set; }
    public decimal? EstimatedTotal { get; set; }
}

public class JobDetail
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public string? TradeType { get; set; }
    public string? JobType { get; set; }
    public string? SystemType { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public decimal? EstimatedDuration { get; set; }
    public decimal? EstimatedTotal { get; set; }
    public decimal? ActualDuration { get; set; }
    public decimal? ActualTotal { get; set; }
    public bool? PermitRequired { get; set; }
    public string? PermitNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public string? SiteAddress { get; set; }
    public int? AssignedEmployeeId { get; set; }
    public string? TechnicianName { get; set; }
    public int? EstimateId { get; set; }
    public string? EstimateNumber { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? MaterialListId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TimeEntrySummary> TimeEntries { get; set; } = [];
    public List<NoteSummary> Notes2 { get; set; } = [];
    public List<JobEmployeeDto> AssignedEmployees { get; set; } = [];
    public List<JobLinkedAsset> LinkedAssets { get; set; } = [];
    public JobMaterialSummary? MaterialSummary { get; set; }
}

public class JobEmployeeDto
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string PayType { get; set; } = "Hourly";
    public decimal? FlatRateAmount { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class JobLinkedAsset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetType { get; set; }
    public AssetStatus Status { get; set; }
    public string? Role { get; set; }
}

public class JobMaterialSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
}

public class TimeEntrySummary
{
    public int Id { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Hours { get; set; }
    public string? Notes { get; set; }
}

public class EmployeeOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Territory { get; set; }
    public decimal HourlyRate { get; set; }
}

public class JobOption
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Display => string.IsNullOrEmpty(Title) ? JobNumber : $"{JobNumber} – {Title}";
}

public class JobEditModel
{
    public string JobNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Title is required.")]
    public string? Title { get; set; }

    public string? Description { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Lead;
    public JobPriority Priority { get; set; } = JobPriority.Standard;
    public string? TradeType { get; set; }
    public string? JobType { get; set; }
    public string? SystemType { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public decimal? EstimatedDuration { get; set; }
    public decimal? EstimatedTotal { get; set; }
    public decimal? ActualDuration { get; set; }
    public decimal? ActualTotal { get; set; }
    public bool? PermitRequired { get; set; }
    public string? PermitNumber { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public int? AssignedEmployeeId { get; set; }
    public List<int> AdditionalEmployeeIds { get; set; } = [];
    public List<int> AssetIds { get; set; } = [];
}
