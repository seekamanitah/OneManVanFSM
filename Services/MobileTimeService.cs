using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileTimeService(AppDbContext db) : IMobileTimeService
{
    // ── Legacy single-layer (kept for backward compat — treated as shift) ──

    public async Task<TimeEntry?> ClockInAsync(int employeeId, int? jobId = null, string? timeCategory = null)
    {
        if (jobId.HasValue)
            return await JobClockInAsync(employeeId, jobId.Value);

        return await ShiftClockInAsync(employeeId);
    }

    public async Task<TimeEntry?> ClockOutAsync(int employeeId)
    {
        return await ShiftClockOutAsync(employeeId);
    }

    public async Task<TimeEntry?> GetActiveClockAsync(int employeeId)
    {
        return await GetActiveShiftAsync(employeeId);
    }

    // ── Shift (daily payroll clock) ──

    public async Task<TimeEntry?> ShiftClockInAsync(int employeeId)
    {
        if (employeeId <= 0) return null;

        var emp = await db.Employees.FindAsync(employeeId);
        if (emp is null) return null;

        // Prevent double shift clock-in
        var existingShift = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Shift
                && t.EndTime == null);
        if (existingShift != null) return existingShift;

        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            StartTime = DateTime.Now,
            EntryType = TimeEntryType.Shift,
            HourlyRate = emp.HourlyRate,
            IsBillable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<TimeEntry?> ShiftClockOutAsync(int employeeId)
    {
        // First, close all active job clocks
        var activeJobClocks = await db.TimeEntries
            .Where(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.JobClock
                && t.EndTime == null)
            .ToListAsync();

        foreach (var jc in activeJobClocks)
        {
            jc.EndTime = DateTime.Now;
            jc.Hours = Math.Round((decimal)(jc.EndTime.Value - jc.StartTime).TotalHours, 2);
            jc.UpdatedAt = DateTime.UtcNow;
        }

        // Close any active break
        var activeBreak = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Break
                && t.EndTime == null);
        if (activeBreak is not null)
        {
            activeBreak.EndTime = DateTime.Now;
            activeBreak.Hours = Math.Round((decimal)(activeBreak.EndTime.Value - activeBreak.StartTime).TotalHours, 2);
            activeBreak.UpdatedAt = DateTime.UtcNow;
        }

        // Then close the shift
        var shift = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Shift
                && t.EndTime == null);
        if (shift is null) return null;

        shift.EndTime = DateTime.Now;
        shift.Hours = Math.Round((decimal)(shift.EndTime.Value - shift.StartTime).TotalHours, 2);

        // Calculate overtime (daily: hours > 8)
        if (shift.Hours > 8)
        {
            shift.OvertimeHours = shift.Hours - 8;
        }
        shift.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return shift;
    }

    public async Task<TimeEntry?> GetActiveShiftAsync(int employeeId)
    {
        return await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Shift
                && t.EndTime == null);
    }

    // ── Job Clock (per-job, runs within a shift) ──

    public async Task<TimeEntry?> JobClockInAsync(int employeeId, int jobId, decimal? rateOverride = null)
    {
        if (employeeId <= 0) return null;

        var emp = await db.Employees.FindAsync(employeeId);
        if (emp is null) return null;

        // Ensure employee has an active shift first
        var activeShift = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Shift
                && t.EndTime == null);
        if (activeShift is null) return null; // Must be clocked in to shift first

        // Prevent double clock-in to same job
        var existing = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.JobClock
                && t.JobId == jobId
                && t.EndTime == null);
        if (existing != null) return existing;

        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            JobId = jobId,
            StartTime = DateTime.Now,
            EntryType = TimeEntryType.JobClock,
            HourlyRate = rateOverride ?? emp.HourlyRate,
            IsBillable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.TimeEntries.Add(entry);

        // Auto-assign employee to job team if not already a member
        var alreadyOnTeam = await db.Set<JobEmployee>()
            .AnyAsync(je => je.JobId == jobId && je.EmployeeId == employeeId);
        if (!alreadyOnTeam)
        {
            db.Set<JobEmployee>().Add(new JobEmployee
            {
                JobId = jobId,
                EmployeeId = employeeId,
                Role = "Technician",
                AssignedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<TimeEntry?> JobClockOutAsync(int employeeId, int jobId)
    {
        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.JobClock
                && t.JobId == jobId
                && t.EndTime == null);
        if (entry is null) return null;

        entry.EndTime = DateTime.Now;
        entry.Hours = Math.Round((decimal)(entry.EndTime.Value - entry.StartTime).TotalHours, 2);
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<List<TimeEntry>> GetActiveJobClocksAsync(int employeeId)
    {
        return await db.TimeEntries
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.JobClock
                && t.EndTime == null)
            .ToListAsync();
    }

    // ── Break / Pause (within a shift) ──

    public async Task<TimeEntry?> ShiftPauseAsync(int employeeId, string? reason = null)
    {
        // Must have an active shift
        var activeShift = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Shift
                && t.EndTime == null);
        if (activeShift is null) return null;

        // Prevent double-pause
        var existingBreak = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Break
                && t.EndTime == null);
        if (existingBreak is not null) return existingBreak;

        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            StartTime = DateTime.Now,
            EntryType = TimeEntryType.Break,
            IsBillable = false,
            TimeCategory = "Break",
            Notes = reason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<TimeEntry?> ShiftResumeAsync(int employeeId)
    {
        var activeBreak = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Break
                && t.EndTime == null);
        if (activeBreak is null) return null;

        activeBreak.EndTime = DateTime.Now;
        activeBreak.Hours = Math.Round((decimal)(activeBreak.EndTime.Value - activeBreak.StartTime).TotalHours, 2);
        activeBreak.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return activeBreak;
    }

    public async Task<TimeEntry?> GetActiveBreakAsync(int employeeId)
    {
        return await db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                && t.EntryType == TimeEntryType.Break
                && t.EndTime == null);
    }

    // ── Queries ──

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
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= monthStart)
            .ToListAsync();

        var activeShift = entries.FirstOrDefault(t => t.EntryType == TimeEntryType.Shift && t.EndTime == null);
        var activeJobClocks = entries.Where(t => t.EntryType == TimeEntryType.JobClock && t.EndTime == null).ToList();

        var completedJobs = await db.Jobs
            .CountAsync(j => j.AssignedEmployeeId == employeeId
                && j.Status == JobStatus.Completed
                && j.CompletedDate >= weekStart);

        var shiftEntries = entries.Where(t => t.EntryType == TimeEntryType.Shift).ToList();
        var jobEntries = entries.Where(t => t.EntryType == TimeEntryType.JobClock).ToList();

        return new MobileTimeSummary
        {
            HoursToday = shiftEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours),
            HoursThisWeek = shiftEntries.Where(t => t.StartTime >= weekStart).Sum(t => t.Hours),
            HoursThisMonth = shiftEntries.Sum(t => t.Hours),
            ShiftHoursToday = shiftEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours),
            ShiftHoursThisWeek = shiftEntries.Where(t => t.StartTime >= weekStart).Sum(t => t.Hours),
            JobHoursToday = jobEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours),
            JobsCompletedThisWeek = completedJobs,
            IsClockedIn = activeShift != null,
            CurrentClockInTime = activeShift?.StartTime,
            ActiveJobClockCount = activeJobClocks.Count,
            ActiveJobName = activeJobClocks.FirstOrDefault()?.Job?.Title
                ?? activeJobClocks.FirstOrDefault()?.Job?.JobNumber,
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
            catch { /* malformed JSON */ }
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
            HourlyRate = emp.HourlyRate,
            OvertimeRate = emp.OvertimeRate,
        };
    }

    // ── Payroll Summary ──

    public async Task<MobilePayrollSummary> GetPayrollSummaryAsync(int employeeId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        var emp = await db.Employees.FindAsync(employeeId);
        if (emp is null)
            return new MobilePayrollSummary { WeekStart = weekStart, WeekEnd = weekEnd };

        var entries = await db.TimeEntries
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId
                && t.StartTime >= weekStart
                && t.StartTime < weekEnd)
            .ToListAsync();

        var shiftEntries = entries.Where(t => t.EntryType == TimeEntryType.Shift).ToList();
        var jobEntries = entries.Where(t => t.EntryType == TimeEntryType.JobClock).ToList();

        var totalShiftHours = shiftEntries.Sum(e => e.Hours);
        var regularHours = Math.Min(totalShiftHours, 40);
        var overtimeHours = Math.Max(totalShiftHours - 40, 0);
        var otRate = emp.OvertimeRate ?? emp.HourlyRate * 1.5m;

        var regularPay = regularHours * emp.HourlyRate;
        var overtimePay = overtimeHours * otRate;

        // Daily breakdown
        var days = new List<MobilePayrollDaySummary>();
        for (var d = weekStart; d < weekEnd; d = d.AddDays(1))
        {
            var dayDate = d.Date;
            days.Add(new MobilePayrollDaySummary
            {
                Date = dayDate,
                ShiftHours = shiftEntries.Where(e => e.StartTime.Date == dayDate).Sum(e => e.Hours),
                JobHours = jobEntries.Where(e => e.StartTime.Date == dayDate).Sum(e => e.Hours),
                JobCount = jobEntries.Count(e => e.StartTime.Date == dayDate && e.JobId.HasValue),
            });
        }

        // Per-job breakdown
        var jobs = jobEntries
            .Where(e => e.JobId.HasValue)
            .GroupBy(e => e.JobId!.Value)
            .Select(g =>
            {
                var first = g.First();
                var hours = g.Sum(e => e.Hours);
                var rate = first.HourlyRate ?? emp.HourlyRate;
                return new MobilePayrollJobSummary
                {
                    JobId = g.Key,
                    JobNumber = first.Job?.JobNumber ?? string.Empty,
                    Title = first.Job?.Title,
                    Hours = hours,
                    RateUsed = rate,
                    Amount = hours * rate,
                };
            }).ToList();

        return new MobilePayrollSummary
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            TotalShiftHours = totalShiftHours,
            RegularHours = regularHours,
            OvertimeHours = overtimeHours,
            HourlyRate = emp.HourlyRate,
            OvertimeRate = otRate,
            RegularPay = regularPay,
            OvertimePay = overtimePay,
            TotalGrossPay = regularPay + overtimePay,
            Days = days,
            Jobs = jobs,
        };
    }
}
