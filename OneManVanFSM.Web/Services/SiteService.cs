using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class SiteService : ISiteService
{
    private readonly AppDbContext _db;

    public SiteService(AppDbContext db) => _db = db;

    public async Task<List<SiteListItem>> GetSitesAsync(SiteFilter? filter = null)
    {
        var query = _db.Sites.Where(s => !s.IsArchived).AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    (s.Address != null && s.Address.ToLower().Contains(term)) ||
                    (s.City != null && s.City.ToLower().Contains(term)));
            }
            if (filter.PropertyType.HasValue)
                query = query.Where(s => s.PropertyType == filter.PropertyType.Value);
            if (filter.CustomerId.HasValue)
                query = query.Where(s => s.CustomerId == filter.CustomerId.Value);

            query = filter.SortBy?.ToLower() switch
            {
                "address" => filter.SortDescending ? query.OrderByDescending(s => s.Address) : query.OrderBy(s => s.Address),
                "type" => filter.SortDescending ? query.OrderByDescending(s => s.PropertyType) : query.OrderBy(s => s.PropertyType),
                _ => filter.SortDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name)
            };
        }
        else
        {
            query = query.OrderBy(s => s.Name);
        }

        return await query.Select(s => new SiteListItem
        {
            Id = s.Id,
            Name = s.Name,
            Address = s.Address,
            City = s.City,
            State = s.State,
            PropertyType = s.PropertyType,
            SqFt = s.SqFt,
            Zones = s.Zones,
            OwnerName = s.Customer != null ? s.Customer.Name : (s.Company != null ? s.Company.Name : null),
            AssetCount = s.Assets.Count(a => !a.IsArchived),
            OpenJobCount = s.Jobs.Count(j => !j.IsArchived && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
        }).ToListAsync();
    }

    public async Task<SiteDetail?> GetSiteAsync(int id)
    {
        var site = await _db.Sites
            .Include(s => s.Customer).Include(s => s.Company)
            .Include(s => s.Assets)
            .Include(s => s.Jobs).ThenInclude(j => j.AssignedEmployee)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (site is null) return null;

        return new SiteDetail
        {
            Id = site.Id,
            Name = site.Name,
            Address = site.Address, City = site.City, State = site.State, Zip = site.Zip,
            Latitude = site.Latitude, Longitude = site.Longitude,
            PropertyType = site.PropertyType, SqFt = site.SqFt, Zones = site.Zones, Stories = site.Stories,
            AccessCodes = site.AccessCodes, Instructions = site.Instructions,
            Parking = site.Parking, EquipmentLocation = site.EquipmentLocation, Notes = site.Notes,
            GasLineLocation = site.GasLineLocation, ElectricalPanelLocation = site.ElectricalPanelLocation,
            WaterShutoffLocation = site.WaterShutoffLocation, HeatingFuelSource = site.HeatingFuelSource,
            YearBuilt = site.YearBuilt, HasAtticAccess = site.HasAtticAccess,
            HasCrawlSpace = site.HasCrawlSpace, HasBasement = site.HasBasement,
            CustomerId = site.CustomerId, CustomerName = site.Customer?.Name,
            CompanyId = site.CompanyId, CompanyName = site.Company?.Name,
            CreatedAt = site.CreatedAt, UpdatedAt = site.UpdatedAt,

            Assets = site.Assets.Where(a => !a.IsArchived).Select(a => new AssetSummary
            {
                Id = a.Id, Name = a.Name, AssetType = a.AssetType, SiteName = site.Name,
                InstallDate = a.InstallDate, WarrantyExpiry = a.WarrantyExpiry, Status = a.Status
            }).ToList(),

            UpcomingJobs = site.Jobs
                .Where(j => !j.IsArchived && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
                .OrderBy(j => j.ScheduledDate).Take(10)
                .Select(j => new JobSummary
                {
                    Id = j.Id, JobNumber = j.JobNumber, Title = j.Title, SiteName = site.Name,
                    Status = j.Status, ScheduledDate = j.ScheduledDate, TechnicianName = j.AssignedEmployee?.Name
                }).ToList(),

            RecentJobs = site.Jobs
                .Where(j => !j.IsArchived && (j.Status == JobStatus.Completed || j.Status == JobStatus.Cancelled))
                .OrderByDescending(j => j.CompletedDate ?? j.ScheduledDate).Take(10)
                .Select(j => new JobSummary
                {
                    Id = j.Id, JobNumber = j.JobNumber, Title = j.Title, SiteName = site.Name,
                    Status = j.Status, ScheduledDate = j.ScheduledDate, TechnicianName = j.AssignedEmployee?.Name
                }).ToList()
        };
    }

    public async Task<Site> CreateSiteAsync(SiteEditModel model)
    {
        var site = new Site
        {
            Name = model.Name, Address = model.Address, City = model.City,
            State = model.State, Zip = model.Zip,
            Latitude = model.Latitude, Longitude = model.Longitude,
            PropertyType = model.PropertyType, SqFt = model.SqFt, Zones = model.Zones, Stories = model.Stories,
            AccessCodes = model.AccessCodes, Instructions = model.Instructions,
            Parking = model.Parking, EquipmentLocation = model.EquipmentLocation,
            GasLineLocation = model.GasLineLocation, ElectricalPanelLocation = model.ElectricalPanelLocation,
            WaterShutoffLocation = model.WaterShutoffLocation, HeatingFuelSource = model.HeatingFuelSource,
            YearBuilt = model.YearBuilt, HasAtticAccess = model.HasAtticAccess,
            HasCrawlSpace = model.HasCrawlSpace, HasBasement = model.HasBasement,
            Notes = model.Notes, CustomerId = model.CustomerId, CompanyId = model.CompanyId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Sites.Add(site);
        await _db.SaveChangesAsync();
        return site;
    }

    public async Task<Site> UpdateSiteAsync(int id, SiteEditModel model)
    {
        var site = await _db.Sites.FindAsync(id) ?? throw new InvalidOperationException("Site not found.");
        site.Name = model.Name; site.Address = model.Address; site.City = model.City;
        site.State = model.State; site.Zip = model.Zip;
        site.Latitude = model.Latitude; site.Longitude = model.Longitude;
        site.PropertyType = model.PropertyType; site.SqFt = model.SqFt;
        site.Zones = model.Zones; site.Stories = model.Stories;
        site.AccessCodes = model.AccessCodes; site.Instructions = model.Instructions;
        site.Parking = model.Parking; site.EquipmentLocation = model.EquipmentLocation;
        site.GasLineLocation = model.GasLineLocation; site.ElectricalPanelLocation = model.ElectricalPanelLocation;
        site.WaterShutoffLocation = model.WaterShutoffLocation; site.HeatingFuelSource = model.HeatingFuelSource;
        site.YearBuilt = model.YearBuilt; site.HasAtticAccess = model.HasAtticAccess;
        site.HasCrawlSpace = model.HasCrawlSpace; site.HasBasement = model.HasBasement;
        site.Notes = model.Notes; site.CustomerId = model.CustomerId; site.CompanyId = model.CompanyId;
        site.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return site;
    }

    public async Task<bool> ArchiveSiteAsync(int id)
    {
        var site = await _db.Sites.FindAsync(id);
        if (site is null) return false;
        site.IsArchived = true; site.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<CompanyOption>> GetCompaniesForDropdownAsync()
    {
        return await _db.Companies.Where(c => !c.IsArchived)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyOption { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }
}
