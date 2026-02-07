namespace OneManVanFSM.Shared.Models;

public class Expense
{
    public int Id { get; set; }
    public string? Category { get; set; } // Fuel, Tools, Disposal, Supplies, etc.
    public decimal Amount { get; set; }
    public bool IsBillable { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;
    public string? Description { get; set; }
    public string? ReceiptPath { get; set; }
    public string? Notes { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
}

public enum ExpenseStatus
{
    Pending,
    Approved,
    Reimbursed,
    Denied
}
