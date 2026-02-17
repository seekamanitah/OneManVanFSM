namespace OneManVanFSM.Shared.Models;

public class Expense
{
    public int Id { get; set; }
    public string? Category { get; set; } // Materials, Fuel, Tools, Disposal, Supplies, etc.
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; } // Amount + TaxAmount
    public string? PaymentMethod { get; set; } // Cash, Check, Card, ACH, Zelle, etc.
    public bool IsBillable { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;
    public string? VendorName { get; set; } // Manual vendor name entry
    public string? ReceiptNumber { get; set; } // Receipt/Invoice Number
    public string? ReceiptPath { get; set; } // File path for receipt attachment
    public string? Notes { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Now;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }

    // Navigation properties
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public List<ExpenseLine> Lines { get; set; } = [];
}

public enum ExpenseStatus
{
    Pending,
    Approved,
    Reimbursed,
    Denied
}
