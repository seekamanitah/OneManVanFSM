using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/documents")]
public class DocumentsApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public DocumentsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Document>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Documents.AsNoTracking().AsQueryable();
        if (since.HasValue)
            query = query.Where(d => d.CreatedAt > since.Value);

        var data = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return Ok(new SyncResponse<Document> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Document>> Get(int id)
    {
        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        return doc is not null ? Ok(doc) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Document>> Create([FromBody] Document document)
    {
        document.Id = 0;
        document.CreatedAt = DateTime.UtcNow;
        document.UploadDate = DateTime.UtcNow;
        _db.Documents.Add(document);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = document.Id }, document);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Document>> Update(int id, [FromBody] Document document)
    {
        var existing = await _db.Documents.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Name = document.Name;
        existing.Category = document.Category;
        existing.FilePath = document.FilePath;
        existing.StoredFileName = document.StoredFileName;
        existing.FileType = document.FileType;
        existing.FileSize = document.FileSize;
        existing.Version = document.Version;
        existing.AccessLevel = document.AccessLevel;
        existing.CustomTags = document.CustomTags;
        existing.Notes = document.Notes;
        existing.CustomerId = document.CustomerId;
        existing.SiteId = document.SiteId;
        existing.AssetId = document.AssetId;
        existing.JobId = document.JobId;
        existing.EmployeeId = document.EmployeeId;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return NotFound();

        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
