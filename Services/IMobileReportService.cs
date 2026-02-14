using OneManVanFSM.Shared.Data;

namespace OneManVanFSM.Services;

public interface IMobileReportService
{
    Task<MobileTechReport> GetTechReportAsync(int employeeId);
    Task<MobileBusinessKPIs> GetBusinessKPIsAsync();
    Task<List<MobileJobProfitItem>> GetJobProfitabilityAsync(int count = 20);
    Task<MobilePayrollPreview> GetPayrollPreviewAsync(int employeeId, DateTime weekStart);
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

public class MobileBusinessKPIs
{
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int OpenJobs { get; set; }
    public decimal RevenueTotal { get; set; }
    public decimal OutstandingAR { get; set; }
    public int UnpaidInvoiceCount { get; set; }
    public int ActiveEmployees { get; set; }
    public decimal TotalHoursLogged { get; set; }
    public int TotalAssets { get; set; }
    public int ActiveAgreements { get; set; }
    public int PendingEstimates { get; set; }
    public int TotalCustomers { get; set; }
    public int InventoryItemCount { get; set; }
    public int LowStockCount { get; set; }
}

public class MobileJobProfitItem
{
    public int JobId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? Trade { get; set; }
    public decimal Revenue { get; set; }
    public decimal LaborCost { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal ExpenseCost { get; set; }
    public decimal Profit => Revenue - LaborCost - MaterialCost - ExpenseCost;
    public decimal MarginPercent => Revenue > 0 ? Math.Round(Profit / Revenue * 100, 1) : 0;
    public DateTime? CompletedDate { get; set; }
}

public class MobilePayrollPreview
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal TotalShiftHours { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal RegularPay { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal TotalPay { get; set; }
    public decimal HourlyRate { get; set; }
    public int JobsClockedThisWeek { get; set; }
    public List<MobilePayrollDayPreview> Days { get; set; } = [];
}

public class MobilePayrollDayPreview
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = "";
    public decimal ShiftHours { get; set; }
    public decimal JobHours { get; set; }
    public int JobCount { get; set; }
}
