namespace OneManVanFSM.Web.Services;

public interface ISupplierService
{
    Task<List<SupplierListItem>> GetSuppliersAsync(SupplierFilter? filter = null);
    Task<SupplierDetail?> GetSupplierAsync(int id);
    Task<SupplierListItem> CreateSupplierAsync(SupplierEditModel model);
    Task<SupplierListItem> UpdateSupplierAsync(int id, SupplierEditModel model);
    Task<bool> ArchiveSupplierAsync(int id);
    Task<bool> RestoreSupplierAsync(int id);
    Task<bool> DeleteSupplierPermanentlyAsync(int id);
    Task<int> BulkArchiveAsync(List<int> ids);
    Task<int> BulkRestoreAsync(List<int> ids);
    Task<int> BulkDeletePermanentlyAsync(List<int> ids);
    Task<List<SupplierOption>> GetSupplierOptionsAsync();
    Task<List<CompanyOption>> GetVendorCompanyOptionsAsync();
    Task<SupplierCompanyDetail?> GetCompanyBasicAsync(int companyId);
}

public class SupplierCompanyDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
}

public class SupplierFilter
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
    public bool ShowArchived { get; set; }
}

public class SupplierListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AccountNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public bool IsActive { get; set; }
}

public class SupplierDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? AccountNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }

    // Linked items
    public List<SupplierLinkedProduct> LinkedProducts { get; set; } = [];
    public List<SupplierLinkedInventory> LinkedInventory { get; set; } = [];
}

public class SupplierLinkedProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? PartNumber { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
}

public class SupplierLinkedInventory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }
}

public class SupplierEditModel
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? AccountNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional link to an existing Company. When null and CreateCompanyRecord is true,
    /// a new Company of type Supplier is auto-created from the supplier fields.
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>
    /// When true, automatically create or update a linked Company record.
    /// </summary>
    public bool CreateCompanyRecord { get; set; } = true;
}

public class SupplierOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
