using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileMaterialListService
{
    Task<List<MobileMaterialListCard>> GetListsAsync(MobileMaterialListFilter? filter = null);
    Task<MobileMaterialListDetail?> GetListDetailAsync(int id);
    Task<int> CreateListAsync(MobileMaterialListCreate model);
    Task<bool> UpdateListAsync(int id, MobileMaterialListUpdate model);
    Task<bool> UpdateStatusAsync(int id, MaterialListStatus status);
    Task<bool> DeleteListAsync(int id);
    Task<MobileMaterialListStats> GetStatsAsync();

    // Line items
    Task<int> AddItemAsync(int listId, MobileMaterialListItemCreate model);
    Task<bool> UpdateItemAsync(int listId, int itemId, MobileMaterialListItemUpdate model);
    Task<bool> RemoveItemAsync(int listId, int itemId);

    // Product picker
    Task<List<MobileMaterialProductOption>> GetProductOptionsAsync(string? search = null);

    // Job picker
    Task<List<MobileJobOption>> GetJobOptionsAsync();
}

public class MobileMaterialListFilter
{
    public string? Search { get; set; }
    public MaterialListStatus? Status { get; set; }
    public bool? IsTemplate { get; set; }
}

public class MobileMaterialListCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsTemplate { get; set; }
    public MaterialListStatus Status { get; set; }
    public string? TradeType { get; set; }
    public decimal GrandTotal { get; set; }
    public int ItemCount { get; set; }
    public string? CustomerName { get; set; }
    public string? JobNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MobileMaterialListDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsTemplate { get; set; }
    public MaterialListStatus Status { get; set; }
    public string? TradeType { get; set; }
    public PricingMethod PricingMethod { get; set; }
    public decimal Subtotal { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public string? PONumber { get; set; }
    public int? JobId { get; set; }
    public string? JobNumber { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MobileMaterialListItemDto> Items { get; set; } = [];
}

public class MobileMaterialListItemDto
{
    public int Id { get; set; }
    public string? Section { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal BaseCost { get; set; }
    public decimal LaborHours { get; set; }
    public decimal FlatPrice { get; set; }
    public decimal MarkupPercent { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? ProductName { get; set; }
}

public class MobileMaterialListCreate
{
    public string Name { get; set; } = string.Empty;
    public bool IsTemplate { get; set; }
    public string? TradeType { get; set; } = "HVAC";
    public PricingMethod PricingMethod { get; set; } = PricingMethod.FlatRate;
    public string? Notes { get; set; }
    public string? PONumber { get; set; }
    public int? JobId { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
}

public class MobileMaterialListUpdate
{
    public string Name { get; set; } = string.Empty;
    public string? TradeType { get; set; }
    public PricingMethod PricingMethod { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public string? Notes { get; set; }
    public string? PONumber { get; set; }
    public int? JobId { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
}

public class MobileMaterialListItemCreate
{
    public string ItemName { get; set; } = string.Empty;
    public string Section { get; set; } = "General";
    public int Quantity { get; set; } = 1;
    public string? Unit { get; set; } = "ea";
    public decimal BaseCost { get; set; }
    public decimal LaborHours { get; set; }
    public decimal FlatPrice { get; set; }
    public decimal MarkupPercent { get; set; }
    public string? Notes { get; set; }
    public int? ProductId { get; set; }
    public int? InventoryItemId { get; set; }
}

public class MobileMaterialListItemUpdate
{
    public string ItemName { get; set; } = string.Empty;
    public string? Section { get; set; }
    public int Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal BaseCost { get; set; }
    public decimal LaborHours { get; set; }
    public decimal FlatPrice { get; set; }
    public decimal MarkupPercent { get; set; }
    public string? Notes { get; set; }
}

public class MobileMaterialListStats
{
    public int TotalLists { get; set; }
    public int DraftCount { get; set; }
    public int TemplateCount { get; set; }
    public decimal TotalValue { get; set; }
}

public class MobileMaterialProductOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
}

public class MobileJobOption
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
}
