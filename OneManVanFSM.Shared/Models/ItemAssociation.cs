namespace OneManVanFSM.Shared.Models;

/// <summary>
/// Configuration table for HVAC auto-pairings (e.g., adding flex duct suggests a matching boot, takeoff, and collar).
/// TradeType allows future expansion to plumbing/electrical pairings.
/// </summary>
public class ItemAssociation
{
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty; // e.g., "Flex Duct 6\""
    public string AssociatedItemName { get; set; } = string.Empty; // e.g., "Floor Boot 4x10x6"
    public string? AssociatedSection { get; set; } // e.g., "Boots" — the section the associated item belongs to
    public decimal Ratio { get; set; } = 1; // 1:1 default (1 flex run = 1 boot)
    public string TradeType { get; set; } = "HVAC"; // HVAC, Plumbing, Electrical
    public bool IsActive { get; set; } = true;
}
