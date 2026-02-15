using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class ShiftService : IShiftService
{
    private readonly AppDbContext _db;

    public ShiftService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeeShiftStatus>> GetTeamBoardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var activeEmployees = await _db.Employees
            .Where(e => e.Status == EmployeeStatus.Active && !e.IsArchived)
            .OrderBy(e => e.Name)
            .ToListAsync();

        var todaysEntries = await _db.TimeEntries
            .Include(t => t.Job)
                .ThenInclude(j => j!.Customer)
            .Where(t => t.StartTime >= today)
            .ToListAsync();

        var result = new List<EmployeeShiftStatus>();
        foreach (var emp in activeEmployees)
        {
            var empEntries = todaysEntries.Where(t => t.EmployeeId == emp.Id).ToList();
            var activeEntry = empEntries.FirstOrDefault(t => t.EndTime == null);

            var status = new EmployeeShiftStatus
            {
                EmployeeId = emp.Id,
                Name = emp.Name,
                Role = emp.Role,
                Territory = emp.Territory,
                VehicleAssigned = emp.VehicleAssigned,
                IsOnShift = activeEntry is not null,
                ShiftStartTime = activeEntry?.StartTime,
                ShiftDuration = activeEntry is not null ? DateTime.UtcNow - activeEntry.StartTime : null,
                ActiveTimeEntryId = activeEntry?.Id,
                CurrentJobId = activeEntry?.JobId,
                CurrentJobNumber = activeEntry?.Job?.JobNumber,
                CurrentJobTitle = activeEntry?.Job?.Title,
                CurrentJobPriority = activeEntry?.Job?.Priority,
                CurrentCustomerName = activeEntry?.Job?.Customer?.Name,
                HoursToday = empEntries.Sum(t => t.EndTime.HasValue
                    ? Math.Round((decimal)(t.EndTime.Value - t.StartTime).TotalHours, 2)
                    : Math.Round((decimal)(DateTime.UtcNow - t.StartTime).TotalHours, 2))
            };
            result.Add(status);
        }

        return result;
    }

    public async Task ClockInEmployeeAsync(int employeeId, string? notes = null)
    {
        var existing = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);

        if (existing is not null)
            throw new InvalidOperationException("Employee is already clocked in.");

        var emp = await _db.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found.");

        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            EntryType = TimeEntryType.Shift,
            StartTime = DateTime.UtcNow,
            IsBillable = false,
            TimeCategory = "Shift",
            HourlyRate = emp.HourlyRate,
            Notes = notes
        };
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task ClockOutEmployeeAsync(int employeeId)
    {
        var active = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);

        if (active is null)
            throw new InvalidOperationException("Employee is not clocked in.");

        active.EndTime = DateTime.UtcNow;
        active.Hours = Math.Round((decimal)(active.EndTime.Value - active.StartTime).TotalHours, 2);
        active.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task AssignEmployeeToJobAsync(int employeeId, int jobId)
    {
        // Close any existing active time entry for this employee
        var existing = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);

        if (existing is not null)
        {
            existing.EndTime = DateTime.UtcNow;
            existing.Hours = Math.Round((decimal)(existing.EndTime.Value - existing.StartTime).TotalHours, 2);
            existing.UpdatedAt = DateTime.UtcNow;
        }

        var emp = await _db.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found.");

        var job = await _db.Jobs
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == jobId)
            ?? throw new InvalidOperationException("Job not found.");

        // Create new job-clock time entry
        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            JobId = jobId,
            EntryType = TimeEntryType.JobClock,
            StartTime = DateTime.UtcNow,
            IsBillable = true,
            TimeCategory = "On-Site",
            HourlyRate = emp.HourlyRate,
            Notes = $"Assigned via Team Board to {job.JobNumber}"
        };
        _db.TimeEntries.Add(entry);

        // Update job assignment and status if needed
        job.AssignedEmployeeId = employeeId;
        if (job.Status is JobStatus.Scheduled or JobStatus.Approved)
            job.Status = JobStatus.EnRoute;
        job.UpdatedAt = DateTime.UtcNow;

        // Ensure JobEmployees junction record exists
        var hasJunction = await _db.JobEmployees
            .AnyAsync(je => je.JobId == jobId && je.EmployeeId == employeeId);
        if (!hasJunction)
        {
            _db.JobEmployees.Add(new JobEmployee
            {
                JobId = jobId,
                EmployeeId = employeeId,
                Role = emp.Role.ToString()
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task UnassignEmployeeFromJobAsync(int employeeId)
    {
        var active = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null && t.JobId != null);

        if (active is null)
            throw new InvalidOperationException("Employee is not assigned to a job.");

        // Close the job-clock entry
        active.EndTime = DateTime.UtcNow;
        active.Hours = Math.Round((decimal)(active.EndTime.Value - active.StartTime).TotalHours, 2);
        active.UpdatedAt = DateTime.UtcNow;

        // Start a new shift entry so they stay clocked in
        var emp = await _db.Employees.FindAsync(employeeId);
        var shiftEntry = new TimeEntry
        {
            EmployeeId = employeeId,
            EntryType = TimeEntryType.Shift,
            StartTime = DateTime.UtcNow,
            IsBillable = false,
            TimeCategory = "Shift",
            HourlyRate = emp?.HourlyRate ?? 0,
            Notes = "Returned from job assignment"
        };
        _db.TimeEntries.Add(shiftEntry);

        await _db.SaveChangesAsync();
    }

    public async Task<List<ShiftJobOption>> GetAssignableJobsAsync()
    {
        var assignableStatuses = new[]
        {
            JobStatus.Scheduled, JobStatus.Approved, JobStatus.EnRoute,
            JobStatus.OnSite, JobStatus.InProgress, JobStatus.Paused
        };

        return await _db.Jobs
            .Include(j => j.Customer)
            .Where(j => assignableStatuses.Contains(j.Status) && !j.IsArchived)
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.ScheduledDate)
            .Select(j => new ShiftJobOption
            {
                Id = j.Id,
                Display = string.IsNullOrEmpty(j.Title)
                    ? j.JobNumber
                    : $"{j.JobNumber} – {j.Title}",
                CustomerName = j.Customer != null ? j.Customer.Name : null,
                Priority = j.Priority,
                Status = j.Status
            })
            .ToListAsync();
    }
}
