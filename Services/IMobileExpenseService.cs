using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileExpenseService
{
    Task<MobileExpenseStats> GetStatsAsync();
    Task<List<MobileExpenseCard>> GetExpensesAsync(MobileExpenseFilter? filter = null);
    Task<MobileExpenseDetail?> GetExpenseDetailAsync(int id);
    Task<int> CreateExpenseAsync(MobileExpenseCreate model);
}

public class MobileExpenseStats
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public decimal TotalThisMonth { get; set; }
    public decimal BillableAmount { get; set; }
}

public class MobileExpenseCard
{
    public int Id { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public decimal Total { get; set; }
    public ExpenseStatus Status { get; set; }
    public bool IsBillable { get; set; }
    public string? VendorName { get; set; }
    public string? JobNumber { get; set; }
    public string? CustomerName { get; set; }
    public DateTime ExpenseDate { get; set; }
    public int LineCount { get; set; }
}

public class MobileExpenseDetail
{
    public int Id { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? PaymentMethod { get; set; }
    public ExpenseStatus Status { get; set; }
    public bool IsBillable { get; set; }
    public string? VendorName { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime ExpenseDate { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int? JobId { get; set; }
    public string? JobNumber { get; set; }
    public string? JobTitle { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MobileExpenseLine> Lines { get; set; } = [];
}

public class MobileExpenseLine
{
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class MobileExpenseCreate
{
    public string? Category { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public string? PaymentMethod { get; set; } = "Cash";
    public bool IsBillable { get; set; }
    public string? VendorName { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public int? JobId { get; set; }
    public int? CustomerId { get; set; }
}

public class MobileExpenseFilter
{
    public string? Search { get; set; }
    public ExpenseStatus? Status { get; set; }
    public string? Category { get; set; }
}
