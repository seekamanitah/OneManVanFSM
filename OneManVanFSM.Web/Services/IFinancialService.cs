namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IFinancialService
{
    // Invoices
    Task<List<InvoiceListItem>> GetInvoicesAsync(InvoiceFilter? filter = null);
    Task<InvoiceDetail?> GetInvoiceAsync(int id);
    Task<Invoice> CreateInvoiceAsync(InvoiceEditModel model);
    Task<Invoice> UpdateInvoiceAsync(int id, InvoiceEditModel model);
    Task<bool> UpdateInvoiceStatusAsync(int id, InvoiceStatus status);
    Task<bool> ArchiveInvoiceAsync(int id);
    Task<bool> RestoreInvoiceAsync(int id);
    Task<bool> DeleteInvoicePermanentlyAsync(int id);
    Task<int> BulkArchiveInvoicesAsync(List<int> ids);
    Task<int> BulkRestoreInvoicesAsync(List<int> ids);
    Task<int> BulkDeleteInvoicesPermanentlyAsync(List<int> ids);
    // Expenses
    Task<List<ExpenseListItem>> GetExpensesAsync(ExpenseFilter? filter = null);
    Task<ExpenseDetail?> GetExpenseAsync(int id);
    Task<Expense> CreateExpenseAsync(ExpenseEditModel model);
    Task<Expense> UpdateExpenseAsync(int id, ExpenseEditModel model);
    Task<bool> UpdateExpenseStatusAsync(int id, ExpenseStatus status);
    Task<bool> DeleteExpensePermanentlyAsync(int id);
    Task<int> BulkDeleteExpensesPermanentlyAsync(List<int> ids);
    // Payments
    Task<List<PaymentListItem>> GetPaymentsAsync(PaymentFilter? filter = null);
    Task<Payment> CreatePaymentAsync(PaymentEditModel model);
    // Dashboard
    Task<FinancialDashboard> GetDashboardAsync();
    // Products for line items
    Task<List<ProductOption>> GetProductOptionsAsync();
    // Company options for expense vendor dropdown
    Task<List<CompanyOption>> GetCompanyOptionsAsync();
}

public class CompanyOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Invoice DTOs
public class InvoiceFilter
{
    public string? Search { get; set; }
    public InvoiceStatus? Status { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public bool ShowArchived { get; set; }
}

public class InvoiceListItem
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public InvoiceStatus Status { get; set; }
    public decimal Total { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InvoiceDetail
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
    public bool TaxIncludedInPrice { get; set; }
    public decimal MarkupAmount { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DepositApplied { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public bool IncludeSiteLocation { get; set; }
    public bool IncludeAssetInfo { get; set; }
    public bool IncludeJobDescription { get; set; }
    public bool IncludeNotes { get; set; }
    public bool HideLineItemPrices { get; set; }
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
    public string? SiteCity { get; set; }
    public string? SiteState { get; set; }
    public string? SiteZip { get; set; }
    public int? JobId { get; set; }
    public string? JobNumber { get; set; }
    public string? JobTitle { get; set; }
    public string? JobDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PaymentListItem> Payments { get; set; } = [];
    public List<InvoiceLineDto> Lines { get; set; } = [];
    public List<InvoiceAssetInfo> SiteAssets { get; set; } = [];
}

public class InvoiceLineDto
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
}

public class InvoiceAssetInfo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetType { get; set; }
    public DateTime? LastServiceDate { get; set; }
}

public class InvoiceLineEditModel
{
    public int? ProductId { get; set; }
    public int? AssetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}

public class ProductOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public string? Unit { get; set; }
}

public class InvoiceEditModel
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? PaymentTerms { get; set; }
    public string? PricingType { get; set; } = "Material & Labor (Itemized)";
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public bool TaxIncludedInPrice { get; set; } = true;
    public decimal MarkupAmount { get; set; }
    public string? DiscountType { get; set; } // "Percent" or "Dollar"
    public decimal DiscountValue { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DepositApplied { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
    public int? JobId { get; set; }
    public int? SiteId { get; set; }
    public bool IncludeSiteLocation { get; set; } = true;
    public bool IncludeAssetInfo { get; set; } = true;
    public bool IncludeJobDescription { get; set; }
    public bool IncludeNotes { get; set; } = true;
    public bool HideLineItemPrices { get; set; }
    public List<InvoiceLineEditModel> Lines { get; set; } = [];
}

// Expense DTOs
public class ExpenseFilter
{
    public string? Search { get; set; }
    public ExpenseStatus? Status { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class ExpenseListItem
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
    public string? CompanyName { get; set; }
    public string? EmployeeName { get; set; }
    public int? JobId { get; set; }
    public string? JobTitle { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime ExpenseDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpenseDetail
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
    public string? JobTitle { get; set; }
    public string? JobNumber { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ExpenseLineDto> Lines { get; set; } = [];
}

public class ExpenseEditModel
{
    public string? Category { get; set; }
    public string? Description { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive.")]
    public decimal Amount { get; set; }

    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; } // Percentage (e.g., 9.75 for 9.75%)
    public bool TaxEnabled { get; set; } = true;
    public decimal Total { get; set; }
    public string? PaymentMethod { get; set; } = "Cash";
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;
    public bool IsBillable { get; set; }
    public string? VendorName { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Now;
    public int? EmployeeId { get; set; }
    public int? JobId { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
    public int? InvoiceId { get; set; }
    public List<ExpenseLineEditModel> Lines { get; set; } = [];
}

public class ExpenseLineEditModel
{
    public int? ProductId { get; set; }
    public int? InventoryItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class ExpenseLineDto
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? InventoryItemId { get; set; }
    public string? InventoryItemName { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineType { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
public class PaymentFilter
{
    public string? Search { get; set; }
    public PaymentStatus? Status { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class PaymentListItem
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentEditModel
{
    [System.ComponentModel.DataAnnotations.Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive.")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public int InvoiceId { get; set; }
}

// Financial Dashboard
public class FinancialDashboard
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
    public int TotalPaymentCount { get; set; }
    public decimal TotalPaymentsReceived { get; set; }
    public List<PaymentListItem> RecentPayments { get; set; } = [];
    public List<InvoiceListItem> RecentInvoices { get; set; } = [];
    public List<ExpenseListItem> RecentExpenses { get; set; } = [];
}
