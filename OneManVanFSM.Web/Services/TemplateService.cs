using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class TemplateService : ITemplateService
{
    private readonly AppDbContext _db;
    public TemplateService(AppDbContext db) => _db = db;

    public async Task<List<TemplateListItem>> GetTemplatesAsync(TemplateFilter? filter = null)
    {
        var query = _db.Templates.Where(t => (filter != null && filter.ShowArchived) ? t.IsArchived : !t.IsArchived).AsQueryable();
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(term) ||
                    (t.Description != null && t.Description.ToLower().Contains(term)));
            }
            if (filter.Type.HasValue) query = query.Where(t => t.Type == filter.Type.Value);
            if (filter.IsCompanyDefault.HasValue) query = query.Where(t => t.IsCompanyDefault == filter.IsCompanyDefault.Value);

            query = filter.SortBy?.ToLower() switch
            {
                "type" => filter.SortDescending ? query.OrderByDescending(t => t.Type) : query.OrderBy(t => t.Type),
                "usage" => filter.SortDescending ? query.OrderByDescending(t => t.UsageCount) : query.OrderBy(t => t.UsageCount),
                "lastused" => filter.SortDescending ? query.OrderByDescending(t => t.LastUsed) : query.OrderBy(t => t.LastUsed),
                _ => filter.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name)
            };
        }
        else query = query.OrderBy(t => t.Name);

        return await query.Select(t => new TemplateListItem
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Type = t.Type,
            IsCompanyDefault = t.IsCompanyDefault,
            UsageCount = t.UsageCount,
            LastUsed = t.LastUsed,
            CustomerName = t.Customer != null ? t.Customer.Name : null,
            CompanyName = t.Company != null ? t.Company.Name : null,
            IsArchived = t.IsArchived
        }).ToListAsync();
    }

    public async Task<TemplateDetail?> GetTemplateAsync(int id)
    {
        return await _db.Templates
            .Include(t => t.Customer)
            .Include(t => t.Company)
            .Include(t => t.Versions.OrderByDescending(v => v.VersionNumber))
            .Where(t => t.Id == id)
            .Select(t => new TemplateDetail
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Type = t.Type,
                Data = t.Data,
                IsCompanyDefault = t.IsCompanyDefault,
                UsageCount = t.UsageCount,
                LastUsed = t.LastUsed,
                Notes = t.Notes,
                IsArchived = t.IsArchived,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer != null ? t.Customer.Name : null,
                CompanyId = t.CompanyId,
                CompanyName = t.Company != null ? t.Company.Name : null,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Versions = t.Versions.Select(v => new TemplateVersionItem
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    ChangeNotes = v.ChangeNotes,
                    CreatedAt = v.CreatedAt
                }).ToList()
            }).FirstOrDefaultAsync();
    }

    public async Task<Template> CreateTemplateAsync(TemplateEditModel model)
    {
        var template = new Template
        {
            Name = model.Name,
            Description = model.Description,
            Type = model.Type,
            Data = model.Data,
            IsCompanyDefault = model.IsCompanyDefault,
            Notes = model.Notes,
            CustomerId = model.CustomerId,
            CompanyId = model.CompanyId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Templates.Add(template);
        await _db.SaveChangesAsync();
        return template;
    }

    public async Task<bool> UpdateTemplateAsync(int id, TemplateEditModel model)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template is null) return false;

        // Save version snapshot before updating
        var version = new TemplateVersion
        {
            TemplateId = id,
            VersionNumber = await _db.TemplateVersions.CountAsync(v => v.TemplateId == id) + 1,
            Data = template.Data,
            ChangeNotes = $"Updated: {model.Name}",
            CreatedAt = DateTime.UtcNow
        };
        _db.TemplateVersions.Add(version);

        template.Name = model.Name;
        template.Description = model.Description;
        template.Type = model.Type;
        template.Data = model.Data;
        template.IsCompanyDefault = model.IsCompanyDefault;
        template.Notes = model.Notes;
        template.CustomerId = model.CustomerId;
        template.CompanyId = model.CompanyId;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTemplateAsync(int id)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template is null) return false;
        template.IsArchived = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveTemplateAsync(int id)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template is null) return false;
        template.IsArchived = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreTemplateAsync(int id)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template is null) return false;
        template.IsArchived = false;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTemplatePermanentlyAsync(int id)
    {
        var template = await _db.Templates.Include(t => t.Versions).FirstOrDefaultAsync(t => t.Id == id);
        if (template is null) return false;
        _db.TemplateVersions.RemoveRange(template.Versions);
        _db.Templates.Remove(template);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveTemplatesAsync(List<int> ids)
    {
        var templates = await _db.Templates.Where(t => ids.Contains(t.Id) && !t.IsArchived).ToListAsync();
        foreach (var t in templates) { t.IsArchived = true; t.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return templates.Count;
    }

    public async Task<int> BulkRestoreTemplatesAsync(List<int> ids)
    {
        var templates = await _db.Templates.Where(t => ids.Contains(t.Id) && t.IsArchived).ToListAsync();
        foreach (var t in templates) { t.IsArchived = false; t.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return templates.Count;
    }

    public async Task<int> BulkDeleteTemplatesPermanentlyAsync(List<int> ids)
    {
        var templates = await _db.Templates.Include(t => t.Versions).Where(t => ids.Contains(t.Id)).ToListAsync();
        foreach (var t in templates) _db.TemplateVersions.RemoveRange(t.Versions);
        _db.Templates.RemoveRange(templates);
        await _db.SaveChangesAsync();
        return templates.Count;
    }

    public async Task<bool> CloneTemplateAsync(int id, string newName)
    {
        var source = await _db.Templates.FindAsync(id);
        if (source is null) return false;

        var clone = new Template
        {
            Name = newName,
            Description = source.Description,
            Type = source.Type,
            Data = source.Data,
            IsCompanyDefault = false,
            Notes = source.Notes,
            CustomerId = source.CustomerId,
            CompanyId = source.CompanyId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Templates.Add(clone);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IncrementUsageAsync(int id)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template is null) return false;
        template.UsageCount++;
        template.LastUsed = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
