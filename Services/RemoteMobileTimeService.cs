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

    public async Task<TimeEntry?> ClockInAsync(int employeeId, int? jobId = null, string? timeCategory = null)
    {
        try
        {
            var request = new { employeeId, jobId, timeCategory };
            var entry = await _api.PostAsync<TimeEntry>("api/timeentries/clockin", request);

            if (entry is not null)
            {
                // Cache locally
                var existing = await _db.TimeEntries.FindAsync(entry.Id);
                if (existing is null)
                {
                    _db.TimeEntries.Add(entry);
                    await _db.SaveChangesAsync();
                    _db.ChangeTracker.Clear();
                }
            }

            return entry;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ClockIn failed (offline) for employee {EmployeeId}, creating local entry and queueing.", employeeId);
            // Create local entry so the UI reflects clock-in immediately
            var localEntry = new TimeEntry
            {
                EmployeeId = employeeId,
                JobId = jobId,
                StartTime = DateTime.UtcNow,
                TimeCategory = timeCategory ?? "Regular",
                Notes = "[Offline clock-in]",
                CreatedAt = DateTime.UtcNow
            };
            _db.TimeEntries.Add(localEntry);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "POST",
                Endpoint = "api/timeentries/clockin",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { employeeId, jobId, timeCategory }),
                Description = $"Clock in employee #{employeeId}"
            });

            return localEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClockIn failed for employee {EmployeeId}.", employeeId);
            return null;
        }
    }

    public async Task<TimeEntry?> ClockOutAsync(int employeeId)
    {
        try
        {
            var request = new { employeeId };
            var entry = await _api.PostAsync<TimeEntry>("api/timeentries/clockout", request);

            if (entry is not null)
            {
                // Update local cache
                var existing = await _db.TimeEntries.FindAsync(entry.Id);
                if (existing is not null)
                {
                    existing.EndTime = entry.EndTime;
                    existing.Hours = entry.Hours;
                    existing.Notes = entry.Notes;
                    await _db.SaveChangesAsync();
                }
            }

            return entry;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ClockOut failed (offline) for employee {EmployeeId}, updating local entry and queueing.", employeeId);
            // Close local entry so the UI reflects clock-out immediately
            var active = await _db.TimeEntries
                .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);
            if (active is not null)
            {
                active.EndTime = DateTime.UtcNow;
                active.Hours = (decimal)(active.EndTime.Value - active.StartTime).TotalHours;
                await _db.SaveChangesAsync();
            }

            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "POST",
                Endpoint = "api/timeentries/clockout",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { employeeId }),
                Description = $"Clock out employee #{employeeId}"
            });

            return active;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClockOut failed for employee {EmployeeId}.", employeeId);
            return null;
        }
    }

    public async Task<TimeEntry?> GetActiveClockAsync(int employeeId)
    {
        // Try local cache first
        var active = await _db.TimeEntries.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);

        if (active is not null) return active;

        // Fallback: query API
        try
        {
            return await _api.GetAsync<TimeEntry>($"api/timeentries/active/{employeeId}");
        }
        catch
        {
            return null;
        }
    }

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
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var entries = await _db.TimeEntries.AsNoTracking()
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= monthStart)
            .ToListAsync();

        var activeClock = entries.FirstOrDefault(t => t.EndTime == null);
        var completedThisWeek = await _db.Jobs
            .CountAsync(j => j.AssignedEmployeeId == employeeId
                && j.Status == JobStatus.Completed
                && j.CompletedDate != null && j.CompletedDate.Value >= weekStart);

        return new MobileTimeSummary
        {
            HoursToday = entries.Where(t => t.StartTime.Date == today).Sum(t => t.Hours),
            HoursThisWeek = entries.Where(t => t.StartTime >= weekStart).Sum(t => t.Hours),
            HoursThisMonth = entries.Sum(t => t.Hours),
            JobsCompletedThisWeek = completedThisWeek,
            IsClockedIn = activeClock is not null,
            CurrentClockInTime = activeClock?.StartTime
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
            HireDate = emp.HireDate
        };
    }
}
