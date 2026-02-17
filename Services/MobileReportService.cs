using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileReportService : IMobileReportService
{
    private readonly AppDbContext _db;

    public MobileReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MobileTechReport> GetTechReportAsync(int employeeId)
    {
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var report = new MobileTechReport();

        // Time entries
        var monthEntries = await _db.TimeEntries
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= monthStart)
            .ToListAsync();

        report.HoursToday = monthEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours);
        report.HoursThisWeek = monthEntries.Where(t => t.StartTime.Date >= weekStart).Sum(t => t.Hours);
        report.HoursThisMonth = monthEntries.Sum(t => t.Hours);
        report.BillableHoursThisMonth = monthEntries.Where(t => t.IsBillable).Sum(t => t.Hours);
        report.NonBillableHoursThisMonth = monthEntries.Where(t => !t.IsBillable).Sum(t => t.Hours);

        // Time category breakdown
        var categories = monthEntries
            .GroupBy(t => t.TimeCategory ?? "Other")
            .Select(g => new MobileTimeCategoryBreakdown
            {
                Category = g.Key,
                Hours = g.Sum(t => t.Hours),
            })
            .OrderByDescending(c => c.Hours)
            .ToList();

        var totalHours = categories.Sum(c => c.Hours);
        foreach (var cat in categories)
        {
            cat.Percent = totalHours > 0 ? Math.Round(cat.Hours / totalHours * 100, 1) : 0;
        }
        report.TimeCategories = categories;

        // Jobs
        var monthJobs = await _db.Jobs
            .Where(j => j.AssignedEmployeeId == employeeId)
            .ToListAsync();

        var assignedThisMonth = monthJobs.Where(j =>
            j.ScheduledDate.HasValue && j.ScheduledDate.Value >= monthStart).ToList();
        var completedThisMonth = monthJobs.Where(j =>
            j.Status == JobStatus.Completed && j.CompletedDate.HasValue && j.CompletedDate.Value >= monthStart).ToList();

        report.JobsCompletedToday = completedThisMonth.Count(j => j.CompletedDate!.Value.Date == today);
        report.JobsCompletedThisWeek = completedThisMonth.Count(j => j.CompletedDate!.Value.Date >= weekStart);
        report.JobsCompletedThisMonth = completedThisMonth.Count;
        report.JobsAssignedThisMonth = assignedThisMonth.Count;

        if (completedThisMonth.Any())
        {
            var avgDuration = completedThisMonth
                .Where(j => j.EstimatedDuration.HasValue)
                .Select(j => j.EstimatedDuration!.Value)
                .DefaultIfEmpty(0)
                .Average();
            report.AvgJobDurationHours = Math.Round(avgDuration, 1);
        }

        // Job status distribution
        var activeJobs = monthJobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
        report.ScheduledJobs = activeJobs.Count(j => j.Status == JobStatus.Scheduled);
        report.InProgressJobs = activeJobs.Count(j => j.Status == JobStatus.InProgress || j.Status == JobStatus.OnSite || j.Status == JobStatus.EnRoute);
        report.CompletedJobs = completedThisMonth.Count;
        report.OverdueJobs = activeJobs.Count(j =>
            j.ScheduledDate.HasValue && j.ScheduledDate.Value.Date < today);

        // Daily breakdown (last 7 days)
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            report.DailyBreakdown.Add(new MobileDailyBreakdown
            {
                Date = date,
                DayLabel = date == today ? "Today" : date.ToString("ddd"),
                Hours = monthEntries.Where(t => t.StartTime.Date == date).Sum(t => t.Hours),
                JobsCompleted = completedThisMonth.Count(j => j.CompletedDate!.Value.Date == date),
            });
        }

        // Materials this month
        var jobIds = monthJobs.Select(j => j.Id).ToList();
        var materialLists = await _db.MaterialLists
            .Where(m => m.Id > 0)
            .Include(m => m.Items)
            .ToListAsync();

        report.MaterialsCostThisMonth = materialLists.Sum(m => m.Total);
        report.MaterialItemsUsed = materialLists.SelectMany(m => m.Items).Count();

        return report;
    }

    public async Task<MobileBusinessKPIs> GetBusinessKPIsAsync()
    {
        var jobs = await _db.Jobs.Where(j => !j.IsArchived).ToListAsync();
        var invoices = await _db.Invoices.Where(i => !i.IsArchived).ToListAsync();
        var inventoryItems = await _db.InventoryItems.Where(i => !i.IsArchived).ToListAsync();

        return new MobileBusinessKPIs
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
        };
    }

    public async Task<List<MobileJobProfitItem>> GetJobProfitabilityAsync(int count = 20)
    {
        var completedJobs = await _db.Jobs.AsNoTracking()
            .Include(j => j.Customer)
            .Where(j => !j.IsArchived && j.Status == JobStatus.Completed)
            .OrderByDescending(j => j.CompletedDate)
            .Take(count)
            .ToListAsync();

        var jobIds = completedJobs.Select(j => j.Id).ToList();

        var timeEntries = await _db.TimeEntries.AsNoTracking()
            .Where(t => t.JobId.HasValue && jobIds.Contains(t.JobId.Value))
            .ToListAsync();

        var expenses = await _db.Expenses.AsNoTracking()
            .Where(e => e.JobId.HasValue && jobIds.Contains(e.JobId.Value))
            .ToListAsync();

        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => !i.IsArchived && i.JobId.HasValue && jobIds.Contains(i.JobId.Value))
            .ToListAsync();

        return completedJobs.Select(j =>
        {
            var jobTime = timeEntries.Where(t => t.JobId == j.Id).ToList();
            var laborCost = jobTime.Sum(t => t.Hours * (t.HourlyRate ?? 0));
            var expenseCost = expenses.Where(e => e.JobId == j.Id).Sum(e => e.Total);
            var revenue = invoices.Where(i => i.JobId == j.Id && i.Status == InvoiceStatus.Paid).Sum(i => i.Total);

            return new MobileJobProfitItem
            {
                JobId = j.Id,
                JobNumber = j.JobNumber,
                CustomerName = j.Customer?.Name,
                Trade = j.TradeType,
                Revenue = revenue,
                LaborCost = laborCost,
                MaterialCost = 0, // TODO: derive from material lists when linked
                ExpenseCost = expenseCost,
                CompletedDate = j.CompletedDate,
            };
        }).ToList();
    }

    public async Task<MobilePayrollPreview> GetPayrollPreviewAsync(int employeeId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);
        if (emp is null)
            return new MobilePayrollPreview { WeekStart = weekStart, WeekEnd = weekEnd };

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

        var days = new List<MobilePayrollDayPreview>();
        for (var d = weekStart; d < weekEnd; d = d.AddDays(1))
        {
            var dayDate = d.Date;
            days.Add(new MobilePayrollDayPreview
            {
                Date = dayDate,
                DayLabel = dayDate.ToString("ddd"),
                ShiftHours = shiftEntries.Where(e => e.StartTime.Date == dayDate).Sum(e => e.Hours),
                JobHours = jobEntries.Where(e => e.StartTime.Date == dayDate).Sum(e => e.Hours),
                JobCount = jobEntries.Count(e => e.StartTime.Date == dayDate),
            });
        }

        return new MobilePayrollPreview
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            TotalShiftHours = totalShiftHours,
            RegularHours = regularHours,
            OvertimeHours = overtimeHours,
            RegularPay = regularHours * emp.HourlyRate,
            OvertimePay = overtimeHours * otRate,
            TotalPay = (regularHours * emp.HourlyRate) + (overtimeHours * otRate),
            HourlyRate = emp.HourlyRate,
            JobsClockedThisWeek = jobEntries.Select(e => e.JobId).Distinct().Count(),
            Days = days,
        };
    }
}
