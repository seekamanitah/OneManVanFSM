namespace OneManVanFSM.Shared.Models;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime? InvoiceDate { get; set; } // When invoice was issued
    public DateTime? DueDate { get; set; }
    public string? PaymentTerms { get; set; } // Net 30, Due on Receipt, etc.
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; } // Tax percentage, e.g. 7.0
    public bool TaxIncludedInPrice { get; set; } // Flat rate with tax already included
    public string? PricingType { get; set; } // "Material & Labor (Itemized)", "Flat Rate", "Time & Materials"
    public decimal MarkupAmount { get; set; }
    public decimal Total { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DepositApplied { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; } // Separate payment terms text (e.g. "Payment due within 30 days")
    public bool IncludeSiteLocation { get; set; } = true;
    public bool IncludeAssetInfo { get; set; } = true;
    public bool IncludeJobDescription { get; set; }
    public bool IncludeNotes { get; set; } = true;
    public bool HideLineItemPrices { get; set; }
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
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}

public enum InvoiceStatus
{
    Draft,
    Sent,
    Invoiced,
    Paid,
    Overdue,
    Void
}
