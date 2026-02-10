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
            query = query.Where(t => t.CreatedAt > since.Value);

        var data = await query.OrderByDescending(t => t.StartTime).ToListAsync();
        return Ok(new SyncResponse<TimeEntry> { Data = data, TotalCount = data.Count });
    }

    /// <summary>POST /api/timeentries/clockin</summary>
    [HttpPost("clockin")]
    public async Task<ActionResult<TimeEntry>> ClockIn([FromBody] TimeEntryClockInRequest req)
    {
        // Validate employee exists
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == req.EmployeeId);
        if (!employeeExists)
            return BadRequest("Employee not found.");

        // Check not already clocked in
        var active = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EndTime == null);
        if (active is not null)
            return BadRequest("Already clocked in.");

        var entry = new TimeEntry
        {
            EmployeeId = req.EmployeeId,
            JobId = req.JobId,
            StartTime = DateTime.UtcNow,
            TimeCategory = req.TimeCategory,
            ClockInLatitude = req.Latitude,
            ClockInLongitude = req.Longitude,
            CreatedAt = DateTime.UtcNow,
        };

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(entry);
    }

    /// <summary>POST /api/timeentries/clockout</summary>
    [HttpPost("clockout")]
    public async Task<ActionResult<TimeEntry>> ClockOut([FromBody] TimeEntryClockOutRequest req)
    {
        var active = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == req.EmployeeId && t.EndTime == null);
        if (active is null)
            return BadRequest("Not clocked in.");

        active.EndTime = DateTime.UtcNow;
        active.Hours = (decimal)(active.EndTime.Value - active.StartTime).TotalHours;
        active.ClockOutLatitude = req.Latitude;
        active.ClockOutLongitude = req.Longitude;
        await _db.SaveChangesAsync();
        return Ok(active);
    }

    /// <summary>GET /api/timeentries/active/{employeeId}</summary>
    [HttpGet("active/{employeeId:int}")]
    public async Task<ActionResult<TimeEntry?>> GetActive(int employeeId)
    {
        var active = await _db.TimeEntries.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);
        return Ok(active);
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
}

public class TimeEntryClockInRequest
{
    public int EmployeeId { get; set; }
    public int? JobId { get; set; }
    public string? TimeCategory { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class TimeEntryClockOutRequest
{
    public int EmployeeId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
