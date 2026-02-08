using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileExpenseService : IMobileExpenseService
{
    private readonly AppDbContext _db;
    public MobileExpenseService(AppDbContext db) => _db = db;

    public async Task<MobileExpenseStats> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var expenses = await _db.Expenses.ToListAsync();

        return new MobileExpenseStats
        {
            TotalCount = expenses.Count,
            PendingCount = expenses.Count(e => e.Status == ExpenseStatus.Pending),
            TotalThisMonth = expenses.Where(e => e.ExpenseDate >= monthStart).Sum(e => e.Total),
            BillableAmount = expenses.Where(e => e.IsBillable).Sum(e => e.Total),
        };
    }

    public async Task<List<MobileExpenseCard>> GetExpensesAsync(MobileExpenseFilter? filter = null)
    {
        var query = _db.Expenses
            .Include(e => e.Job)
            .Include(e => e.Customer)
            .Include(e => e.Lines)
            .AsQueryable();

        if (filter?.Status.HasValue == true)
            query = query.Where(e => e.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter?.Category))
            query = query.Where(e => e.Category == filter.Category);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(e =>
                (e.Description != null && e.Description.ToLower().Contains(s))
                || (e.VendorName != null && e.VendorName.ToLower().Contains(s))
                || (e.Category != null && e.Category.ToLower().Contains(s))
                || (e.Customer != null && e.Customer.Name.ToLower().Contains(s)));
        }

        var expenses = await query.OrderByDescending(e => e.ExpenseDate).ToListAsync();

        return expenses.Select(e => new MobileExpenseCard
        {
            Id = e.Id,
            Category = e.Category,
            Description = e.Description,
            Total = e.Total,
            Status = e.Status,
            IsBillable = e.IsBillable,
            VendorName = e.VendorName,
            JobNumber = e.Job?.JobNumber,
            CustomerName = e.Customer?.Name,
            ExpenseDate = e.ExpenseDate,
            LineCount = e.Lines.Count,
        }).ToList();
    }

    public async Task<MobileExpenseDetail?> GetExpenseDetailAsync(int id)
    {
        var e = await _db.Expenses
            .Include(e => e.Employee)
            .Include(e => e.Job)
            .Include(e => e.Customer)
            .Include(e => e.Company)
            .Include(e => e.Invoice)
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (e == null) return null;

        return new MobileExpenseDetail
        {
            Id = e.Id,
            Category = e.Category,
            Description = e.Description,
            Amount = e.Amount,
            TaxAmount = e.TaxAmount,
            Total = e.Total,
            PaymentMethod = e.PaymentMethod,
            Status = e.Status,
            IsBillable = e.IsBillable,
            VendorName = e.VendorName,
            ReceiptNumber = e.ReceiptNumber,
            Notes = e.Notes,
            ExpenseDate = e.ExpenseDate,
            EmployeeId = e.EmployeeId,
            EmployeeName = e.Employee != null ? $"{e.Employee.FirstName} {e.Employee.LastName}".Trim() : null,
            JobId = e.JobId,
            JobNumber = e.Job?.JobNumber,
            JobTitle = e.Job?.Title,
            CustomerId = e.CustomerId,
            CustomerName = e.Customer?.Name,
            CompanyId = e.CompanyId,
            CompanyName = e.Company?.Name,
            InvoiceId = e.InvoiceId,
            InvoiceNumber = e.Invoice?.InvoiceNumber,
            CreatedAt = e.CreatedAt,
            Lines = e.Lines.OrderBy(l => l.SortOrder).Select(l => new MobileExpenseLine
            {
                Description = l.Description,
                LineType = l.LineType,
                Unit = l.Unit,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
            }).ToList(),
        };
    }

    public async Task<int> CreateExpenseAsync(MobileExpenseCreate model)
    {
        var expense = new Expense
        {
            Category = model.Category,
            Description = model.Description,
            Amount = model.Amount,
            TaxAmount = model.TaxAmount,
            Total = model.Amount + model.TaxAmount,
            PaymentMethod = model.PaymentMethod,
            IsBillable = model.IsBillable,
            VendorName = model.VendorName,
            ReceiptNumber = model.ReceiptNumber,
            Notes = model.Notes,
            ExpenseDate = model.ExpenseDate,
            JobId = model.JobId,
            CustomerId = model.CustomerId,
            Status = ExpenseStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        return expense.Id;
    }
}
