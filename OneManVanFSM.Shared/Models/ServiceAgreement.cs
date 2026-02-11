namespace OneManVanFSM.Shared.Models;

public class ServiceAgreement
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public CoverageLevel CoverageLevel { get; set; } = CoverageLevel.Basic;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int VisitsIncluded { get; set; }
    public int VisitsUsed { get; set; }
    public decimal Fee { get; set; }
    public string? TradeType { get; set; } // HVAC, Plumbing, Electrical
    public string? BillingFrequency { get; set; } // Monthly, Quarterly, Annual
    public decimal? DiscountPercent { get; set; }
    public DateTime? RenewalDate { get; set; }
    public bool AutoRenew { get; set; }
    public string? AddOns { get; set; } // JSON
    public AgreementStatus Status { get; set; } = AgreementStatus.Active;
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
    public ICollection<ServiceAgreementAsset> ServiceAgreementAssets { get; set; } = [];
}

public enum CoverageLevel
{
    Basic,
    Premium,
    Gold
}

public enum AgreementStatus
{
    Active,
    Expiring,
    Expired,
    Cancelled,
    Renewed
}
