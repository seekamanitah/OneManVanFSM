using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class ReportService(AppDbContext db) : IReportService
{
    public async Task<ReportKPIs> GetKPIsAsync(ReportPeriod period = ReportPeriod.ThisMonth)
    {
        var now = DateTime.UtcNow;
        var rangeStart = period switch
        {
            ReportPeriod.Today => now.Date,
            ReportPeriod.ThisWeek => now.Date.AddDays(-(int)now.DayOfWeek),
            ReportPeriod.ThisMonth => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportPeriod.ThisQuarter => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportPeriod.ThisYear => new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportPeriod.AllTime => DateTime.MinValue,
            _ => now.Date
        };

        var jobsQuery = db.Jobs.Where(j => !j.IsArchived);
        if (rangeStart > DateTime.MinValue)
            jobsQuery = jobsQuery.Where(j => j.CreatedAt >= rangeStart);

        var totalJobs = await jobsQuery.CountAsync();
        var completedJobs = await jobsQuery.CountAsync(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed);
        var openJobs = await jobsQuery.CountAsync(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Closed && j.Status != JobStatus.Cancelled);

        var invoiceQuery = db.Invoices.Where(i => !i.IsArchived);
        if (rangeStart > DateTime.MinValue)
            invoiceQuery = invoiceQuery.Where(i => i.CreatedAt >= rangeStart);

        var revenueTotal = await invoiceQuery
            .Where(i => i.Status == InvoiceStatus.Paid)
            .SumAsync(i => (decimal?)i.Total) ?? 0;

        var outstandingAR = await db.Invoices
            .Where(i => !i.IsArchived && i.BalanceDue > 0 && i.Status != InvoiceStatus.Void)
            .SumAsync(i => (decimal?)i.BalanceDue) ?? 0;

        var unpaidInvoiceCount = await db.Invoices
            .CountAsync(i => !i.IsArchived && i.BalanceDue > 0 && i.Status != InvoiceStatus.Void && i.Status != InvoiceStatus.Paid);

        var activeEmployees = await db.Employees
            .CountAsync(e => !e.IsArchived && e.Status == EmployeeStatus.Active);

        var timeQuery = db.TimeEntries.AsQueryable();
        if (rangeStart > DateTime.MinValue)
            timeQuery = timeQuery.Where(t => t.StartTime >= rangeStart);
        var totalHours = await timeQuery.SumAsync(t => (decimal?)t.Hours) ?? 0;

        var totalAssets = await db.Assets.CountAsync(a => !a.IsArchived);

        var activeAgreements = await db.ServiceAgreements
            .CountAsync(sa => !sa.IsArchived && sa.Status == AgreementStatus.Active);

        var pendingEstimates = await db.Estimates
            .CountAsync(e => !e.IsArchived && (e.Status == EstimateStatus.Draft || e.Status == EstimateStatus.Sent));

        var totalCustomers = await db.Customers.CountAsync(c => !c.IsArchived);

        var documentCount = await db.Documents.CountAsync();

        var inventoryItemCount = await db.InventoryItems.CountAsync(i => !i.IsArchived);

        return new ReportKPIs
        {
            TotalJobs = totalJobs,
            CompletedJobs = completedJobs,
            OpenJobs = openJobs,
            RevenueTotal = revenueTotal,
            OutstandingAR = outstandingAR,
            UnpaidInvoiceCount = unpaidInvoiceCount,
            ActiveEmployees = activeEmployees,
            TotalHoursLogged = totalHours,
            TotalAssets = totalAssets,
            ActiveAgreements = activeAgreements,
            PendingEstimates = pendingEstimates,
            TotalCustomers = totalCustomers,
            DocumentCount = documentCount,
            InventoryItemCount = inventoryItemCount
        };
    }

    public async Task<List<AuditLogListItem>> GetAuditLogsAsync(AuditLogFilter? filter = null)
    {
        var query = db.AuditLogs.Include(a => a.User).AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(a =>
                    a.Action.ToLower().Contains(term) ||
                    a.EntityType.ToLower().Contains(term) ||
                    (a.Details != null && a.Details.ToLower().Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                query = query.Where(a => a.EntityType == filter.EntityType);
            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(a => a.Action == filter.Action);
            if (filter.DateFrom.HasValue)
                query = query.Where(a => a.Timestamp >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(a => a.Timestamp <= filter.DateTo.Value.AddDays(1));
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((Math.Max(1, filter?.Page ?? 1) - 1) * (filter?.PageSize ?? 25))
            .Take(filter?.PageSize ?? 25)
            .Select(a => new AuditLogListItem
            {
                Id = a.Id,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Details = a.Details,
                UserName = a.User != null ? a.User.Username : null,
                IpAddress = a.IpAddress,
                Timestamp = a.Timestamp
            }).ToListAsync();
    }

    public async Task<int> GetAuditLogCountAsync(AuditLogFilter? filter = null)
    {
        var query = db.AuditLogs.AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(a =>
                    a.Action.ToLower().Contains(term) ||
                    a.EntityType.ToLower().Contains(term) ||
                    (a.Details != null && a.Details.ToLower().Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                query = query.Where(a => a.EntityType == filter.EntityType);
            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(a => a.Action == filter.Action);
            if (filter.DateFrom.HasValue)
                query = query.Where(a => a.Timestamp >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(a => a.Timestamp <= filter.DateTo.Value.AddDays(1));
        }

        return await query.CountAsync();
    }
}
