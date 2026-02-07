using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileDashboardService(AppDbContext db) : IMobileDashboardService
{
    public async Task<MobileDashboardData> GetDashboardAsync(int employeeId)
    {
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var todayJobs = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Where(j => !j.IsArchived
                && j.AssignedEmployeeId == employeeId
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
                && j.AssignedEmployeeId == employeeId
                && j.Status != JobStatus.Completed
                && j.Status != JobStatus.Closed
                && j.Status != JobStatus.Cancelled);

        var pendingNotes = await db.QuickNotes
            .CountAsync(n => n.CreatedByEmployeeId == employeeId
                && n.Status == QuickNoteStatus.Active
                && n.IsUrgent);

        var timeEntries = await db.TimeEntries
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= weekStart)
            .ToListAsync();

        var hoursToday = timeEntries
            .Where(t => t.StartTime.Date == today)
            .Sum(t => t.Hours);

        var hoursThisWeek = timeEntries.Sum(t => t.Hours);

        var activeClock = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);

        var completedThisWeek = await db.Jobs
            .CountAsync(j => j.AssignedEmployeeId == employeeId
                && j.Status == JobStatus.Completed
                && j.CompletedDate != null
                && j.CompletedDate.Value >= weekStart);

        var overdueJobCount = await db.Jobs
            .CountAsync(j => !j.IsArchived
                && j.AssignedEmployeeId == employeeId
                && j.ScheduledDate != null
                && j.ScheduledDate.Value.Date < today
                && j.Status != JobStatus.Completed
                && j.Status != JobStatus.Closed
                && j.Status != JobStatus.Cancelled);

        var upcomingJobs = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Where(j => !j.IsArchived
                && j.AssignedEmployeeId == employeeId
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

        var recentJobs = await db.Jobs
            .Include(j => j.Customer)
            .Where(j => j.AssignedEmployeeId == employeeId)
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
                && (a.LaborWarrantyExpiry.HasValue || a.PartsWarrantyExpiry.HasValue || a.CompressorWarrantyExpiry.HasValue)
                && ((a.LaborWarrantyExpiry.HasValue && a.LaborWarrantyExpiry.Value >= today.AddDays(-30) && a.LaborWarrantyExpiry.Value <= today.AddDays(90))
                    || (a.PartsWarrantyExpiry.HasValue && a.PartsWarrantyExpiry.Value >= today.AddDays(-30) && a.PartsWarrantyExpiry.Value <= today.AddDays(90))
                    || (a.CompressorWarrantyExpiry.HasValue && a.CompressorWarrantyExpiry.Value >= today.AddDays(-30) && a.CompressorWarrantyExpiry.Value <= today.AddDays(90))));

        return new MobileDashboardData
        {
            TodayJobCount = todayJobs.Count,
            OpenJobCount = openJobCount,
            PendingNoteCount = pendingNotes,
            HoursToday = hoursToday,
            HoursThisWeek = hoursThisWeek,
            IsClockedIn = activeClock != null,
            ClockInTime = activeClock?.StartTime,
            CompletedThisWeek = completedThisWeek,
            OverdueJobCount = overdueJobCount,
            UpcomingJobCount = upcomingJobs.Count,
            LowStockCount = lowStockCount,
            ExpiringAgreementCount = expiringAgreementCount,
            MaintenanceDueCount = maintenanceDueCount,
            WarrantyAlertCount = warrantyAlertCount,
            TodayJobs = todayJobs,
            UpcomingJobs = upcomingJobs,
            RecentActivity = recentActivity,
        };
    }
}
