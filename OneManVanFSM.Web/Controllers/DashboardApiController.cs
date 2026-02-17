using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Controllers;

/// <summary>
/// Dashboard API endpoint that computes the same aggregated dashboard data
/// the mobile app needs — job counts, time summaries, alerts, etc.
/// </summary>
[Route("api/dashboard")]
public class DashboardApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public DashboardApiController(AppDbContext db) => _db = db;

    /// <summary>
    /// GET /api/dashboard/{employeeId}
    /// Returns the full mobile dashboard payload for the given employee.
    /// </summary>
    [HttpGet("{employeeId:int}")]
    public async Task<ActionResult<MobileDashboardResponse>> GetDashboard(int employeeId, [FromQuery] bool elevated = false)
    {
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var todayJobs = await _db.Jobs
            .Include(j => j.Customer).Include(j => j.Company).Include(j => j.Site)
            .Where(j => !j.IsArchived && (elevated || j.AssignedEmployeeId == employeeId)
                && j.ScheduledDate != null && j.ScheduledDate.Value.Date == today
                && j.Status != JobStatus.Completed && j.Status != JobStatus.Closed && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.ScheduledDate).ThenBy(j => j.ScheduledTime)
            .Select(j => new MobileDashboardJobCard
            {
                Id = j.Id, JobNumber = j.JobNumber, Title = j.Title,
                CustomerName = j.Customer != null ? j.Customer.Name : null,
                CompanyName = j.Company != null ? j.Company.Name : null,
                SiteAddress = j.Site != null ? (j.Site.Address + ", " + j.Site.City) : null,
                Status = j.Status, Priority = j.Priority,
                ScheduledDate = j.ScheduledDate, ScheduledTime = j.ScheduledTime,
                EstimatedDuration = j.EstimatedDuration
            }).ToListAsync();

        var openJobCount = await _db.Jobs
            .CountAsync(j => !j.IsArchived && (elevated || j.AssignedEmployeeId == employeeId)
                && j.Status != JobStatus.Completed && j.Status != JobStatus.Closed && j.Status != JobStatus.Cancelled);

        var pendingNotes = await _db.QuickNotes
            .CountAsync(n => (elevated || n.CreatedByEmployeeId == employeeId) && n.Status == QuickNoteStatus.Active && n.IsUrgent);

        var timeEntries = await _db.TimeEntries
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= weekStart)
            .ToListAsync();

        var shiftEntries = timeEntries.Where(t => t.EntryType == TimeEntryType.Shift).ToList();
        var jobEntries = timeEntries.Where(t => t.EntryType == TimeEntryType.JobClock).ToList();
        var breakEntries = timeEntries.Where(t => t.EntryType == TimeEntryType.Break).ToList();
        var hoursToday = shiftEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours);
        var hoursThisWeek = shiftEntries.Sum(t => t.Hours);

        // Month totals
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthShiftEntries = await _db.TimeEntries
            .Where(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Shift && t.StartTime >= monthStart)
            .ToListAsync();
        var hoursThisMonth = monthShiftEntries.Sum(t => t.Hours);
        var jobHoursToday = jobEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours);

        var activeShift = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Shift && t.EndTime == null);

        var activeBreak = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Break && t.EndTime == null);

        var totalBreakMinutesToday = (int)breakEntries
            .Where(t => t.StartTime.Date == today && t.EndTime.HasValue)
            .Sum(t => (decimal)(t.EndTime!.Value - t.StartTime).TotalMinutes);

        var activeJobClocks = await _db.TimeEntries.AsNoTracking()
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.JobClock && t.EndTime == null)
            .ToListAsync();

        var draftEstimateCount = await _db.Estimates
            .CountAsync(e => !e.IsArchived && e.Status == EstimateStatus.Draft);

        var pendingInvoiceCount = await _db.Invoices
            .CountAsync(i => !i.IsArchived && (i.Status == InvoiceStatus.Draft || i.Status == InvoiceStatus.Sent));

        var completedThisWeek = await _db.Jobs
            .CountAsync(j => (elevated || j.AssignedEmployeeId == employeeId)
                && j.Status == JobStatus.Completed && j.CompletedDate != null && j.CompletedDate.Value >= weekStart);

        var overdueJobCount = await _db.Jobs
            .CountAsync(j => !j.IsArchived && (elevated || j.AssignedEmployeeId == employeeId)
                && j.ScheduledDate != null && j.ScheduledDate.Value.Date < today
                && j.Status != JobStatus.Completed && j.Status != JobStatus.Closed && j.Status != JobStatus.Cancelled);

        var upcomingJobs = await _db.Jobs
            .Include(j => j.Customer).Include(j => j.Company).Include(j => j.Site)
            .Where(j => !j.IsArchived && (elevated || j.AssignedEmployeeId == employeeId)
                && j.ScheduledDate != null && j.ScheduledDate.Value.Date > today
                && j.ScheduledDate.Value.Date <= today.AddDays(3)
                && j.Status != JobStatus.Completed && j.Status != JobStatus.Closed && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.ScheduledDate).ThenBy(j => j.ScheduledTime).Take(5)
            .Select(j => new MobileDashboardJobCard
            {
                Id = j.Id, JobNumber = j.JobNumber, Title = j.Title,
                CustomerName = j.Customer != null ? j.Customer.Name : null,
                CompanyName = j.Company != null ? j.Company.Name : null,
                SiteAddress = j.Site != null ? (j.Site.Address + ", " + j.Site.City) : null,
                Status = j.Status, Priority = j.Priority,
                ScheduledDate = j.ScheduledDate, ScheduledTime = j.ScheduledTime,
                EstimatedDuration = j.EstimatedDuration
            }).ToListAsync();

        var lowStockCount = await _db.InventoryItems
            .CountAsync(i => !i.IsArchived && i.Quantity <= i.MinThreshold && i.MinThreshold > 0);

        var expiringAgreementCount = await _db.ServiceAgreements
            .CountAsync(sa => sa.Status == AgreementStatus.Active && sa.EndDate <= today.AddDays(30));

        var maintenanceDueCount = await _db.Assets
            .CountAsync(a => !a.IsArchived && a.NextServiceDue.HasValue && a.NextServiceDue.Value.Date <= today.AddDays(14));

        var warrantyAlertCount = await _db.Assets
            .CountAsync(a => !a.IsArchived
                && !a.NoWarranty
                && a.Status == AssetStatus.Active
                && ((a.LaborWarrantyExpiry.HasValue && (a.LaborWarrantyTermYears ?? 0) > 0 && a.LaborWarrantyExpiry.Value >= today.AddDays(-30) && a.LaborWarrantyExpiry.Value <= today.AddDays(90))
                    || (a.PartsWarrantyExpiry.HasValue && (a.PartsWarrantyTermYears ?? 0) > 0 && a.PartsWarrantyExpiry.Value >= today.AddDays(-30) && a.PartsWarrantyExpiry.Value <= today.AddDays(90))
                    || (a.CompressorWarrantyExpiry.HasValue && (a.CompressorWarrantyTermYears ?? 0) > 0 && a.CompressorWarrantyExpiry.Value >= today.AddDays(-30) && a.CompressorWarrantyExpiry.Value <= today.AddDays(90))));

        var recentJobs = await _db.Jobs
            .Include(j => j.Customer)
            .Where(j => elevated || j.AssignedEmployeeId == employeeId)
            .OrderByDescending(j => j.UpdatedAt).Take(5).ToListAsync();

        var recentActivity = recentJobs.Select(j => new MobileDashboardActivity
        {
            Description = j.JobNumber + " - " + j.Status + (j.Customer != null ? " (" + j.Customer.Name + ")" : ""),
            Icon = j.Status == JobStatus.Completed ? "bi-check-circle-fill text-success" : "bi-wrench text-primary",
            Timestamp = j.UpdatedAt
        }).ToList();

        return Ok(new MobileDashboardResponse
        {
            TodayJobCount = todayJobs.Count, OpenJobCount = openJobCount,
            PendingNoteCount = pendingNotes, HoursToday = hoursToday,
            HoursThisWeek = hoursThisWeek, HoursThisMonth = hoursThisMonth,
            IsClockedIn = activeShift != null,
            ClockInTime = activeShift?.StartTime,
            IsPaused = activeBreak != null,
            PauseStartTime = activeBreak?.StartTime,
            TotalBreakMinutesToday = totalBreakMinutesToday,
            CompletedThisWeek = completedThisWeek,
            OverdueJobCount = overdueJobCount, UpcomingJobCount = upcomingJobs.Count,
            LowStockCount = lowStockCount, ExpiringAgreementCount = expiringAgreementCount,
            MaintenanceDueCount = maintenanceDueCount, WarrantyAlertCount = warrantyAlertCount,
            ActiveJobClockCount = activeJobClocks.Count,
            ActiveJobName = activeJobClocks.FirstOrDefault()?.Job?.Title
                ?? activeJobClocks.FirstOrDefault()?.Job?.JobNumber,
            JobHoursToday = jobHoursToday,
            DraftEstimateCount = draftEstimateCount,
            PendingInvoiceCount = pendingInvoiceCount,
            TodayJobs = todayJobs, UpcomingJobs = upcomingJobs, RecentActivity = recentActivity
        });
    }
}

