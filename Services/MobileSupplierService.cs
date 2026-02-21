using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileSupplierService(AppDbContext db) : IMobileSupplierService
{
    public async Task<List<MobileSupplierCard>> GetSuppliersAsync(MobileSupplierFilter? filter = null)
    {
        var query = db.Suppliers.AsQueryable();

        if (filter?.ShowArchived != true)
            query = query.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(search)
                || (s.ContactName != null && s.ContactName.ToLower().Contains(search))
                || (s.AccountNumber != null && s.AccountNumber.ToLower().Contains(search))
                || (s.Email != null && s.Email.ToLower().Contains(search)));
        }

        var suppliers = await query.OrderBy(s => s.Name).ToListAsync();
        var supplierIds = suppliers.Select(s => s.Id).ToList();

        var productCounts = await db.Products
            .Where(p => p.SupplierId.HasValue && supplierIds.Contains(p.SupplierId.Value))
            .GroupBy(p => p.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Count);

        var inventoryCounts = await db.InventoryItems
            .Where(i => i.SupplierId.HasValue && supplierIds.Contains(i.SupplierId.Value))
            .GroupBy(i => i.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Count);

        return suppliers.Select(s => new MobileSupplierCard
        {
            Id = s.Id,
            Name = s.Name,
            ContactName = s.ContactName,
            Phone = s.Phone,
            Email = s.Email,
            AccountNumber = s.AccountNumber,
            PaymentTerms = s.PaymentTerms,
            IsActive = s.IsActive,
            LinkedProductCount = productCounts.GetValueOrDefault(s.Id),
            LinkedInventoryCount = inventoryCounts.GetValueOrDefault(s.Id),
        }).ToList();
    }

    public async Task<MobileSupplierDetail?> GetSupplierDetailAsync(int id)
    {
        var s = await db.Suppliers
            .Include(sup => sup.Company)
            .FirstOrDefaultAsync(sup => sup.Id == id);

        if (s is null) return null;

        var products = await db.Products
            .Where(p => p.SupplierId == id && !p.IsArchived)
            .OrderBy(p => p.Name)
            .Select(p => new MobileSupplierLinkedProduct
            {
                Id = p.Id,
                Name = p.Name,
                Brand = p.Brand,
                Cost = p.Cost,
                Price = p.Price,
            }).ToListAsync();

        var inventory = await db.InventoryItems
            .Where(i => i.SupplierId == id && !i.IsArchived)
            .OrderBy(i => i.Name)
            .Select(i => new MobileSupplierLinkedInventory
            {
                Id = i.Id,
                Name = i.Name,
                SKU = i.SKU,
                Quantity = i.Quantity,
                Cost = i.Cost,
            }).ToListAsync();

        return new MobileSupplierDetail
        {
            Id = s.Id,
            Name = s.Name,
            ContactName = s.ContactName,
            Phone = s.Phone,
            Email = s.Email,
            Website = s.Website,
            AccountNumber = s.AccountNumber,
            PaymentTerms = s.PaymentTerms,
            Notes = s.Notes,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            CompanyName = s.Company?.Name,
            Products = products,
            Inventory = inventory,
        };
    }

    public async Task<int> CreateSupplierAsync(MobileSupplierCreate model)
    {
        var supplier = new Supplier
        {
            Name = model.Name,
            ContactName = model.ContactName,
            Phone = model.Phone,
            Email = model.Email,
            Website = model.Website,
            AccountNumber = model.AccountNumber,
            PaymentTerms = model.PaymentTerms,
            Notes = model.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier.Id;
    }

    public async Task<bool> UpdateSupplierAsync(int id, MobileSupplierUpdate model)
    {
        var supplier = await db.Suppliers.FindAsync(id);
        if (supplier is null) return false;

        supplier.Name = model.Name;
        supplier.ContactName = model.ContactName;
        supplier.Phone = model.Phone;
        supplier.Email = model.Email;
        supplier.Website = model.Website;
        supplier.AccountNumber = model.AccountNumber;
        supplier.PaymentTerms = model.PaymentTerms;
        supplier.Notes = model.Notes;
        supplier.IsActive = model.IsActive;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveSupplierAsync(int id)
    {
        var supplier = await db.Suppliers.FindAsync(id);
        if (supplier is null) return false;
        supplier.IsActive = false;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreSupplierAsync(int id)
    {
        var supplier = await db.Suppliers.FindAsync(id);
        if (supplier is null) return false;
        supplier.IsActive = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<MobileSupplierStats> GetStatsAsync()
    {
        var suppliers = await db.Suppliers.ToListAsync();
        return new MobileSupplierStats
        {
            TotalSuppliers = suppliers.Count,
            ActiveCount = suppliers.Count(s => s.IsActive),
            InactiveCount = suppliers.Count(s => !s.IsActive),
        };
    }
}
