using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileInventoryService
{
    Task<List<MobileInventoryItem>> GetInventoryAsync(MobileInventoryFilter? filter = null);
    Task<MobileInventoryStats> GetStatsAsync();
    Task AdjustQuantityAsync(int itemId, decimal delta);
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
    public bool IsExpiringSoon => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow.AddDays(30);
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
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
