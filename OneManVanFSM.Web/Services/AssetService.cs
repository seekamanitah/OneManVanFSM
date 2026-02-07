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
            })
            .ToListAsync();

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
        return a;
    }

    public async Task<bool> ArchiveAssetAsync(int id)
    {
        var a = await _db.Assets.FindAsync(id);
        if (a is null) return false;
        a.IsArchived = true; a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
