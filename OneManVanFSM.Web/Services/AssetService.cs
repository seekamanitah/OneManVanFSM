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
                AFUE = a.AFUE, HSPF = a.HSPF, Voltage = a.Voltage, Phase = a.Phase,
                LocationOnSite = a.LocationOnSite, ManufactureDate = a.ManufactureDate,
                AmpRating = a.AmpRating, PanelType = a.PanelType,
                PipeMaterial = a.PipeMaterial, GallonCapacity = a.GallonCapacity,
                RefrigerantType = a.RefrigerantType, RefrigerantQuantity = a.RefrigerantQuantity,
                InstallDate = a.InstallDate, LastServiceDate = a.LastServiceDate,
                NextServiceDue = a.NextServiceDue, WarrantyStartDate = a.WarrantyStartDate,
                WarrantyTermYears = a.WarrantyTermYears, WarrantyExpiry = a.WarrantyExpiry,
                LaborWarrantyExpiry = a.LaborWarrantyExpiry,
                PartsWarrantyExpiry = a.PartsWarrantyExpiry,
                CompressorWarrantyExpiry = a.CompressorWarrantyExpiry,
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
            AFUE = model.AFUE, HSPF = model.HSPF, Voltage = model.Voltage, Phase = model.Phase,
            LocationOnSite = model.LocationOnSite, ManufactureDate = model.ManufactureDate,
            AmpRating = model.AmpRating, PanelType = model.PanelType,
            PipeMaterial = model.PipeMaterial, GallonCapacity = model.GallonCapacity,
            RefrigerantType = model.RefrigerantType, RefrigerantQuantity = model.RefrigerantQuantity,
            InstallDate = model.InstallDate, LastServiceDate = model.LastServiceDate,
            NextServiceDue = model.NextServiceDue, WarrantyStartDate = model.WarrantyStartDate,
            WarrantyTermYears = model.WarrantyTermYears, WarrantyExpiry = model.WarrantyExpiry,
            Status = model.Status, Value = model.Value, Notes = model.Notes,
            ProductId = model.ProductId, CustomerId = model.CustomerId, SiteId = model.SiteId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();
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
        a.AFUE = model.AFUE; a.HSPF = model.HSPF; a.Voltage = model.Voltage; a.Phase = model.Phase;
        a.LocationOnSite = model.LocationOnSite; a.ManufactureDate = model.ManufactureDate;
        a.AmpRating = model.AmpRating; a.PanelType = model.PanelType;
        a.PipeMaterial = model.PipeMaterial; a.GallonCapacity = model.GallonCapacity;
        a.RefrigerantType = model.RefrigerantType; a.RefrigerantQuantity = model.RefrigerantQuantity;
        a.InstallDate = model.InstallDate; a.LastServiceDate = model.LastServiceDate;
        a.NextServiceDue = model.NextServiceDue; a.WarrantyStartDate = model.WarrantyStartDate;
        a.WarrantyTermYears = model.WarrantyTermYears; a.WarrantyExpiry = model.WarrantyExpiry;
        a.Status = model.Status; a.Value = model.Value; a.Notes = model.Notes;
        a.ProductId = model.ProductId; a.CustomerId = model.CustomerId; a.SiteId = model.SiteId;
        a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await CalculateWarrantyExpiriesAsync(a);
        return a;
    }

    /// <summary>
    /// Auto-calculates Labor/Parts/Compressor warranty expiry dates from InstallDate + Product warranty terms.
    /// Falls back to WarrantyStartDate if InstallDate is null. Defaults: 1yr labor, 10yr parts, 10yr compressor.
    /// </summary>
    private async Task CalculateWarrantyExpiriesAsync(Asset asset)
    {
        var startDate = asset.InstallDate ?? asset.WarrantyStartDate;
        if (startDate is null) return;

        int laborYears = 1, partsYears = 10, compressorYears = 10;

        if (asset.ProductId.HasValue)
        {
            var product = await _db.Products.FindAsync(asset.ProductId.Value);
            if (product is not null)
            {
                laborYears = product.LaborWarrantyYears;
                partsYears = product.PartsWarrantyYears;
                compressorYears = product.CompressorWarrantyYears;
            }
        }

        asset.LaborWarrantyExpiry = startDate.Value.AddYears(laborYears);
        asset.PartsWarrantyExpiry = startDate.Value.AddYears(partsYears);
        asset.CompressorWarrantyExpiry = startDate.Value.AddYears(compressorYears);

        // Set the general WarrantyExpiry to the latest of the three
        asset.WarrantyExpiry = new[] { asset.LaborWarrantyExpiry, asset.PartsWarrantyExpiry, asset.CompressorWarrantyExpiry }.Max();

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
}
