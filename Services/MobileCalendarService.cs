using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileCalendarService(AppDbContext db) : IMobileCalendarService
{
    public async Task<List<MobileCalendarEvent>> GetEventsAsync(DateTime date, int employeeId)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var calendarEvents = await db.CalendarEvents
            .Include(e => e.Job)
            .Where(e => e.EmployeeId == employeeId
                && e.StartDateTime < dayEnd
                && e.EndDateTime > dayStart)
            .OrderBy(e => e.StartDateTime)
            .Select(e => new MobileCalendarEvent
            {
                Id = e.Id,
                JobId = e.JobId,
                Title = e.Title ?? (e.Job != null ? e.Job.Title : "Event"),
                StartDateTime = e.StartDateTime,
                EndDateTime = e.EndDateTime,
                Status = e.Status,
                JobNumber = e.Job != null ? e.Job.JobNumber : null,
                EventType = e.JobId.HasValue ? "Job" : "Event",
                Color = e.Color,
            })
            .ToListAsync();

        // Also include scheduled jobs that might not have calendar events
        var scheduledJobs = await db.Jobs
            .Include(j => j.Site)
            .Where(j => !j.IsArchived
                && j.AssignedEmployeeId == employeeId
                && j.ScheduledDate != null
                && j.ScheduledDate.Value.Date == date.Date
                && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.ScheduledTime)
            .ToListAsync();

        // Add jobs that don't already have calendar events
        var existingJobNumbers = calendarEvents
            .Where(e => e.JobNumber != null)
            .Select(e => e.JobNumber)
            .ToHashSet();

        foreach (var job in scheduledJobs.Where(j => !existingJobNumbers.Contains(j.JobNumber)))
        {
            var startTime = date.Date;
            if (job.ScheduledTime.HasValue)
                startTime = startTime.Add(job.ScheduledTime.Value);
            else
                startTime = startTime.AddHours(8); // default 8 AM

            calendarEvents.Add(new MobileCalendarEvent
            {
                Id = job.Id,
                JobId = job.Id,
                Title = job.Title ?? job.JobNumber,
                StartDateTime = startTime,
                EndDateTime = startTime.AddHours((double)(job.EstimatedDuration ?? 1)),
                Status = CalendarEventStatus.Confirmed,
                JobNumber = job.JobNumber,
                SiteAddress = job.Site != null ? $"{job.Site.Address}, {job.Site.City}" : null,
                EventType = "Job",
            });
        }

        // Include estimates with ExpiryDate on this day
        var estimates = await db.Estimates
            .Include(e => e.Customer)
            .Where(e => !e.IsArchived
                && e.ExpiryDate != null
                && e.ExpiryDate.Value.Date == date.Date
                && e.Status != EstimateStatus.Expired
                && e.Status != EstimateStatus.Rejected)
            .ToListAsync();

        foreach (var est in estimates)
        {
            calendarEvents.Add(new MobileCalendarEvent
            {
                Id = est.Id,
                Title = $"EST: {est.Title ?? est.EstimateNumber}",
                StartDateTime = date.Date.AddHours(9),
                EndDateTime = date.Date.AddHours(10),
                Status = CalendarEventStatus.Tentative,
                EventType = "Estimate",
                Color = "#6f42c1",
            });
        }

        return calendarEvents.OrderBy(e => e.StartDateTime).ToList();
    }

    public async Task<List<MobileCalendarEvent>> GetWeekEventsAsync(DateTime weekStart, int employeeId)
    {
        var allEvents = new List<MobileCalendarEvent>();
        for (var i = 0; i < 7; i++)
        {
            var dayEvents = await GetEventsAsync(weekStart.AddDays(i), employeeId);
            allEvents.AddRange(dayEvents);
        }
        return allEvents;
    }
}
