using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

/// <summary>
/// Remote-mode job service. Reads from the local SQLite cache (populated by SyncService)
/// and pushes mutations (status updates, new jobs) to the REST API, then syncs the
/// result back into the local cache.
/// </summary>
public class RemoteMobileJobService : IMobileJobService
{
    private readonly AppDbContext _db;
    private readonly ApiClient _api;
    private readonly IOfflineQueueService _offlineQueue;
    private readonly ILogger<RemoteMobileJobService> _logger;

    public RemoteMobileJobService(AppDbContext db, ApiClient api, IOfflineQueueService offlineQueue, ILogger<RemoteMobileJobService> logger)
    {
        _db = db;
        _api = api;
        _offlineQueue = offlineQueue;
        _logger = logger;
    }

    public async Task<List<MobileJobCard>> GetAssignedJobsAsync(int employeeId, MobileJobFilter? filter = null, bool isElevated = false)
    {
        var query = _db.Jobs.AsNoTracking()
            .Include(j => j.Customer).Include(j => j.Company).Include(j => j.Site)
            .Where(j => !j.IsArchived && (isElevated || j.AssignedEmployeeId == employeeId));

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(j => (j.Title ?? "").Contains(filter.Search) || j.JobNumber.Contains(filter.Search));
            if (filter.Status.HasValue)
                query = query.Where(j => j.Status == filter.Status.Value);
            if (filter.Priority.HasValue)
                query = query.Where(j => j.Priority == filter.Priority.Value);
            if (filter.DateFrom.HasValue)
                query = query.Where(j => j.ScheduledDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(j => j.ScheduledDate <= filter.DateTo.Value);
        }

        return await query.OrderBy(j => j.ScheduledDate).ThenBy(j => j.ScheduledTime)
            .Select(j => new MobileJobCard
            {
                Id = j.Id, JobNumber = j.JobNumber, Title = j.Title,
                CustomerName = j.Customer != null ? j.Customer.Name : null,
                CompanyName = j.Company != null ? j.Company.Name : null,
                SiteAddress = j.Site != null ? (j.Site.Address + ", " + j.Site.City) : null,
                Status = j.Status, Priority = j.Priority,
                ScheduledDate = j.ScheduledDate, ScheduledTime = j.ScheduledTime,
                EstimatedDuration = j.EstimatedDuration
            }).ToListAsync();
    }

    public async Task<MobileJobDetail?> GetJobDetailAsync(int id)
    {
        var job = await _db.Jobs.AsNoTracking()
            .Include(j => j.Customer).Include(j => j.Company).Include(j => j.Site)
            .Include(j => j.AssignedEmployee)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return null;

        var assets = await _db.Assets.AsNoTracking()
            .Where(a => !a.IsArchived && a.SiteId == job.SiteId)
            .Select(a => new MobileAssetSummary
            {
                Id = a.Id, Name = a.Name, AssetType = a.AssetType,
                Model = a.Model, SerialNumber = a.SerialNumber, Status = a.Status,
                WarrantyExpiry = a.PartsWarrantyExpiry
            }).ToListAsync();

        var timeEntries = await _db.TimeEntries.AsNoTracking()
            .Where(t => t.JobId == id)
            .OrderByDescending(t => t.StartTime)
            .Select(t => new MobileTimeEntrySummary
            {
                Id = t.Id, StartTime = t.StartTime, EndTime = t.EndTime,
                Hours = t.Hours, Notes = t.Notes
            }).ToListAsync();

        var materials = await _db.MaterialListItems.AsNoTracking()
            .Where(m => m.MaterialList != null && m.MaterialList.JobId == id)
            .Select(m => new MobileMaterialItem
            {
                Id = m.Id, Section = m.Section, ItemName = m.ItemName,
                Quantity = m.Quantity, Unit = m.Unit, BaseCost = m.BaseCost, Notes = m.Notes
            }).ToListAsync();

        var documents = await _db.Documents.AsNoTracking()
            .Where(d => d.JobId == id)
            .Select(d => new MobileJobDocument
            {
                Id = d.Id, Name = d.Name, FileType = d.FileType,
                Category = d.Category, UploadDate = d.UploadDate
            }).ToListAsync();

        return new MobileJobDetail
        {
            Id = job.Id, JobNumber = job.JobNumber, Title = job.Title,
            Description = job.Description, Status = job.Status, Priority = job.Priority,
            SystemType = job.SystemType, ScheduledDate = job.ScheduledDate,
            ScheduledTime = job.ScheduledTime, EstimatedDuration = job.EstimatedDuration,
            EstimatedTotal = job.EstimatedTotal, Notes = job.Notes, CompletedDate = job.CompletedDate,
            CustomerId = job.CustomerId, CustomerName = job.Customer?.Name,
            CustomerPhone = job.Customer?.PrimaryPhone, CompanyId = job.CompanyId,
            CompanyName = job.Company?.Name, SiteName = job.Site?.Name,
            SiteAddress = job.Site != null ? $"{job.Site.Address}, {job.Site.City}" : null,
            SiteId = job.SiteId, AccessCodes = job.Site?.AccessCodes,
            EquipmentLocation = job.Site?.EquipmentLocation,
            TechnicianName = job.AssignedEmployee?.Name,
            Assets = assets, TimeEntries = timeEntries, Materials = materials, Documents = documents
        };
    }

