using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileDashboardService
{
    Task<MobileDashboardData> GetDashboardAsync(int employeeId, bool isElevated = false);
}

public class MobileDashboardData
{
    public int TodayJobCount { get; set; }
    public int OpenJobCount { get; set; }
    public int PendingNoteCount { get; set; }
    public decimal HoursToday { get; set; }
    public decimal HoursThisWeek { get; set; }
    public bool IsClockedIn { get; set; }
    public DateTime? ClockInTime { get; set; }
    public int CompletedThisWeek { get; set; }
    public int OverdueJobCount { get; set; }
    public int UpcomingJobCount { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringAgreementCount { get; set; }
    public int MaintenanceDueCount { get; set; }
    public int WarrantyAlertCount { get; set; }
    public int DraftEstimateCount { get; set; }
    public int PendingInvoiceCount { get; set; }
    public int ActiveJobClockCount { get; set; }
    public string? ActiveJobName { get; set; }
    public decimal JobHoursToday { get; set; }
    public List<MobileJobCard> TodayJobs { get; set; } = [];
    public List<MobileJobCard> UpcomingJobs { get; set; } = [];
    public List<MobileActivityItem> RecentActivity { get; set; } = [];
}

public class MobileJobCard
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? CompanyName { get; set; }
    public string? SiteAddress { get; set; }
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public decimal? EstimatedDuration { get; set; }
}

public class MobileActivityItem
{
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-circle";
    public DateTime Timestamp { get; set; }
}
