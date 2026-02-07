namespace OneManVanFSM.Shared.Models;

public class AssetServiceLog
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string ServiceType { get; set; } = string.Empty; // Filter Change, Refrigerant Charge, Coil Cleaning, etc.
    public DateTime ServiceDate { get; set; }
    public string? PerformedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime? NextDueDate { get; set; }
    public decimal? Cost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
