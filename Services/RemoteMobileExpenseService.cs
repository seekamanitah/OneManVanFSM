using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

/// <summary>
/// Remote-mode expense service. Reads from local SQLite cache,
/// pushes mutations to the REST API with offline queue fallback.
/// </summary>
public class RemoteMobileExpenseService : IMobileExpenseService
{
    private readonly AppDbContext _db;
    private readonly ApiClient _api;
    private readonly IOfflineQueueService _offlineQueue;
    private readonly ILogger<RemoteMobileExpenseService> _logger;

    public RemoteMobileExpenseService(AppDbContext db, ApiClient api, IOfflineQueueService offlineQueue, ILogger<RemoteMobileExpenseService> logger)
    {
        _db = db;
        _api = api;
        _offlineQueue = offlineQueue;
        _logger = logger;
    }

    public async Task<MobileExpenseStats> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var expenses = await _db.Expenses.Where(e => !e.IsArchived).ToListAsync();

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
        var query = _db.Expenses.Include(e => e.Job).Include(e => e.Customer).Include(e => e.Lines)
            .Where(e => !e.IsArchived).AsQueryable();

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
            Id = e.Id, Category = e.Category, Description = e.Description,
            Total = e.Total, Status = e.Status, IsBillable = e.IsBillable,
            VendorName = e.VendorName, JobNumber = e.Job?.JobNumber,
            CustomerName = e.Customer?.Name, ExpenseDate = e.ExpenseDate,
            LineCount = e.Lines.Count,
        }).ToList();
    }

    public async Task<MobileExpenseDetail?> GetExpenseDetailAsync(int id)
    {
        var e = await _db.Expenses
            .Include(e => e.Employee).Include(e => e.Job).Include(e => e.Customer)
            .Include(e => e.Company).Include(e => e.Invoice).Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (e is null) return null;

        return new MobileExpenseDetail
        {
            Id = e.Id, Category = e.Category, Description = e.Description,
            Amount = e.Amount, TaxAmount = e.TaxAmount, Total = e.Total,
            PaymentMethod = e.PaymentMethod, Status = e.Status, IsBillable = e.IsBillable,
            VendorName = e.VendorName, ReceiptNumber = e.ReceiptNumber,
            Notes = e.Notes, ExpenseDate = e.ExpenseDate,
            EmployeeId = e.EmployeeId,
            EmployeeName = e.Employee != null ? $"{e.Employee.FirstName} {e.Employee.LastName}".Trim() : null,
            JobId = e.JobId, JobNumber = e.Job?.JobNumber, JobTitle = e.Job?.Title,
            CustomerId = e.CustomerId, CustomerName = e.Customer?.Name,
            CompanyId = e.CompanyId, CompanyName = e.Company?.Name,
            InvoiceId = e.InvoiceId, InvoiceNumber = e.Invoice?.InvoiceNumber,
            CreatedAt = e.CreatedAt,
            Lines = e.Lines.OrderBy(l => l.SortOrder).Select(l => new MobileExpenseLine
            {
                Description = l.Description, LineType = l.LineType, Unit = l.Unit,
                Quantity = l.Quantity, UnitPrice = l.UnitPrice, LineTotal = l.LineTotal,
            }).ToList(),
        };
    }

    public async Task<int> CreateExpenseAsync(MobileExpenseCreate model)
    {
        var expense = new Expense
        {
            Category = model.Category, Description = model.Description,
            Amount = model.Amount, TaxAmount = model.TaxAmount,
            Total = model.Amount + model.TaxAmount,
            PaymentMethod = model.PaymentMethod, IsBillable = model.IsBillable,
            VendorName = model.VendorName, ReceiptNumber = model.ReceiptNumber,
            Notes = model.Notes, ExpenseDate = model.ExpenseDate,
            JobId = model.JobId, CustomerId = model.CustomerId,
            Status = ExpenseStatus.Pending,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };

        try
        {
            var created = await _api.PostAsync<Expense>("api/expenses", expense);
            if (created is not null)
            {
                _db.Expenses.Add(created);
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
                return created.Id;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Expense create failed (offline), saving locally and queueing.");
        }

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        _offlineQueue.Enqueue(new OfflineQueueItem
        {
            HttpMethod = "POST", Endpoint = "api/expenses",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(expense),
            Description = $"Create expense: {model.Description}"
        });
        return expense.Id;
    }

    public async Task<bool> UpdateExpenseAsync(int id, MobileExpenseUpdate model)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return false;

        expense.Category = model.Category;
        expense.Description = model.Description;
        expense.Amount = model.Amount;
        expense.TaxAmount = model.TaxAmount;
        expense.Total = model.Amount + model.TaxAmount;
        expense.PaymentMethod = model.PaymentMethod;
        expense.IsBillable = model.IsBillable;
        expense.VendorName = model.VendorName;
        expense.ReceiptNumber = model.ReceiptNumber;
        expense.Notes = model.Notes;
        expense.Status = model.Status;
        expense.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        try
        {
            await _api.PutAsync<Expense>($"api/expenses/{id}", expense);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Expense {Id} update failed (offline), queueing.", id);
            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "PUT", Endpoint = $"api/expenses/{id}",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(expense),
                Description = $"Update expense #{id}"
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Expense {Id} update failed.", id);
            return false;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return false;

        expense.IsArchived = true;
        expense.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        try
        {
            await _api.DeleteAsync($"api/expenses/{id}");
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Expense {Id} delete failed (offline), queueing.", id);
            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "DELETE", Endpoint = $"api/expenses/{id}",
                Description = $"Delete expense #{id}"
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Expense {Id} delete failed.", id);
            return false;
        }
    }
}
