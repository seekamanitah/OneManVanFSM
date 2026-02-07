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
        var today = DateTime.UtcNow.Date;
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
}
