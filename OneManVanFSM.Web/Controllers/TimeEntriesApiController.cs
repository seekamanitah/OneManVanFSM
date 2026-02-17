using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/timeentries")]
public class TimeEntriesApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public TimeEntriesApiController(AppDbContext db) => _db = db;

    /// <summary>GET /api/timeentries?employeeId=1&amp;since=...</summary>
    [HttpGet]
    public async Task<ActionResult<SyncResponse<TimeEntry>>> GetAll([FromQuery] int? employeeId, [FromQuery] DateTime? since)
    {
        var query = _db.TimeEntries.AsNoTracking().AsQueryable();
        if (employeeId.HasValue)
            query = query.Where(t => t.EmployeeId == employeeId.Value);
        if (since.HasValue)
            query = query.Where(t => t.UpdatedAt > since.Value);

        var data = await query.OrderByDescending(t => t.StartTime).ToListAsync();
        return Ok(new SyncResponse<TimeEntry> { Data = data, TotalCount = data.Count });
    }

    /// <summary>POST /api/timeentries/clockin</summary>
    [HttpPost("clockin")]
    public async Task<ActionResult<TimeEntry>> ClockIn([FromBody] TimeEntryClockInRequest req)
    {
        // Validate employee exists
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == req.EmployeeId);
        if (employee is null)
            return BadRequest("Employee not found.");

        var entryType = req.EntryType?.Equals("JobClock", StringComparison.OrdinalIgnoreCase) == true
            ? TimeEntryType.JobClock
            : TimeEntryType.Shift;

        if (entryType == TimeEntryType.Shift)
        {
            // Only one active shift per employee
            var activeShift = await _db.TimeEntries
                .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.Shift && t.EndTime == null);
            if (activeShift is not null)
                return BadRequest("Already clocked in to a shift.");
        }
        else
        {
            // Must have an active shift to start a job clock
            var hasShift = await _db.TimeEntries
                .AnyAsync(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.Shift && t.EndTime == null);
            if (!hasShift)
                return BadRequest("Must clock in to a shift before starting a job clock.");

            // Prevent double job clock on same job
            if (req.JobId.HasValue)
            {
                var existingJobClock = await _db.TimeEntries
                    .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.JobClock && t.JobId == req.JobId && t.EndTime == null);
                if (existingJobClock is not null)
                    return Ok(existingJobClock); // Idempotent — return the existing clock
            }
        }

        var entry = new TimeEntry
        {
            EmployeeId = req.EmployeeId,
            JobId = req.JobId,
            StartTime = DateTime.Now,
            EntryType = entryType,
            HourlyRate = req.RateOverride ?? employee.HourlyRate,
            TimeCategory = req.TimeCategory,
            IsBillable = entryType == TimeEntryType.JobClock,
            ClockInLatitude = req.Latitude,
            ClockInLongitude = req.Longitude,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.TimeEntries.Add(entry);

        // Auto-assign employee to job team if clocking into a job
        if (entryType == TimeEntryType.JobClock && req.JobId.HasValue)
        {
            var alreadyOnTeam = await _db.Set<JobEmployee>()
                .AnyAsync(je => je.JobId == req.JobId.Value && je.EmployeeId == req.EmployeeId);
            if (!alreadyOnTeam)
            {
                _db.Set<JobEmployee>().Add(new JobEmployee
                {
                    JobId = req.JobId.Value,
                    EmployeeId = req.EmployeeId,
                    Role = "Technician",
                    AssignedAt = DateTime.UtcNow,
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(entry);
    }

    /// <summary>POST /api/timeentries/clockout — Ends the active shift and auto-closes any open job clocks</summary>
    [HttpPost("clockout")]
    public async Task<ActionResult<TimeEntry>> ClockOut([FromBody] TimeEntryClockOutRequest req)
    {
        var entryType = req.EntryType?.Equals("JobClock", StringComparison.OrdinalIgnoreCase) == true
            ? TimeEntryType.JobClock
            : TimeEntryType.Shift;

        if (entryType == TimeEntryType.Shift)
        {
            var activeShift = await _db.TimeEntries
                .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.Shift && t.EndTime == null);
            if (activeShift is null)
                return BadRequest("No active shift.");

            // Auto-close all open job clocks
            var openJobClocks = await _db.TimeEntries
                .Where(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.JobClock && t.EndTime == null)
                .ToListAsync();
            foreach (var jc in openJobClocks)
            {
                jc.EndTime = DateTime.Now;
                jc.Hours = (decimal)(jc.EndTime.Value - jc.StartTime).TotalHours;
                jc.UpdatedAt = DateTime.UtcNow;
            }

            activeShift.EndTime = DateTime.Now;
            activeShift.Hours = (decimal)(activeShift.EndTime.Value - activeShift.StartTime).TotalHours;
            if (activeShift.Hours > 8)
                activeShift.OvertimeHours = activeShift.Hours - 8;
            activeShift.ClockOutLatitude = req.Latitude;
            activeShift.ClockOutLongitude = req.Longitude;
            activeShift.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(activeShift);
        }
        else
        {
            // Clock out a specific job clock (fallback — prefer /jobclockout)
            var activeJob = await _db.TimeEntries
                .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.JobClock && t.JobId == req.JobId && t.EndTime == null);
            if (activeJob is null)
                return BadRequest("No active job clock.");

            activeJob.EndTime = DateTime.Now;
            activeJob.Hours = (decimal)(activeJob.EndTime.Value - activeJob.StartTime).TotalHours;
            activeJob.ClockOutLatitude = req.Latitude;
            activeJob.ClockOutLongitude = req.Longitude;
            activeJob.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(activeJob);
        }
    }

    /// <summary>POST /api/timeentries/jobclockout — Ends a specific job clock</summary>
    [HttpPost("jobclockout")]
    public async Task<ActionResult<TimeEntry>> JobClockOut([FromBody] TimeEntryClockOutRequest req)
    {
        if (!req.JobId.HasValue)
            return BadRequest("JobId is required for job clock out.");

        var active = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.JobClock && t.JobId == req.JobId && t.EndTime == null);
        if (active is null)
            return BadRequest("No active job clock for this job.");

        active.EndTime = DateTime.Now;
        active.Hours = (decimal)(active.EndTime.Value - active.StartTime).TotalHours;
        active.ClockOutLatitude = req.Latitude;
        active.ClockOutLongitude = req.Longitude;
        active.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(active);
    }

    /// <summary>GET /api/timeentries/active/{employeeId}?entryType=Shift</summary>
    [HttpGet("active/{employeeId:int}")]
    public async Task<ActionResult<TimeEntry?>> GetActive(int employeeId, [FromQuery] string? entryType = null)
    {
        var query = _db.TimeEntries.AsNoTracking()
            .Where(t => t.EmployeeId == employeeId && t.EndTime == null);

        if (!string.IsNullOrEmpty(entryType))
        {
            var type = entryType.Equals("JobClock", StringComparison.OrdinalIgnoreCase)
                ? TimeEntryType.JobClock
                : TimeEntryType.Shift;
            query = query.Where(t => t.EntryType == type);
        }
        else
        {
            // Default to shift for backward compatibility
            query = query.Where(t => t.EntryType == TimeEntryType.Shift);
        }

        var active = await query.FirstOrDefaultAsync();
        return Ok(active);
    }

    /// <summary>GET /api/timeentries/activejobs/{employeeId} — Returns all active job clocks</summary>
    [HttpGet("activejobs/{employeeId:int}")]
    public async Task<ActionResult<List<TimeEntry>>> GetActiveJobClocks(int employeeId)
    {
        var clocks = await _db.TimeEntries.AsNoTracking()
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId && t.EntryType == TimeEntryType.JobClock && t.EndTime == null)
            .ToListAsync();
        return Ok(clocks);
    }

    [HttpPost]
    public async Task<ActionResult<TimeEntry>> Create([FromBody] TimeEntry entry)
    {
        entry.Id = 0;
        entry.CreatedAt = DateTime.UtcNow;
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(entry);
    }

    /// <summary>POST /api/timeentries/pause — Start a break (pause shift)</summary>
    [HttpPost("pause")]
    public async Task<ActionResult<TimeEntry>> PauseShift([FromBody] PauseRequest req)
    {
        // Must have an active shift
        var shift = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.Shift && t.EndTime == null);
        if (shift is null)
            return BadRequest("No active shift to pause.");

        // Prevent double-pause
        var existingBreak = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.Break && t.EndTime == null);
        if (existingBreak is not null)
            return Ok(existingBreak); // Idempotent — return existing break

        var breakEntry = new TimeEntry
        {
            EmployeeId = req.EmployeeId,
            StartTime = DateTime.Now,
            EntryType = TimeEntryType.Break,
            IsBillable = false,
            TimeCategory = "Break",
            Notes = req.Reason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.TimeEntries.Add(breakEntry);
        await _db.SaveChangesAsync();
        return Ok(breakEntry);
    }

    /// <summary>POST /api/timeentries/resume — End break (resume shift)</summary>
    [HttpPost("resume")]
    public async Task<ActionResult<TimeEntry>> ResumeShift([FromBody] ResumeRequest req)
    {
        var activeBreak = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EntryType == TimeEntryType.Break && t.EndTime == null);
        if (activeBreak is null)
            return BadRequest("No active break to resume from.");

        activeBreak.EndTime = DateTime.Now;
        activeBreak.Hours = Math.Round((decimal)(activeBreak.EndTime.Value - activeBreak.StartTime).TotalHours, 2);
        activeBreak.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(activeBreak);
    }
}

public class PauseRequest
{
    public int EmployeeId { get; set; }
    public string? Reason { get; set; }
}

public class ResumeRequest
{
    public int EmployeeId { get; set; }
}

public class TimeEntryClockInRequest
{
    public int EmployeeId { get; set; }
    public int? JobId { get; set; }
    public string? EntryType { get; set; } // "Shift" or "JobClock"
    public decimal? RateOverride { get; set; }
    public string? TimeCategory { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class TimeEntryClockOutRequest
{
    public int EmployeeId { get; set; }
    public int? JobId { get; set; }
    public string? EntryType { get; set; } // "Shift" or "JobClock"
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
