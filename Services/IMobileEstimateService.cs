using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileEstimateService
{
    Task<List<MobileEstimateCard>> GetEstimatesAsync(MobileEstimateFilter? filter = null);
    Task<MobileEstimateDetail?> GetEstimateDetailAsync(int id);
}

public class MobileEstimateCard
{
    public int Id { get; set; }
    public string EstimateNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public EstimateStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteAddress { get; set; }
    public decimal Total { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LineCount { get; set; }
}

public class MobileEstimateDetail
{
    public int Id { get; set; }
    public string EstimateNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public EstimateStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public string? TradeType { get; set; }
    public string? SystemType { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? SiteName { get; set; }
    public string? SiteAddress { get; set; }
    public int? SiteId { get; set; }
    public int? CustomerId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal Total { get; set; }
    public decimal? DepositRequired { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MobileEstimateLine> Lines { get; set; } = [];
}

public class MobileEstimateLine
{
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Section { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Unit { get; set; }
}

public class MobileEstimateFilter
{
    public string? Search { get; set; }
    public EstimateStatus? Status { get; set; }
}
