using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileSupplierService
{
    Task<List<MobileSupplierCard>> GetSuppliersAsync(MobileSupplierFilter? filter = null);
    Task<MobileSupplierDetail?> GetSupplierDetailAsync(int id);
    Task<int> CreateSupplierAsync(MobileSupplierCreate model);
    Task<bool> UpdateSupplierAsync(int id, MobileSupplierUpdate model);
    Task<bool> ArchiveSupplierAsync(int id);
    Task<bool> RestoreSupplierAsync(int id);
    Task<MobileSupplierStats> GetStatsAsync();
}

public class MobileSupplierFilter
{
    public string? Search { get; set; }
    public bool ShowArchived { get; set; }
}

public class MobileSupplierCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AccountNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public bool IsActive { get; set; }
    public int LinkedProductCount { get; set; }
    public int LinkedInventoryCount { get; set; }
}

public class MobileSupplierDetail
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
    public string? CompanyName { get; set; }
    public List<MobileSupplierLinkedProduct> Products { get; set; } = [];
    public List<MobileSupplierLinkedInventory> Inventory { get; set; } = [];
}

public class MobileSupplierLinkedProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
}

public class MobileSupplierLinkedInventory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }
}

public class MobileSupplierCreate
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? AccountNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
}

public class MobileSupplierUpdate
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
}

public class MobileSupplierStats
{
    public int TotalSuppliers { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}
