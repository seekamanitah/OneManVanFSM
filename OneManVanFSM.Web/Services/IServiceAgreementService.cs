namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IServiceAgreementService
{
    Task<List<AgreementListItem>> GetAgreementsAsync(AgreementFilter? filter = null);
    Task<AgreementFullDetail?> GetAgreementAsync(int id);
    Task<ServiceAgreement> CreateAgreementAsync(AgreementEditModel model);
    Task<ServiceAgreement> UpdateAgreementAsync(int id, AgreementEditModel model);
    Task<bool> ArchiveAgreementAsync(int id);
    Task<bool> RestoreAgreementAsync(int id);
    Task<bool> DeleteAgreementPermanentlyAsync(int id);
    Task<int> BulkArchiveAgreementsAsync(List<int> ids);
    Task<int> BulkRestoreAgreementsAsync(List<int> ids);
    Task<int> BulkDeleteAgreementsPermanentlyAsync(List<int> ids);
    Task<int> GenerateAgreementJobsAsync();
    Task<int> UpdateAgreementStatusesAsync();
    Task<int> ProcessAutoRenewalsAsync();
}

public class AgreementFilter
{
    public string? Search { get; set; }
    public AgreementStatus? Status { get; set; }
    public CoverageLevel? CoverageLevel { get; set; }
    public string? SortBy { get; set; } = "EndDate";
    public bool SortDescending { get; set; }
    public bool ShowArchived { get; set; }
}

public class AgreementListItem
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? CompanyName { get; set; }
    public string? TradeType { get; set; }
    public string? BillingFrequency { get; set; }
    public CoverageLevel CoverageLevel { get; set; }
    public AgreementStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int VisitsIncluded { get; set; }
    public int VisitsUsed { get; set; }
    public decimal Fee { get; set; }
}

public class AgreementFullDetail
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public CoverageLevel CoverageLevel { get; set; }
    public AgreementStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int VisitsIncluded { get; set; }
    public int VisitsUsed { get; set; }
    public decimal Fee { get; set; }
    public string? TradeType { get; set; }
    public string? BillingFrequency { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTime? RenewalDate { get; set; }
    public bool AutoRenew { get; set; }
    public string? AddOns { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public List<CoveredAssetDto> CoveredAssets { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CoveredAssetDto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? CoverageNotes { get; set; }
}

public class AgreementEditModel
{
    public string AgreementNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Title is required.")]
    public string? Title { get; set; }

    public CoverageLevel CoverageLevel { get; set; } = CoverageLevel.Basic;
    public AgreementStatus Status { get; set; } = AgreementStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddYears(1);
    public int VisitsIncluded { get; set; }
    public int VisitsUsed { get; set; }
    public decimal Fee { get; set; }
    public string? TradeType { get; set; }
    public string? BillingFrequency { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTime? RenewalDate { get; set; }
    public bool AutoRenew { get; set; }
    public string? AddOns { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
}
