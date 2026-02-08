using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileAssetService(AppDbContext db) : IMobileAssetService
{
    public async Task<List<MobileAssetCard>> GetAssetsAsync(MobileAssetFilter? filter = null)
    {
        var query = db.Assets.Where(a => !a.IsArchived).AsQueryable();

        if (filter?.Status.HasValue == true)
            query = query.Where(a => a.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter?.AssetType))
            query = query.Where(a => a.AssetType == filter.AssetType);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(s)
                || (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(s))
                || (a.Brand != null && a.Brand.ToLower().Contains(s))
                || (a.Model != null && a.Model.ToLower().Contains(s))
                || (a.AssetType != null && a.AssetType.ToLower().Contains(s)));
        }

        return await query
            .Include(a => a.Customer)
            .Include(a => a.Site)
            .OrderBy(a => a.Name)
            .Select(a => new MobileAssetCard
            {
                Id = a.Id,
                Name = a.Name,
                AssetType = a.AssetType,
                Brand = a.Brand,
                Model = a.Model,
                SerialNumber = a.SerialNumber,
                Status = a.Status,
                CustomerName = a.Customer != null ? a.Customer.Name : null,
                SiteName = a.Site != null ? a.Site.Name : null,
                SiteId = a.SiteId,
                WarrantyExpiry = a.WarrantyExpiry,
                LastServiceDate = a.LastServiceDate,
                NextServiceDue = a.NextServiceDue,
            })
            .ToListAsync();
    }

    public async Task<MobileAssetStats> GetStatsAsync()
    {
        var assets = await db.Assets.Where(a => !a.IsArchived).ToListAsync();
        var now = DateTime.UtcNow;

        return new MobileAssetStats
        {
            TotalAssets = assets.Count,
            ActiveCount = assets.Count(a => a.Status == AssetStatus.Active),
            MaintenanceNeededCount = assets.Count(a => a.Status == AssetStatus.MaintenanceNeeded),
            ExpiringWarrantyCount = assets.Count(a => a.WarrantyExpiry.HasValue && a.WarrantyExpiry.Value < now.AddDays(90) && a.WarrantyExpiry.Value > now),
        };
    }

    public async Task<MobileAssetDetail?> GetAssetDetailAsync(int assetId)
    {
        var asset = await db.Assets
            .Include(a => a.Site)
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == assetId);

        if (asset is null) return null;

        var linkedJobs = await db.JobAssets
            .Where(ja => ja.AssetId == assetId)
            .Include(ja => ja.Job)
            .OrderByDescending(ja => ja.Job!.ScheduledDate)
            .Take(10)
            .Select(ja => new MobileAssetJob
            {
                JobId = ja.JobId,
                JobNumber = ja.Job!.JobNumber,
                Title = ja.Job.Title,
                Status = ja.Job.Status,
                Role = ja.Role,
                ScheduledDate = ja.Job.ScheduledDate,
            })
            .ToListAsync();

        var serviceLogs = await db.AssetServiceLogs
            .Where(l => l.AssetId == assetId)
            .OrderByDescending(l => l.ServiceDate)
            .Take(20)
            .Select(l => new MobileServiceLogItem
            {
                Id = l.Id,
                ServiceType = l.ServiceType,
                ServiceDate = l.ServiceDate,
                PerformedBy = l.PerformedBy,
                Notes = l.Notes,
                NextDueDate = l.NextDueDate,
                Cost = l.Cost,
                RefrigerantType = l.RefrigerantType,
                RefrigerantAmountAdded = l.RefrigerantAmountAdded,
                RefrigerantBeforeReading = l.RefrigerantBeforeReading,
                RefrigerantAfterReading = l.RefrigerantAfterReading,
            })
            .ToListAsync();

        var documents = await db.Documents
            .Where(d => d.AssetId == assetId)
            .OrderByDescending(d => d.UploadDate)
            .Take(10)
            .Select(d => new MobileAssetDocument
            {
                Id = d.Id,
                Name = d.Name,
                FileType = d.FileType,
                Category = d.Category,
                UploadDate = d.UploadDate,
            })
            .ToListAsync();

        return new MobileAssetDetail
        {
            Id = asset.Id,
            Name = asset.Name,
            AssetType = asset.AssetType,
            Brand = asset.Brand,
            Model = asset.Model,
            SerialNumber = asset.SerialNumber,
            Status = asset.Status,
            Tonnage = asset.Tonnage,
            SEER = asset.SEER,
            AFUE = asset.AFUE,
            HSPF = asset.HSPF,
            BTURating = asset.BTURating,
            RefrigerantType = asset.RefrigerantType,
            RefrigerantQuantity = asset.RefrigerantQuantity,
            FilterSize = asset.FilterSize,
            FuelType = asset.FuelType,
            UnitConfiguration = asset.UnitConfiguration,
            Voltage = asset.Voltage,
            AmpRating = asset.AmpRating,
            PipeMaterial = asset.PipeMaterial,
            GallonCapacity = asset.GallonCapacity,
            LocationOnSite = asset.LocationOnSite,
            InstallDate = asset.InstallDate,
            ManufactureDate = asset.ManufactureDate,
            LastServiceDate = asset.LastServiceDate,
            NextServiceDue = asset.NextServiceDue,
            WarrantyStartDate = asset.WarrantyStartDate,
            WarrantyTermYears = asset.WarrantyTermYears,
            WarrantyExpiry = asset.WarrantyExpiry,
            LaborWarrantyExpiry = asset.LaborWarrantyExpiry,
            PartsWarrantyExpiry = asset.PartsWarrantyExpiry,
            CompressorWarrantyExpiry = asset.CompressorWarrantyExpiry,
            Value = asset.Value,
            Notes = asset.Notes,
            SiteName = asset.Site?.Name,
            SiteAddress = asset.Site != null ? $"{asset.Site.Address}, {asset.Site.City}" : null,
            SiteId = asset.SiteId,
            CustomerName = asset.Customer?.Name,
            CustomerId = asset.CustomerId,
            LinkedJobs = linkedJobs,
            ServiceLogs = serviceLogs,
            Documents = documents,
        };
    }

    public async Task<List<MobileServiceLogItem>> GetServiceLogsAsync(int assetId)
    {
        return await db.AssetServiceLogs
            .Where(l => l.AssetId == assetId)
            .OrderByDescending(l => l.ServiceDate)
            .Select(l => new MobileServiceLogItem
            {
                Id = l.Id,
                ServiceType = l.ServiceType,
                ServiceDate = l.ServiceDate,
                PerformedBy = l.PerformedBy,
                Notes = l.Notes,
                NextDueDate = l.NextDueDate,
                Cost = l.Cost,
                RefrigerantType = l.RefrigerantType,
                RefrigerantAmountAdded = l.RefrigerantAmountAdded,
                RefrigerantBeforeReading = l.RefrigerantBeforeReading,
                RefrigerantAfterReading = l.RefrigerantAfterReading,
            })
            .ToListAsync();
    }

    public async Task<AssetServiceLog> AddServiceLogAsync(MobileServiceLogCreate model)
    {
        var log = new AssetServiceLog
        {
            AssetId = model.AssetId,
            ServiceType = model.ServiceType,
            ServiceDate = model.ServiceDate,
            PerformedBy = model.PerformedBy,
            Notes = model.Notes,
            NextDueDate = model.NextDueDate,
            Cost = model.Cost,
            RefrigerantType = model.RefrigerantType,
            RefrigerantAmountAdded = model.RefrigerantAmountAdded,
            RefrigerantBeforeReading = model.RefrigerantBeforeReading,
            RefrigerantAfterReading = model.RefrigerantAfterReading,
            CreatedAt = DateTime.UtcNow,
        };
        db.AssetServiceLogs.Add(log);

        // Update the asset's LastServiceDate
        var asset = await db.Assets.FindAsync(model.AssetId);
        if (asset != null)
        {
            asset.LastServiceDate = model.ServiceDate;
            if (model.NextDueDate.HasValue)
                asset.NextServiceDue = model.NextDueDate;
            asset.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return log;
    }
}
