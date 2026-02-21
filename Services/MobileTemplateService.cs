using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileTemplateService(AppDbContext db) : IMobileTemplateService
{
    public async Task<List<MobileTemplateCard>> GetTemplatesAsync(MobileTemplateFilter? filter = null)
    {
        var query = db.Templates.AsQueryable();

        if (filter?.ShowArchived != true)
            query = query.Where(t => !t.IsArchived);

        if (filter?.Type.HasValue == true)
            query = query.Where(t => t.Type == filter.Type.Value);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(s)
                || (t.Description != null && t.Description.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new MobileTemplateCard
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Type = t.Type,
                IsCompanyDefault = t.IsCompanyDefault,
                UsageCount = t.UsageCount,
                LastUsed = t.LastUsed,
                IsArchived = t.IsArchived,
                CreatedAt = t.CreatedAt,
            }).ToListAsync();
    }

    public async Task<MobileTemplateDetail?> GetTemplateDetailAsync(int id)
    {
        var t = await db.Templates
            .Include(tmpl => tmpl.Customer)
            .Include(tmpl => tmpl.Company)
            .Include(tmpl => tmpl.Versions)
            .FirstOrDefaultAsync(tmpl => tmpl.Id == id);

        if (t is null) return null;

        return new MobileTemplateDetail
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
            CustomerName = t.Customer?.Name,
            CompanyId = t.CompanyId,
            CompanyName = t.Company?.Name,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            Versions = t.Versions.OrderByDescending(v => v.VersionNumber).Select(v => new MobileTemplateVersionItem
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                ChangeNotes = v.ChangeNotes,
                CreatedAt = v.CreatedAt,
            }).ToList(),
        };
    }

    public async Task<int> CreateTemplateAsync(MobileTemplateCreate model)
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
            UpdatedAt = DateTime.UtcNow,
        };
        db.Templates.Add(template);
        await db.SaveChangesAsync();

        // Create initial version
        db.Set<TemplateVersion>().Add(new TemplateVersion
        {
            TemplateId = template.Id,
            VersionNumber = 1,
            Data = model.Data,
            ChangeNotes = "Initial version",
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return template.Id;
    }

    public async Task<bool> UpdateTemplateAsync(int id, MobileTemplateUpdate model)
    {
        var template = await db.Templates.Include(t => t.Versions).FirstOrDefaultAsync(t => t.Id == id);
        if (template is null) return false;

        var dataChanged = template.Data != model.Data;

        template.Name = model.Name;
        template.Description = model.Description;
        template.Type = model.Type;
        template.Data = model.Data;
        template.IsCompanyDefault = model.IsCompanyDefault;
        template.Notes = model.Notes;
        template.UpdatedAt = DateTime.UtcNow;

        if (dataChanged)
        {
            var nextVersion = (template.Versions.MaxBy(v => v.VersionNumber)?.VersionNumber ?? 0) + 1;
            db.Set<TemplateVersion>().Add(new TemplateVersion
            {
                TemplateId = id,
                VersionNumber = nextVersion,
                Data = model.Data,
                ChangeNotes = $"Updated from mobile",
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveTemplateAsync(int id)
    {
        var template = await db.Templates.FindAsync(id);
        if (template is null) return false;
        template.IsArchived = true;
        template.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreTemplateAsync(int id)
    {
        var template = await db.Templates.FindAsync(id);
        if (template is null) return false;
        template.IsArchived = false;
        template.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTemplateAsync(int id)
    {
        var template = await db.Templates.Include(t => t.Versions).FirstOrDefaultAsync(t => t.Id == id);
        if (template is null) return false;
        db.Set<TemplateVersion>().RemoveRange(template.Versions);
        db.Templates.Remove(template);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CloneTemplateAsync(int id, string newName)
    {
        var source = await db.Templates.FirstOrDefaultAsync(t => t.Id == id);
        if (source is null) return false;

        var clone = new Template
        {
            Name = newName,
            Description = source.Description,
            Type = source.Type,
            Data = source.Data,
            Notes = source.Notes,
            CustomerId = source.CustomerId,
            CompanyId = source.CompanyId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Templates.Add(clone);
        await db.SaveChangesAsync();

        db.Set<TemplateVersion>().Add(new TemplateVersion
        {
            TemplateId = clone.Id,
            VersionNumber = 1,
            Data = source.Data,
            ChangeNotes = $"Cloned from \"{source.Name}\"",
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<MobileTemplateStats> GetStatsAsync()
    {
        var templates = await db.Templates.ToListAsync();
        return new MobileTemplateStats
        {
            TotalTemplates = templates.Count(t => !t.IsArchived),
            DefaultCount = templates.Count(t => t.IsCompanyDefault && !t.IsArchived),
            ArchivedCount = templates.Count(t => t.IsArchived),
            TotalUsages = templates.Sum(t => t.UsageCount),
            CountByType = templates
                .Where(t => !t.IsArchived)
                .GroupBy(t => t.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
        };
    }
}
