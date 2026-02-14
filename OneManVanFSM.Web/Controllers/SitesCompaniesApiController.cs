using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/sites")]
public class SitesApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public SitesApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Site>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Sites.AsNoTracking().Where(s => !s.IsArchived);
        if (since.HasValue)
            query = query.Where(s => s.UpdatedAt > since.Value);
        var data = await query.OrderBy(s => s.Name).ToListAsync();
        return Ok(new SyncResponse<Site> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Site>> Get(int id)
    {
        var site = await _db.Sites.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Assets.Where(a => a.Status != AssetStatus.Decommissioned))
            .FirstOrDefaultAsync(s => s.Id == id);
        return site is not null ? Ok(site) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Site>> Create([FromBody] Site site)
    {
        site.Id = 0;
        site.CreatedAt = DateTime.UtcNow;
        site.UpdatedAt = DateTime.UtcNow;
        _db.Sites.Add(site);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = site.Id }, site);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Site>> Update(int id, [FromBody] Site site)
    {
        var existing = await _db.Sites.FindAsync(id);
        if (existing is null) return NotFound();
        if (site.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict { EntityId = id, EntityType = "Site", Message = "Server version is newer.", ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = site.UpdatedAt });
        var createdAt = existing.CreatedAt;
        _db.Entry(existing).CurrentValues.SetValues(site);
        existing.Id = id;
        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var site = await _db.Sites.FindAsync(id);
        if (site is null) return NotFound();
        site.IsArchived = true;
        site.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[Route("api/companies")]
public class CompaniesApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public CompaniesApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Company>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Companies.AsNoTracking().Where(c => !c.IsArchived);
        if (since.HasValue)
            query = query.Where(c => c.UpdatedAt > since.Value);
        var data = await query.OrderBy(c => c.Name).ToListAsync();
        return Ok(new SyncResponse<Company> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Company>> Get(int id)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return company is not null ? Ok(company) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Company>> Create([FromBody] Company company)
    {
        company.Id = 0;
        company.CreatedAt = DateTime.UtcNow;
        company.UpdatedAt = DateTime.UtcNow;
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = company.Id }, company);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Company>> Update(int id, [FromBody] Company company)
    {
        var existing = await _db.Companies.FindAsync(id);
        if (existing is null) return NotFound();
        if (company.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict { EntityId = id, EntityType = "Company", Message = "Server version is newer.", ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = company.UpdatedAt });
        var createdAt = existing.CreatedAt;
        _db.Entry(existing).CurrentValues.SetValues(company);
        existing.Id = id;
        existing.CreatedAt = createdAt;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }
}
