namespace OneManVanFSM.Shared.Models;

public class InvoiceLine
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; } // Labor, Material, Equipment, Fee, Discount
    public string? Unit { get; set; } // Each, Hour, Ft, Box, Roll
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
}
