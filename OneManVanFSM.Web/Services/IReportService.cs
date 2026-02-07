namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IReportService
{
    Task<ReportKPIs> GetKPIsAsync(ReportPeriod period = ReportPeriod.ThisMonth);
    Task<List<AuditLogListItem>> GetAuditLogsAsync(AuditLogFilter? filter = null);
    Task<int> GetAuditLogCountAsync(AuditLogFilter? filter = null);
}

public enum ReportPeriod
{
    Today,
    ThisWeek,
    ThisMonth,
    ThisQuarter,
    ThisYear,
    AllTime
}

public class ReportKPIs
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
    public int DocumentCount { get; set; }
    public int InventoryItemCount { get; set; }
}

public class AuditLogFilter
{
    public string? Search { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class AuditLogListItem
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}
