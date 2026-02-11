using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileDocumentService(AppDbContext db, ApiClient api) : IMobileDocumentService
{
    public async Task<List<MobileDocumentItem>> GetDocumentsAsync(MobileDocumentFilter? filter = null)
    {
        var query = db.Documents.AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(d =>
                    d.Name.ToLower().Contains(term) ||
                    (d.Notes != null && d.Notes.ToLower().Contains(term)));
            }
            if (filter.Category.HasValue)
                query = query.Where(d => d.Category == filter.Category.Value);
            if (filter.JobId.HasValue)
                query = query.Where(d => d.JobId == filter.JobId.Value);
        }

        return await query
            .OrderByDescending(d => d.UploadDate)
            .Take(50)
            .Select(d => new MobileDocumentItem
            {
                Id = d.Id,
                Name = d.Name,
                Category = d.Category,
                FileType = d.FileType,
                FileSize = d.FileSize,
                StoredFileName = d.StoredFileName,
                Notes = d.Notes,
                LinkedEntity = d.JobId.HasValue ? "Job"
                    : d.SiteId.HasValue ? "Site"
                    : d.AssetId.HasValue ? "Asset"
                    : d.CustomerId.HasValue ? "Customer"
                    : null,
                LinkedEntityId = d.JobId ?? d.SiteId ?? d.AssetId ?? d.CustomerId,
                UploadDate = d.UploadDate,
            })
            .ToListAsync();
    }

    public async Task<MobileDocumentItem?> GetDocumentAsync(int id)
    {
        return await db.Documents
            .Where(d => d.Id == id)
            .Select(d => new MobileDocumentItem
            {
                Id = d.Id,
                Name = d.Name,
                Category = d.Category,
                FileType = d.FileType,
                FileSize = d.FileSize,
                StoredFileName = d.StoredFileName,
                Notes = d.Notes,
                LinkedEntity = d.JobId.HasValue ? "Job"
                    : d.SiteId.HasValue ? "Site"
                    : d.AssetId.HasValue ? "Asset"
                    : d.CustomerId.HasValue ? "Customer"
                    : null,
                LinkedEntityId = d.JobId ?? d.SiteId ?? d.AssetId ?? d.CustomerId,
                UploadDate = d.UploadDate,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Document> CreateDocumentAsync(MobileDocumentCreate model)
    {
        var doc = new Document
        {
            Name = model.Name,
            Category = model.Category,
            FileType = model.FileType,
            Notes = model.Notes,
            JobId = model.JobId,
            SiteId = model.SiteId,
            AssetId = model.AssetId,
            UploadedByEmployeeId = model.UploadedByEmployeeId,
            UploadDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();
        return doc;
    }

    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var doc = await db.Documents.FindAsync(id);
        if (doc is null) return false;
        db.Documents.Remove(doc);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GetCachedFilePathAsync(int docId, string? storedFileName)
    {
        if (string.IsNullOrEmpty(storedFileName)) return null;

        var cacheDir = Path.Combine(FileSystem.CacheDirectory, "documents");
        Directory.CreateDirectory(cacheDir);
        var cachedPath = Path.Combine(cacheDir, storedFileName);

        if (File.Exists(cachedPath))
            return cachedPath;

        try
        {
            var stream = await api.GetStreamAsync($"api/documents/{docId}/file");
            if (stream is null) return null;

            await using var fs = new FileStream(cachedPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fs);
            return cachedPath;
        }
        catch
        {
            return null;
        }
    }

    public async Task OpenDocumentAsync(int docId, string? storedFileName, string? fileType)
    {
        var path = await GetCachedFilePathAsync(docId, storedFileName);
        if (path is null) return;

        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(path, GetMimeType(fileType))
            });
        }
        catch
        {
            // Device has no app to handle this file type — ignore gracefully
        }
    }

    private static string GetMimeType(string? fileType) => fileType?.ToUpperInvariant() switch
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
        _ => "application/octet-stream"
    };
}
