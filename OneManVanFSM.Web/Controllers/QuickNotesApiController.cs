using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/notes")]
public class QuickNotesApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public QuickNotesApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<QuickNote>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.QuickNotes.AsNoTracking()
            .Where(n => n.Status != QuickNoteStatus.Archived);
        if (since.HasValue)
            query = query.Where(n => n.UpdatedAt > since.Value);

        var data = await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
        return Ok(new SyncResponse<QuickNote> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuickNote>> Get(int id)
    {
        var note = await _db.QuickNotes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        return note is not null ? Ok(note) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<QuickNote>> Create([FromBody] QuickNote note)
    {
        note.Id = 0;
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;
        _db.QuickNotes.Add(note);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = note.Id }, note);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<QuickNote>> Update(int id, [FromBody] QuickNote note)
    {
        var existing = await _db.QuickNotes.FindAsync(id);
        if (existing is null) return NotFound();

        if (note.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "QuickNote",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = note.UpdatedAt
            });

        existing.Title = note.Title;
        existing.Text = note.Text;
        existing.Category = note.Category;
        existing.EntityType = note.EntityType;
        existing.EntityId = note.EntityId;
        existing.IsUrgent = note.IsUrgent;
        existing.Status = note.Status;
        existing.Tags = note.Tags;
        existing.PhotoPath = note.PhotoPath;
        existing.AudioPath = note.AudioPath;
        existing.CustomerId = note.CustomerId;
        existing.JobId = note.JobId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var note = await _db.QuickNotes.FindAsync(id);
        if (note is null) return NotFound();

        note.Status = QuickNoteStatus.Archived;
        note.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
