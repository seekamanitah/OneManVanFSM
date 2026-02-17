using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Controllers;

/// <summary>
/// Reports API endpoints for mobile business KPIs, job profitability,
/// payroll preview, and tech reports.
/// </summary>
[Route("api/reports")]
public class ReportsApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public ReportsApiController(AppDbContext db) => _db = db;

    /// <summary>GET /api/reports/techreport/{employeeId}</summary>
    [HttpGet("techreport/{employeeId:int}")]
    public async Task<ActionResult<MobileTechReportDto>> GetTechReport(int employeeId)
    {
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var monthEntries = await _db.TimeEntries
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= monthStart)
            .ToListAsync();

        var hoursToday = monthEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours);
        var hoursThisWeek = monthEntries.Where(t => t.StartTime.Date >= weekStart).Sum(t => t.Hours);
        var hoursThisMonth = monthEntries.Sum(t => t.Hours);
        var billable = monthEntries.Where(t => t.IsBillable).Sum(t => t.Hours);
        var nonBillable = monthEntries.Where(t => !t.IsBillable).Sum(t => t.Hours);

        var categories = monthEntries
            .GroupBy(t => t.TimeCategory ?? "Other")
            .Select(g => new MobileTimeCategoryDto { Category = g.Key, Hours = g.Sum(t => t.Hours) })
            .OrderByDescending(c => c.Hours).ToList();
        var totalHours = categories.Sum(c => c.Hours);
        foreach (var cat in categories)
            cat.Percent = totalHours > 0 ? Math.Round(cat.Hours / totalHours * 100, 1) : 0;

        var monthJobs = await _db.Jobs
            .Where(j => j.AssignedEmployeeId == employeeId).ToListAsync();

        var assignedThisMonth = monthJobs.Where(j => j.ScheduledDate.HasValue && j.ScheduledDate.Value >= monthStart).ToList();
        var completedThisMonth = monthJobs.Where(j => j.Status == JobStatus.Completed && j.CompletedDate.HasValue && j.CompletedDate.Value >= monthStart).ToList();

        var activeJobs = monthJobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();

        var dailyBreakdown = new List<MobileDailyDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            dailyBreakdown.Add(new MobileDailyDto
            {
                Date = date,
                DayLabel = date == today ? "Today" : date.ToString("ddd"),
                Hours = monthEntries.Where(t => t.StartTime.Date == date).Sum(t => t.Hours),
                JobsCompleted = completedThisMonth.Count(j => j.CompletedDate!.Value.Date == date),
            });
        }

        return Ok(new MobileTechReportDto
        {
            HoursToday = hoursToday,
            HoursThisWeek = hoursThisWeek,
            HoursThisMonth = hoursThisMonth,
            BillableHoursThisMonth = billable,
            NonBillableHoursThisMonth = nonBillable,
            JobsCompletedToday = completedThisMonth.Count(j => j.CompletedDate!.Value.Date == today),
            JobsCompletedThisWeek = completedThisMonth.Count(j => j.CompletedDate!.Value.Date >= weekStart),
            JobsCompletedThisMonth = completedThisMonth.Count,
            JobsAssignedThisMonth = assignedThisMonth.Count,
            AvgJobDurationHours = completedThisMonth.Any()
                ? Math.Round(completedThisMonth.Where(j => j.EstimatedDuration.HasValue).Select(j => j.EstimatedDuration!.Value).DefaultIfEmpty(0).Average(), 1) : 0,
            ScheduledJobs = activeJobs.Count(j => j.Status == JobStatus.Scheduled),
            InProgressJobs = activeJobs.Count(j => j.Status == JobStatus.InProgress || j.Status == JobStatus.OnSite || j.Status == JobStatus.EnRoute),
            CompletedJobs = completedThisMonth.Count,
            OverdueJobs = activeJobs.Count(j => j.ScheduledDate.HasValue && j.ScheduledDate.Value.Date < today),
            DailyBreakdown = dailyBreakdown,
            TimeCategories = categories,
        });
    }

    /// <summary>GET /api/reports/kpis</summary>
    [HttpGet("kpis")]
    public async Task<ActionResult<MobileBusinessKPIsDto>> GetBusinessKPIs()
    {
        var jobs = await _db.Jobs.Where(j => !j.IsArchived).ToListAsync();
        var invoices = await _db.Invoices.Where(i => !i.IsArchived).ToListAsync();
        var inventoryItems = await _db.InventoryItems.Where(i => !i.IsArchived).ToListAsync();

        return Ok(new MobileBusinessKPIsDto
        {
            TotalJobs = jobs.Count,
            CompletedJobs = jobs.Count(j => j.Status == JobStatus.Completed),
            OpenJobs = jobs.Count(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled && j.Status != JobStatus.Closed),
            RevenueTotal = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total),
            OutstandingAR = invoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void).Sum(i => i.BalanceDue),
            UnpaidInvoiceCount = invoices.Count(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void),
            ActiveEmployees = await _db.Employees.CountAsync(e => !e.IsArchived && e.Status == EmployeeStatus.Active),
            TotalHoursLogged = await _db.TimeEntries.Where(t => t.EntryType == TimeEntryType.Shift).SumAsync(t => t.Hours),
            TotalAssets = await _db.Assets.CountAsync(a => !a.IsArchived),
            ActiveAgreements = await _db.ServiceAgreements.CountAsync(sa => sa.Status == AgreementStatus.Active),
            PendingEstimates = await _db.Estimates.CountAsync(e => !e.IsArchived && e.Status == EstimateStatus.Draft),
            TotalCustomers = await _db.Customers.CountAsync(c => !c.IsArchived),
            InventoryItemCount = inventoryItems.Count,
            LowStockCount = inventoryItems.Count(i => i.Quantity <= i.MinThreshold && i.MinThreshold > 0),
        });
    }

    /// <summary>GET /api/reports/profitability?count=20</summary>
    [HttpGet("profitability")]
    public async Task<ActionResult<List<MobileJobProfitItemDto>>> GetJobProfitability([FromQuery] int count = 20)
    {
        var completedJobs = await _db.Jobs.AsNoTracking()
            .Include(j => j.Customer)
            .Where(j => !j.IsArchived && j.Status == JobStatus.Completed)
            .OrderByDescending(j => j.CompletedDate)
            .Take(count).ToListAsync();

        var jobIds = completedJobs.Select(j => j.Id).ToList();

        var timeEntries = await _db.TimeEntries.AsNoTracking()
            .Where(t => t.JobId.HasValue && jobIds.Contains(t.JobId.Value)).ToListAsync();
        var expenses = await _db.Expenses.AsNoTracking()
            .Where(e => e.JobId.HasValue && jobIds.Contains(e.JobId.Value)).ToListAsync();
        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => !i.IsArchived && i.JobId.HasValue && jobIds.Contains(i.JobId.Value)).ToListAsync();

        var result = completedJobs.Select(j =>
        {
            var laborCost = timeEntries.Where(t => t.JobId == j.Id).Sum(t => t.Hours * (t.HourlyRate ?? 0));
            var expenseCost = expenses.Where(e => e.JobId == j.Id).Sum(e => e.Total);
            var revenue = invoices.Where(i => i.JobId == j.Id && i.Status == InvoiceStatus.Paid).Sum(i => i.Total);

            return new MobileJobProfitItemDto
            {
                JobId = j.Id, JobNumber = j.JobNumber, CustomerName = j.Customer?.Name,
                Trade = j.TradeType, Revenue = revenue, LaborCost = laborCost,
                MaterialCost = 0, ExpenseCost = expenseCost, CompletedDate = j.CompletedDate,
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>GET /api/reports/payroll/{employeeId}?weekStart=2025-01-06</summary>
    [HttpGet("payroll/{employeeId:int}")]
    public async Task<ActionResult<MobilePayrollPreviewDto>> GetPayrollPreview(int employeeId, [FromQuery] DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);
        if (emp is null)
            return Ok(new MobilePayrollPreviewDto { WeekStart = weekStart, WeekEnd = weekEnd });

        var entries = await _db.TimeEntries.AsNoTracking()
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= weekStart && t.StartTime < weekEnd)
            .ToListAsync();

        var shiftEntries = entries.Where(t => t.EntryType == TimeEntryType.Shift).ToList();
        var jobEntries = entries.Where(t => t.EntryType == TimeEntryType.JobClock).ToList();

        var totalShiftHours = shiftEntries.Sum(e => e.Hours);
        var regularHours = Math.Min(totalShiftHours, 40);
        var overtimeHours = Math.Max(totalShiftHours - 40, 0);
        var otRate = emp.OvertimeRate ?? emp.HourlyRate * 1.5m;

        var days = new List<MobilePayrollDayDto>();
        for (var d = weekStart; d < weekEnd; d = d.AddDays(1))
        {
            var dayDate = d.Date;
            days.Add(new MobilePayrollDayDto
            {
                Date = dayDate,
                DayLabel = dayDate.ToString("ddd"),
                ShiftHours = shiftEntries.Where(e => e.StartTime.Date == dayDate).Sum(e => e.Hours),
                JobHours = jobEntries.Where(e => e.StartTime.Date == dayDate).Sum(e => e.Hours),
                JobCount = jobEntries.Count(e => e.StartTime.Date == dayDate),
            });
        }

        return Ok(new MobilePayrollPreviewDto
        {
            WeekStart = weekStart, WeekEnd = weekEnd,
            TotalShiftHours = totalShiftHours, RegularHours = regularHours,
            OvertimeHours = overtimeHours,
            RegularPay = regularHours * emp.HourlyRate,
            OvertimePay = overtimeHours * otRate,
            TotalPay = (regularHours * emp.HourlyRate) + (overtimeHours * otRate),
            HourlyRate = emp.HourlyRate,
            JobsClockedThisWeek = jobEntries.Select(e => e.JobId).Distinct().Count(),
            Days = days,
        });
    }
}

// ── API DTOs ──────────────────────────────────────────────────────────
public class MobileTechReportDto
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
    public List<MobileDailyDto> DailyBreakdown { get; set; } = [];
    public List<MobileTimeCategoryDto> TimeCategories { get; set; } = [];
}

public class MobileDailyDto
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = "";
    public decimal Hours { get; set; }
    public int JobsCompleted { get; set; }
}

public class MobileTimeCategoryDto
{
    public string Category { get; set; } = "";
    public decimal Hours { get; set; }
    public decimal Percent { get; set; }
}

public class MobileBusinessKPIsDto
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

public class MobileJobProfitItemDto
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

public class MobilePayrollPreviewDto
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
    public List<MobilePayrollDayDto> Days { get; set; } = [];
}

public class MobilePayrollDayDto
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = "";
    public decimal ShiftHours { get; set; }
    public decimal JobHours { get; set; }
    public int JobCount { get; set; }
}
