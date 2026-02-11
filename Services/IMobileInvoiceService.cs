using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileInvoiceService
{
    Task<MobileInvoiceStats> GetStatsAsync();
    Task<List<MobileInvoiceCard>> GetInvoicesAsync(MobileInvoiceFilter? filter = null);
    Task<MobileInvoiceDetail?> GetInvoiceDetailAsync(int id);
    Task<Invoice> QuickCreateAsync(MobileInvoiceQuickCreate model);
}

public class MobileInvoiceStats
{
    public int TotalCount { get; set; }
    public int DraftCount { get; set; }
    public int OverdueCount { get; set; }
    public decimal TotalOutstanding { get; set; }
}

public class MobileInvoiceCard
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public string? CustomerName { get; set; }
    public string? JobNumber { get; set; }
    public decimal Total { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int LineCount { get; set; }
}

public class MobileInvoiceDetail
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? PaymentTerms { get; set; }
    public string? PricingType { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public bool NeedsReview { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public string? SiteAddress { get; set; }
    public int? JobId { get; set; }
    public string? JobNumber { get; set; }
    public string? JobTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MobileInvoiceLine> Lines { get; set; } = [];
    public List<MobileInvoicePayment> Payments { get; set; } = [];
}

public class MobileInvoiceLine
{
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class MobileInvoicePayment
{
    public decimal Amount { get; set; }
    public string? Method { get; set; }
    public string? Reference { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class MobileInvoiceFilter
{
    public string? Search { get; set; }
    public InvoiceStatus? Status { get; set; }
}

public class MobileInvoiceQuickCreate
{
    public int? CustomerId { get; set; }
    public int? JobId { get; set; }
    public int? SiteId { get; set; }
    public string? Notes { get; set; }
}
