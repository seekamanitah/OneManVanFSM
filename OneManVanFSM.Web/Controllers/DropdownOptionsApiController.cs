using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/dropdownoptions")]
public class DropdownOptionsApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public DropdownOptionsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<DropdownOption>>> GetAll([FromQuery] DateTime? since)
    {
        _ = TrackDeviceAsync(_db);
        var query = _db.DropdownOptions.AsNoTracking().AsQueryable();
        if (since.HasValue)
            query = query.Where(d => d.CreatedAt > since.Value);
        var data = await query.OrderBy(d => d.Category).ThenBy(d => d.SortOrder).ToListAsync();
        return Ok(new SyncResponse<DropdownOption> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DropdownOption>> Get(int id)
    {
        var option = await _db.DropdownOptions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        return option is not null ? Ok(option) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<DropdownOption>> Create([FromBody] DropdownOption option)
    {
        option.Id = 0;
        option.CreatedAt = DateTime.UtcNow;
        _db.DropdownOptions.Add(option);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = option.Id }, option);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DropdownOption>> Update(int id, [FromBody] DropdownOption option)
    {
        var existing = await _db.DropdownOptions.FindAsync(id);
        if (existing is null) return NotFound();

        var createdAt = existing.CreatedAt;
        _db.Entry(existing).CurrentValues.SetValues(option);
        existing.Id = id;
        existing.CreatedAt = createdAt;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var option = await _db.DropdownOptions.FindAsync(id);
        if (option is null) return NotFound();

        if (option.IsSystem)
        {
            option.IsActive = false;
            await _db.SaveChangesAsync();
        }
        else
        {
            _db.DropdownOptions.Remove(option);
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }
}
