using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

/// <summary>
/// Remote-mode time service. Clock in/out operations are pushed to the server API
/// immediately. Read operations use the local SQLite cache.
/// </summary>
public class RemoteMobileTimeService : IMobileTimeService
{
    private readonly AppDbContext _db;
    private readonly ApiClient _api;
    private readonly IOfflineQueueService _offlineQueue;
    private readonly ILogger<RemoteMobileTimeService> _logger;

    public RemoteMobileTimeService(AppDbContext db, ApiClient api, IOfflineQueueService offlineQueue, ILogger<RemoteMobileTimeService> logger)
    {
        _db = db;
        _api = api;
        _offlineQueue = offlineQueue;
        _logger = logger;
    }

    // ── Legacy single-layer (delegates to dual-layer) ──

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
        try
        {
            var request = new { employeeId, entryType = "Shift" };
            var entry = await _api.PostAsync<TimeEntry>("api/timeentries/clockin", request);
            if (entry is not null)
            {
                var existing = await _db.TimeEntries.FindAsync(entry.Id);
                if (existing is null)
                {
                    // Clear navigation properties to prevent EF Core tracking conflicts
                    // The API may include Employee/Job references that conflict with local tracking
                    entry.Employee = null!;
                    entry.Job = null;
                    entry.Asset = null;

                    _db.TimeEntries.Add(entry);
                    await _db.SaveChangesAsync();
                    _db.ChangeTracker.Clear();
                }
            }
            return entry;
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue && (int)ex.StatusCode.Value >= 400 && (int)ex.StatusCode.Value < 500)
        {
            // API validation error (400 Bad Request, etc.) — do NOT create offline entry
            _logger.LogWarning(ex, "ShiftClockIn rejected by server (HTTP {StatusCode}) for employee {EmployeeId}.", (int)ex.StatusCode.Value, employeeId);
            throw new InvalidOperationException(
                ex.StatusCode.Value == System.Net.HttpStatusCode.BadRequest
                    ? "Employee record not found on server. Please sync or ask an admin to link your user to an Employee."
                    : $"Clock in rejected by server (HTTP {(int)ex.StatusCode.Value}).", ex);
        }
        catch (HttpRequestException ex)
        {
            // Connectivity / transient failure — create offline entry
            _logger.LogWarning(ex, "ShiftClockIn failed (offline) for employee {EmployeeId}, creating local entry.", employeeId);
            var emp = await _db.Employees.FindAsync(employeeId);
            var localEntry = new TimeEntry
            {
                EmployeeId = employeeId,
                StartTime = DateTime.UtcNow,
                EntryType = TimeEntryType.Shift,
                HourlyRate = emp?.HourlyRate,
                Notes = "[Offline shift clock-in]",
                CreatedAt = DateTime.UtcNow
            };
            _db.TimeEntries.Add(localEntry);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "POST",
                Endpoint = "api/timeentries/clockin",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { employeeId, entryType = "Shift" }),
                Description = $"Shift clock in employee #{employeeId}"
            });
            return localEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ShiftClockIn failed for employee {EmployeeId}.", employeeId);
            return null;
        }
    }

    public async Task<TimeEntry?> ShiftClockOutAsync(int employeeId)
    {
        try
        {
            var request = new { employeeId, entryType = "Shift" };
            var entry = await _api.PostAsync<TimeEntry>("api/timeentries/clockout", request);
            if (entry is not null)
            {
                var existing = await _db.TimeEntries.FindAsync(entry.Id);
                if (existing is not null)
                {
                    existing.EndTime = entry.EndTime;
                    existing.Hours = entry.Hours;
                    existing.OvertimeHours = entry.OvertimeHours;
                    await _db.SaveChangesAsync();
                }
            }
            // Also close local job clocks
            var localJobClocks = await _db.TimeEntries
                .Where(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.JobClock && t.EndTime == null)
                .ToListAsync();
            foreach (var jc in localJobClocks)
            {
                jc.EndTime = DateTime.UtcNow;
                jc.Hours = (decimal)(jc.EndTime.Value - jc.StartTime).TotalHours;
            }
            if (localJobClocks.Count > 0) await _db.SaveChangesAsync();
            return entry;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ShiftClockOut failed (offline) for employee {EmployeeId}.", employeeId);
            // Close all local open entries
            var activeJobs = await _db.TimeEntries
                .Where(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.JobClock && t.EndTime == null)
                .ToListAsync();
            foreach (var jc in activeJobs)
            {
                jc.EndTime = DateTime.UtcNow;
                jc.Hours = (decimal)(jc.EndTime.Value - jc.StartTime).TotalHours;
            }
            var active = await _db.TimeEntries
                .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Shift && t.EndTime == null);
            if (active is not null)
            {
                active.EndTime = DateTime.UtcNow;
                active.Hours = (decimal)(active.EndTime.Value - active.StartTime).TotalHours;
                if (active.Hours > 8) active.OvertimeHours = active.Hours - 8;
                await _db.SaveChangesAsync();
            }
            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "POST",
                Endpoint = "api/timeentries/clockout",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { employeeId }),
                Description = $"Shift clock out employee #{employeeId}"
            });
            return active;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ShiftClockOut failed for employee {EmployeeId}.", employeeId);
            return null;
        }
    }

    public async Task<TimeEntry?> GetActiveShiftAsync(int employeeId)
    {
        var active = await _db.TimeEntries.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Shift && t.EndTime == null);
        if (active is not null) return active;
        try
        {
            return await _api.GetAsync<TimeEntry>($"api/timeentries/active/{employeeId}");
        }
        catch { return null; }
    }

    // ── Job Clock ──

    public async Task<TimeEntry?> JobClockInAsync(int employeeId, int jobId, decimal? rateOverride = null)
    {
        // Must have active shift
        var shift = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Shift && t.EndTime == null);
        if (shift is null) return null;

        // Prevent double
        var existing = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.JobClock && t.JobId == jobId && t.EndTime == null);
        if (existing is not null) return existing;

        try
        {
            var request = new { employeeId, jobId, rateOverride, entryType = "JobClock" };
            var entry = await _api.PostAsync<TimeEntry>("api/timeentries/clockin", request);
            if (entry is not null)
            {
                var local = await _db.TimeEntries.FindAsync(entry.Id);
                if (local is null)
                {
                    // Clear navigation properties to prevent EF Core tracking conflicts
                    entry.Employee = null!;
                    entry.Job = null;
                    entry.Asset = null;

                    _db.TimeEntries.Add(entry);
                    await _db.SaveChangesAsync();
                    _db.ChangeTracker.Clear();
                }
            }
            return entry;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "JobClockIn offline for employee {EmployeeId} job {JobId}.", employeeId, jobId);
            var emp = await _db.Employees.FindAsync(employeeId);
            var localEntry = new TimeEntry
            {
                EmployeeId = employeeId,
                JobId = jobId,
                StartTime = DateTime.UtcNow,
                EntryType = TimeEntryType.JobClock,
                HourlyRate = rateOverride ?? emp?.HourlyRate,
                IsBillable = true,
                Notes = "[Offline job clock-in]",
                CreatedAt = DateTime.UtcNow
            };
            _db.TimeEntries.Add(localEntry);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "POST",
                Endpoint = "api/timeentries/clockin",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { employeeId, jobId, rateOverride, entryType = "JobClock" }),
                Description = $"Job clock in employee #{employeeId} job #{jobId}"
            });
            return localEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JobClockIn failed for employee {EmployeeId} job {JobId}.", employeeId, jobId);
            return null;
        }
    }

    public async Task<TimeEntry?> JobClockOutAsync(int employeeId, int jobId)
    {
        try
        {
            var request = new { employeeId, jobId };
            var entry = await _api.PostAsync<TimeEntry>("api/timeentries/jobclockout", request);
            if (entry is not null)
            {
                var existing = await _db.TimeEntries.FindAsync(entry.Id);
                if (existing is not null)
                {
                    existing.EndTime = entry.EndTime;
                    existing.Hours = entry.Hours;
                    await _db.SaveChangesAsync();
                }
            }
            return entry;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "JobClockOut offline for employee {EmployeeId} job {JobId}.", employeeId, jobId);
            var active = await _db.TimeEntries
                .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.JobClock && t.JobId == jobId && t.EndTime == null);
            if (active is not null)
            {
                active.EndTime = DateTime.UtcNow;
                active.Hours = (decimal)(active.EndTime.Value - active.StartTime).TotalHours;
                await _db.SaveChangesAsync();
            }
            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "POST",
                Endpoint = "api/timeentries/jobclockout",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { employeeId, jobId }),
                Description = $"Job clock out employee #{employeeId} job #{jobId}"
            });
            return active;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JobClockOut failed for employee {EmployeeId} job {JobId}.", employeeId, jobId);
            return null;
        }
    }

    public async Task<List<TimeEntry>> GetActiveJobClocksAsync(int employeeId)
    {
        return await _db.TimeEntries.AsNoTracking()
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.JobClock && t.EndTime == null)
            .ToListAsync();
    }

    // ── Break / Pause ──

    public async Task<TimeEntry?> ShiftPauseAsync(int employeeId, string? reason = null)
    {
        // Must have an active shift
        var shift = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Shift && t.EndTime == null);
        if (shift is null) return null;

        // Prevent double-pause
        var existingBreak = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Break && t.EndTime == null);
        if (existingBreak is not null) return existingBreak;

        try
        {
            var request = new { employeeId, reason };
            var entry = await _api.PostAsync<TimeEntry>("api/timeentries/pause", request);
            if (entry is not null)
            {
                var local = await _db.TimeEntries.FindAsync(entry.Id);
                if (local is null)
                {
                    // Clear navigation properties
                    entry.Employee = null!;
                    entry.Job = null;
                    entry.Asset = null;

                    _db.TimeEntries.Add(entry);
                    await _db.SaveChangesAsync();
                    _db.ChangeTracker.Clear();
                }
            }
            return entry;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ShiftPause offline for employee {EmployeeId}.", employeeId);
            // Offline fallback - save locally
            var entry = new TimeEntry
            {
                EmployeeId = employeeId,
                StartTime = DateTime.UtcNow,
                EntryType = TimeEntryType.Break,
                IsBillable = false,
                TimeCategory = "Break",
                Notes = reason,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.TimeEntries.Add(entry);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "POST",
                Endpoint = "api/timeentries/pause",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { employeeId, reason }),
                Description = $"Pause shift for employee {employeeId}"
            });
            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ShiftPause failed for employee {EmployeeId}.", employeeId);
            return null;
        }
    }

    public async Task<TimeEntry?> ShiftResumeAsync(int employeeId)
    {
        var activeBreak = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Break && t.EndTime == null);
        if (activeBreak is null) return null;

        try
        {
            var request = new { employeeId };
            var entry = await _api.PostAsync<TimeEntry>("api/timeentries/resume", request);
            if (entry is not null)
            {
                // Update local record
                activeBreak.EndTime = entry.EndTime;
                activeBreak.Hours = entry.Hours;
                activeBreak.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
            }
            return entry;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ShiftResume offline for employee {EmployeeId}.", employeeId);
            // Offline fallback - update locally
            activeBreak.EndTime = DateTime.UtcNow;
            activeBreak.Hours = Math.Round((decimal)(activeBreak.EndTime.Value - activeBreak.StartTime).TotalHours, 2);
            activeBreak.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "POST",
                Endpoint = "api/timeentries/resume",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { employeeId }),
                Description = $"Resume shift for employee {employeeId}"
            });
            return activeBreak;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ShiftResume failed for employee {EmployeeId}.", employeeId);
            return null;
        }
    }

    public async Task<TimeEntry?> GetActiveBreakAsync(int employeeId)
    {
        return await _db.TimeEntries.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.Break && t.EndTime == null);
    }

    // ── Queries ──

    public async Task<List<MobileTimeEntrySummary>> GetRecentEntriesAsync(int employeeId, int count = 10)
    {
        return await _db.TimeEntries.AsNoTracking()
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.StartTime)
            .Take(count)
            .Select(t => new MobileTimeEntrySummary
            {
                Id = t.Id, StartTime = t.StartTime, EndTime = t.EndTime,
                Hours = t.Hours, Notes = t.Notes
            }).ToListAsync();
    }

    public async Task<MobileTimeSummary> GetTimeSummaryAsync(int employeeId)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var entries = await _db.TimeEntries.AsNoTracking()
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= monthStart)
            .ToListAsync();

        var activeShift = entries.FirstOrDefault(t => t.EntryType == TimeEntryType.Shift && t.EndTime == null);
        var activeJobClocks = entries.Where(t => t.EntryType == TimeEntryType.JobClock && t.EndTime == null).ToList();
        var shiftEntries = entries.Where(t => t.EntryType == TimeEntryType.Shift).ToList();
        var jobEntries = entries.Where(t => t.EntryType == TimeEntryType.JobClock).ToList();

        var completedThisWeek = await _db.Jobs
            .CountAsync(j => j.AssignedEmployeeId == employeeId
                && j.Status == JobStatus.Completed
                && j.CompletedDate != null && j.CompletedDate.Value >= weekStart);

        return new MobileTimeSummary
        {
            HoursToday = shiftEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours),
            HoursThisWeek = shiftEntries.Where(t => t.StartTime >= weekStart).Sum(t => t.Hours),
            HoursThisMonth = shiftEntries.Sum(t => t.Hours),
            ShiftHoursToday = shiftEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours),
            ShiftHoursThisWeek = shiftEntries.Where(t => t.StartTime >= weekStart).Sum(t => t.Hours),
            JobHoursToday = jobEntries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours),
            JobsCompletedThisWeek = completedThisWeek,
            IsClockedIn = activeShift is not null,
            CurrentClockInTime = activeShift?.StartTime,
            ActiveJobClockCount = activeJobClocks.Count,
            ActiveJobName = activeJobClocks.FirstOrDefault()?.Job?.Title
                ?? activeJobClocks.FirstOrDefault()?.Job?.JobNumber,
        };
    }

    public async Task<MobileEmployeeProfile?> GetEmployeeProfileAsync(int employeeId)
    {
        var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);
        if (emp is null) return null;

        List<string> certs = [];
        if (!string.IsNullOrWhiteSpace(emp.Certifications))
        {
            try { certs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(emp.Certifications) ?? []; }
            catch { /* not valid JSON */ }
        }

        return new MobileEmployeeProfile
        {
            Id = emp.Id, Name = emp.Name, Role = emp.Role.ToString(),
            Phone = emp.Phone, Email = emp.Email, Territory = emp.Territory,
            Certifications = certs, LicenseNumber = emp.LicenseNumber,
            LicenseExpiry = emp.LicenseExpiry, VehicleAssigned = emp.VehicleAssigned,
            HireDate = emp.HireDate,
            HourlyRate = emp.HourlyRate,
            OvertimeRate = emp.OvertimeRate,
        };
    }

    public async Task<MobilePayrollSummary> GetPayrollSummaryAsync(int employeeId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);
        if (emp is null)
            return new MobilePayrollSummary { WeekStart = weekStart, WeekEnd = weekEnd };

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

        var regularPay = regularHours * emp.HourlyRate;
        var overtimePay = overtimeHours * otRate;

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
