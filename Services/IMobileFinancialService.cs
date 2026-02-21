using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileFinancialService
{
    Task<MobileFinancialDashboard> GetDashboardAsync();
    Task<List<MobileRecentPayment>> GetRecentPaymentsAsync(int count = 10);
    Task<List<MobileAgingInvoice>> GetAgingInvoicesAsync();
    Task<MobileMonthlyBreakdown> GetMonthlyBreakdownAsync(int? year = null, int? month = null);
}

public class MobileFinancialDashboard
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int TotalInvoiceCount { get; set; }
    public int OverdueInvoiceCount { get; set; }
    public int DraftInvoiceCount { get; set; }
    public int PendingExpenseCount { get; set; }
    public int TotalExpenseCount { get; set; }
    public decimal TotalPaymentsReceived { get; set; }
    public int TotalPaymentCount { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal ExpensesThisMonth { get; set; }
    public decimal ProfitThisMonth { get; set; }
}

public class MobileRecentPayment
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? Reference { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class MobileAgingInvoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime? DueDate { get; set; }
    public int DaysOverdue { get; set; }
}

public class MobileMonthlyBreakdown
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public int InvoiceCount { get; set; }
    public int ExpenseCount { get; set; }
    public int PaymentCount { get; set; }
    public List<MobileCategoryBreakdown> ExpensesByCategory { get; set; } = [];
}

public class MobileCategoryBreakdown
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}
