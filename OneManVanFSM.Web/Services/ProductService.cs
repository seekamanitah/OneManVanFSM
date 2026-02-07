using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public async Task<List<ProductListItem>> GetProductsAsync(ProductFilter? filter = null)
    {
        var query = _db.Products.Where(p => !p.IsArchived).AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                    (p.Category != null && p.Category.ToLower().Contains(term)) ||
                    (p.SupplierName != null && p.SupplierName.ToLower().Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(p => p.Category == filter.Category);

            query = filter.SortBy?.ToLower() switch
            {
                "category" => filter.SortDescending ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
                "price" => filter.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                _ => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
            };
        }
        else query = query.OrderBy(p => p.Name);

        return await query.Select(p => new ProductListItem
        {
            Id = p.Id, Name = p.Name, Brand = p.Brand, PartNumber = p.PartNumber,
            Category = p.Category,
            Cost = p.Cost, Price = p.Price, MarkupPercent = p.MarkupPercent,
            Unit = p.Unit, SupplierName = p.SupplierName, IsTemplate = p.IsTemplate,
            InventoryCount = p.InventoryItems.Count(i => !i.IsArchived)
        }).ToListAsync();
    }

    public async Task<ProductDetail?> GetProductAsync(int id)
    {
        return await _db.Products.Where(p => p.Id == id && !p.IsArchived)
            .Select(p => new ProductDetail
            {
                Id = p.Id, Name = p.Name, Brand = p.Brand, PartNumber = p.PartNumber,
                Barcode = p.Barcode, Category = p.Category,
                Cost = p.Cost, Price = p.Price, MarkupPercent = p.MarkupPercent,
                Unit = p.Unit, Specs = p.Specs, SupplierName = p.SupplierName,
                IsTemplate = p.IsTemplate, Notes = p.Notes,
                InventoryCount = p.InventoryItems.Count(i => !i.IsArchived),
                AssetCount = p.Assets.Count(a => !a.IsArchived),
                CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt
            }).FirstOrDefaultAsync();
    }

    public async Task<Product> CreateProductAsync(ProductEditModel model)
    {
        var product = new Product
        {
            Name = model.Name, Brand = model.Brand, PartNumber = model.PartNumber,
            Barcode = model.Barcode, Category = model.Category, Cost = model.Cost,
            Price = model.Price, MarkupPercent = model.MarkupPercent, Unit = model.Unit,
            Specs = model.Specs, SupplierName = model.SupplierName, IsTemplate = model.IsTemplate,
            Notes = model.Notes, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateProductAsync(int id, ProductEditModel model)
    {
        var p = await _db.Products.FindAsync(id) ?? throw new InvalidOperationException("Product not found.");
        p.Name = model.Name; p.Brand = model.Brand; p.PartNumber = model.PartNumber;
        p.Barcode = model.Barcode; p.Category = model.Category; p.Cost = model.Cost;
        p.Price = model.Price; p.MarkupPercent = model.MarkupPercent; p.Unit = model.Unit;
        p.Specs = model.Specs; p.SupplierName = model.SupplierName; p.IsTemplate = model.IsTemplate;
        p.Notes = model.Notes; p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return p;
    }

    public async Task<bool> ArchiveProductAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return false;
        p.IsArchived = true; p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
