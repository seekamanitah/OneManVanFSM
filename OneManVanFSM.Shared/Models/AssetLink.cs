namespace OneManVanFSM.Shared.Models;

public class AssetLink
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int LinkedAssetId { get; set; }
    public Asset? LinkedAsset { get; set; }
    public string? LinkType { get; set; } // "Split System", "Packaged Unit", etc.
    public string? Label { get; set; } // Friendly name e.g. "Upstairs HVAC System"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
