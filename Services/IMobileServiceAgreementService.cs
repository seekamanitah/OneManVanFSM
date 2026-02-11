using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileServiceAgreementService
{
    Task<List<MobileAgreementCard>> GetAgreementsAsync(string? statusFilter = null, string? search = null);
    Task<MobileAgreementDetail?> GetAgreementDetailAsync(int id);
    Task<ServiceAgreement> QuickCreateAsync(MobileAgreementQuickCreate model);
}

public class MobileAgreementCard
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = "";
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
    public CoverageLevel CoverageLevel { get; set; }
    public AgreementStatus Status { get; set; }
    public DateTime EndDate { get; set; }
    public int VisitsIncluded { get; set; }
    public int VisitsUsed { get; set; }
    public int VisitsRemaining => Math.Max(VisitsIncluded - VisitsUsed, 0);
    public decimal Fee { get; set; }
}

public class MobileAgreementDetail
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = "";
    public string? Title { get; set; }
    public CoverageLevel CoverageLevel { get; set; }
    public AgreementStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int VisitsIncluded { get; set; }
    public int VisitsUsed { get; set; }
    public int VisitsRemaining => Math.Max(VisitsIncluded - VisitsUsed, 0);
    public decimal Fee { get; set; }
    public string? TradeType { get; set; }
    public string? BillingFrequency { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTime? RenewalDate { get; set; }
    public bool AutoRenew { get; set; }
    public string? Notes { get; set; }

    // Customer & Site
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public string? SiteAddress { get; set; }

    // Covered Assets
    public List<MobileAgreementAsset> Assets { get; set; } = [];
}

public class MobileAgreementAsset
{
    public int AssetId { get; set; }
    public string AssetName { get; set; } = "";
    public string? AssetType { get; set; }
    public string? CoverageNotes { get; set; }
}

public class MobileAgreementQuickCreate
{
    public string? Title { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
    public CoverageLevel CoverageLevel { get; set; } = CoverageLevel.Basic;
    public string? TradeType { get; set; }
    public int VisitsIncluded { get; set; } = 2;
    public decimal Fee { get; set; }
    public string? BillingFrequency { get; set; } = "Annual";
    public string? Notes { get; set; }
}
