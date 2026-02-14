using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class CalendarService : ICalendarService
{
    private readonly AppDbContext _db;
    public CalendarService(AppDbContext db) => _db = db;

    public async Task<List<CalendarEventItem>> GetEventsAsync(DateTime? start = null, DateTime? end = null, int? employeeId = null)
    {
        var query = _db.CalendarEvents.AsQueryable();
        if (start.HasValue) query = query.Where(e => e.EndDateTime >= start.Value);
        if (end.HasValue) query = query.Where(e => e.StartDateTime <= end.Value);
        if (employeeId.HasValue) query = query.Where(e => e.EmployeeId == employeeId.Value);

        var calendarEvents = await query.OrderBy(e => e.StartDateTime).Select(e => new CalendarEventItem
        {
            Id = e.Id, Title = e.Title, StartDateTime = e.StartDateTime,
            EndDateTime = e.EndDateTime, Duration = e.Duration, Status = e.Status,
            EventType = e.EventType, Color = e.Color, IsRecurring = e.IsRecurring,
            JobNumber = e.Job != null ? e.Job.JobNumber : null,
            EmployeeName = e.Employee != null ? e.Employee.Name : null,
            CustomerName = e.Job != null && e.Job.Customer != null ? e.Job.Customer.Name : null,
            SiteName = e.Job != null && e.Job.Site != null ? e.Job.Site.Name : null,
            Notes = e.Notes
        }).ToListAsync();

        // Also include scheduled jobs that don't have explicit calendar events
        var jobQuery = _db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Include(j => j.AssignedEmployee)
            .Where(j => !j.IsArchived
                && j.ScheduledDate != null
                && j.Status != JobStatus.Cancelled);

        if (start.HasValue)
            jobQuery = jobQuery.Where(j => j.ScheduledDate.Value >= start.Value.Date);
        if (end.HasValue)
            jobQuery = jobQuery.Where(j => j.ScheduledDate.Value <= end.Value.Date);
        if (employeeId.HasValue)
            jobQuery = jobQuery.Where(j => j.AssignedEmployeeId == employeeId.Value);

        var scheduledJobs = await jobQuery.OrderBy(j => j.ScheduledDate).ThenBy(j => j.ScheduledTime).ToListAsync();

        // Add jobs that don't already have calendar events
        var existingJobNumbers = calendarEvents
            .Where(e => e.JobNumber != null)
            .Select(e => e.JobNumber)
            .ToHashSet();

        foreach (var job in scheduledJobs.Where(j => !existingJobNumbers.Contains(j.JobNumber)))
        {
            var jobDate = job.ScheduledDate!.Value.Date;
            var startTime = jobDate;
            if (job.ScheduledTime.HasValue)
                startTime = jobDate.Add(job.ScheduledTime.Value);
            else
                startTime = jobDate.AddHours(8);

            calendarEvents.Add(new CalendarEventItem
            {
                Id = -job.Id, // Negative to distinguish from real CalendarEvent IDs
                Title = job.Title ?? job.JobNumber,
                StartDateTime = startTime,
                EndDateTime = startTime.AddHours((double)(job.EstimatedDuration ?? 1)),
                Status = CalendarEventStatus.Confirmed,
                EventType = "Job",
                JobNumber = job.JobNumber,
                EmployeeName = job.AssignedEmployee?.Name,
                CustomerName = job.Customer?.Name,
                SiteName = job.Site?.Name,
            });
        }

        return calendarEvents.OrderBy(e => e.StartDateTime).ToList();
    }

    public async Task<CalendarEventDetail?> GetEventDetailAsync(int id)
    {
        return await _db.CalendarEvents
            .Include(e => e.Job).ThenInclude(j => j!.Customer)
            .Include(e => e.Job).ThenInclude(j => j!.Site)
            .Include(e => e.Employee)
            .Include(e => e.ServiceAgreement)
            .Where(e => e.Id == id)
            .Select(e => new CalendarEventDetail
            {
                Id = e.Id, Title = e.Title,
                StartDateTime = e.StartDateTime, EndDateTime = e.EndDateTime,
                Duration = e.Duration, Status = e.Status,
                EventType = e.EventType, IsRecurring = e.IsRecurring,
                RecurrenceRule = e.RecurrenceRule, Color = e.Color,
                Notes = e.Notes, Checklist = e.Checklist,
                CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
                JobId = e.JobId,
                JobNumber = e.Job != null ? e.Job.JobNumber : null,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee != null ? e.Employee.Name : null,
                ServiceAgreementId = e.ServiceAgreementId,
                AgreementNumber = e.ServiceAgreement != null ? e.ServiceAgreement.AgreementNumber : null,
                CustomerName = e.Job != null && e.Job.Customer != null ? e.Job.Customer.Name : null,
                SiteName = e.Job != null && e.Job.Site != null ? e.Job.Site.Name : null
            }).FirstOrDefaultAsync();
    }

    public async Task<CalendarEvent> CreateEventAsync(CalendarEventEditModel model)
    {
        var ev = new CalendarEvent
        {
            Title = model.Title, StartDateTime = model.StartDateTime, EndDateTime = model.EndDateTime,
            Duration = model.Duration, Status = model.Status, EventType = model.EventType,
            IsRecurring = model.IsRecurring, RecurrenceRule = model.RecurrenceRule, Color = model.Color,
            Notes = model.Notes, Checklist = model.Checklist,
            JobId = model.JobId, EmployeeId = model.EmployeeId,
            ServiceAgreementId = model.ServiceAgreementId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.CalendarEvents.Add(ev);
        await _db.SaveChangesAsync();
        return ev;
    }

    public async Task<CalendarEvent> UpdateEventAsync(int id, CalendarEventEditModel model)
    {
        var ev = await _db.CalendarEvents.FindAsync(id) ?? throw new InvalidOperationException("Event not found.");
        ev.Title = model.Title; ev.StartDateTime = model.StartDateTime;
        ev.EndDateTime = model.EndDateTime; ev.Duration = model.Duration;
        ev.Status = model.Status; ev.EventType = model.EventType;
        ev.IsRecurring = model.IsRecurring; ev.RecurrenceRule = model.RecurrenceRule;
        ev.Color = model.Color; ev.Notes = model.Notes; ev.Checklist = model.Checklist;
        ev.JobId = model.JobId; ev.EmployeeId = model.EmployeeId;
        ev.ServiceAgreementId = model.ServiceAgreementId;
        ev.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ev;
    }

    public async Task<bool> UpdateStatusAsync(int id, CalendarEventStatus status)
    {
        var ev = await _db.CalendarEvents.FindAsync(id);
        if (ev is null) return false;
        ev.Status = status; ev.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var ev = await _db.CalendarEvents.FindAsync(id);
        if (ev is null) return false;
        _db.CalendarEvents.Remove(ev);
        await _db.SaveChangesAsync();
        return true;
    }
}
