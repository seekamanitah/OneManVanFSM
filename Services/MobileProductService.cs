using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileProductService(AppDbContext db) : IMobileProductService
{
    public async Task<List<MobileProductCard>> GetProductsAsync(MobileProductFilter? filter = null)
    {
        var query = db.Products.Where(p => !p.IsArchived).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.Category))
            query = query.Where(p => p.Category == filter.Category);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s)
                || (p.Brand != null && p.Brand.ToLower().Contains(s))
                || (p.PartNumber != null && p.PartNumber.ToLower().Contains(s))
                || (p.Category != null && p.Category.ToLower().Contains(s)));
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync();
        var productIds = products.Select(p => p.Id).ToHashSet();

        var stockCounts = await db.InventoryItems
            .Where(i => i.ProductId.HasValue && productIds.Contains(i.ProductId.Value) && !i.IsArchived)
            .GroupBy(i => i.ProductId!.Value)
            .Select(g => new { ProductId = g.Key, Count = g.Sum(i => (int)i.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count);

        var assetCounts = await db.Assets
            .Where(a => a.ProductId.HasValue && productIds.Contains(a.ProductId.Value) && !a.IsArchived)
            .GroupBy(a => a.ProductId!.Value)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count);

        return products.Select(p => new MobileProductCard
        {
            Id = p.Id,
            Name = p.Name,
            Brand = p.Brand,
            Category = p.Category,
            PartNumber = p.PartNumber,
            Unit = p.Unit,
            Cost = p.Cost,
            Price = p.Price,
            MarkupPercent = p.MarkupPercent,
            StockCount = stockCounts.GetValueOrDefault(p.Id),
            AssetCount = assetCounts.GetValueOrDefault(p.Id),
        }).ToList();
    }

    public async Task<MobileProductStats> GetStatsAsync()
    {
        var products = await db.Products.Where(p => !p.IsArchived).ToListAsync();
        var productIds = products.Select(p => p.Id).ToHashSet();

        var inStockIds = await db.InventoryItems
            .Where(i => i.ProductId.HasValue && productIds.Contains(i.ProductId.Value) && !i.IsArchived && i.Quantity > 0)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .CountAsync();

        return new MobileProductStats
        {
            TotalProducts = products.Count,
            Categories = products.Where(p => p.Category != null).Select(p => p.Category).Distinct().Count(),
            InStockProducts = inStockIds,
            AvgMarkup = products.Count > 0 ? products.Average(p => p.MarkupPercent) : 0,
        };
    }

    public async Task<Product> QuickCreateAsync(MobileProductQuickCreate model)
    {
        var product = new Product
        {
            Name = model.Name,
            Brand = model.Brand,
            ModelNumber = model.ModelNumber,
            Category = model.Category,
            EquipmentType = model.EquipmentType,
            Cost = model.Cost,
            Price = model.Price,
            Notes = model.Notes,
            IsActive = true,
            NeedsReview = true,
            CreatedFrom = "mobile",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}
