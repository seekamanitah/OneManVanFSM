namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IEstimateService
{
    Task<List<EstimateListItem>> GetEstimatesAsync(EstimateFilter? filter = null);
    Task<EstimateDetail?> GetEstimateAsync(int id);
    Task<Estimate> CreateEstimateAsync(EstimateEditModel model);
    Task<Estimate> UpdateEstimateAsync(int id, EstimateEditModel model);
    Task<bool> UpdateStatusAsync(int id, EstimateStatus status);
    Task<bool> ArchiveEstimateAsync(int id);
    Task<List<ProductOption>> GetProductOptionsAsync();
}

public class EstimateFilter
{
    public string? Search { get; set; }
    public EstimateStatus? Status { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class EstimateListItem
{
    public int Id { get; set; }
    public string EstimateNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
    public EstimateStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public PricingMethod PricingMethod { get; set; }
    public decimal Total { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EstimateDetail
{
    public int Id { get; set; }
    public string EstimateNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public EstimateStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public string? TradeType { get; set; }
    public int VersionNumber { get; set; }
    public PricingMethod PricingMethod { get; set; }
    public int? SqFt { get; set; }
    public int? Zones { get; set; }
    public int? Stories { get; set; }
    public string? SystemType { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public decimal Total { get; set; }
    public decimal? DepositRequired { get; set; }
    public bool? DepositReceived { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public int? MaterialListId { get; set; }
    public string? MaterialListName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<EstimateLineDto> Lines { get; set; } = [];
}

public class EstimateLineDto
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Unit { get; set; }
    public string? Section { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
}

public class EstimateEditModel
{
    public string EstimateNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Title is required.")]
    public string? Title { get; set; }

    public EstimateStatus Status { get; set; } = EstimateStatus.Draft;
    public JobPriority Priority { get; set; } = JobPriority.Standard;
    public string? TradeType { get; set; }
    public PricingMethod PricingMethod { get; set; } = PricingMethod.FlatRate;
    public DateTime? ExpiryDate { get; set; }
    public decimal? DepositRequired { get; set; }
    public bool? DepositReceived { get; set; }
    public int? SqFt { get; set; }
    public int? Zones { get; set; }
    public int? Stories { get; set; }
    public string? SystemType { get; set; }
    public decimal Subtotal { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal ContingencyPercent { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public int? MaterialListId { get; set; }
    public List<EstimateLineEditModel> Lines { get; set; } = [];
}

public class EstimateLineEditModel
{
    public int? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Unit { get; set; }
    public string? Section { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
