namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IMaterialListService
{
    Task<List<MaterialListListItem>> GetListsAsync(MaterialListFilter? filter = null);
    Task<MaterialListDetail?> GetListAsync(int id);
    Task<MaterialList> CreateListAsync(MaterialListEditModel model);
    Task<MaterialList> UpdateListAsync(int id, MaterialListEditModel model);
    Task<bool> ArchiveListAsync(int id);
    Task<MaterialListItemDto> AddItemAsync(int listId, MaterialListItemEditModel model);
    Task<MaterialListItemDto> UpdateItemAsync(int listId, int itemId, MaterialListItemEditModel model);
    Task<bool> RemoveItemAsync(int listId, int itemId);

    // Template cloning
    Task<MaterialList> CloneFromTemplateAsync(int templateListId, string newName);

    // Status workflow
    Task<bool> UpdateStatusAsync(int id, MaterialListStatus status);

    // Product picker for item add
    Task<List<MaterialProductOption>> GetProductOptionsAsync(string? search = null);

    // Inventory stock check
    Task<List<MaterialStockCheck>> CheckInventoryStockAsync(int listId);

    // HVAC auto-pairings
    Task<List<ItemAssociation>> GetPairingsAsync(string itemName, string tradeType = "HVAC");

    // Convert to Estimate
    Task<int> ConvertToEstimateAsync(int listId, string? estimateTitle = null);

    // Job options for linkage dropdown
    Task<List<MaterialJobOption>> GetJobOptionsAsync();
}

public class MaterialListFilter
{
    public string? Search { get; set; }
    public bool? IsTemplate { get; set; }
    public MaterialListStatus? Status { get; set; }
    public string? TradeType { get; set; }
    public bool ShowArchived { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class MaterialListListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsTemplate { get; set; }
    public MaterialListStatus Status { get; set; }
    public string? TradeType { get; set; }
    public PricingMethod PricingMethod { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal GrandTotal { get; set; }
    public int ItemCount { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
    public string? JobNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MaterialListDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsTemplate { get; set; }
    public MaterialListStatus Status { get; set; }
    public string? TradeType { get; set; }
    public PricingMethod PricingMethod { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? ExternalNotes { get; set; }
    public string? PONumber { get; set; }
    public int? JobId { get; set; }
    public string? JobNumber { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public string? SiteAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MaterialListItemDto> Items { get; set; } = [];
}

public class MaterialListItemDto
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
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? InventoryItemId { get; set; }
    public decimal? InventoryStockQty { get; set; }
    public string? Notes { get; set; }
}

public class MaterialListEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "List name is required.")]
    public string Name { get; set; } = string.Empty;
    public bool IsTemplate { get; set; }
    public MaterialListStatus Status { get; set; } = MaterialListStatus.Draft;
    public string? TradeType { get; set; } = "HVAC";
    public PricingMethod PricingMethod { get; set; } = PricingMethod.FlatRate;
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? ExternalNotes { get; set; }
    public string? PONumber { get; set; }
    public int? JobId { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
}

public class MaterialListItemEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Item name is required.")]
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
    public int SortOrder { get; set; }
}

public class MaterialProductOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal MarkupPercent { get; set; }
    public int? InventoryItemId { get; set; }
    public decimal? StockQty { get; set; }
}

public class MaterialStockCheck
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal RequestedQty { get; set; }
    public decimal AvailableQty { get; set; }
    public bool IsLow => AvailableQty < RequestedQty;
}

public class MaterialJobOption
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Display => string.IsNullOrEmpty(Title) ? JobNumber : $"{JobNumber} – {Title}";
}
