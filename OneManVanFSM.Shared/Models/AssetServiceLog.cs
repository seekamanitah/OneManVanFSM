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
    // Refrigerant tracking (EPA compliance)
    public string? RefrigerantType { get; set; } // R-410A, R-22, R-32, etc.
    public decimal? RefrigerantAmountAdded { get; set; } // lbs
    public decimal? RefrigerantBeforeReading { get; set; } // psi or lbs
    public decimal? RefrigerantAfterReading { get; set; } // psi or lbs
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
