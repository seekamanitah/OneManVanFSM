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
    Task<bool> RestoreEstimateAsync(int id);
    Task<bool> DeleteEstimatePermanentlyAsync(int id);
    Task<int> BulkArchiveEstimatesAsync(List<int> ids);
    Task<int> BulkRestoreEstimatesAsync(List<int> ids);
    Task<int> BulkDeleteEstimatesPermanentlyAsync(List<int> ids);
    Task<List<ProductOption>> GetProductOptionsAsync();
}

public class EstimateFilter
{
    public string? Search { get; set; }
    public EstimateStatus? Status { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool ShowArchived { get; set; }
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
    public decimal? DepositAmountPaid { get; set; }
    public PaymentMethod? DepositPaymentMethod { get; set; }
    public string? DepositPaymentReference { get; set; }
    public DateTime? DepositReceivedDate { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerCity { get; set; }
    public string? CustomerState { get; set; }
    public string? CustomerZip { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public string? SiteAddress { get; set; }
    public int? MaterialListId { get; set; }
    public string? MaterialListName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? LinkedJobId { get; set; }
    public string? LinkedJobNumber { get; set; }
    public List<EstimateLineDto> Lines { get; set; } = [];
}

public class EstimateLineDto
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
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
    public decimal? DepositAmountPaid { get; set; }
    public PaymentMethod? DepositPaymentMethod { get; set; }
    public string? DepositPaymentReference { get; set; }
    public DateTime? DepositReceivedDate { get; set; }
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
    public int? AssetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Unit { get; set; }
    public string? Section { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
