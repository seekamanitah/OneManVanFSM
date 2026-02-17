namespace OneManVanFSM.Shared.Models;

public class Payment
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? Reference { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
}

public enum PaymentMethod
{
    Cash,
    Check,
    Card,
    ACH,
    Zelle,
    Other
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Refunded,
    Failed
}
