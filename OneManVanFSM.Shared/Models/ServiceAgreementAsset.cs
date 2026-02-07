namespace OneManVanFSM.Shared.Models;

public class ServiceAgreementAsset
{
    public int Id { get; set; }
    public int ServiceAgreementId { get; set; }
    public ServiceAgreement? ServiceAgreement { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string? CoverageNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
