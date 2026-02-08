using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileSiteService(AppDbContext db) : IMobileSiteService
{
    public async Task<List<MobileSiteCard>> GetSitesAsync(MobileSiteFilter? filter = null)
    {
        var query = db.Sites.Where(s => !s.IsArchived).AsQueryable();

        if (filter?.PropertyType.HasValue == true)
            query = query.Where(s => s.PropertyType == filter.PropertyType.Value);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(site => site.Name.ToLower().Contains(s)
                || (site.Address != null && site.Address.ToLower().Contains(s))
                || (site.City != null && site.City.ToLower().Contains(s)));
        }

        var sites = await query
            .Include(s => s.Customer)
            .Include(s => s.Company)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var siteIds = sites.Select(s => s.Id).ToHashSet();

        var assetCounts = await db.Assets
            .Where(a => a.SiteId.HasValue && siteIds.Contains(a.SiteId.Value) && !a.IsArchived)
            .GroupBy(a => a.SiteId!.Value)
            .Select(g => new { SiteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SiteId, x => x.Count);

        var jobData = await db.Jobs
            .Where(j => j.SiteId.HasValue && siteIds.Contains(j.SiteId.Value))
            .GroupBy(j => j.SiteId!.Value)
            .Select(g => new { SiteId = g.Key, Count = g.Count(), LastDate = g.Max(j => j.ScheduledDate) })
            .ToDictionaryAsync(x => x.SiteId);

        return sites.Select(s => new MobileSiteCard
        {
            Id = s.Id,
            Name = s.Name,
            Address = s.Address,
            City = s.City,
            State = s.State,
            PropertyType = s.PropertyType,
            IsNewConstruction = s.IsNewConstruction,
            CustomerName = s.Customer?.Name,
            CustomerId = s.CustomerId,
            CompanyId = s.CompanyId,
            CompanyName = s.Company?.Name,
            AssetCount = assetCounts.GetValueOrDefault(s.Id),
            JobCount = jobData.TryGetValue(s.Id, out var jd) ? jd.Count : 0,
            LastJobDate = jobData.TryGetValue(s.Id, out var jd2) ? jd2.LastDate : null,
        }).ToList();
    }

    public async Task<MobileSiteStats> GetStatsAsync()
    {
        var sites = await db.Sites.Where(s => !s.IsArchived).ToListAsync();
        var totalAssets = await db.Assets.CountAsync(a => a.SiteId.HasValue && !a.IsArchived);

        return new MobileSiteStats
        {
            TotalSites = sites.Count,
            ResidentialCount = sites.Count(s => s.PropertyType == PropertyType.Residential),
            CommercialCount = sites.Count(s => s.PropertyType == PropertyType.Commercial),
            TotalAssets = totalAssets,
        };
    }
}
