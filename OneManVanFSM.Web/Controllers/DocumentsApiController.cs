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
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DocumentsApiController> _logger;
    public DocumentsApiController(AppDbContext db, IWebHostEnvironment env, ILogger<DocumentsApiController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Document>>> GetAll([FromQuery] DateTime? since)
    {
        try
        {
            var query = _db.Documents.AsNoTracking().Where(d => !d.IsArchived);
            if (since.HasValue)
                query = query.Where(d => d.UpdatedAt > since.Value);

            // Project to a clean DTO to avoid navigation property serialization issues
            // (Document has 6 FK nav properties including 2 Employee references)
            var data = await query
                .OrderByDescending(d => d.UpdatedAt)
                .Select(d => new Document
                {
                    Id = d.Id,
                    Name = d.Name,
                    Category = d.Category,
                    FilePath = d.FilePath,
                    StoredFileName = d.StoredFileName,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    Version = d.Version,
                    AccessLevel = d.AccessLevel,
                    CustomTags = d.CustomTags,
                    Notes = d.Notes,
                    UploadDate = d.UploadDate,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    IsArchived = d.IsArchived,
                    CustomerId = d.CustomerId,
                    SiteId = d.SiteId,
                    AssetId = d.AssetId,
                    JobId = d.JobId,
                    EmployeeId = d.EmployeeId,
                    UploadedByEmployeeId = d.UploadedByEmployeeId,
                })
                .ToListAsync();
            return Ok(new SyncResponse<Document> { Data = data, TotalCount = data.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documents for sync.");
            return StatusCode(500, new { error = "Failed to retrieve documents.", detail = ex.Message });
        }
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
        document.UpdatedAt = DateTime.UtcNow;
        document.UploadDate = DateTime.Now;
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
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return NotFound();

        doc.IsArchived = true;
        doc.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var doc = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null || string.IsNullOrEmpty(doc.StoredFileName))
            return NotFound();

        var filePath = Path.Combine(_env.WebRootPath, "uploads", doc.StoredFileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = (doc.FileType?.ToUpperInvariant()) switch
        {
            "PDF" => "application/pdf",
            "JPG" or "JPEG" => "image/jpeg",
            "PNG" => "image/png",
            "GIF" => "image/gif",
            "WEBP" => "image/webp",
            "BMP" => "image/bmp",
            "DOC" => "application/msword",
            "DOCX" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "XLS" => "application/vnd.ms-excel",
            "XLSX" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "CSV" => "text/csv",
            "TXT" => "text/plain",
            "ZIP" => "application/zip",
            "RAR" => "application/x-rar-compressed",
            _ => "application/octet-stream"
        };

        var fileName = doc.FilePath ?? doc.Name + "." + (doc.FileType?.ToLowerInvariant() ?? "bin");
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, contentType, fileName);
    }
}
