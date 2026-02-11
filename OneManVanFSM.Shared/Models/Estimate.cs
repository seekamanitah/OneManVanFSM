namespace OneManVanFSM.Shared.Models;

public class Estimate
{
    public int Id { get; set; }
    public string EstimateNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public EstimateStatus Status { get; set; } = EstimateStatus.Draft;
    public JobPriority Priority { get; set; } = JobPriority.Standard;
    public string? TradeType { get; set; } // HVAC, Plumbing, Electrical
    public int VersionNumber { get; set; } = 1;
    public string? SystemType { get; set; }
    public int? SqFt { get; set; }
    public int? Zones { get; set; }
    public int? Stories { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public PricingMethod PricingMethod { get; set; } = PricingMethod.TimeBased;
    public decimal Subtotal { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public decimal Total { get; set; }
    public decimal? DepositRequired { get; set; }
    public bool? DepositReceived { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public bool NeedsReview { get; set; }
    public string? CreatedFrom { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    public int? MaterialListId { get; set; }
    public MaterialList? MaterialList { get; set; }
    public ICollection<EstimateLine> Lines { get; set; } = [];
    public Job? Job { get; set; }
}

public enum EstimateStatus
{
    Draft,
    Sent,
    Approved,
    Rejected,
    Expired
}

public enum PricingMethod
{
    TimeBased,
    FlatRate,
    Hybrid
}