    public async Task<bool> UpdateJobStatusAsync(int id, JobStatus status)
    {
        // Always update local cache immediately for responsive UI
        var job = await _db.Jobs.FindAsync(id);
        if (job is not null)
        {
            job.Status = status;
            if (status == JobStatus.Completed)
                job.CompletedDate = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        try
        {
            await _api.PutAsync<object>($"api/jobs/{id}/status", status);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Job {JobId} status update failed (offline), queueing.", id);
            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "PUT",
                Endpoint = $"api/jobs/{id}/status",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(status, new System.Text.Json.JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }),
                Description = $"Job #{id} status → {status}"
            });
            return true; // Local cache is updated, queued for server
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} status update failed.", id);
            return false;
        }
    }

    public async Task<int> CreateJobAsync(MobileJobCreate model)
    {
        var job = new Job
        {
            Title = model.Title,
            Description = model.Description,
            Priority = model.Priority,
            ScheduledDate = model.ScheduledDate,
            ScheduledTime = model.ScheduledTime,
            EstimatedDuration = model.EstimatedDuration,
            CustomerId = model.CustomerId,
            SiteId = model.SiteId,
            AssignedEmployeeId = model.AssignedEmployeeId,
            Status = JobStatus.Lead,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            var created = await _api.PostAsync<Job>("api/jobs", job);
            if (created is not null)
            {
                // Add to local cache
                _db.Jobs.Add(created);
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
                return created.Id;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Job create failed (offline), queueing: {Title}", model.Title);
            // Save locally with temp ID and queue for server
            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "POST",
                Endpoint = "api/jobs",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(job),
                Description = $"Create job: {job.Title ?? job.JobNumber}"
            });
            return job.Id; // Return local ID
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job create failed.");
        }

        return 0;
    }

    public async Task<MobileSiteDetail?> GetSiteDetailAsync(int siteId)
    {
        var site = await _db.Sites.AsNoTracking()
            .Include(s => s.Customer).Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.Id == siteId);

        if (site is null) return null;

        var assets = await _db.Assets.AsNoTracking()
            .Where(a => !a.IsArchived && a.SiteId == siteId)
            .Select(a => new MobileAssetSummary
            {
                Id = a.Id, Name = a.Name, AssetType = a.AssetType,
                Model = a.Model, SerialNumber = a.SerialNumber, Status = a.Status,
                WarrantyExpiry = a.PartsWarrantyExpiry
            }).ToListAsync();

        var recentJobs = await _db.Jobs.AsNoTracking()
            .Where(j => j.SiteId == siteId && !j.IsArchived)
            .OrderByDescending(j => j.ScheduledDate).Take(5)
            .Select(j => new MobileSiteJob
            {
                Id = j.Id, JobNumber = j.JobNumber, Title = j.Title,
                Status = j.Status, ScheduledDate = j.ScheduledDate
            }).ToListAsync();

        var agreements = await _db.ServiceAgreements.AsNoTracking()
            .Where(sa => sa.SiteId == siteId && !sa.IsArchived)
            .Select(sa => new MobileSiteAgreement
            {
                Id = sa.Id, AgreementNumber = sa.AgreementNumber, Title = sa.Title,
                CoverageLevel = sa.CoverageLevel, Status = sa.Status,
                EndDate = sa.EndDate, VisitsIncluded = sa.VisitsIncluded, VisitsUsed = sa.VisitsUsed
            }).ToListAsync();

        return new MobileSiteDetail
        {
            Id = site.Id, Name = site.Name, Address = site.Address, City = site.City,
            State = site.State, Zip = site.Zip, PropertyType = site.PropertyType,
            SqFt = site.SqFt, Zones = site.Zones, Stories = site.Stories,
            AccessCodes = site.AccessCodes, Instructions = site.Instructions,
            Parking = site.Parking, EquipmentLocation = site.EquipmentLocation,
            GasLineLocation = site.GasLineLocation, ElectricalPanelLocation = site.ElectricalPanelLocation,
            WaterShutoffLocation = site.WaterShutoffLocation, HeatingFuelSource = site.HeatingFuelSource,
            YearBuilt = site.YearBuilt, HasAtticAccess = site.HasAtticAccess,
            HasCrawlSpace = site.HasCrawlSpace, HasBasement = site.HasBasement,
            Notes = site.Notes, CustomerId = site.CustomerId, CustomerName = site.Customer?.Name,
            CompanyId = site.CompanyId, CompanyName = site.Company?.Name,
            IsNewConstruction = site.IsNewConstruction,
            Assets = assets, RecentJobs = recentJobs, Agreements = agreements
        };
    }

    public async Task<MobileMaterialItem> AddMaterialItemAsync(MobileMaterialCreate model)
    {
        // Find or create a material list for the job
        var list = await _db.MaterialLists
            .Include(m => m.Items)
            .FirstOrDefaultAsync(m => m.JobId == model.JobId && !m.IsArchived);

        if (list is null)
        {
            list = new MaterialList
            {
                Name = "Field Materials",
                JobId = model.JobId,
                TradeType = "HVAC",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.MaterialLists.Add(list);
            await _db.SaveChangesAsync();
        }

        var item = new MaterialListItem
        {
            MaterialListId = list.Id,
            Section = model.Section,
            ItemName = model.ItemName,
            Quantity = model.Quantity,
            Unit = model.Unit,
            BaseCost = model.BaseCost,
            Notes = model.Notes,
            SortOrder = (list.Items?.Count ?? 0) + 1
        };

        _db.MaterialListItems.Add(item);
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new MobileMaterialItem
        {
            Id = item.Id, Section = item.Section, ItemName = item.ItemName,
            Quantity = item.Quantity, Unit = item.Unit, BaseCost = item.BaseCost, Notes = item.Notes
        };
    }

    public async Task<List<MobileJobCard>> GetAllJobCardsAsync()
    {
        return await _db.Jobs.AsNoTracking()
            .Include(j => j.Customer).Include(j => j.Company).Include(j => j.Site)
            .Where(j => !j.IsArchived)
            .OrderByDescending(j => j.ScheduledDate)
            .Select(j => new MobileJobCard
            {
                Id = j.Id, JobNumber = j.JobNumber, Title = j.Title,
                CustomerName = j.Customer != null ? j.Customer.Name : null,
                CompanyName = j.Company != null ? j.Company.Name : null,
                SiteAddress = j.Site != null ? (j.Site.Address + ", " + j.Site.City) : null,
                Status = j.Status, Priority = j.Priority,
                ScheduledDate = j.ScheduledDate, ScheduledTime = j.ScheduledTime,
                EstimatedDuration = j.EstimatedDuration
            }).ToListAsync();
    }

    public async Task<List<MobileCustomerOption>> GetCustomerOptionsAsync()
    {
        return await _db.Customers.AsNoTracking()
            .Where(c => !c.IsArchived)
            .OrderBy(c => c.Name)
            .Select(c => new MobileCustomerOption { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }

    public async Task<List<MobileSiteOption>> GetSiteOptionsAsync(int? customerId)
    {
        var query = _db.Sites.AsNoTracking().Where(s => !s.IsArchived);
        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        return await query.OrderBy(s => s.Name)
            .Select(s => new MobileSiteOption { Id = s.Id, Name = s.Name, Address = s.Address })
            .ToListAsync();
    }
}
