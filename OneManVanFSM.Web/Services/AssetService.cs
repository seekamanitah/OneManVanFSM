using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class AssetService : IAssetService
{
    private readonly AppDbContext _db;
    public AssetService(AppDbContext db) => _db = db;

    public async Task<List<AssetListItem>> GetAssetsAsync(AssetFilter? filter = null)
    {
        var query = _db.Assets.Where(a => !a.IsArchived).AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(term) ||
                    (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(term)) ||
                    (a.Model != null && a.Model.ToLower().Contains(term)));
            }
            if (filter.Status.HasValue) query = query.Where(a => a.Status == filter.Status.Value);
            if (!string.IsNullOrWhiteSpace(filter.AssetType)) query = query.Where(a => a.AssetType == filter.AssetType);
            if (filter.CustomerId.HasValue) query = query.Where(a => a.CustomerId == filter.CustomerId);
            if (filter.SiteId.HasValue) query = query.Where(a => a.SiteId == filter.SiteId);

            query = filter.SortBy?.ToLower() switch
            {
                "type" => filter.SortDescending ? query.OrderByDescending(a => a.AssetType) : query.OrderBy(a => a.AssetType),
                "status" => filter.SortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
                "installdate" => filter.SortDescending ? query.OrderByDescending(a => a.InstallDate) : query.OrderBy(a => a.InstallDate),
                _ => filter.SortDescending ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name)
            };
        }
        else query = query.OrderBy(a => a.Name);

        return await query.Select(a => new AssetListItem
        {
            Id = a.Id, Name = a.Name, AssetType = a.AssetType, Model = a.Model,
            SerialNumber = a.SerialNumber, Status = a.Status,
            CustomerName = a.Customer != null ? a.Customer.Name : null,
            SiteName = a.Site != null ? a.Site.Name : null,
            InstallDate = a.InstallDate, WarrantyExpiry = a.WarrantyExpiry
        }).ToListAsync();
    }

    public async Task<AssetDetail?> GetAssetAsync(int id)
    {
        var detail = await _db.Assets.Where(a => a.Id == id && !a.IsArchived)
            .Select(a => new AssetDetail
            {
                Id = a.Id, Name = a.Name, Model = a.Model, SerialNumber = a.SerialNumber,
                AssetType = a.AssetType, Brand = a.Brand, FuelType = a.FuelType,
                UnitConfiguration = a.UnitConfiguration, BTURating = a.BTURating,
                FilterSize = a.FilterSize, Tonnage = a.Tonnage, SEER = a.SEER,
                SEER2 = a.SEER2, AFUE = a.AFUE, HSPF = a.HSPF, HSPF2 = a.HSPF2,
                EER = a.EER, AssetTag = a.AssetTag, Nickname = a.Nickname,
                Voltage = a.Voltage, Phase = a.Phase,
                LocationOnSite = a.LocationOnSite, ManufactureDate = a.ManufactureDate,
                AmpRating = a.AmpRating, PanelType = a.PanelType,
                PipeMaterial = a.PipeMaterial, GallonCapacity = a.GallonCapacity,
                RefrigerantType = a.RefrigerantType, RefrigerantQuantity = a.RefrigerantQuantity,
                FilterType = a.FilterType, FilterChangeIntervalMonths = a.FilterChangeIntervalMonths,
                FilterLastChanged = a.FilterLastChanged, FilterNextDue = a.FilterNextDue,
                ThermostatBrand = a.ThermostatBrand, ThermostatModel = a.ThermostatModel,
                ThermostatType = a.ThermostatType, ThermostatWiFiEnabled = a.ThermostatWiFiEnabled,
                InstallDate = a.InstallDate, LastServiceDate = a.LastServiceDate,
                NextServiceDue = a.NextServiceDue, WarrantyStartDate = a.WarrantyStartDate,
                WarrantyTermYears = a.WarrantyTermYears, WarrantyExpiry = a.WarrantyExpiry,
                LaborWarrantyExpiry = a.LaborWarrantyExpiry,
                PartsWarrantyExpiry = a.PartsWarrantyExpiry,
                CompressorWarrantyExpiry = a.CompressorWarrantyExpiry,
                LaborWarrantyTermYears = a.LaborWarrantyTermYears,
                PartsWarrantyTermYears = a.PartsWarrantyTermYears,
                CompressorWarrantyTermYears = a.CompressorWarrantyTermYears,
                RegisteredOnline = a.RegisteredOnline, InstalledBy = a.InstalledBy,
                WarrantedByCompany = a.WarrantedByCompany,
                Status = a.Status, Value = a.Value, Notes = a.Notes,
                ProductId = a.ProductId, ProductName = a.Product != null ? a.Product.Name : null,
                CustomerId = a.CustomerId, CustomerName = a.Customer != null ? a.Customer.Name : null,
                SiteId = a.SiteId, SiteName = a.Site != null ? a.Site.Name : null,
                SiteAddress = a.Site != null ? (a.Site.Address + ", " + a.Site.City + ", " + a.Site.State) : null,
                CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt
            }).FirstOrDefaultAsync();

        if (detail is null) return null;

        detail.LinkedJobs = await _db.JobAssets
            .Where(ja => ja.AssetId == id)
            .Select(ja => new AssetLinkedJob
            {
                Id = ja.Job.Id,
                JobNumber = ja.Job.JobNumber,
                Title = ja.Job.Title,
                Status = ja.Job.Status,
                ScheduledDate = ja.Job.ScheduledDate,
                Role = ja.Role,
            })
            .OrderByDescending(j => j.ScheduledDate)
            .Take(20)
            .ToListAsync();

        detail.ServiceHistory = await _db.AssetServiceLogs
            .Where(sl => sl.AssetId == id)
            .OrderByDescending(sl => sl.ServiceDate)
            .Take(20)
            .Select(sl => new AssetServiceLogItem
            {
                Id = sl.Id,
                ServiceType = sl.ServiceType,
                ServiceDate = sl.ServiceDate,
                PerformedBy = sl.PerformedBy,
                Notes = sl.Notes,
                NextDueDate = sl.NextDueDate,
                Cost = sl.Cost,
                RefrigerantType = sl.RefrigerantType,
                RefrigerantAmountAdded = sl.RefrigerantAmountAdded,
                RefrigerantBeforeReading = sl.RefrigerantBeforeReading,
                RefrigerantAfterReading = sl.RefrigerantAfterReading,
            })
            .ToListAsync();

        detail.UnifiedTimeline = await GetUnifiedTimelineAsync(id);
        detail.LinkedAssets = await GetLinkedAssetsAsync(id);

        return detail;
    }

    public async Task<Asset> CreateAssetAsync(AssetEditModel model)
    {
        var asset = new Asset
        {
            Name = model.Name, Model = model.Model, SerialNumber = model.SerialNumber,
            AssetType = model.AssetType, Brand = model.Brand, FuelType = model.FuelType,
            UnitConfiguration = model.UnitConfiguration, BTURating = model.BTURating,
            FilterSize = model.FilterSize, Tonnage = model.Tonnage, SEER = model.SEER,
            SEER2 = model.SEER2, AFUE = model.AFUE, HSPF = model.HSPF, HSPF2 = model.HSPF2,
            EER = model.EER, AssetTag = model.AssetTag, Nickname = model.Nickname,
            Voltage = model.Voltage, Phase = model.Phase,
            LocationOnSite = model.LocationOnSite, ManufactureDate = model.ManufactureDate,
            AmpRating = model.AmpRating, PanelType = model.PanelType,
            PipeMaterial = model.PipeMaterial, GallonCapacity = model.GallonCapacity,
            RefrigerantType = model.RefrigerantType, RefrigerantQuantity = model.RefrigerantQuantity,
            FilterType = model.FilterType, FilterChangeIntervalMonths = model.FilterChangeIntervalMonths,
            FilterLastChanged = model.FilterLastChanged, FilterNextDue = model.FilterNextDue,
            ThermostatBrand = model.ThermostatBrand, ThermostatModel = model.ThermostatModel,
            ThermostatType = model.ThermostatType, ThermostatWiFiEnabled = model.ThermostatWiFiEnabled,
            InstallDate = model.InstallDate, LastServiceDate = model.LastServiceDate,
            NextServiceDue = model.NextServiceDue, WarrantyStartDate = model.WarrantyStartDate,
            WarrantyTermYears = model.WarrantyTermYears, WarrantyExpiry = model.WarrantyExpiry,
            LaborWarrantyTermYears = model.LaborWarrantyTermYears,
            PartsWarrantyTermYears = model.PartsWarrantyTermYears,
            CompressorWarrantyTermYears = model.CompressorWarrantyTermYears,
            RegisteredOnline = model.RegisteredOnline, InstalledBy = model.InstalledBy,
            WarrantedByCompany = model.WarrantedByCompany,
            Status = model.Status, Value = model.Value, Notes = model.Notes,
            ProductId = model.ProductId, CustomerId = model.CustomerId, SiteId = model.SiteId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        // Link jobs
        if (model.JobIds.Count > 0)
        {
            foreach (var jobId in model.JobIds)
                _db.Set<JobAsset>().Add(new JobAsset { JobId = jobId, AssetId = asset.Id, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }

        await CalculateWarrantyExpiriesAsync(asset);
        return asset;
    }

    public async Task<Asset> UpdateAssetAsync(int id, AssetEditModel model)
    {
        var a = await _db.Assets.FindAsync(id) ?? throw new InvalidOperationException("Asset not found.");
        a.Name = model.Name; a.Model = model.Model; a.SerialNumber = model.SerialNumber;
        a.AssetType = model.AssetType; a.Brand = model.Brand; a.FuelType = model.FuelType;
        a.UnitConfiguration = model.UnitConfiguration; a.BTURating = model.BTURating;
        a.FilterSize = model.FilterSize; a.Tonnage = model.Tonnage; a.SEER = model.SEER;
        a.SEER2 = model.SEER2; a.AFUE = model.AFUE; a.HSPF = model.HSPF; a.HSPF2 = model.HSPF2;
        a.EER = model.EER; a.AssetTag = model.AssetTag; a.Nickname = model.Nickname;
        a.Voltage = model.Voltage; a.Phase = model.Phase;
        a.LocationOnSite = model.LocationOnSite; a.ManufactureDate = model.ManufactureDate;
        a.AmpRating = model.AmpRating; a.PanelType = model.PanelType;
        a.PipeMaterial = model.PipeMaterial; a.GallonCapacity = model.GallonCapacity;
        a.RefrigerantType = model.RefrigerantType; a.RefrigerantQuantity = model.RefrigerantQuantity;
        a.FilterType = model.FilterType; a.FilterChangeIntervalMonths = model.FilterChangeIntervalMonths;
        a.FilterLastChanged = model.FilterLastChanged; a.FilterNextDue = model.FilterNextDue;
        a.ThermostatBrand = model.ThermostatBrand; a.ThermostatModel = model.ThermostatModel;
        a.ThermostatType = model.ThermostatType; a.ThermostatWiFiEnabled = model.ThermostatWiFiEnabled;
        a.InstallDate = model.InstallDate; a.LastServiceDate = model.LastServiceDate;
        a.NextServiceDue = model.NextServiceDue; a.WarrantyStartDate = model.WarrantyStartDate;
        a.WarrantyTermYears = model.WarrantyTermYears; a.WarrantyExpiry = model.WarrantyExpiry;
        a.LaborWarrantyTermYears = model.LaborWarrantyTermYears;
        a.PartsWarrantyTermYears = model.PartsWarrantyTermYears;
        a.CompressorWarrantyTermYears = model.CompressorWarrantyTermYears;
        a.RegisteredOnline = model.RegisteredOnline; a.InstalledBy = model.InstalledBy;
        a.WarrantedByCompany = model.WarrantedByCompany;
        a.Status = model.Status; a.Value = model.Value; a.Notes = model.Notes;
        a.ProductId = model.ProductId; a.CustomerId = model.CustomerId; a.SiteId = model.SiteId;
        a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Sync linked jobs
        var existingLinks = await _db.Set<JobAsset>().Where(ja => ja.AssetId == id).ToListAsync();
        _db.Set<JobAsset>().RemoveRange(existingLinks);
        foreach (var jobId in model.JobIds)
            _db.Set<JobAsset>().Add(new JobAsset { JobId = jobId, AssetId = id, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        await CalculateWarrantyExpiriesAsync(a);
        return a;
    }

    /// <summary>
    /// Auto-calculates Labor/Parts/Compressor warranty expiry dates.
    /// Priority: form-level term years > Product warranty terms > defaults (1yr labor, 10yr parts, 10yr compressor).
    /// Uses WarrantyStartDate (or InstallDate fallback) as the base date.
    /// Also auto-sets NextServiceDue to 1 year from InstallDate if not already set.
    /// </summary>
    private async Task CalculateWarrantyExpiriesAsync(Asset asset)
    {
        var startDate = asset.WarrantyStartDate ?? asset.InstallDate;
        if (startDate is not null)
        {
            // Determine warranty term years: form-level > product > defaults
            int laborYears = asset.LaborWarrantyTermYears ?? 0;
            int partsYears = asset.PartsWarrantyTermYears ?? 0;
            int compressorYears = asset.CompressorWarrantyTermYears ?? 0;

            // Fall back to product warranty terms if form-level not set
            if (laborYears == 0 || partsYears == 0 || compressorYears == 0)
            {
                int pLabor = 1, pParts = 10, pCompressor = 10;
                if (asset.ProductId.HasValue)
                {
                    var product = await _db.Products.FindAsync(asset.ProductId.Value);
                    if (product is not null)
                    {
                        pLabor = product.LaborWarrantyYears;
                        pParts = product.PartsWarrantyYears;
                        pCompressor = product.CompressorWarrantyYears;
                    }
                }
                if (laborYears == 0) { laborYears = pLabor; asset.LaborWarrantyTermYears = laborYears; }
                if (partsYears == 0) { partsYears = pParts; asset.PartsWarrantyTermYears = partsYears; }
                if (compressorYears == 0) { compressorYears = pCompressor; asset.CompressorWarrantyTermYears = compressorYears; }
            }

            asset.LaborWarrantyExpiry = startDate.Value.AddYears(laborYears);
            asset.PartsWarrantyExpiry = startDate.Value.AddYears(partsYears);
            asset.CompressorWarrantyExpiry = startDate.Value.AddYears(compressorYears);

            // Set the general WarrantyExpiry to the latest of the three
            asset.WarrantyExpiry = new[] { asset.LaborWarrantyExpiry, asset.PartsWarrantyExpiry, asset.CompressorWarrantyExpiry }.Max();
        }

        // Auto-set NextServiceDue to 1 year from install if not already set
        if (!asset.NextServiceDue.HasValue && asset.InstallDate.HasValue)
            asset.NextServiceDue = asset.InstallDate.Value.AddYears(1);

        await _db.SaveChangesAsync();
    }

    public async Task<bool> ArchiveAssetAsync(int id)
    {
        var a = await _db.Assets.FindAsync(id);
        if (a is null) return false;
        a.IsArchived = true; a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<AssetTimelineEntry>> GetUnifiedTimelineAsync(int assetId)
    {
        var timeline = new List<AssetTimelineEntry>();

        // 1. Jobs linked to this asset
        var jobs = await _db.JobAssets
            .Where(ja => ja.AssetId == assetId)
            .Include(ja => ja.Job).ThenInclude(j => j!.AssignedEmployee)
            .Select(ja => new { ja.Job, ja.Role })
            .ToListAsync();

        foreach (var item in jobs)
        {
            if (item.Job is null) continue;
            timeline.Add(new AssetTimelineEntry
            {
                Date = item.Job.CompletedDate ?? item.Job.ScheduledDate ?? item.Job.CreatedAt,
                Source = "Job",
                Title = $"{item.Job.JobNumber}: {item.Job.Title}",
                Description = $"Role: {item.Role ?? "General"}. {item.Job.JobType ?? ""}",
                Status = item.Job.Status.ToString(),
                PerformedBy = item.Job.AssignedEmployee?.Name,
                Cost = item.Job.ActualTotal ?? item.Job.EstimatedTotal,
                SourceId = item.Job.Id,
                Badge = item.Job.Status == JobStatus.Completed ? "bg-success" : "bg-primary"
            });
        }

        // 2. ServiceHistoryRecords linked to this asset
        var serviceRecords = await _db.ServiceHistoryRecords
            .Where(sh => sh.AssetId == assetId)
            .Include(sh => sh.Tech)
            .OrderByDescending(sh => sh.ServiceDate)
            .Take(50)
            .ToListAsync();

        foreach (var sh in serviceRecords)
        {
            timeline.Add(new AssetTimelineEntry
            {
                Date = sh.ServiceDate,
                Source = "ServiceHistory",
                Title = $"{sh.RecordNumber}: {sh.Type}",
                Description = sh.Description,
                Status = sh.Status.ToString(),
                PerformedBy = sh.Tech?.Name,
                Cost = sh.Cost,
                SourceId = sh.Id,
                Badge = sh.Status == ServiceHistoryStatus.Resolved ? "bg-info" : "bg-warning text-dark"
            });
        }

        // 3. AssetServiceLogs (manual service entries)
        var serviceLogs = await _db.AssetServiceLogs
            .Where(sl => sl.AssetId == assetId)
            .OrderByDescending(sl => sl.ServiceDate)
            .Take(50)
            .ToListAsync();

        foreach (var sl in serviceLogs)
        {
            timeline.Add(new AssetTimelineEntry
            {
                Date = sl.ServiceDate,
                Source = "ServiceLog",
                Title = sl.ServiceType ?? "Service Log",
                Description = sl.Notes,
                Status = "Completed",
                PerformedBy = sl.PerformedBy,
                Cost = sl.Cost,
                SourceId = sl.Id,
                Badge = "bg-secondary"
            });
        }

        return timeline.OrderByDescending(t => t.Date).ToList();
    }

    public async Task<List<LinkedAssetDto>> GetLinkedAssetsAsync(int assetId)
    {
        // Query both directions of the link
        var fromLinks = await _db.AssetLinks
            .Where(al => al.AssetId == assetId)
            .Select(al => new LinkedAssetDto
            {
                Id = al.LinkedAsset!.Id,
                Name = al.LinkedAsset.Name,
                AssetType = al.LinkedAsset.AssetType,
                Brand = al.LinkedAsset.Brand,
                Model = al.LinkedAsset.Model,
                LocationOnSite = al.LinkedAsset.LocationOnSite,
                LinkType = al.LinkType,
                Label = al.Label,
                Status = al.LinkedAsset.Status
            }).ToListAsync();

        var toLinks = await _db.AssetLinks
            .Where(al => al.LinkedAssetId == assetId)
            .Select(al => new LinkedAssetDto
            {
                Id = al.Asset!.Id,
                Name = al.Asset.Name,
                AssetType = al.Asset.AssetType,
                Brand = al.Asset.Brand,
                Model = al.Asset.Model,
                LocationOnSite = al.Asset.LocationOnSite,
                LinkType = al.LinkType,
                Label = al.Label,
                Status = al.Asset.Status
            }).ToListAsync();

        return [.. fromLinks, .. toLinks];
    }

    public async Task LinkAssetsAsync(int assetId, int linkedAssetId, string? linkType = null, string? label = null)
    {
        if (assetId == linkedAssetId) return;

        // Ensure canonical ordering to avoid duplicates (always store smaller ID as AssetId)
        var (a, b) = assetId < linkedAssetId ? (assetId, linkedAssetId) : (linkedAssetId, assetId);
        var exists = await _db.AssetLinks.AnyAsync(al => al.AssetId == a && al.LinkedAssetId == b);
        if (exists) return;

        _db.AssetLinks.Add(new AssetLink
        {
            AssetId = a,
            LinkedAssetId = b,
            LinkType = linkType,
            Label = label,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task UnlinkAssetAsync(int assetId, int linkedAssetId)
    {
        var (a, b) = assetId < linkedAssetId ? (assetId, linkedAssetId) : (linkedAssetId, assetId);
        var link = await _db.AssetLinks.FirstOrDefaultAsync(al => al.AssetId == a && al.LinkedAssetId == b);
        if (link is not null)
        {
            _db.AssetLinks.Remove(link);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<AssetOption>> GetAssetOptionsAsync(int? customerId = null, int? siteId = null)
    {
        var query = _db.Assets.Where(a => !a.IsArchived);
        if (siteId.HasValue)
            query = query.Where(a => a.SiteId == siteId.Value);
        else if (customerId.HasValue)
            query = query.Where(a => a.CustomerId == customerId.Value);
        return await query
            .OrderBy(a => a.Name)
            .Select(a => new AssetOption { Id = a.Id, Name = a.Name, AssetType = a.AssetType })
            .ToListAsync();
    }
}
