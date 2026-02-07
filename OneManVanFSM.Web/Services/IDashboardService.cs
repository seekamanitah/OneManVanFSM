using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class DashboardData
{
    public List<Job> ScheduledJobs { get; set; } = [];
    public List<Job> FinishedJobsWaitingInvoice { get; set; } = [];
    public List<Invoice> UnpaidInvoices { get; set; } = [];
    public List<Job> UnscheduledJobs { get; set; } = [];
    public List<Estimate> PendingEstimates { get; set; } = [];
    public List<EmployeeSummary> EmployeeSummaries { get; set; } = [];
    public decimal TotalPendingExpenses { get; set; }
    public List<ActivityItem> RecentActivities { get; set; } = [];
    public List<LowStockItem> LowStockItems { get; set; } = [];
    public List<ExpiringAgreement> ExpiringAgreements { get; set; } = [];
    public List<UrgentNote> UrgentNotes { get; set; } = [];
    public DashboardKPIs KPIs { get; set; } = new();
}

public class EmployeeSummary
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal HoursToday { get; set; }
    public decimal HoursThisWeek { get; set; }
    public decimal PendingExpenses { get; set; }
}

public class ActivityItem
{
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-circle";
    public DateTime Timestamp { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
}

public class LowStockItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public string? Location { get; set; }
}

public class ExpiringAgreement
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string? CoverageLevel { get; set; }
}

public class UrgentNote
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashboardKPIs
{
    public int TotalJobsInPeriod { get; set; }
    public int CompletedJobsInPeriod { get; set; }
    public decimal RevenueCollected { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int ActiveCustomers { get; set; }
    public int ActiveAgreements { get; set; }
}

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync(DashboardPeriod period = DashboardPeriod.Today);
}

public enum DashboardPeriod
{
    Today,
    ThisWeek,
    ThisMonth
}