// DTO classes — mirror the mobile-side types so JSON serialisation round-trips cleanly
public class MobileDashboardResponse
{
    public int TodayJobCount { get; set; }
    public int OpenJobCount { get; set; }
    public int PendingNoteCount { get; set; }
    public decimal HoursToday { get; set; }
    public decimal HoursThisWeek { get; set; }
    public decimal HoursThisMonth { get; set; }
    public bool IsClockedIn { get; set; }
    public DateTime? ClockInTime { get; set; }
    public bool IsPaused { get; set; }
    public DateTime? PauseStartTime { get; set; }
    public int TotalBreakMinutesToday { get; set; }
    public int CompletedThisWeek { get; set; }
    public int OverdueJobCount { get; set; }
    public int UpcomingJobCount { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringAgreementCount { get; set; }
    public int MaintenanceDueCount { get; set; }
    public int WarrantyAlertCount { get; set; }
    public int ActiveJobClockCount { get; set; }
    public string? ActiveJobName { get; set; }
    public decimal JobHoursToday { get; set; }
    public int DraftEstimateCount { get; set; }
    public int PendingInvoiceCount { get; set; }
    public List<MobileDashboardJobCard> TodayJobs { get; set; } = [];
    public List<MobileDashboardJobCard> UpcomingJobs { get; set; } = [];
    public List<MobileDashboardActivity> RecentActivity { get; set; } = [];
}

public class MobileDashboardJobCard
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? CompanyName { get; set; }
    public string? SiteAddress { get; set; }
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public decimal? EstimatedDuration { get; set; }
}

public class MobileDashboardActivity
{
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-circle";
    public DateTime Timestamp { get; set; }
}
