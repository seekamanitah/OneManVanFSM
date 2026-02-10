using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileTimeService(AppDbContext db) : IMobileTimeService
{
    public async Task<TimeEntry?> ClockInAsync(int employeeId, int? jobId = null, string? timeCategory = null)
    {
        // Validate the employee exists before inserting
        if (employeeId <= 0 || !await db.Employees.AnyAsync(e => e.Id == employeeId))
            return null;

        // Prevent double clock-in
        var existing = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);
        if (existing != null) return existing;

        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            JobId = jobId,
            StartTime = DateTime.Now,
            IsBillable = true,
            TimeCategory = timeCategory,
            CreatedAt = DateTime.Now,
        };
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<TimeEntry?> ClockOutAsync(int employeeId)
    {
        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);
        if (entry is null) return null;

        entry.EndTime = DateTime.Now;
        entry.Hours = Math.Round((decimal)(entry.EndTime.Value - entry.StartTime).TotalHours, 2);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<TimeEntry?> GetActiveClockAsync(int employeeId)
    {
        return await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);
    }

    public async Task<List<MobileTimeEntrySummary>> GetRecentEntriesAsync(int employeeId, int count = 10)
    {
        return await db.TimeEntries
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.StartTime)
            .Take(count)
            .Select(t => new MobileTimeEntrySummary
            {
                Id = t.Id,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Hours = t.Hours,
                Notes = t.Notes,
            })
            .ToListAsync();
    }

    public async Task<MobileTimeSummary> GetTimeSummaryAsync(int employeeId)
    {
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var entries = await db.TimeEntries
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= monthStart)
            .ToListAsync();

        var activeClock = entries.FirstOrDefault(t => t.EndTime == null);

        var completedJobs = await db.Jobs
            .CountAsync(j => j.AssignedEmployeeId == employeeId
                && j.Status == JobStatus.Completed
                && j.CompletedDate >= weekStart);

        return new MobileTimeSummary
        {
            HoursToday = entries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours),
            HoursThisWeek = entries.Where(t => t.StartTime >= weekStart).Sum(t => t.Hours),
            HoursThisMonth = entries.Sum(t => t.Hours),
            JobsCompletedThisWeek = completedJobs,
            IsClockedIn = activeClock != null,
            CurrentClockInTime = activeClock?.StartTime,
        };
    }

    public async Task<MobileEmployeeProfile?> GetEmployeeProfileAsync(int employeeId)
    {
        var emp = await db.Employees.FindAsync(employeeId);
        if (emp is null) return null;

        var certs = new List<string>();
        if (!string.IsNullOrEmpty(emp.Certifications))
        {
            try { certs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(emp.Certifications) ?? []; }
            catch { /* malformed JSON — ignore */ }
        }

        return new MobileEmployeeProfile
        {
            Id = emp.Id,
            Name = emp.Name,
            Role = emp.Role.ToString(),
            Phone = emp.Phone,
            Email = emp.Email,
            Territory = emp.Territory,
            Certifications = certs,
            LicenseNumber = emp.LicenseNumber,
            LicenseExpiry = emp.LicenseExpiry,
            VehicleAssigned = emp.VehicleAssigned,
            HireDate = emp.HireDate,
        };
    }
}
