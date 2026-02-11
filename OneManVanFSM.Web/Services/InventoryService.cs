using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    public InventoryService(AppDbContext db) => _db = db;

    public async Task<List<InventoryListItem>> GetItemsAsync(InventoryFilter? filter = null)
    {
        var query = _db.InventoryItems.Where(i => !i.IsArchived).AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(i => i.Name.ToLower().Contains(term) ||
                    (i.LotNumber != null && i.LotNumber.ToLower().Contains(term)));
            }
            if (filter.Location.HasValue)
                query = query.Where(i => i.Location == filter.Location.Value);
            if (filter.LowStockOnly)
                query = query.Where(i => i.Quantity <= i.MinThreshold);

            query = filter.SortBy?.ToLower() switch
            {
                "location" => filter.SortDescending ? query.OrderByDescending(i => i.Location) : query.OrderBy(i => i.Location),
                "quantity" => filter.SortDescending ? query.OrderByDescending(i => i.Quantity) : query.OrderBy(i => i.Quantity),
                "cost" => filter.SortDescending ? query.OrderByDescending(i => i.Cost) : query.OrderBy(i => i.Cost),
                _ => filter.SortDescending ? query.OrderByDescending(i => i.Name) : query.OrderBy(i => i.Name)
            };
        }
        else query = query.OrderBy(i => i.Name);

        return await query.Select(i => new InventoryListItem
        {
            Id = i.Id, Name = i.Name, SKU = i.SKU, PartNumber = i.PartNumber,
            Category = i.Category, Unit = i.Unit,
            ProductName = i.Product != null ? i.Product.Name : null,
            Location = i.Location, Quantity = i.Quantity, MinThreshold = i.MinThreshold,
            MaxCapacity = i.MaxCapacity, Cost = i.Cost, Price = i.Price,
            TaxIncludedInPrice = i.TaxIncludedInPrice,
            LotNumber = i.LotNumber, ExpiryDate = i.ExpiryDate
        }).ToListAsync();
    }

    public async Task<InventoryDetail?> GetItemAsync(int id)
    {
        return await _db.InventoryItems.Where(i => i.Id == id && !i.IsArchived)
            .Select(i => new InventoryDetail
            {
                Id = i.Id, Name = i.Name, SKU = i.SKU, PartNumber = i.PartNumber,
                Category = i.Category, Unit = i.Unit, Description = i.Description,
                Barcode = i.Barcode, ShelfBin = i.ShelfBin,
                PreferredSupplier = i.PreferredSupplier, Location = i.Location,
                Quantity = i.Quantity, MinThreshold = i.MinThreshold, MaxCapacity = i.MaxCapacity,
                Cost = i.Cost, Price = i.Price, MarkupPercent = i.MarkupPercent,
                TaxIncludedInPrice = i.TaxIncludedInPrice,
                LotNumber = i.LotNumber, ExpiryDate = i.ExpiryDate,
                LastRestockedDate = i.LastRestockedDate,
                Notes = i.Notes,
                ProductId = i.ProductId, ProductName = i.Product != null ? i.Product.Name : null,
                CreatedAt = i.CreatedAt, UpdatedAt = i.UpdatedAt
            }).FirstOrDefaultAsync();
    }

    public async Task<InventoryItem> CreateItemAsync(InventoryEditModel model)
    {
        var item = new InventoryItem
        {
            Name = model.Name, SKU = model.SKU, PartNumber = model.PartNumber,
            Category = model.Category, Unit = model.Unit, Description = model.Description,
            Barcode = model.Barcode, ShelfBin = model.ShelfBin,
            PreferredSupplier = model.PreferredSupplier,
            Location = model.Location, Quantity = model.Quantity,
            MinThreshold = model.MinThreshold, MaxCapacity = model.MaxCapacity,
            Cost = model.Cost, Price = model.Price, MarkupPercent = model.MarkupPercent,
            TaxIncludedInPrice = model.TaxIncludedInPrice,
            LotNumber = model.LotNumber, ExpiryDate = model.ExpiryDate,
            Notes = model.Notes, ProductId = model.ProductId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<InventoryItem> UpdateItemAsync(int id, InventoryEditModel model)
    {
        var item = await _db.InventoryItems.FindAsync(id) ?? throw new InvalidOperationException("Item not found.");
        item.Name = model.Name; item.SKU = model.SKU; item.PartNumber = model.PartNumber;
        item.Category = model.Category; item.Unit = model.Unit; item.Description = model.Description;
        item.Barcode = model.Barcode; item.ShelfBin = model.ShelfBin;
        item.PreferredSupplier = model.PreferredSupplier;
        item.Location = model.Location; item.Quantity = model.Quantity;
        item.MinThreshold = model.MinThreshold; item.MaxCapacity = model.MaxCapacity;
        item.Cost = model.Cost; item.Price = model.Price; item.MarkupPercent = model.MarkupPercent;
        item.TaxIncludedInPrice = model.TaxIncludedInPrice;
        item.LotNumber = model.LotNumber; item.ExpiryDate = model.ExpiryDate;
        item.Notes = model.Notes; item.ProductId = model.ProductId;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> ArchiveItemAsync(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null) return false;
        item.IsArchived = true; item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<InventoryDashboard> GetDashboardAsync()
    {
        var items = await _db.InventoryItems.Where(i => !i.IsArchived).ToListAsync();
        return new InventoryDashboard
        {
            TotalItems = items.Count,
            LowStockCount = items.Count(i => i.Quantity <= i.MinThreshold),
            ExpiringCount = items.Count(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30)),
            TotalValue = items.Sum(i => i.Cost * i.Quantity)
        };
    }
}
