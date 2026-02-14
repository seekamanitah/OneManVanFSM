using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileInventoryService : IMobileInventoryService
{
    private readonly AppDbContext _db;
    public MobileInventoryService(AppDbContext db) => _db = db;

    public async Task<List<MobileInventoryItem>> GetInventoryAsync(MobileInventoryFilter? filter = null)
    {
        var query = _db.InventoryItems.Where(i => !i.IsArchived).AsQueryable();

        if (filter?.Location.HasValue == true)
            query = query.Where(i => i.Location == filter.Location.Value);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(s)
                || (i.SKU != null && i.SKU.ToLower().Contains(s))
                || (i.ShelfBin != null && i.ShelfBin.ToLower().Contains(s)));
        }

        var items = await query.OrderBy(i => i.Name).ToListAsync();

        var result = items.Select(i => new MobileInventoryItem
        {
            Id = i.Id,
            Name = i.Name,
            SKU = i.SKU,
            ShelfBin = i.ShelfBin,
            Location = i.Location,
            Quantity = i.Quantity,
            MinThreshold = i.MinThreshold,
            Cost = i.Cost,
            Price = i.Price,
            ExpiryDate = i.ExpiryDate,
        }).ToList();

        if (filter?.LowStockOnly == true)
            result = result.Where(i => i.IsLowStock).ToList();

        return result;
    }

    public async Task<MobileInventoryStats> GetStatsAsync()
    {
        var items = await _db.InventoryItems.Where(i => !i.IsArchived).ToListAsync();

        return new MobileInventoryStats
        {
            TotalItems = items.Count,
            LowStockCount = items.Count(i => i.Quantity <= i.MinThreshold && i.MinThreshold > 0),
            TruckItems = items.Count(i => i.Location == InventoryLocation.Truck),
            TotalValue = items.Sum(i => i.Quantity * i.Cost),
        };
    }

    public async Task AdjustQuantityAsync(int itemId, decimal delta)
    {
        var item = await _db.InventoryItems.FindAsync(itemId);
        if (item == null) return;
        item.Quantity = Math.Max(0, item.Quantity + delta);
        await _db.SaveChangesAsync();
    }

    public async Task<MobileInventoryDetail?> GetItemDetailAsync(int id)
    {
        var item = await _db.InventoryItems.AsNoTracking()
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return null;

        return new MobileInventoryDetail
        {
            Id = item.Id,
            Name = item.Name,
            SKU = item.SKU,
            PartNumber = item.PartNumber,
            Barcode = item.Barcode,
            Category = item.Category,
            Unit = item.Unit,
            Description = item.Description,
            ShelfBin = item.ShelfBin,
            PreferredSupplier = item.PreferredSupplier,
            Location = item.Location,
            Quantity = item.Quantity,
            MinThreshold = item.MinThreshold,
            MaxCapacity = item.MaxCapacity,
            LotNumber = item.LotNumber,
            ExpiryDate = item.ExpiryDate,
            Cost = item.Cost,
            Price = item.Price,
            MarkupPercent = item.MarkupPercent,
            LastRestockedDate = item.LastRestockedDate,
            Notes = item.Notes,
            ProductName = item.Product?.Name,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
    }

    public async Task<bool> UpdateItemAsync(int id, MobileInventoryUpdate model)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null) return false;

        item.Name = model.Name;
        item.SKU = model.SKU;
        item.PartNumber = model.PartNumber;
        item.Category = model.Category;
        item.Unit = model.Unit;
        item.Description = model.Description;
        item.ShelfBin = model.ShelfBin;
        item.Location = model.Location;
        item.Quantity = model.Quantity;
        item.MinThreshold = model.MinThreshold;
        item.Cost = model.Cost;
        item.Price = model.Price;
        item.Notes = model.Notes;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<InventoryItem> QuickCreateAsync(MobileInventoryQuickCreate model)
    {
        var item = new InventoryItem
        {
            Name = model.Name,
            SKU = model.SKU,
            Location = model.Location,
            Quantity = model.Quantity,
            Cost = model.Cost,
            Price = model.Price,
            ShelfBin = model.ShelfBin,
            Notes = model.Notes,
            NeedsReview = true,
            CreatedFrom = "mobile",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }
}
