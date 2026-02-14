using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;
    public SupplierService(AppDbContext db) => _db = db;

    public async Task<List<SupplierListItem>> GetSuppliersAsync(SupplierFilter? filter = null)
    {
        var showArchived = filter?.ShowArchived ?? false;
        var query = _db.Suppliers
            .Where(s => showArchived ? !s.IsActive : s.IsActive)
            .AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    (s.ContactName != null && s.ContactName.ToLower().Contains(term)) ||
                    (s.Email != null && s.Email.ToLower().Contains(term)) ||
                    (s.AccountNumber != null && s.AccountNumber.ToLower().Contains(term)));
            }

            query = filter.SortBy?.ToLower() switch
            {
                "contact" => filter.SortDescending ? query.OrderByDescending(s => s.ContactName) : query.OrderBy(s => s.ContactName),
                "email" => filter.SortDescending ? query.OrderByDescending(s => s.Email) : query.OrderBy(s => s.Email),
                "account" => filter.SortDescending ? query.OrderByDescending(s => s.AccountNumber) : query.OrderBy(s => s.AccountNumber),
                "terms" => filter.SortDescending ? query.OrderByDescending(s => s.PaymentTerms) : query.OrderBy(s => s.PaymentTerms),
                _ => filter.SortDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name)
            };
        }
        else
        {
            query = query.OrderBy(s => s.Name);
        }

        return await query.Select(s => new SupplierListItem
        {
            Id = s.Id,
            Name = s.Name,
            ContactName = s.ContactName,
            Phone = s.Phone,
            Email = s.Email,
            AccountNumber = s.AccountNumber,
            PaymentTerms = s.PaymentTerms,
            IsActive = s.IsActive,
        }).ToListAsync();
    }

    public async Task<SupplierDetail?> GetSupplierAsync(int id)
    {
        var supplier = await _db.Suppliers
            .Where(s => s.Id == id)
            .Select(s => new SupplierDetail
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
                CompanyId = s.CompanyId,
                CompanyName = s.Company != null ? s.Company.Name : null,
            }).FirstOrDefaultAsync();

        if (supplier is null) return null;

        // Linked products (by SupplierId FK)
        supplier.LinkedProducts = await _db.Products
            .Where(p => p.SupplierId == id && !p.IsArchived)
            .OrderBy(p => p.Name)
            .Select(p => new SupplierLinkedProduct
            {
                Id = p.Id,
                Name = p.Name,
                Brand = p.Brand,
                PartNumber = p.PartNumber,
                Cost = p.Cost,
                Price = p.Price,
            })
            .ToListAsync();

        // Linked inventory (by SupplierId FK)
        supplier.LinkedInventory = await _db.InventoryItems
            .Where(i => i.SupplierId == id && !i.IsArchived)
            .OrderBy(i => i.Name)
            .Select(i => new SupplierLinkedInventory
            {
                Id = i.Id,
                Name = i.Name,
                SKU = i.SKU,
                Quantity = i.Quantity,
                Cost = i.Cost,
            })
            .ToListAsync();

        return supplier;
    }

    public async Task<SupplierListItem> CreateSupplierAsync(SupplierEditModel model)
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
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            CompanyId = model.CompanyId,
        };

        // Auto-create a linked Company record if requested and no existing company selected
        if (model.CreateCompanyRecord && !model.CompanyId.HasValue)
        {
            var company = new Company
            {
                Name = model.Name,
                Type = CompanyType.Supplier,
                Phone = model.Phone,
                Email = model.Email,
                Website = model.Website,
                Notes = $"Auto-created from supplier: {model.Name}",
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();
            supplier.CompanyId = company.Id;
        }

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        return new SupplierListItem
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            AccountNumber = supplier.AccountNumber,
            PaymentTerms = supplier.PaymentTerms,
            IsActive = supplier.IsActive,
        };
    }

    public async Task<SupplierListItem> UpdateSupplierAsync(int id, SupplierEditModel model)
    {
        var s = await _db.Suppliers.FindAsync(id)
            ?? throw new InvalidOperationException("Supplier not found.");

        s.Name = model.Name;
        s.ContactName = model.ContactName;
        s.Phone = model.Phone;
        s.Email = model.Email;
        s.Website = model.Website;
        s.AccountNumber = model.AccountNumber;
        s.PaymentTerms = model.PaymentTerms;
        s.Notes = model.Notes;
        s.IsActive = model.IsActive;
        s.CompanyId = model.CompanyId;

        // Auto-create linked Company if requested and none linked
        if (model.CreateCompanyRecord && !model.CompanyId.HasValue && !s.CompanyId.HasValue)
        {
            var company = new Company
            {
                Name = model.Name,
                Type = CompanyType.Supplier,
                Phone = model.Phone,
                Email = model.Email,
                Website = model.Website,
                Notes = $"Auto-created from supplier: {model.Name}",
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();
            s.CompanyId = company.Id;
        }
        else if (s.CompanyId.HasValue)
        {
            // Sync changes to linked company
            var linkedCompany = await _db.Companies.FindAsync(s.CompanyId.Value);
            if (linkedCompany is not null)
            {
                linkedCompany.Name = model.Name;
                linkedCompany.Phone = model.Phone;
                linkedCompany.Email = model.Email;
                linkedCompany.Website = model.Website;
                linkedCompany.IsActive = model.IsActive;
                linkedCompany.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        return new SupplierListItem
        {
            Id = s.Id,
            Name = s.Name,
            ContactName = s.ContactName,
            Phone = s.Phone,
            Email = s.Email,
            AccountNumber = s.AccountNumber,
            PaymentTerms = s.PaymentTerms,
            IsActive = s.IsActive,
        };
    }

    public async Task<bool> ArchiveSupplierAsync(int id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s is null) return false;
        s.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreSupplierAsync(int id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s is null) return false;
        s.IsActive = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSupplierPermanentlyAsync(int id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s is null) return false;
        _db.Suppliers.Remove(s);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveAsync(List<int> ids)
    {
        var items = await _db.Suppliers.Where(s => ids.Contains(s.Id) && s.IsActive).ToListAsync();
        foreach (var s in items) s.IsActive = false;
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkRestoreAsync(List<int> ids)
    {
        var items = await _db.Suppliers.Where(s => ids.Contains(s.Id) && !s.IsActive).ToListAsync();
        foreach (var s in items) s.IsActive = true;
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkDeletePermanentlyAsync(List<int> ids)
    {
        var items = await _db.Suppliers.Where(s => ids.Contains(s.Id)).ToListAsync();
        _db.Suppliers.RemoveRange(items);
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<List<SupplierOption>> GetSupplierOptionsAsync()
    {
        return await _db.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SupplierOption { Id = s.Id, Name = s.Name })
            .ToListAsync();
    }

    public async Task<List<CompanyOption>> GetVendorCompanyOptionsAsync()
    {
        return await _db.Companies
            .Where(c => (c.Type == CompanyType.Vendor || c.Type == CompanyType.Supplier) && c.IsActive && !c.IsArchived)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyOption { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }

    public async Task<SupplierCompanyDetail?> GetCompanyBasicAsync(int companyId)
    {
        var c = await _db.Companies
            .Where(co => co.Id == companyId)
            .Select(co => new { co.Id, co.Name, co.Phone, co.Email, co.Website })
            .FirstOrDefaultAsync();
        return c is null ? null : new SupplierCompanyDetail { Id = c.Id, Name = c.Name, Phone = c.Phone, Email = c.Email, Website = c.Website };
    }
}
