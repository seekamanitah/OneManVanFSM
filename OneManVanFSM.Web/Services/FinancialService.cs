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
        var showArchived = filter?.ShowArchived ?? false;
        var query = _db.Invoices.Where(i => i.IsArchived == showArchived).AsQueryable();
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
            .Include(inv => inv.Site).ThenInclude(s => s!.Assets)
            .Include(inv => inv.Job)
            .Include(inv => inv.Payments)
            .Include(inv => inv.Lines).ThenInclude(l => l.Product)
            .Include(inv => inv.Lines).ThenInclude(l => l.Asset)
            .FirstOrDefaultAsync(inv => inv.Id == id && !inv.IsArchived);

        if (i is null) return null;

        return new InvoiceDetail
        {
            Id = i.Id, InvoiceNumber = i.InvoiceNumber, Status = i.Status,
            InvoiceDate = i.InvoiceDate, DueDate = i.DueDate,
            PaymentTerms = i.PaymentTerms, PricingType = i.PricingType,
            Subtotal = i.Subtotal, TaxAmount = i.TaxAmount,
            TaxRate = i.TaxRate, TaxIncludedInPrice = i.TaxIncludedInPrice,
            MarkupAmount = i.MarkupAmount, DiscountAmount = i.DiscountAmount,
            DepositApplied = i.DepositApplied,
            Total = i.Total, AmountPaid = i.AmountPaid, BalanceDue = i.BalanceDue,
            Notes = i.Notes, Terms = i.Terms,
            IncludeSiteLocation = i.IncludeSiteLocation,
            IncludeAssetInfo = i.IncludeAssetInfo,
            IncludeJobDescription = i.IncludeJobDescription,
            IncludeNotes = i.IncludeNotes,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.Name,
            CustomerEmail = i.Customer?.PrimaryEmail,
            CustomerPhone = i.Customer?.PrimaryPhone,
            CustomerAddress = i.Customer?.Address,
            CustomerCity = i.Customer?.City,
            CustomerState = i.Customer?.State,
            CustomerZip = i.Customer?.Zip,
            CompanyId = i.CompanyId,
            CompanyName = i.Company?.Name,
            SiteId = i.SiteId,
            SiteName = i.Site?.Name,
            SiteAddress = i.Site?.Address,
            SiteCity = i.Site?.City,
            SiteState = i.Site?.State,
            SiteZip = i.Site?.Zip,
            JobId = i.JobId, JobNumber = i.Job?.JobNumber,
            JobTitle = i.Job?.Title, JobDescription = i.Job?.Description,
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
                AssetId = l.AssetId,
                AssetName = l.Asset?.Name,
                Description = l.Description, LineType = l.LineType, Unit = l.Unit,
                Quantity = l.Quantity, UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal, SortOrder = l.SortOrder
            }).ToList(),
            SiteAssets = i.Site?.Assets?.Where(a => !a.IsArchived).Select(a => new InvoiceAssetInfo
            {
                Id = a.Id,
                Name = a.Name,
                Brand = a.Brand,
                Model = a.Model,
                SerialNumber = a.SerialNumber,
                AssetType = a.AssetType,
                LastServiceDate = a.LastServiceDate
            }).ToList() ?? []
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

        // Calculate totals from lines if lines exist; for flat rate, preserve manual subtotal
        if (model.Lines.Count > 0)
        {
            model.Subtotal = model.Lines.Sum(l => l.Quantity * l.UnitPrice);
        }
        else if (model.PricingType != "Flat Rate")
        {
            model.Subtotal = 0;
        }
        var discount = model.DiscountAmount ?? 0;
        if (model.TaxIncludedInPrice)
            model.TaxAmount = 0;
        else
            model.TaxAmount = Math.Round(model.Subtotal * (model.TaxRate / 100m), 2);
        model.Total = Math.Round(model.Subtotal + model.TaxAmount + model.MarkupAmount - discount, 2);
        model.BalanceDue = Math.Max(0, model.Total - model.AmountPaid);

        var inv = new Invoice
        {
            InvoiceNumber = num, Status = model.Status,
            InvoiceDate = model.InvoiceDate, DueDate = model.DueDate,
            PaymentTerms = model.PaymentTerms, PricingType = model.PricingType,
            Subtotal = model.Subtotal, TaxAmount = model.TaxAmount,
            TaxRate = model.TaxRate, TaxIncludedInPrice = model.TaxIncludedInPrice,
            MarkupAmount = model.MarkupAmount,
            DiscountAmount = model.DiscountAmount, DepositApplied = model.DepositApplied,
            Total = model.Total, AmountPaid = model.AmountPaid, BalanceDue = model.BalanceDue,
            Notes = model.Notes, Terms = model.Terms,
            IncludeSiteLocation = model.IncludeSiteLocation,
            IncludeAssetInfo = model.IncludeAssetInfo,
            IncludeJobDescription = model.IncludeJobDescription,
            IncludeNotes = model.IncludeNotes,
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
                AssetId = line.AssetId,
                Description = line.Description, LineType = line.LineType, Unit = line.Unit,
                Quantity = line.Quantity, UnitPrice = line.UnitPrice,
                LineTotal = line.Quantity * line.UnitPrice,
                SortOrder = order++
            });
        }
        if (model.Lines.Count > 0) await _db.SaveChangesAsync();

        // Bidirectional link: set Job.InvoiceId so the job knows it has an invoice
        if (inv.JobId.HasValue)
        {
            var job = await _db.Jobs.FindAsync(inv.JobId.Value);
            if (job is not null && !job.InvoiceId.HasValue)
            {
                job.InvoiceId = inv.Id;
                await _db.SaveChangesAsync();
            }
        }

        if (inv.CustomerId.HasValue)
            await RecalcCustomerBalanceAsync(inv.CustomerId.Value);

        return inv;
    }

    public async Task<Invoice> UpdateInvoiceAsync(int id, InvoiceEditModel model)
    {
        var inv = await _db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("Invoice not found.");

        // Recalculate from lines if present; for flat rate, preserve manual subtotal
        if (model.Lines.Count > 0)
        {
            model.Subtotal = model.Lines.Sum(l => l.Quantity * l.UnitPrice);
        }
        else if (model.PricingType != "Flat Rate")
        {
            model.Subtotal = 0;
        }
        var discount = model.DiscountAmount ?? 0;
        if (model.TaxIncludedInPrice)
            model.TaxAmount = 0;
        else
            model.TaxAmount = Math.Round(model.Subtotal * (model.TaxRate / 100m), 2);
        model.Total = Math.Round(model.Subtotal + model.TaxAmount + model.MarkupAmount - discount, 2);
        var totalPaid = model.AmountPaid + inv.Payments.Sum(p => p.Status == PaymentStatus.Completed ? p.Amount : 0);
        model.BalanceDue = Math.Max(0, model.Total - totalPaid);

        inv.Status = model.Status; inv.InvoiceDate = model.InvoiceDate; inv.DueDate = model.DueDate;
        inv.PaymentTerms = model.PaymentTerms; inv.PricingType = model.PricingType;
        inv.Subtotal = model.Subtotal; inv.TaxAmount = model.TaxAmount;
        inv.TaxRate = model.TaxRate; inv.TaxIncludedInPrice = model.TaxIncludedInPrice;
        inv.MarkupAmount = model.MarkupAmount; inv.DiscountAmount = model.DiscountAmount;
        inv.DepositApplied = model.DepositApplied;
        inv.Total = model.Total;
        inv.AmountPaid = model.AmountPaid;
        inv.BalanceDue = model.BalanceDue; inv.Notes = model.Notes; inv.Terms = model.Terms;
        inv.IncludeSiteLocation = model.IncludeSiteLocation;
        inv.IncludeAssetInfo = model.IncludeAssetInfo;
        inv.IncludeJobDescription = model.IncludeJobDescription;
        inv.IncludeNotes = model.IncludeNotes;
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
                AssetId = line.AssetId,
                Description = line.Description, LineType = line.LineType, Unit = line.Unit,
                Quantity = line.Quantity, UnitPrice = line.UnitPrice,
                LineTotal = line.Quantity * line.UnitPrice,
                SortOrder = order++
            });
        }

        await _db.SaveChangesAsync();

        if (inv.CustomerId.HasValue)
            await RecalcCustomerBalanceAsync(inv.CustomerId.Value);

        return inv;
    }

    public async Task<bool> UpdateInvoiceStatusAsync(int id, InvoiceStatus status)
    {
        var inv = await _db.Invoices.FindAsync(id);
        if (inv is null) return false;
        inv.Status = status; inv.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // When voiding an invoice, clear Job.InvoiceId so a new invoice can be created
        if (status == InvoiceStatus.Void && inv.JobId.HasValue)
        {
            var job = await _db.Jobs.FindAsync(inv.JobId.Value);
            if (job is not null && job.InvoiceId == inv.Id)
            {
                job.InvoiceId = null;
                await _db.SaveChangesAsync();
            }
        }

        if (inv.CustomerId.HasValue)
            await RecalcCustomerBalanceAsync(inv.CustomerId.Value);

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
                    (e.Description != null && e.Description.ToLower().Contains(term)) ||
                    (e.VendorName != null && e.VendorName.ToLower().Contains(term)));
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
            Amount = e.Amount, TaxAmount = e.TaxAmount, Total = e.Total,
            PaymentMethod = e.PaymentMethod,
            Status = e.Status, IsBillable = e.IsBillable,
            VendorName = e.VendorName,
            CompanyName = e.Company != null ? e.Company.Name : null,
            EmployeeName = e.Employee != null ? e.Employee.Name : null,
            JobId = e.JobId, JobTitle = e.Job != null ? e.Job.Title : null,
            CustomerId = e.CustomerId, CustomerName = e.Customer != null ? e.Customer.Name : null,
            ExpenseDate = e.ExpenseDate, CreatedAt = e.CreatedAt
        }).ToListAsync();
    }

    public async Task<ExpenseDetail?> GetExpenseAsync(int id)
    {
        var e = await _db.Expenses
            .Include(x => x.Employee)
            .Include(x => x.Job)
            .Include(x => x.Customer)
            .Include(x => x.Company)
            .Include(x => x.Invoice)
            .Include(x => x.Lines).ThenInclude(l => l.Product)
            .Include(x => x.Lines).ThenInclude(l => l.InventoryItem)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return null;
        return new ExpenseDetail
        {
            Id = e.Id, Category = e.Category, Description = e.Description,
            Amount = e.Amount, TaxAmount = e.TaxAmount, Total = e.Total,
            PaymentMethod = e.PaymentMethod,
            Status = e.Status, IsBillable = e.IsBillable,
            VendorName = e.VendorName, ReceiptNumber = e.ReceiptNumber,
            Notes = e.Notes, ExpenseDate = e.ExpenseDate,
            EmployeeId = e.EmployeeId, EmployeeName = e.Employee?.Name,
            JobId = e.JobId, JobTitle = e.Job?.Title, JobNumber = e.Job?.JobNumber,
            CustomerId = e.CustomerId, CustomerName = e.Customer?.Name,
            CompanyId = e.CompanyId, CompanyName = e.Company?.Name,
            InvoiceId = e.InvoiceId, InvoiceNumber = e.Invoice?.InvoiceNumber,
            CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            Lines = e.Lines.OrderBy(l => l.SortOrder).Select(l => new ExpenseLineDto
            {
                Id = l.Id, ProductId = l.ProductId, ProductName = l.Product?.Name,
                InventoryItemId = l.InventoryItemId, InventoryItemName = l.InventoryItem?.Name,
                Description = l.Description, LineType = l.LineType, Unit = l.Unit,
                Quantity = l.Quantity, UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal, Notes = l.Notes, SortOrder = l.SortOrder
            }).ToList()
        };
    }

    public async Task<Expense> CreateExpenseAsync(ExpenseEditModel model)
    {
        if (model.Lines.Count > 0)
            model.Amount = model.Lines.Sum(l => l.Quantity * l.UnitPrice);
        model.Total = model.Amount + model.TaxAmount;
        var exp = new Expense
        {
            Category = model.Category, Description = model.Description,
            Amount = model.Amount, TaxAmount = model.TaxAmount, Total = model.Total,
            PaymentMethod = model.PaymentMethod,
            Status = model.Status, IsBillable = model.IsBillable,
            VendorName = model.VendorName, ReceiptNumber = model.ReceiptNumber,
            Notes = model.Notes, ExpenseDate = model.ExpenseDate,
            EmployeeId = model.EmployeeId,
            JobId = model.JobId, CustomerId = model.CustomerId,
            CompanyId = model.CompanyId, InvoiceId = model.InvoiceId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Expenses.Add(exp);
        await _db.SaveChangesAsync();

        int order = 0;
        foreach (var line in model.Lines)
        {
            _db.ExpenseLines.Add(new OneManVanFSM.Shared.Models.ExpenseLine
            {
                ExpenseId = exp.Id, ProductId = line.ProductId,
                InventoryItemId = line.InventoryItemId,
                Description = line.Description, LineType = line.LineType, Unit = line.Unit,
                Quantity = line.Quantity, UnitPrice = line.UnitPrice,
                LineTotal = line.Quantity * line.UnitPrice,
                Notes = line.Notes, SortOrder = order++
            });
        }
        if (model.Lines.Count > 0) await _db.SaveChangesAsync();

        return exp;
    }

    public async Task<Expense> UpdateExpenseAsync(int id, ExpenseEditModel model)
    {
        var e = await _db.Expenses.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Expense not found.");
        if (model.Lines.Count > 0)
            model.Amount = model.Lines.Sum(l => l.Quantity * l.UnitPrice);
        model.Total = model.Amount + model.TaxAmount;
        e.Category = model.Category; e.Description = model.Description;
        e.Amount = model.Amount; e.TaxAmount = model.TaxAmount; e.Total = model.Total;
        e.PaymentMethod = model.PaymentMethod;
        e.Status = model.Status; e.IsBillable = model.IsBillable;
        e.VendorName = model.VendorName; e.ReceiptNumber = model.ReceiptNumber;
        e.Notes = model.Notes; e.ExpenseDate = model.ExpenseDate;
        e.EmployeeId = model.EmployeeId;
        e.JobId = model.JobId; e.CustomerId = model.CustomerId;
        e.CompanyId = model.CompanyId; e.InvoiceId = model.InvoiceId;
        e.UpdatedAt = DateTime.UtcNow;

        _db.ExpenseLines.RemoveRange(e.Lines);
        int order = 0;
        foreach (var line in model.Lines)
        {
            _db.ExpenseLines.Add(new OneManVanFSM.Shared.Models.ExpenseLine
            {
                ExpenseId = e.Id, ProductId = line.ProductId,
                InventoryItemId = line.InventoryItemId,
                Description = line.Description, LineType = line.LineType, Unit = line.Unit,
                Quantity = line.Quantity, UnitPrice = line.UnitPrice,
                LineTotal = line.Quantity * line.UnitPrice,
                Notes = line.Notes, SortOrder = order++
            });
        }

        await _db.SaveChangesAsync();
        return e;
    }

    public async Task<bool> UpdateExpenseStatusAsync(int id, ExpenseStatus status)
    {
        var e = await _db.Expenses.FindAsync(id);
        if (e is null) return false;
        e.Status = status; e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
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

            if (inv.CustomerId.HasValue)
                await RecalcCustomerBalanceAsync(inv.CustomerId.Value);
        }
        return pay;
    }

    /// <summary>
    /// Recalculates Customer.BalanceOwed from SUM(Invoices.BalanceDue) for all non-archived, non-void invoices.
    /// </summary>
    private async Task RecalcCustomerBalanceAsync(int customerId)
    {
        var customer = await _db.Customers.FindAsync(customerId);
        if (customer is null) return;

        customer.BalanceOwed = await _db.Invoices
            .Where(i => i.CustomerId == customerId && !i.IsArchived && i.Status != InvoiceStatus.Void)
            .SumAsync(i => (decimal?)i.BalanceDue) ?? 0;
        customer.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Dashboard
    public async Task<FinancialDashboard> GetDashboardAsync()
    {
        var invoices = await _db.Invoices.Where(i => !i.IsArchived).ToListAsync();
        var expenses = await _db.Expenses.ToListAsync();
        var paymentsAll = await _db.Payments.OrderByDescending(p => p.CreatedAt).ToListAsync();

        var totalRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total);
        var totalExpenses = expenses.Where(e => e.Status == ExpenseStatus.Approved || e.Status == ExpenseStatus.Reimbursed).Sum(e => e.Total > 0 ? e.Total : e.Amount);
        var totalPaymentsReceived = paymentsAll.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);

        // Recent items (top 5 each)
        var recentInvoices = invoices.OrderByDescending(i => i.CreatedAt).Take(5)
            .Select(i => new InvoiceListItem
            {
                Id = i.Id, InvoiceNumber = i.InvoiceNumber, Status = i.Status,
                Total = i.Total, BalanceDue = i.BalanceDue,
                InvoiceDate = i.InvoiceDate, DueDate = i.DueDate, CreatedAt = i.CreatedAt
            }).ToList();

        var recentPayments = await _db.Payments.OrderByDescending(p => p.CreatedAt).Take(5)
            .Select(p => new PaymentListItem
            {
                Id = p.Id, Amount = p.Amount, Method = p.Method, Status = p.Status,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : null,
                Reference = p.Reference, Notes = p.Notes,
                PaymentDate = p.PaymentDate, CreatedAt = p.CreatedAt
            }).ToListAsync();

        var recentExpenses = expenses.OrderByDescending(e => e.CreatedAt).Take(5)
            .Select(e => new ExpenseListItem
            {
                Id = e.Id, Category = e.Category, Description = e.Description,
                Amount = e.Amount, TaxAmount = e.TaxAmount, Total = e.Total,
                Status = e.Status, IsBillable = e.IsBillable,
                ExpenseDate = e.ExpenseDate, CreatedAt = e.CreatedAt
            }).ToList();

        return new FinancialDashboard
        {
            TotalRevenue = totalRevenue,
            TotalOutstanding = invoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void).Sum(i => i.BalanceDue),
            TotalExpenses = totalExpenses,
            NetProfit = totalRevenue - totalExpenses,
            TotalInvoiceCount = invoices.Count,
            OverdueInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue),
            DraftInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Draft),
            PendingExpenseCount = expenses.Count(e => e.Status == ExpenseStatus.Pending),
            TotalExpenseCount = expenses.Count,
            TotalPaymentCount = paymentsAll.Count,
            TotalPaymentsReceived = totalPaymentsReceived,
            RecentPayments = recentPayments,
            RecentInvoices = recentInvoices,
            RecentExpenses = recentExpenses
        };
    }

    public async Task<bool> ArchiveInvoiceAsync(int id)
    {
        var i = await _db.Invoices.FindAsync(id);
        if (i is null) return false;
        i.IsArchived = true; i.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreInvoiceAsync(int id)
    {
        var i = await _db.Invoices.FindAsync(id);
        if (i is null) return false;
        i.IsArchived = false; i.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteInvoicePermanentlyAsync(int id)
    {
        var i = await _db.Invoices.FindAsync(id);
        if (i is null) return false;
        _db.Invoices.Remove(i);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveInvoicesAsync(List<int> ids)
    {
        var items = await _db.Invoices.Where(i => ids.Contains(i.Id) && !i.IsArchived).ToListAsync();
        foreach (var i in items) { i.IsArchived = true; i.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkRestoreInvoicesAsync(List<int> ids)
    {
        var items = await _db.Invoices.Where(i => ids.Contains(i.Id) && i.IsArchived).ToListAsync();
        foreach (var i in items) { i.IsArchived = false; i.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkDeleteInvoicesPermanentlyAsync(List<int> ids)
    {
        var items = await _db.Invoices.Where(i => ids.Contains(i.Id)).ToListAsync();
        _db.Invoices.RemoveRange(items);
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<bool> DeleteExpensePermanentlyAsync(int id)
    {
        var e = await _db.Expenses.FindAsync(id);
        if (e is null) return false;
        _db.Expenses.Remove(e);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkDeleteExpensesPermanentlyAsync(List<int> ids)
    {
        var items = await _db.Expenses.Where(e => ids.Contains(e.Id)).ToListAsync();
        _db.Expenses.RemoveRange(items);
        await _db.SaveChangesAsync();
        return items.Count;
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

    public async Task<List<CompanyOption>> GetCompanyOptionsAsync()
    {
        return await _db.Companies
            .Where(c => !c.IsArchived)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyOption { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }
}
