using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileJobService(AppDbContext db) : IMobileJobService
{
    public async Task<List<MobileJobCard>> GetAssignedJobsAsync(int employeeId, MobileJobFilter? filter = null)
    {
        var query = db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Where(j => !j.IsArchived && j.AssignedEmployeeId == employeeId)
            .AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(j =>
                    j.JobNumber.ToLower().Contains(term) ||
                    (j.Title != null && j.Title.ToLower().Contains(term)) ||
                    (j.Customer != null && j.Customer.Name.ToLower().Contains(term)));
            }
            if (filter.Status.HasValue)
                query = query.Where(j => j.Status == filter.Status.Value);
            if (filter.Priority.HasValue)
                query = query.Where(j => j.Priority == filter.Priority.Value);
            if (filter.DateFrom.HasValue)
                query = query.Where(j => j.ScheduledDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(j => j.ScheduledDate <= filter.DateTo.Value);
        }

        return await query
            .OrderBy(j => j.ScheduledDate)
            .ThenBy(j => j.ScheduledTime)
            .Select(j => new MobileJobCard
            {
                Id = j.Id,
                JobNumber = j.JobNumber,
                Title = j.Title,
                CustomerName = j.Customer != null ? j.Customer.Name : null,
                SiteAddress = j.Site != null ? (j.Site.Address + ", " + j.Site.City) : null,
                Status = j.Status,
                Priority = j.Priority,
                ScheduledDate = j.ScheduledDate,
                ScheduledTime = j.ScheduledTime,
                EstimatedDuration = j.EstimatedDuration,
            })
            .ToListAsync();
    }

    public async Task<MobileJobDetail?> GetJobDetailAsync(int id)
    {
        var job = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Include(j => j.AssignedEmployee)
            .Include(j => j.TimeEntries)
            .Include(j => j.MaterialList)
                .ThenInclude(m => m!.Items)
            .Include(j => j.Documents)
            .AsSplitQuery()
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return null;

        var assets = job.SiteId.HasValue
            ? await db.Assets
                .Where(a => a.SiteId == job.SiteId && !a.IsArchived)
                .ToListAsync()
            : [];

        return new MobileJobDetail
        {
            Id = job.Id,
            JobNumber = job.JobNumber,
            Title = job.Title,
            Description = job.Description,
            Status = job.Status,
            Priority = job.Priority,
            SystemType = job.SystemType,
            ScheduledDate = job.ScheduledDate,
            ScheduledTime = job.ScheduledTime,
            EstimatedDuration = job.EstimatedDuration,
            EstimatedTotal = job.EstimatedTotal,
            Notes = job.Notes,
            CompletedDate = job.CompletedDate,
            CustomerName = job.Customer?.Name,
            CustomerId = job.CustomerId,
            CustomerPhone = job.Customer?.PrimaryPhone,
            SiteName = job.Site?.Name,
            SiteAddress = job.Site != null ? $"{job.Site.Address}, {job.Site.City}, {job.Site.State} {job.Site.Zip}" : null,
            SiteId = job.SiteId,
            AccessCodes = job.Site?.AccessCodes,
            EquipmentLocation = job.Site?.EquipmentLocation,
            TechnicianName = job.AssignedEmployee?.Name,
            Assets = assets.Select(a => new MobileAssetSummary
            {
                Id = a.Id,
                Name = a.Name,
                AssetType = a.AssetType,
                Model = a.Model,
                SerialNumber = a.SerialNumber,
                Status = a.Status,
                WarrantyExpiry = a.WarrantyExpiry,
            }).ToList(),
            TimeEntries = job.TimeEntries.Select(t => new MobileTimeEntrySummary
            {
                Id = t.Id,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Hours = t.Hours,
                Notes = t.Notes,
            }).OrderByDescending(t => t.StartTime).ToList(),
            Materials = job.MaterialList?.Items.Select(m => new MobileMaterialItem
            {
                Id = m.Id,
                Section = m.Section,
                ItemName = m.ItemName,
                Quantity = m.Quantity,
                Unit = m.Unit,
                BaseCost = m.BaseCost,
                Notes = m.Notes,
            }).ToList() ?? [],
            Documents = job.Documents.Select(d => new MobileJobDocument
            {
                Id = d.Id,
                Name = d.Name,
                FileType = d.FileType,
                Category = d.Category,
                UploadDate = d.UploadDate,
            }).OrderByDescending(d => d.UploadDate).ToList(),
        };
    }

    public async Task<bool> UpdateJobStatusAsync(int id, JobStatus status)
    {
        var job = await db.Jobs.FindAsync(id);
        if (job is null) return false;

        job.Status = status;
        job.UpdatedAt = DateTime.UtcNow;
        if (status == JobStatus.Completed)
            job.CompletedDate = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<MobileSiteDetail?> GetSiteDetailAsync(int siteId)
    {
        var site = await db.Sites
            .Include(s => s.Customer)
            .Include(s => s.Assets)
            .FirstOrDefaultAsync(s => s.Id == siteId && !s.IsArchived);

        if (site is null) return null;

        var agreements = await db.ServiceAgreements
            .Where(sa => sa.SiteId == siteId && !sa.IsArchived)
            .OrderByDescending(sa => sa.EndDate)
            .Select(sa => new MobileSiteAgreement
            {
                Id = sa.Id,
                AgreementNumber = sa.AgreementNumber,
                Title = sa.Title,
                CoverageLevel = sa.CoverageLevel,
                Status = sa.Status,
                EndDate = sa.EndDate,
                VisitsIncluded = sa.VisitsIncluded,
                VisitsUsed = sa.VisitsUsed,
            })
            .ToListAsync();

        var recentJobs = await db.Jobs
            .Where(j => j.SiteId == siteId && !j.IsArchived)
            .OrderByDescending(j => j.ScheduledDate)
            .Take(5)
            .Select(j => new MobileSiteJob
            {
                Id = j.Id,
                JobNumber = j.JobNumber,
                Title = j.Title,
                Status = j.Status,
                ScheduledDate = j.ScheduledDate,
            })
            .ToListAsync();

        return new MobileSiteDetail
        {
            Id = site.Id,
            Name = site.Name,
            Address = site.Address,
            City = site.City,
            State = site.State,
            Zip = site.Zip,
            PropertyType = site.PropertyType,
            SqFt = site.SqFt,
            Zones = site.Zones,
            Stories = site.Stories,
            AccessCodes = site.AccessCodes,
            Instructions = site.Instructions,
            Parking = site.Parking,
            EquipmentLocation = site.EquipmentLocation,
            GasLineLocation = site.GasLineLocation,
            ElectricalPanelLocation = site.ElectricalPanelLocation,
            WaterShutoffLocation = site.WaterShutoffLocation,
            HeatingFuelSource = site.HeatingFuelSource,
            YearBuilt = site.YearBuilt,
            HasAtticAccess = site.HasAtticAccess,
            HasCrawlSpace = site.HasCrawlSpace,
            HasBasement = site.HasBasement,
            Notes = site.Notes,
            CustomerId = site.CustomerId,
            CustomerName = site.Customer?.Name,
            Agreements = agreements,
            Assets = site.Assets.Where(a => !a.IsArchived).Select(a => new MobileAssetSummary
            {
                Id = a.Id,
                Name = a.Name,
                AssetType = a.AssetType,
                Model = a.Model,
                SerialNumber = a.SerialNumber,
                Status = a.Status,
                WarrantyExpiry = a.WarrantyExpiry,
            }).ToList(),
            RecentJobs = recentJobs,
        };
    }

    public async Task<MobileMaterialItem> AddMaterialItemAsync(MobileMaterialCreate model)
    {
        var job = await db.Jobs.FindAsync(model.JobId);
        if (job is null) throw new InvalidOperationException("Job not found");

        // Auto-create MaterialList if the job doesn't have one
        if (!job.MaterialListId.HasValue)
        {
            var matList = new MaterialList
            {
                Name = $"Job {job.JobNumber} Materials",
                CustomerId = job.CustomerId,
                SiteId = job.SiteId,
            };
            db.MaterialLists.Add(matList);
            await db.SaveChangesAsync();
            job.MaterialListId = matList.Id;
        }

        var item = new MaterialListItem
        {
            MaterialListId = job.MaterialListId!.Value,
            Section = model.Section,
            ItemName = model.ItemName,
            Quantity = model.Quantity,
            Unit = model.Unit,
            BaseCost = model.BaseCost,
            Notes = model.Notes,
        };
        db.MaterialListItems.Add(item);

        // Recalculate material list subtotal/total
        var matListEntity = await db.MaterialLists
            .Include(m => m.Items)
            .FirstAsync(m => m.Id == job.MaterialListId.Value);
        matListEntity.Subtotal = matListEntity.Items.Sum(i => i.Quantity * i.BaseCost) + (model.Quantity * model.BaseCost);
        matListEntity.Total = matListEntity.Subtotal
            * (1 + matListEntity.MarkupPercent / 100)
            * (1 + matListEntity.TaxPercent / 100);
        matListEntity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new MobileMaterialItem
        {
            Id = item.Id,
            Section = item.Section,
            ItemName = item.ItemName,
            Quantity = item.Quantity,
            Unit = item.Unit,
            BaseCost = item.BaseCost,
            Notes = item.Notes,
        };
    }
}
