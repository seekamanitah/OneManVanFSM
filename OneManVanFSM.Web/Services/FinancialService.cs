using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class FinancialService : IFinancialService
{
    private readonly AppDbContext _db;
    public FinancialService(AppDbContext db) => _db = db;

    // Invoices
    public async Task<List<InvoiceListItem>> GetInvoicesAsync(InvoiceFilter? filter = null)
    {
        var query = _db.Invoices.Where(i => !i.IsArchived).AsQueryable();
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(i => i.InvoiceNumber.ToLower().Contains(term) ||
                    (i.Customer != null && i.Customer.Name.ToLower().Contains(term)));
            }
            if (filter.Status.HasValue) query = query.Where(i => i.Status == filter.Status.Value);
            query = filter.SortBy?.ToLower() switch
            {
                "number" => filter.SortDescending ? query.OrderByDescending(i => i.InvoiceNumber) : query.OrderBy(i => i.InvoiceNumber),
                "total" => filter.SortDescending ? query.OrderByDescending(i => i.Total) : query.OrderBy(i => i.Total),
                "duedate" => filter.SortDescending ? query.OrderByDescending(i => i.DueDate) : query.OrderBy(i => i.DueDate),
                _ => filter.SortDescending ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt)
            };
        }
        else query = query.OrderByDescending(i => i.CreatedAt);

        return await query.Select(i => new InvoiceListItem
        {
            Id = i.Id, InvoiceNumber = i.InvoiceNumber,
            CustomerName = i.Customer != null ? i.Customer.Name : null,
            Status = i.Status, Total = i.Total, BalanceDue = i.BalanceDue,
            InvoiceDate = i.InvoiceDate, DueDate = i.DueDate, CreatedAt = i.CreatedAt
        }).ToListAsync();
    }

    public async Task<InvoiceDetail?> GetInvoiceAsync(int id)
    {
        var i = await _db.Invoices
            .Include(inv => inv.Customer)
            .Include(inv => inv.Company)
            .Include(inv => inv.Site)
            .Include(inv => inv.Job)
            .Include(inv => inv.Payments)
            .Include(inv => inv.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(inv => inv.Id == id && !inv.IsArchived);

        if (i is null) return null;

        return new InvoiceDetail
        {
            Id = i.Id, InvoiceNumber = i.InvoiceNumber, Status = i.Status,
            InvoiceDate = i.InvoiceDate, DueDate = i.DueDate,
            PaymentTerms = i.PaymentTerms,
            Subtotal = i.Subtotal, TaxAmount = i.TaxAmount,
            MarkupAmount = i.MarkupAmount, DiscountAmount = i.DiscountAmount,
            DepositApplied = i.DepositApplied,
            Total = i.Total, BalanceDue = i.BalanceDue,
            Notes = i.Notes, CustomerId = i.CustomerId,
            CustomerName = i.Customer?.Name,
            CustomerEmail = i.Customer?.PrimaryEmail,
            CompanyId = i.CompanyId,
            CompanyName = i.Company?.Name,
            SiteId = i.SiteId,
            SiteName = i.Site?.Name,
            JobId = i.JobId, JobNumber = i.Job?.JobNumber,
            CreatedAt = i.CreatedAt, UpdatedAt = i.UpdatedAt,
            Payments = i.Payments.Select(p => new PaymentListItem
            {
                Id = p.Id, Amount = p.Amount, Method = p.Method,
                Status = p.Status, InvoiceId = p.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                Reference = p.Reference, Notes = p.Notes,
                PaymentDate = p.PaymentDate, CreatedAt = p.CreatedAt
            }).ToList(),
            Lines = i.Lines.OrderBy(l => l.SortOrder).Select(l => new InvoiceLineDto
            {
                Id = l.Id, ProductId = l.ProductId,
                ProductName = l.Product?.Name,
                Description = l.Description, LineType = l.LineType, Unit = l.Unit,
                Quantity = l.Quantity, UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal, SortOrder = l.SortOrder
            }).ToList()
        };
    }

    public async Task<Invoice> CreateInvoiceAsync(InvoiceEditModel model)
    {
        var num = model.InvoiceNumber;
        if (string.IsNullOrWhiteSpace(num))
        {
            var count = await _db.Invoices.CountAsync() + 1;
            num = $"INV-{count:D5}";
        }

        // Calculate totals from lines if lines exist
        if (model.Lines.Count > 0)
        {
            model.Subtotal = model.Lines.Sum(l => l.Quantity * l.UnitPrice);
            model.Total = model.Subtotal + model.TaxAmount + model.MarkupAmount;
            model.BalanceDue = model.Total;
        }

        var inv = new Invoice
        {
            InvoiceNumber = num, Status = model.Status,
            InvoiceDate = model.InvoiceDate, DueDate = model.DueDate,
            PaymentTerms = model.PaymentTerms,
            Subtotal = model.Subtotal, TaxAmount = model.TaxAmount, MarkupAmount = model.MarkupAmount,
            DiscountAmount = model.DiscountAmount, DepositApplied = model.DepositApplied,
            Total = model.Total, BalanceDue = model.BalanceDue, Notes = model.Notes,
            CustomerId = model.CustomerId, CompanyId = model.CompanyId,
            JobId = model.JobId, SiteId = model.SiteId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Invoices.Add(inv);
        await _db.SaveChangesAsync();

        // Add line items
        int order = 0;
        foreach (var line in model.Lines)
        {
            _db.InvoiceLines.Add(new OneManVanFSM.Shared.Models.InvoiceLine
            {
                InvoiceId = inv.Id, ProductId = line.ProductId,
                Description = line.Description, LineType = line.LineType, Unit = line.Unit,
                Quantity = line.Quantity, UnitPrice = line.UnitPrice,
                LineTotal = line.Quantity * line.UnitPrice,
                SortOrder = order++
            });
        }
        if (model.Lines.Count > 0) await _db.SaveChangesAsync();

        return inv;
    }

    public async Task<Invoice> UpdateInvoiceAsync(int id, InvoiceEditModel model)
    {
        var inv = await _db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("Invoice not found.");

        // Recalculate from lines if present
        if (model.Lines.Count > 0)
        {
            model.Subtotal = model.Lines.Sum(l => l.Quantity * l.UnitPrice);
            model.Total = model.Subtotal + model.TaxAmount + model.MarkupAmount;
            model.BalanceDue = model.Total - inv.Payments.Sum(p => p.Status == PaymentStatus.Completed ? p.Amount : 0);
        }

        inv.Status = model.Status; inv.InvoiceDate = model.InvoiceDate; inv.DueDate = model.DueDate;
        inv.PaymentTerms = model.PaymentTerms;
        inv.Subtotal = model.Subtotal; inv.TaxAmount = model.TaxAmount;
        inv.MarkupAmount = model.MarkupAmount; inv.DiscountAmount = model.DiscountAmount;
        inv.DepositApplied = model.DepositApplied;
        inv.Total = model.Total;
        inv.BalanceDue = model.BalanceDue; inv.Notes = model.Notes;
        inv.CustomerId = model.CustomerId; inv.CompanyId = model.CompanyId;
        inv.JobId = model.JobId; inv.SiteId = model.SiteId;
        inv.UpdatedAt = DateTime.UtcNow;

        // Replace line items
        _db.InvoiceLines.RemoveRange(inv.Lines);
        int order = 0;
        foreach (var line in model.Lines)
        {
            _db.InvoiceLines.Add(new OneManVanFSM.Shared.Models.InvoiceLine
            {
                InvoiceId = inv.Id, ProductId = line.ProductId,
                Description = line.Description, LineType = line.LineType, Unit = line.Unit,
                Quantity = line.Quantity, UnitPrice = line.UnitPrice,
                LineTotal = line.Quantity * line.UnitPrice,
                SortOrder = order++
            });
        }

        await _db.SaveChangesAsync();
        return inv;
    }

    public async Task<bool> UpdateInvoiceStatusAsync(int id, InvoiceStatus status)
    {
        var inv = await _db.Invoices.FindAsync(id);
        if (inv is null) return false;
        inv.Status = status; inv.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // Expenses
    public async Task<List<ExpenseListItem>> GetExpensesAsync(ExpenseFilter? filter = null)
    {
        var query = _db.Expenses.AsQueryable();
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(e => (e.Category != null && e.Category.ToLower().Contains(term)) ||
                    (e.Description != null && e.Description.ToLower().Contains(term)));
            }
            if (filter.Status.HasValue) query = query.Where(e => e.Status == filter.Status.Value);
            query = filter.SortBy?.ToLower() switch
            {
                "amount" => filter.SortDescending ? query.OrderByDescending(e => e.Amount) : query.OrderBy(e => e.Amount),
                "category" => filter.SortDescending ? query.OrderByDescending(e => e.Category) : query.OrderBy(e => e.Category),
                _ => filter.SortDescending ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt)
            };
        }
        else query = query.OrderByDescending(e => e.CreatedAt);

        return await query.Select(e => new ExpenseListItem
        {
            Id = e.Id, Category = e.Category, Description = e.Description,
            Amount = e.Amount, Status = e.Status, IsBillable = e.IsBillable,
            EmployeeName = e.Employee != null ? e.Employee.Name : null,
            JobId = e.JobId, JobTitle = e.Job != null ? e.Job.Title : null,
            ExpenseDate = e.ExpenseDate, CreatedAt = e.CreatedAt
        }).ToListAsync();
    }

    public async Task<Expense> CreateExpenseAsync(ExpenseEditModel model)
    {
        var exp = new Expense
        {
            Category = model.Category, Description = model.Description,
            Amount = model.Amount, Status = model.Status, IsBillable = model.IsBillable,
            Notes = model.Notes, ExpenseDate = model.ExpenseDate,
            EmployeeId = model.EmployeeId,
            JobId = model.JobId, InvoiceId = model.InvoiceId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Expenses.Add(exp);
        await _db.SaveChangesAsync();
        return exp;
    }

    public async Task<Expense> UpdateExpenseAsync(int id, ExpenseEditModel model)
    {
        var e = await _db.Expenses.FindAsync(id) ?? throw new InvalidOperationException("Expense not found.");
        e.Category = model.Category; e.Description = model.Description;
        e.Amount = model.Amount; e.Status = model.Status; e.IsBillable = model.IsBillable;
        e.Notes = model.Notes; e.ExpenseDate = model.ExpenseDate;
        e.EmployeeId = model.EmployeeId;
        e.JobId = model.JobId; e.InvoiceId = model.InvoiceId;
        await _db.SaveChangesAsync();
        return e;
    }

    // Payments
    public async Task<List<PaymentListItem>> GetPaymentsAsync(PaymentFilter? filter = null)
    {
        var query = _db.Payments.AsQueryable();
        if (filter is not null)
        {
            if (filter.Status.HasValue) query = query.Where(p => p.Status == filter.Status.Value);
            query = filter.SortBy?.ToLower() switch
            {
                "amount" => filter.SortDescending ? query.OrderByDescending(p => p.Amount) : query.OrderBy(p => p.Amount),
                _ => filter.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
            };
        }
        else query = query.OrderByDescending(p => p.CreatedAt);

        return await query.Select(p => new PaymentListItem
        {
            Id = p.Id, Amount = p.Amount, Method = p.Method, Status = p.Status,
            InvoiceId = p.InvoiceId,
            InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : null,
            Reference = p.Reference, Notes = p.Notes,
            PaymentDate = p.PaymentDate, CreatedAt = p.CreatedAt
        }).ToListAsync();
    }

    public async Task<Payment> CreatePaymentAsync(PaymentEditModel model)
    {
        var pay = new Payment
        {
            Amount = model.Amount, Method = model.Method, Status = model.Status,
            Reference = model.Reference, Notes = model.Notes,
            PaymentDate = model.PaymentDate, InvoiceId = model.InvoiceId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Payments.Add(pay);
        await _db.SaveChangesAsync();

        // Update invoice balance
        var inv = await _db.Invoices.FindAsync(model.InvoiceId);
        if (inv is not null)
        {
            var totalPaid = await _db.Payments
                .Where(p => p.InvoiceId == model.InvoiceId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);
            inv.BalanceDue = inv.Total - totalPaid;
            if (inv.BalanceDue <= 0) inv.Status = InvoiceStatus.Paid;
            await _db.SaveChangesAsync();
        }
        return pay;
    }

    // Dashboard
    public async Task<FinancialDashboard> GetDashboardAsync()
    {
        var invoices = await _db.Invoices.Where(i => !i.IsArchived).ToListAsync();
        var expenses = await _db.Expenses.ToListAsync();
        return new FinancialDashboard
        {
            TotalRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total),
            TotalOutstanding = invoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void).Sum(i => i.BalanceDue),
            TotalExpenses = expenses.Where(e => e.Status == ExpenseStatus.Approved || e.Status == ExpenseStatus.Reimbursed).Sum(e => e.Amount),
            OverdueInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue),
            DraftInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Draft),
            PendingExpenseCount = expenses.Count(e => e.Status == ExpenseStatus.Pending)
        };
    }

    public async Task<List<ProductOption>> GetProductOptionsAsync()
    {
        return await _db.Products
            .OrderBy(p => p.Name)
            .Select(p => new ProductOption
            {
                Id = p.Id, Name = p.Name, Category = p.Category,
                Price = p.Price, Unit = p.Unit
            }).ToListAsync();
    }
}
