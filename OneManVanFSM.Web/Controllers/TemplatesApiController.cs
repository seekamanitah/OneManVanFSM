using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/templates")]
public class TemplatesApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public TemplatesApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Template>>> GetAll([FromQuery] DateTime? since)
    {
        _ = TrackDeviceAsync(_db);
        var query = _db.Templates
            .AsNoTracking()
            .Include(t => t.Versions)
            .AsQueryable();

        if (since.HasValue)
            query = query.Where(t => t.UpdatedAt > since.Value);

        var data = await query.OrderBy(t => t.Name).ToListAsync();

        // Clear circular navigation references for serialization
        foreach (var t in data)
            foreach (var v in t.Versions)
                v.Template = null!;

        return Ok(new SyncResponse<Template> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Template>> Get(int id)
    {
        var template = await _db.Templates.AsNoTracking()
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (template is null) return NotFound();
        foreach (var v in template.Versions)
            v.Template = null!;
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<Template>> Create([FromBody] Template template)
    {
        template.Id = 0;
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        _db.Templates.Add(template);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = template.Id }, template);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Template>> Update(int id, [FromBody] Template template)
    {
        var existing = await _db.Templates.FindAsync(id);
        if (existing is null) return NotFound();

        if (template.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "Template",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = template.UpdatedAt
            });

        var createdAt = existing.CreatedAt;
        _db.Entry(existing).CurrentValues.SetValues(template);
        existing.Id = id;
        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template is null) return NotFound();

        template.IsArchived = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
