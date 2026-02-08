namespace OneManVanFSM.Shared.Models;

public class ExpenseLine
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public Expense? Expense { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public int? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; } // Material, Labor, Equipment, Fee, Fuel, Disposal
    public string? Unit { get; set; } // Each, Hour, Ft, Box, Roll, Gallon
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
