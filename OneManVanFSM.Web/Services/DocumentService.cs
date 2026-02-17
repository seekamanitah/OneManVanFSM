using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public DocumentService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<List<DocumentListItem>> GetDocumentsAsync(DocumentFilter? filter = null)
    {
        var query = _db.Documents.AsQueryable();
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(term) ||
                    (d.Notes != null && d.Notes.ToLower().Contains(term)));
            }
            if (filter.Category.HasValue) query = query.Where(d => d.Category == filter.Category.Value);
            if (filter.EntityType == "Customer" && filter.EntityId.HasValue)
                query = query.Where(d => d.CustomerId == filter.EntityId);
            if (filter.EntityType == "Job" && filter.EntityId.HasValue)
                query = query.Where(d => d.JobId == filter.EntityId);
            if (filter.EntityType == "Asset" && filter.EntityId.HasValue)
                query = query.Where(d => d.AssetId == filter.EntityId);

            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
                "category" => filter.SortDescending ? query.OrderByDescending(d => d.Category) : query.OrderBy(d => d.Category),
                _ => filter.SortDescending ? query.OrderByDescending(d => d.UploadDate) : query.OrderBy(d => d.UploadDate)
            };
        }
        else query = query.OrderByDescending(d => d.UploadDate);

        return await query.Select(d => new DocumentListItem
        {
            Id = d.Id, Name = d.Name, Category = d.Category, FileType = d.FileType,
            FileSize = d.FileSize, Version = d.Version, AccessLevel = d.AccessLevel,
            UploadDate = d.UploadDate,
            UploadedByName = d.UploadedByEmployee != null ? d.UploadedByEmployee.Name : null,
            CustomerName = d.Customer != null ? d.Customer.Name : null,
            JobTitle = d.Job != null ? d.Job.Title : null,
            LinkedTo = d.CustomerId.HasValue ? "Customer" :
                       d.JobId.HasValue ? "Job" :
                       d.AssetId.HasValue ? "Asset" :
                       d.EmployeeId.HasValue ? "Employee" : null
        }).ToListAsync();
    }

    public async Task<DocumentDetail?> GetDocumentAsync(int id)
    {
        return await _db.Documents
            .Include(d => d.Customer)
            .Include(d => d.Site)
            .Include(d => d.Asset)
            .Include(d => d.Job)
            .Include(d => d.Employee)
            .Include(d => d.UploadedByEmployee)
            .Where(d => d.Id == id)
            .Select(d => new DocumentDetail
            {
                Id = d.Id, Name = d.Name, Category = d.Category,
                FilePath = d.FilePath, StoredFileName = d.StoredFileName,
                FileType = d.FileType, FileSize = d.FileSize,
                Version = d.Version, AccessLevel = d.AccessLevel,
                CustomTags = d.CustomTags, Notes = d.Notes,
                UploadDate = d.UploadDate, CreatedAt = d.CreatedAt,
                CustomerId = d.CustomerId,
                CustomerName = d.Customer != null ? d.Customer.Name : null,
                SiteId = d.SiteId,
                SiteName = d.Site != null ? d.Site.Name : null,
                AssetId = d.AssetId,
                AssetName = d.Asset != null ? d.Asset.Name : null,
                JobId = d.JobId,
                JobTitle = d.Job != null ? d.Job.Title : null,
                EmployeeId = d.EmployeeId,
                EmployeeName = d.Employee != null ? d.Employee.Name : null,
                UploadedByEmployeeId = d.UploadedByEmployeeId,
                UploadedByName = d.UploadedByEmployee != null ? d.UploadedByEmployee.Name : null
            }).FirstOrDefaultAsync();
    }

    public async Task<Document> CreateDocumentAsync(DocumentEditModel model)
    {
        var doc = new Document
        {
            Name = model.Name, Category = model.Category, FilePath = model.FilePath,
            FileType = model.FileType, FileSize = model.FileSize,
            AccessLevel = model.AccessLevel, CustomTags = model.CustomTags, Notes = model.Notes,
            CustomerId = model.CustomerId, SiteId = model.SiteId, AssetId = model.AssetId,
            JobId = model.JobId, EmployeeId = model.EmployeeId,
            UploadDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        return doc;
    }

    public async Task<Document> UpdateDocumentAsync(int id, DocumentEditModel model)
    {
        var doc = await _db.Documents.FindAsync(id)
            ?? throw new InvalidOperationException("Document not found.");
        doc.Name = model.Name;
        doc.Category = model.Category;
        doc.AccessLevel = model.AccessLevel;
        doc.CustomTags = model.CustomTags;
        doc.Notes = model.Notes;
        doc.CustomerId = model.CustomerId;
        doc.SiteId = model.SiteId;
        doc.AssetId = model.AssetId;
        doc.JobId = model.JobId;
        doc.EmployeeId = model.EmployeeId;
        await _db.SaveChangesAsync();
        return doc;
    }

    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return false;

        // Clean up stored file
        if (!string.IsNullOrEmpty(doc.StoredFileName))
        {
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
            var filePath = Path.Combine(uploadDir, doc.StoredFileName);
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Document> UploadDocumentAsync(DocumentEditModel model, Stream fileStream, string fileName, long fileSize)
    {
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadDir);

        var ext = Path.GetExtension(fileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, storedName);

        await using var fs = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fs);

        var doc = new Document
        {
            Name = model.Name,
            Category = model.Category,
            FilePath = fileName, // Original file name
            StoredFileName = storedName,
            FileType = ext?.TrimStart('.').ToUpperInvariant(),
            FileSize = fileSize,
            AccessLevel = model.AccessLevel,
            CustomTags = model.CustomTags,
            Notes = model.Notes,
            CustomerId = model.CustomerId,
            SiteId = model.SiteId,
            AssetId = model.AssetId,
            JobId = model.JobId,
            EmployeeId = model.EmployeeId,
            UploadDate = DateTime.Now,
            CreatedAt = DateTime.UtcNow
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        return doc;
    }

    public async Task<(Stream stream, string contentType, string fileName)?> DownloadDocumentAsync(int id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null || string.IsNullOrEmpty(doc.StoredFileName)) return null;

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
        var filePath = Path.Combine(uploadDir, doc.StoredFileName);
        if (!File.Exists(filePath)) return null;

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var contentType = doc.FileType?.ToLowerInvariant() switch
        {
            "pdf" => "application/pdf",
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xls" => "application/vnd.ms-excel",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "csv" => "text/csv",
            "txt" => "text/plain",
            "zip" => "application/zip",
            _ => "application/octet-stream"
        };
        var downloadName = !string.IsNullOrEmpty(doc.FilePath) ? doc.FilePath : $"{doc.Name}.{doc.FileType?.ToLowerInvariant() ?? "bin"}";
        return (stream, contentType, downloadName);
    }
}
