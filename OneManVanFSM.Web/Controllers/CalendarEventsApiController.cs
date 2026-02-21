using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/calendarevents")]
public class CalendarEventsApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public CalendarEventsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<CalendarEvent>>> GetAll([FromQuery] DateTime? since)
    {
        _ = TrackDeviceAsync(_db);
        var query = _db.CalendarEvents.AsNoTracking().AsQueryable();
        if (since.HasValue)
            query = query.Where(e => e.UpdatedAt > since.Value);
        var data = await query.OrderBy(e => e.StartDateTime).ToListAsync();
        return Ok(new SyncResponse<CalendarEvent> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CalendarEvent>> Get(int id)
    {
        var calEvent = await _db.CalendarEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return calEvent is not null ? Ok(calEvent) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<CalendarEvent>> Create([FromBody] CalendarEvent calEvent)
    {
        calEvent.Id = 0;
        calEvent.CreatedAt = DateTime.UtcNow;
        calEvent.UpdatedAt = DateTime.UtcNow;
        _db.CalendarEvents.Add(calEvent);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = calEvent.Id }, calEvent);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CalendarEvent>> Update(int id, [FromBody] CalendarEvent calEvent)
    {
        var existing = await _db.CalendarEvents.FindAsync(id);
        if (existing is null) return NotFound();

        if (calEvent.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "CalendarEvent",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = calEvent.UpdatedAt
            });

        var createdAt = existing.CreatedAt;
        _db.Entry(existing).CurrentValues.SetValues(calEvent);
        existing.Id = id;
        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var calEvent = await _db.CalendarEvents.FindAsync(id);
        if (calEvent is null) return NotFound();

        _db.CalendarEvents.Remove(calEvent);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
