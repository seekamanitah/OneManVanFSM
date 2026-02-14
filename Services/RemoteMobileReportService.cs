using Microsoft.Extensions.Logging;

namespace OneManVanFSM.Services;

/// <summary>
/// Remote-mode report service. Fetches aggregated reports from the
/// server's ReportsApiController endpoints.
/// </summary>
public class RemoteMobileReportService : IMobileReportService
{
    private readonly ApiClient _api;
    private readonly ILogger<RemoteMobileReportService> _logger;

    public RemoteMobileReportService(ApiClient api, ILogger<RemoteMobileReportService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<MobileTechReport> GetTechReportAsync(int employeeId)
    {
        try
        {
            var dto = await _api.GetAsync<MobileTechReportApiDto>($"api/reports/techreport/{employeeId}");
            if (dto is not null)
                return MapTechReport(dto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch tech report from API for employee {Id}.", employeeId);
        }

        return new MobileTechReport();
    }

    public async Task<MobileBusinessKPIs> GetBusinessKPIsAsync()
    {
        try
        {
            var dto = await _api.GetAsync<MobileBusinessKPIsApiDto>("api/reports/kpis");
            if (dto is not null)
            {
                return new MobileBusinessKPIs
                {
                    TotalJobs = dto.TotalJobs, CompletedJobs = dto.CompletedJobs, OpenJobs = dto.OpenJobs,
                    RevenueTotal = dto.RevenueTotal, OutstandingAR = dto.OutstandingAR,
                    UnpaidInvoiceCount = dto.UnpaidInvoiceCount, ActiveEmployees = dto.ActiveEmployees,
                    TotalHoursLogged = dto.TotalHoursLogged, TotalAssets = dto.TotalAssets,
                    ActiveAgreements = dto.ActiveAgreements, PendingEstimates = dto.PendingEstimates,
                    TotalCustomers = dto.TotalCustomers, InventoryItemCount = dto.InventoryItemCount,
                    LowStockCount = dto.LowStockCount,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch business KPIs from API.");
        }

        return new MobileBusinessKPIs();
    }

    public async Task<List<MobileJobProfitItem>> GetJobProfitabilityAsync(int count = 20)
    {
        try
        {
            var dtos = await _api.GetAsync<List<MobileJobProfitItemApiDto>>($"api/reports/profitability?count={count}");
            if (dtos is not null)
            {
                return dtos.Select(d => new MobileJobProfitItem
                {
                    JobId = d.JobId, JobNumber = d.JobNumber, CustomerName = d.CustomerName,
                    Trade = d.Trade, Revenue = d.Revenue, LaborCost = d.LaborCost,
                    MaterialCost = d.MaterialCost, ExpenseCost = d.ExpenseCost,
                    CompletedDate = d.CompletedDate,
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch job profitability from API.");
        }

        return [];
    }

    public async Task<MobilePayrollPreview> GetPayrollPreviewAsync(int employeeId, DateTime weekStart)
    {
        try
        {
            var dto = await _api.GetAsync<MobilePayrollPreviewApiDto>(
                $"api/reports/payroll/{employeeId}?weekStart={weekStart:yyyy-MM-dd}");
            if (dto is not null)
            {
                return new MobilePayrollPreview
                {
                    WeekStart = dto.WeekStart, WeekEnd = dto.WeekEnd,
                    TotalShiftHours = dto.TotalShiftHours, RegularHours = dto.RegularHours,
                    OvertimeHours = dto.OvertimeHours, RegularPay = dto.RegularPay,
                    OvertimePay = dto.OvertimePay, TotalPay = dto.TotalPay,
                    HourlyRate = dto.HourlyRate, JobsClockedThisWeek = dto.JobsClockedThisWeek,
                    Days = dto.Days.Select(d => new MobilePayrollDayPreview
                    {
                        Date = d.Date, DayLabel = d.DayLabel,
                        ShiftHours = d.ShiftHours, JobHours = d.JobHours, JobCount = d.JobCount,
                    }).ToList(),
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch payroll preview from API for employee {Id}.", employeeId);
        }

        return new MobilePayrollPreview { WeekStart = weekStart, WeekEnd = weekStart.AddDays(7) };
    }

    private static MobileTechReport MapTechReport(MobileTechReportApiDto dto) => new()
    {
        HoursToday = dto.HoursToday, HoursThisWeek = dto.HoursThisWeek,
        HoursThisMonth = dto.HoursThisMonth,
        BillableHoursThisMonth = dto.BillableHoursThisMonth,
        NonBillableHoursThisMonth = dto.NonBillableHoursThisMonth,
        JobsCompletedToday = dto.JobsCompletedToday,
        JobsCompletedThisWeek = dto.JobsCompletedThisWeek,
        JobsCompletedThisMonth = dto.JobsCompletedThisMonth,
        JobsAssignedThisMonth = dto.JobsAssignedThisMonth,
        AvgJobDurationHours = dto.AvgJobDurationHours,
        ScheduledJobs = dto.ScheduledJobs, InProgressJobs = dto.InProgressJobs,
        CompletedJobs = dto.CompletedJobs, OverdueJobs = dto.OverdueJobs,
        DailyBreakdown = dto.DailyBreakdown.Select(d => new MobileDailyBreakdown
        {
            Date = d.Date, DayLabel = d.DayLabel, Hours = d.Hours, JobsCompleted = d.JobsCompleted,
        }).ToList(),
        TimeCategories = dto.TimeCategories.Select(c => new MobileTimeCategoryBreakdown
        {
            Category = c.Category, Hours = c.Hours, Percent = c.Percent,
        }).ToList(),
    };
}

// ── API response DTOs (match ReportsApiController JSON shapes) ──────
internal class MobileTechReportApiDto
{
    public decimal HoursToday { get; set; }
    public decimal HoursThisWeek { get; set; }
    public decimal HoursThisMonth { get; set; }
    public decimal BillableHoursThisMonth { get; set; }
    public decimal NonBillableHoursThisMonth { get; set; }
    public int JobsCompletedToday { get; set; }
    public int JobsCompletedThisWeek { get; set; }
    public int JobsCompletedThisMonth { get; set; }
    public int JobsAssignedThisMonth { get; set; }
    public decimal AvgJobDurationHours { get; set; }
    public int ScheduledJobs { get; set; }
    public int InProgressJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int OverdueJobs { get; set; }
    public List<MobileDailyApiDto> DailyBreakdown { get; set; } = [];
    public List<MobileTimeCategoryApiDto> TimeCategories { get; set; } = [];
}

internal class MobileDailyApiDto
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = "";
    public decimal Hours { get; set; }
    public int JobsCompleted { get; set; }
}

internal class MobileTimeCategoryApiDto
{
    public string Category { get; set; } = "";
    public decimal Hours { get; set; }
    public decimal Percent { get; set; }
}

internal class MobileBusinessKPIsApiDto
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

internal class MobileJobProfitItemApiDto
{
    public int JobId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? Trade { get; set; }
    public decimal Revenue { get; set; }
    public decimal LaborCost { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal ExpenseCost { get; set; }
    public DateTime? CompletedDate { get; set; }
}

internal class MobilePayrollPreviewApiDto
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
    public List<MobilePayrollDayApiDto> Days { get; set; } = [];
}

internal class MobilePayrollDayApiDto
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = "";
    public decimal ShiftHours { get; set; }
    public decimal JobHours { get; set; }
    public int JobCount { get; set; }
}
