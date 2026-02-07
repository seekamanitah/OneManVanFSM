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
    public decimal MarkupAmount { get; set; }
    public decimal Total { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DepositApplied { get; set; }
    public decimal BalanceDue { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
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
