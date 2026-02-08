namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IInventoryService
{
    Task<List<InventoryListItem>> GetItemsAsync(InventoryFilter? filter = null);
    Task<InventoryDetail?> GetItemAsync(int id);
    Task<InventoryItem> CreateItemAsync(InventoryEditModel model);
    Task<InventoryItem> UpdateItemAsync(int id, InventoryEditModel model);
    Task<bool> ArchiveItemAsync(int id);
    Task<InventoryDashboard> GetDashboardAsync();
}

public class InventoryFilter
{
    public string? Search { get; set; }
    public InventoryLocation? Location { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
    public bool LowStockOnly { get; set; }
}

public class InventoryListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public string? ProductName { get; set; }
    public InventoryLocation Location { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal MaxCapacity { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsLowStock => Quantity <= MinThreshold;
}

public class InventoryDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string? ShelfBin { get; set; }
    public string? PreferredSupplier { get; set; }
    public InventoryLocation Location { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal MaxCapacity { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MarkupPercent { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? LastRestockedDate { get; set; }
    public string? Notes { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InventoryEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Item name is required.")]
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string? ShelfBin { get; set; }
    public string? PreferredSupplier { get; set; }
    public InventoryLocation Location { get; set; } = InventoryLocation.Warehouse;
    public decimal Quantity { get; set; }
    public decimal MinThreshold { get; set; }
    public decimal MaxCapacity { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MarkupPercent { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public int? ProductId { get; set; }
}

public class InventoryDashboard
{
    public int TotalItems { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringCount { get; set; }
    public decimal TotalValue { get; set; }
}
