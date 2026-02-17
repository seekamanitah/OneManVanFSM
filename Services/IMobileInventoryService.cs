using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileInventoryService
{
    Task<List<MobileInventoryItem>> GetInventoryAsync(MobileInventoryFilter? filter = null);
    Task<MobileInventoryStats> GetStatsAsync();
    Task<MobileInventoryDetail?> GetItemDetailAsync(int id);
    Task AdjustQuantityAsync(int itemId, decimal delta);
    Task<bool> UpdateItemAsync(int id, MobileInventoryUpdate model);
    Task<InventoryItem> QuickCreateAsync(MobileInventoryQuickCreate model);
}

public class MobileInventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? ShelfBin { get; set; }
    public InventoryLocation Location { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsLowStock => Quantity <= MinThreshold && MinThreshold > 0;
    public bool IsExpiringSoon => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now.AddDays(30);
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;
}

public class MobileInventoryFilter
{
    public string? Search { get; set; }
    public InventoryLocation? Location { get; set; }
    public bool? LowStockOnly { get; set; }
}

public class MobileInventoryStats
{
    public int TotalItems { get; set; }
    public int LowStockCount { get; set; }
    public int TruckItems { get; set; }
    public decimal TotalValue { get; set; }
}

public class MobileInventoryQuickCreate
{
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public InventoryLocation Location { get; set; } = InventoryLocation.Warehouse;
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public string? ShelfBin { get; set; }
    public string? Notes { get; set; }
}

public class MobileInventoryDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? PartNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public string? ShelfBin { get; set; }
    public string? PreferredSupplier { get; set; }
    public InventoryLocation Location { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal MaxCapacity { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MarkupPercent { get; set; }
    public DateTime? LastRestockedDate { get; set; }
    public string? Notes { get; set; }
    public string? ProductName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MobileInventoryUpdate
{
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public string? ShelfBin { get; set; }
    public InventoryLocation Location { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public string? Notes { get; set; }
}
