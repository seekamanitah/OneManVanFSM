using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileDashboardService(AppDbContext db) : IMobileDashboardService
{
    public async Task<MobileDashboardData> GetDashboardAsync(int employeeId, bool isElevated = false)
    {
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var todayJobs = await db.Jobs.AsNoTracking()
            .Include(j => j.Customer)
            .Include(j => j.Company)
            .Include(j => j.Site)
            .Where(j => !j.IsArchived
                && (isElevated || j.AssignedEmployeeId == employeeId)
                && j.ScheduledDate != null
                && j.ScheduledDate.Value.Date == today
                && j.Status != JobStatus.Completed
                && j.Status != JobStatus.Closed
                && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.ScheduledDate)
            .ThenBy(j => j.ScheduledTime)
            .Select(j => new MobileJobCard
            {
                Id = j.Id,
                JobNumber = j.JobNumber,
                Title = j.Title,
                CustomerName = j.Customer != null ? j.Customer.Name : null,
                CompanyName = j.Company != null ? j.Company.Name : null,
                SiteAddress = j.Site != null ? (j.Site.Address + ", " + j.Site.City) : null,
                Status = j.Status,
                Priority = j.Priority,
                ScheduledDate = j.ScheduledDate,
                ScheduledTime = j.ScheduledTime,
                EstimatedDuration = j.EstimatedDuration,
            })
            .ToListAsync();

        var openJobCount = await db.Jobs
            .CountAsync(j => !j.IsArchived
                && (isElevated || j.AssignedEmployeeId == employeeId)
                && j.Status != JobStatus.Completed
                && j.Status != JobStatus.Closed
                && j.Status != JobStatus.Cancelled);

        var pendingNotes = await db.QuickNotes
            .CountAsync(n => (isElevated || n.CreatedByEmployeeId == employeeId)
                && n.Status == QuickNoteStatus.Active
                && n.IsUrgent);

        var timeEntries = await db.TimeEntries.AsNoTracking()
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= weekStart)
            .ToListAsync();

        var shiftEntries = timeEntries.Where(t => t.EntryType == TimeEntryType.Shift).ToList();
        var jobClockEntries = timeEntries.Where(t => t.EntryType == TimeEntryType.JobClock).ToList();
        var breakEntries = timeEntries.Where(t => t.EntryType == TimeEntryType.Break).ToList();

        var hoursToday = shiftEntries
            .Where(t => t.StartTime.Date == today)
            .Sum(t => t.Hours);

        var hoursThisWeek = shiftEntries.Sum(t => t.Hours);

        // For month totals, query separately since weekStart may not cover full month
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEntries = await db.TimeEntries.AsNoTracking()
            .Where(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Shift
                && t.StartTime >= monthStart)
            .ToListAsync();
        var hoursThisMonth = monthEntries.Sum(t => t.Hours);

        var jobHoursToday = jobClockEntries
            .Where(t => t.StartTime.Date == today)
            .Sum(t => t.Hours);

        var activeClock = await db.TimeEntries.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Shift
                && t.EndTime == null);

        var activeBreak = await db.TimeEntries.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Break
                && t.EndTime == null);

        var totalBreakMinutesToday = (int)breakEntries
            .Where(t => t.StartTime.Date == today && t.EndTime.HasValue)
            .Sum(t => (decimal)(t.EndTime!.Value - t.StartTime).TotalMinutes);

        var activeJobClocks = await db.TimeEntries.AsNoTracking()
            .Include(t => t.Job)
                .ThenInclude(j => j!.Customer)
            .Where(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.JobClock
                && t.EndTime == null)
            .ToListAsync();

        var completedThisWeek = await db.Jobs
            .CountAsync(j => (isElevated || j.AssignedEmployeeId == employeeId)
                && j.Status == JobStatus.Completed
                && j.CompletedDate != null
                && j.CompletedDate.Value >= weekStart);

        var overdueJobCount = await db.Jobs
            .CountAsync(j => !j.IsArchived
                && (isElevated || j.AssignedEmployeeId == employeeId)
                && j.ScheduledDate != null
                && j.ScheduledDate.Value.Date < today
                && j.Status != JobStatus.Completed
                && j.Status != JobStatus.Closed
                && j.Status != JobStatus.Cancelled);

        var upcomingJobs = await db.Jobs.AsNoTracking()
            .Include(j => j.Customer)
            .Include(j => j.Company)
            .Include(j => j.Site)
            .Where(j => !j.IsArchived
                && (isElevated || j.AssignedEmployeeId == employeeId)
                && j.ScheduledDate != null
                && j.ScheduledDate.Value.Date > today
                && j.ScheduledDate.Value.Date <= today.AddDays(3)
                && j.Status != JobStatus.Completed
                && j.Status != JobStatus.Closed
                && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.ScheduledDate)
            .ThenBy(j => j.ScheduledTime)
            .Take(5)
            .Select(j => new MobileJobCard
            {
                Id = j.Id,
                JobNumber = j.JobNumber,
                Title = j.Title,
                CustomerName = j.Customer != null ? j.Customer.Name : null,
                CompanyName = j.Company != null ? j.Company.Name : null,
                SiteAddress = j.Site != null ? (j.Site.Address + ", " + j.Site.City) : null,
                Status = j.Status,
                Priority = j.Priority,
                ScheduledDate = j.ScheduledDate,
                ScheduledTime = j.ScheduledTime,
                EstimatedDuration = j.EstimatedDuration,
            })
            .ToListAsync();

        var lowStockCount = await db.InventoryItems
            .CountAsync(i => !i.IsArchived && i.Quantity <= i.MinThreshold && i.MinThreshold > 0);

        var expiringAgreementCount = await db.ServiceAgreements
            .CountAsync(sa => sa.Status == AgreementStatus.Active
                && sa.EndDate <= today.AddDays(30));

        var recentJobs = await db.Jobs.AsNoTracking()
            .Include(j => j.Customer)
            .Where(j => isElevated || j.AssignedEmployeeId == employeeId)
            .OrderByDescending(j => j.UpdatedAt)
            .Take(5)
            .ToListAsync();

        var recentActivity = recentJobs.Select(j => new MobileActivityItem
        {
            Description = j.JobNumber + " - " + j.Status + (j.Customer != null ? " (" + j.Customer.Name + ")" : ""),
            Icon = j.Status == JobStatus.Completed ? "bi-check-circle-fill text-success" : "bi-wrench text-primary",
            Timestamp = j.UpdatedAt,
        }).ToList();

        var maintenanceDueCount = await db.Assets
            .CountAsync(a => !a.IsArchived
                && a.NextServiceDue.HasValue
                && a.NextServiceDue.Value.Date <= today.AddDays(14));

        var warrantyAlertCount = await db.Assets
            .CountAsync(a => !a.IsArchived
                && !a.NoWarranty
                && a.Status == AssetStatus.Active
                && ((a.LaborWarrantyExpiry.HasValue && (a.LaborWarrantyTermYears ?? 0) > 0 && a.LaborWarrantyExpiry.Value >= today.AddDays(-30) && a.LaborWarrantyExpiry.Value <= today.AddDays(90))
                    || (a.PartsWarrantyExpiry.HasValue && (a.PartsWarrantyTermYears ?? 0) > 0 && a.PartsWarrantyExpiry.Value >= today.AddDays(-30) && a.PartsWarrantyExpiry.Value <= today.AddDays(90))
                    || (a.CompressorWarrantyExpiry.HasValue && (a.CompressorWarrantyTermYears ?? 0) > 0 && a.CompressorWarrantyExpiry.Value >= today.AddDays(-30) && a.CompressorWarrantyExpiry.Value <= today.AddDays(90))));

        var draftEstimateCount = await db.Estimates
            .CountAsync(e => !e.IsArchived && e.Status == EstimateStatus.Draft);

        var pendingInvoiceCount = await db.Invoices
            .CountAsync(i => !i.IsArchived
                && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Invoiced || i.Status == InvoiceStatus.Overdue));

        return new MobileDashboardData
        {
            TodayJobCount = todayJobs.Count,
            OpenJobCount = openJobCount,
            PendingNoteCount = pendingNotes,
            HoursToday = hoursToday,
            HoursThisWeek = hoursThisWeek,
            HoursThisMonth = hoursThisMonth,
            IsClockedIn = activeClock != null,
            ClockInTime = activeClock?.StartTime,
            IsPaused = activeBreak != null,
            PauseStartTime = activeBreak?.StartTime,
            TotalBreakMinutesToday = totalBreakMinutesToday,
            CompletedThisWeek = completedThisWeek,
            OverdueJobCount = overdueJobCount,
            UpcomingJobCount = upcomingJobs.Count,
            LowStockCount = lowStockCount,
            ExpiringAgreementCount = expiringAgreementCount,
            MaintenanceDueCount = maintenanceDueCount,
            WarrantyAlertCount = warrantyAlertCount,
            DraftEstimateCount = draftEstimateCount,
            PendingInvoiceCount = pendingInvoiceCount,
            ActiveJobClockCount = activeJobClocks.Count,
            ActiveJobName = activeJobClocks.FirstOrDefault()?.Job?.Title
                ?? activeJobClocks.FirstOrDefault()?.Job?.JobNumber,
            ActiveJobId = activeJobClocks.FirstOrDefault()?.Job?.Id,
            ActiveJobNumber = activeJobClocks.FirstOrDefault()?.Job?.JobNumber,
            ActiveJobCustomerName = activeJobClocks.FirstOrDefault()?.Job?.Customer?.Name,
            JobHoursToday = jobHoursToday,
            TodayJobs = todayJobs,
            UpcomingJobs = upcomingJobs,
            RecentActivity = recentActivity,
        };
    }
}
