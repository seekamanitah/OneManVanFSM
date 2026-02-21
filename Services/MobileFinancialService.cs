using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileFinancialService(AppDbContext db) : IMobileFinancialService
{
    public async Task<MobileFinancialDashboard> GetDashboardAsync()
    {
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var invoices = await db.Invoices.Where(i => !i.IsArchived).ToListAsync();
        var expenses = await db.Expenses.Where(e => !e.IsArchived).ToListAsync();
        var payments = await db.Payments.ToListAsync();

        var totalRevenue = invoices
            .Where(i => i.Status == InvoiceStatus.Paid)
            .Sum(i => i.AmountPaid);
        totalRevenue += payments.Sum(p => p.Amount);

        var totalOutstanding = invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void && i.Status != InvoiceStatus.Draft)
            .Sum(i => i.BalanceDue);

        var totalExpenses = expenses.Sum(e => e.Amount + e.TaxAmount);

        // Monthly calculations
        var monthInvoices = invoices.Where(i => i.InvoiceDate.HasValue && i.InvoiceDate.Value >= monthStart).ToList();
        var monthExpenses = expenses.Where(e => e.ExpenseDate >= monthStart).ToList();
        var monthPayments = payments.Where(p => p.PaymentDate >= monthStart).ToList();

        return new MobileFinancialDashboard
        {
            TotalRevenue = payments.Sum(p => p.Amount),
            TotalOutstanding = totalOutstanding,
            TotalExpenses = totalExpenses,
            NetProfit = payments.Sum(p => p.Amount) - totalExpenses,
            TotalInvoiceCount = invoices.Count,
            OverdueInvoiceCount = invoices.Count(i => i.DueDate.HasValue && i.DueDate.Value.Date < today && i.BalanceDue > 0 && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void),
            DraftInvoiceCount = invoices.Count(i => i.Status == InvoiceStatus.Draft),
            PendingExpenseCount = expenses.Count(e => e.Status == ExpenseStatus.Pending),
            TotalExpenseCount = expenses.Count,
            TotalPaymentsReceived = payments.Sum(p => p.Amount),
            TotalPaymentCount = payments.Count,
            RevenueThisMonth = monthPayments.Sum(p => p.Amount),
            ExpensesThisMonth = monthExpenses.Sum(e => e.Amount + e.TaxAmount),
            ProfitThisMonth = monthPayments.Sum(p => p.Amount) - monthExpenses.Sum(e => e.Amount + e.TaxAmount),
        };
    }

    public async Task<List<MobileRecentPayment>> GetRecentPaymentsAsync(int count = 10)
    {
        return await db.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i!.Customer)
            .OrderByDescending(p => p.PaymentDate)
            .Take(count)
            .Select(p => new MobileRecentPayment
            {
                Id = p.Id,
                Amount = p.Amount,
                Method = p.Method.ToString(),
                InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : null,
                CustomerName = p.Invoice != null && p.Invoice.Customer != null ? p.Invoice.Customer.Name : null,
                Reference = p.Reference,
                PaymentDate = p.PaymentDate,
            })
            .ToListAsync();
    }

    public async Task<List<MobileAgingInvoice>> GetAgingInvoicesAsync()
    {
        var today = DateTime.Now.Date;

        return await db.Invoices
            .Include(i => i.Customer)
            .Where(i => !i.IsArchived
                && i.BalanceDue > 0
                && i.Status != InvoiceStatus.Paid
                && i.Status != InvoiceStatus.Void
                && i.Status != InvoiceStatus.Draft
                && i.DueDate.HasValue
                && i.DueDate.Value.Date < today)
            .OrderBy(i => i.DueDate)
            .Take(20)
            .Select(i => new MobileAgingInvoice
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                CustomerName = i.Customer != null ? i.Customer.Name : null,
                BalanceDue = i.BalanceDue,
                DueDate = i.DueDate,
                DaysOverdue = (int)(today - i.DueDate!.Value.Date).TotalDays,
            })
            .ToListAsync();
    }

    public async Task<MobileMonthlyBreakdown> GetMonthlyBreakdownAsync(int? year = null, int? month = null)
    {
        var now = DateTime.Now;
        var y = year ?? now.Year;
        var m = month ?? now.Month;
        var monthStart = new DateTime(y, m, 1);
        var monthEnd = monthStart.AddMonths(1);

        var payments = await db.Payments
            .Where(p => p.PaymentDate >= monthStart && p.PaymentDate < monthEnd)
            .ToListAsync();

        var expenses = await db.Expenses
            .Where(e => !e.IsArchived && e.ExpenseDate >= monthStart && e.ExpenseDate < monthEnd)
            .ToListAsync();

        var invoiceCount = await db.Invoices
            .CountAsync(i => !i.IsArchived && i.InvoiceDate.HasValue && i.InvoiceDate.Value >= monthStart && i.InvoiceDate.Value < monthEnd);

        var revenue = payments.Sum(p => p.Amount);
        var expenseTotal = expenses.Sum(e => e.Amount + e.TaxAmount);

        var byCategory = expenses
            .GroupBy(e => e.Category ?? "Other")
            .Select(g => new MobileCategoryBreakdown
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount + e.TaxAmount),
                Count = g.Count(),
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        return new MobileMonthlyBreakdown
        {
            Year = y,
            Month = m,
            MonthName = monthStart.ToString("MMMM yyyy"),
            Revenue = revenue,
            Expenses = expenseTotal,
            Profit = revenue - expenseTotal,
            InvoiceCount = invoiceCount,
            ExpenseCount = expenses.Count,
            PaymentCount = payments.Count,
            ExpensesByCategory = byCategory,
        };
    }
}
