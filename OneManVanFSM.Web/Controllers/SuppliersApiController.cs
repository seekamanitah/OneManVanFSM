using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/suppliers")]
public class SuppliersApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public SuppliersApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Supplier>>> GetAll([FromQuery] DateTime? since)
    {
        _ = TrackDeviceAsync(_db);
        var query = _db.Suppliers.AsNoTracking().AsQueryable();
        if (since.HasValue)
            query = query.Where(s => s.CreatedAt > since.Value);
        var data = await query.OrderBy(s => s.Name).ToListAsync();
        return Ok(new SyncResponse<Supplier> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Supplier>> Get(int id)
    {
        var supplier = await _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        return supplier is not null ? Ok(supplier) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Supplier>> Create([FromBody] Supplier supplier)
    {
        supplier.Id = 0;
        supplier.CreatedAt = DateTime.UtcNow;
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Supplier>> Update(int id, [FromBody] Supplier supplier)
    {
        var existing = await _db.Suppliers.FindAsync(id);
        if (existing is null) return NotFound();

        var createdAt = existing.CreatedAt;
        _db.Entry(existing).CurrentValues.SetValues(supplier);
        existing.Id = id;
        existing.CreatedAt = createdAt;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null) return NotFound();

        supplier.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
