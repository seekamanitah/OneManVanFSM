using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileDocumentService(AppDbContext db) : IMobileDocumentService
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
}
