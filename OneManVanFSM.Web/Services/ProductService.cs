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
        var showArchived = filter?.ShowArchived ?? false;
        var query = _db.Products.Where(p => p.IsArchived == showArchived).AsQueryable();

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
                "cost" => filter.SortDescending ? query.OrderByDescending(p => p.Cost) : query.OrderBy(p => p.Cost),
                "price" => filter.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                _ => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
            };
        }
        else query = query.OrderBy(p => p.Name);

        return await query.Select(p => new ProductListItem
        {
            Id = p.Id, Name = p.Name, Brand = p.Brand, ModelNumber = p.ModelNumber,
            PartNumber = p.PartNumber, Category = p.Category, EquipmentType = p.EquipmentType,
            Cost = p.Cost, Price = p.Price, MSRP = p.MSRP, MarkupPercent = p.MarkupPercent,
            TaxIncludedInPrice = p.TaxIncludedInPrice,
            Unit = p.Unit, SupplierName = p.SupplierName, IsTemplate = p.IsTemplate,
            IsActive = p.IsActive, IsDiscontinued = p.IsDiscontinued,
            InventoryCount = p.InventoryItems.Count(i => !i.IsArchived)
        }).ToListAsync();
    }

    public async Task<ProductDetail?> GetProductAsync(int id)
    {
        return await _db.Products.Where(p => p.Id == id && !p.IsArchived)
            .Select(p => new ProductDetail
            {
                Id = p.Id, Name = p.Name, Brand = p.Brand, ModelNumber = p.ModelNumber,
                ProductNumber = p.ProductNumber, PartNumber = p.PartNumber,
                Barcode = p.Barcode, Category = p.Category,
                EquipmentType = p.EquipmentType, FuelType = p.FuelType,
                Description = p.Description,
                Cost = p.Cost, Price = p.Price, MSRP = p.MSRP, MarkupPercent = p.MarkupPercent,
                TaxIncludedInPrice = p.TaxIncludedInPrice,
                Unit = p.Unit, Specs = p.Specs, SupplierName = p.SupplierName,
                Tonnage = p.Tonnage, RefrigerantType = p.RefrigerantType,
                SEERRating = p.SEERRating, AFUERating = p.AFUERating, Voltage = p.Voltage,
                LaborWarrantyYears = p.LaborWarrantyYears,
                PartsWarrantyYears = p.PartsWarrantyYears,
                CompressorWarrantyYears = p.CompressorWarrantyYears,
                RegistrationRequired = p.RegistrationRequired,
                IsActive = p.IsActive, IsDiscontinued = p.IsDiscontinued,
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
            Name = model.Name, Brand = model.Brand, ModelNumber = model.ModelNumber,
            PartNumber = model.PartNumber, ProductNumber = model.ProductNumber,
            Barcode = model.Barcode, Category = model.Category,
            EquipmentType = model.EquipmentType, FuelType = model.FuelType,
            Description = model.Description,
            Cost = model.Cost, Price = model.Price, MSRP = model.MSRP,
            MarkupPercent = model.MarkupPercent, TaxIncludedInPrice = model.TaxIncludedInPrice,
            Unit = model.Unit,
            Specs = model.Specs, SupplierName = model.SupplierName,
            Tonnage = model.Tonnage, RefrigerantType = model.RefrigerantType,
            SEERRating = model.SEERRating, AFUERating = model.AFUERating,
            Voltage = model.Voltage, IsTemplate = model.IsTemplate,
            LaborWarrantyYears = model.LaborWarrantyYears,
            PartsWarrantyYears = model.PartsWarrantyYears,
            CompressorWarrantyYears = model.CompressorWarrantyYears,
            RegistrationRequired = model.RegistrationRequired,
            IsActive = model.IsActive, IsDiscontinued = model.IsDiscontinued,
            Notes = model.Notes, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateProductAsync(int id, ProductEditModel model)
    {
        var p = await _db.Products.FindAsync(id) ?? throw new InvalidOperationException("Product not found.");
        p.Name = model.Name; p.Brand = model.Brand; p.ModelNumber = model.ModelNumber;
        p.PartNumber = model.PartNumber; p.ProductNumber = model.ProductNumber;
        p.Barcode = model.Barcode; p.Category = model.Category;
        p.EquipmentType = model.EquipmentType; p.FuelType = model.FuelType;
        p.Description = model.Description;
        p.Cost = model.Cost; p.Price = model.Price; p.MSRP = model.MSRP;
        p.MarkupPercent = model.MarkupPercent; p.TaxIncludedInPrice = model.TaxIncludedInPrice;
        p.Unit = model.Unit;
        p.Specs = model.Specs; p.SupplierName = model.SupplierName;
        p.Tonnage = model.Tonnage; p.RefrigerantType = model.RefrigerantType;
        p.SEERRating = model.SEERRating; p.AFUERating = model.AFUERating;
        p.Voltage = model.Voltage; p.IsTemplate = model.IsTemplate;
        p.LaborWarrantyYears = model.LaborWarrantyYears;
        p.PartsWarrantyYears = model.PartsWarrantyYears;
        p.CompressorWarrantyYears = model.CompressorWarrantyYears;
        p.RegistrationRequired = model.RegistrationRequired;
        p.IsActive = model.IsActive; p.IsDiscontinued = model.IsDiscontinued;
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

    public async Task<bool> RestoreProductAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return false;
        p.IsArchived = false; p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteProductPermanentlyAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return false;
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveProductsAsync(List<int> ids)
    {
        var products = await _db.Products.Where(p => ids.Contains(p.Id) && !p.IsArchived).ToListAsync();
        foreach (var p in products) { p.IsArchived = true; p.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return products.Count;
    }

    public async Task<int> BulkRestoreProductsAsync(List<int> ids)
    {
        var products = await _db.Products.Where(p => ids.Contains(p.Id) && p.IsArchived).ToListAsync();
        foreach (var p in products) { p.IsArchived = false; p.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return products.Count;
    }

    public async Task<int> BulkDeleteProductsPermanentlyAsync(List<int> ids)
    {
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        _db.Products.RemoveRange(products);
        await _db.SaveChangesAsync();
        return products.Count;
    }
}
