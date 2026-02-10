using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/assets")]
public class AssetsApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public AssetsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Asset>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Assets.AsNoTracking().Where(a => a.Status != AssetStatus.Decommissioned);
        if (since.HasValue)
            query = query.Where(a => a.UpdatedAt > since.Value);
        var data = await query.OrderBy(a => a.Name).ToListAsync();
        return Ok(new SyncResponse<Asset> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Asset>> Get(int id)
    {
        var asset = await _db.Assets.AsNoTracking()
            .Include(a => a.Site)
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == id);
        return asset is not null ? Ok(asset) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Asset>> Create([FromBody] Asset asset)
    {
        asset.Id = 0;
        asset.CreatedAt = DateTime.UtcNow;
        asset.UpdatedAt = DateTime.UtcNow;
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = asset.Id }, asset);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Asset>> Update(int id, [FromBody] Asset asset)
    {
        var existing = await _db.Assets.FindAsync(id);
        if (existing is null) return NotFound();
        if (asset.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict { EntityId = id, EntityType = "Asset", Message = "Server version is newer.", ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = asset.UpdatedAt });
        _db.Entry(existing).CurrentValues.SetValues(asset);
        existing.Id = id;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }
}

[Route("api/employees")]
public class EmployeesApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public EmployeesApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Employee>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Employees.AsNoTracking().Where(e => !e.IsArchived);
        if (since.HasValue)
            query = query.Where(e => e.UpdatedAt > since.Value);
        var data = await query.OrderBy(e => e.Name).ToListAsync();
        return Ok(new SyncResponse<Employee> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Employee>> Get(int id)
    {
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return employee is not null ? Ok(employee) : NotFound();
    }
}
