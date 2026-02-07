namespace OneManVanFSM.Shared.Models;

public class JobAsset
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job? Job { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string? Role { get; set; } // Serviced, Installed, Replaced, Inspected, Diagnosed, Decommissioned
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
