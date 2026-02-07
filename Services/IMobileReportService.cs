using OneManVanFSM.Shared.Data;

namespace OneManVanFSM.Services;

public interface IMobileReportService
{
    Task<MobileTechReport> GetTechReportAsync(int employeeId);
}

public class MobileTechReport
{
    // Time Tracking
    public decimal HoursToday { get; set; }
    public decimal HoursThisWeek { get; set; }
    public decimal HoursThisMonth { get; set; }
    public decimal BillableHoursThisMonth { get; set; }
    public decimal NonBillableHoursThisMonth { get; set; }
    public decimal BillablePercent => HoursThisMonth > 0 ? Math.Round(BillableHoursThisMonth / HoursThisMonth * 100, 1) : 0;

    // Job Metrics
    public int JobsCompletedToday { get; set; }
    public int JobsCompletedThisWeek { get; set; }
    public int JobsCompletedThisMonth { get; set; }
    public int JobsAssignedThisMonth { get; set; }
    public decimal CompletionRate => JobsAssignedThisMonth > 0 ? Math.Round((decimal)JobsCompletedThisMonth / JobsAssignedThisMonth * 100, 1) : 0;
    public decimal AvgJobDurationHours { get; set; }

    // Daily Breakdown (last 7 days)
    public List<MobileDailyBreakdown> DailyBreakdown { get; set; } = [];

    // Job Status Distribution
    public int ScheduledJobs { get; set; }
    public int InProgressJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int OverdueJobs { get; set; }

    // Revenue / Materials
    public decimal MaterialsCostThisMonth { get; set; }
    public int MaterialItemsUsed { get; set; }

    // Time Category Breakdown
    public List<MobileTimeCategoryBreakdown> TimeCategories { get; set; } = [];
}

public class MobileDailyBreakdown
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = "";
    public decimal Hours { get; set; }
    public int JobsCompleted { get; set; }
}

public class MobileTimeCategoryBreakdown
{
    public string Category { get; set; } = "";
    public decimal Hours { get; set; }
    public decimal Percent { get; set; }
}
