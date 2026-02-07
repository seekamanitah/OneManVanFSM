namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IReportService
{
    Task<ReportKPIs> GetKPIsAsync(ReportPeriod period = ReportPeriod.ThisMonth);
    Task<List<AuditLogListItem>> GetAuditLogsAsync(AuditLogFilter? filter = null);
    Task<int> GetAuditLogCountAsync(AuditLogFilter? filter = null);
    Task<List<JobProfitabilityItem>> GetJobProfitabilityAsync(ReportPeriod period = ReportPeriod.ThisMonth);
    Task<List<TechUtilizationItem>> GetTechUtilizationAsync(ReportPeriod period = ReportPeriod.ThisMonth);
    Task<List<SeasonalDemandItem>> GetSeasonalDemandAsync();
    Task<List<ARAgingItem>> GetARAgingAsync();
    Task<List<AssetRepairTrendItem>> GetAssetRepairTrendsAsync();
    Task<List<InventoryUsageItem>> GetInventoryUsageAsync();
    Task<List<AgreementRenewalItem>> GetAgreementRenewalsAsync();
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

public class JobProfitabilityItem
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

public class TechUtilizationItem
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int JobsCompleted { get; set; }
    public int JobsAssigned { get; set; }
    public decimal HoursLogged { get; set; }
    public decimal AvgHoursPerJob => JobsCompleted > 0 ? Math.Round(HoursLogged / JobsCompleted, 1) : 0;
    public decimal RevenueGenerated { get; set; }
    public decimal RevenuePerHour => HoursLogged > 0 ? Math.Round(RevenueGenerated / HoursLogged, 2) : 0;
}

public class SeasonalDemandItem
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int JobCount { get; set; }
    public decimal Revenue { get; set; }
    public string? TopTrade { get; set; }
    public int Year { get; set; }
}

public class ARAgingItem
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal BalanceDue { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty; // Current, 1-30, 31-60, 61-90, 90+
}

public class AssetRepairTrendItem
{
    public string AssetType { get; set; } = string.Empty;
    public int AssetCount { get; set; }
    public int RepairCount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AvgCostPerRepair => RepairCount > 0 ? Math.Round(TotalCost / RepairCount, 2) : 0;
    public string? TopServiceType { get; set; }
    public int UnderWarranty { get; set; }
    public int OutOfWarranty { get; set; }
}

public class InventoryUsageItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal Cost { get; set; }
    public decimal StockValue => CurrentStock * Cost;
    public bool ReorderNeeded => CurrentStock <= MinThreshold;
    public string? PreferredSupplier { get; set; }
    public DateTime? LastRestockedDate { get; set; }
}

public class AgreementRenewalItem
{
    public int AgreementId { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? TradeType { get; set; }
    public string? CoverageLevel { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public int VisitsIncluded { get; set; }
    public int VisitsUsed { get; set; }
    public decimal Fee { get; set; }
    public bool AutoRenew { get; set; }
    public string Status { get; set; } = string.Empty;
}
