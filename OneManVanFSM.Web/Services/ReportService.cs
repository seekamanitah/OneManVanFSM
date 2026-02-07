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

    public async Task<List<JobProfitabilityItem>> GetJobProfitabilityAsync(ReportPeriod period = ReportPeriod.ThisMonth)
    {
        var rangeStart = GetRangeStart(period);

        var jobs = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Invoice)
            .Include(j => j.TimeEntries)
            .Include(j => j.Expenses)
            .Include(j => j.AssignedEmployee)
            .Where(j => !j.IsArchived
                && (j.Status == JobStatus.Completed || j.Status == JobStatus.Closed)
                && (rangeStart == DateTime.MinValue || j.CompletedDate >= rangeStart))
            .OrderByDescending(j => j.CompletedDate)
            .Take(50)
            .ToListAsync();

        return jobs.Select(j =>
        {
            var revenue = j.Invoice?.Total ?? j.ActualTotal ?? j.EstimatedTotal ?? 0;
            var laborHours = j.TimeEntries.Sum(t => t.Hours);
            var laborRate = j.AssignedEmployee?.HourlyRate ?? 0;
            var laborCost = laborHours * laborRate;
            var materialCost = j.Expenses.Where(e => e.Category == "Materials" || e.Category == "Parts" || e.Category == "Supplies").Sum(e => e.Amount);
            var expenseCost = j.Expenses.Where(e => e.Category != "Materials" && e.Category != "Parts" && e.Category != "Supplies").Sum(e => e.Amount);

            return new JobProfitabilityItem
            {
                JobId = j.Id,
                JobNumber = j.JobNumber,
                CustomerName = j.Customer?.Name,
                Trade = j.TradeType,
                Revenue = revenue,
                LaborCost = laborCost,
                MaterialCost = materialCost,
                ExpenseCost = expenseCost,
                CompletedDate = j.CompletedDate,
            };
        }).ToList();
    }

    public async Task<List<TechUtilizationItem>> GetTechUtilizationAsync(ReportPeriod period = ReportPeriod.ThisMonth)
    {
        var rangeStart = GetRangeStart(period);

        var employees = await db.Employees
            .Where(e => !e.IsArchived && e.Status == EmployeeStatus.Active)
            .ToListAsync();

        var jobQuery = db.Jobs
            .Include(j => j.Invoice)
            .Where(j => !j.IsArchived && (rangeStart == DateTime.MinValue || j.CreatedAt >= rangeStart));

        var allJobs = await jobQuery.ToListAsync();

        var timeQuery = db.TimeEntries.AsQueryable();
        if (rangeStart > DateTime.MinValue)
            timeQuery = timeQuery.Where(t => t.StartTime >= rangeStart);
        var allTimeEntries = await timeQuery.ToListAsync();

        return employees.Select(e =>
        {
            var assignedJobs = allJobs.Where(j => j.AssignedEmployeeId == e.Id).ToList();
            var completedJobs = assignedJobs.Where(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed).ToList();
            var hours = allTimeEntries.Where(t => t.EmployeeId == e.Id).Sum(t => t.Hours);
            var revenue = completedJobs.Sum(j => j.Invoice?.Total ?? j.ActualTotal ?? j.EstimatedTotal ?? 0);

            return new TechUtilizationItem
            {
                EmployeeId = e.Id,
                Name = e.Name,
                JobsCompleted = completedJobs.Count,
                JobsAssigned = assignedJobs.Count,
                HoursLogged = hours,
                RevenueGenerated = revenue,
            };
        })
        .OrderByDescending(t => t.JobsCompleted)
        .ToList();
    }

    public async Task<List<SeasonalDemandItem>> GetSeasonalDemandAsync()
    {
        var twoYearsAgo = DateTime.UtcNow.AddYears(-2);

        var jobs = await db.Jobs
            .Where(j => !j.IsArchived && j.CreatedAt >= twoYearsAgo)
            .ToListAsync();

        var invoiceLookup = await db.Invoices
            .Where(i => !i.IsArchived && i.CreatedAt >= twoYearsAgo)
            .ToDictionaryAsync(i => i.Id, i => i.Total);

        return jobs
            .GroupBy(j => new { j.CreatedAt.Year, j.CreatedAt.Month })
            .Select(g =>
            {
                var revenue = g.Sum(j =>
                {
                    if (j.InvoiceId.HasValue && invoiceLookup.TryGetValue(j.InvoiceId.Value, out var total))
                        return total;
                    return j.ActualTotal ?? j.EstimatedTotal ?? 0;
                });

                var topTrade = g
                    .Where(j => !string.IsNullOrWhiteSpace(j.TradeType))
                    .GroupBy(j => j.TradeType!)
                    .OrderByDescending(tg => tg.Count())
                    .FirstOrDefault()?.Key;

                return new SeasonalDemandItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    JobCount = g.Count(),
                    Revenue = revenue,
                    TopTrade = topTrade,
                };
            })
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToList();
    }

    private DateTime GetRangeStart(ReportPeriod period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            ReportPeriod.Today => now.Date,
            ReportPeriod.ThisWeek => now.Date.AddDays(-(int)now.DayOfWeek),
            ReportPeriod.ThisMonth => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportPeriod.ThisQuarter => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportPeriod.ThisYear => new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportPeriod.AllTime => DateTime.MinValue,
            _ => now.Date
        };
    }

    public async Task<List<ARAgingItem>> GetARAgingAsync()
    {
        var now = DateTime.UtcNow;

        var invoices = await db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Site)
            .Where(i => !i.IsArchived && i.BalanceDue > 0 && i.Status != InvoiceStatus.Void && i.Status != InvoiceStatus.Paid)
            .OrderByDescending(i => i.BalanceDue)
            .Take(100)
            .ToListAsync();

        return invoices.Select(i =>
        {
            var dueDate = i.DueDate ?? i.InvoiceDate ?? i.CreatedAt;
            var daysOverdue = Math.Max(0, (int)(now - dueDate).TotalDays);
            var bucket = daysOverdue switch
            {
                0 => "Current",
                <= 30 => "1-30",
                <= 60 => "31-60",
                <= 90 => "61-90",
                _ => "90+"
            };

            return new ARAgingItem
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                CustomerName = i.Customer?.Name,
                SiteName = i.Site?.Name,
                InvoiceDate = i.InvoiceDate ?? i.CreatedAt,
                DueDate = i.DueDate,
                Total = i.Total,
                BalanceDue = i.BalanceDue,
                DaysOverdue = daysOverdue,
                AgingBucket = bucket,
            };
        })
        .OrderByDescending(a => a.DaysOverdue)
        .ToList();
    }

    public async Task<List<AssetRepairTrendItem>> GetAssetRepairTrendsAsync()
    {
        var now = DateTime.UtcNow;

        var assets = await db.Assets
            .Include(a => a.ServiceLogs)
            .Where(a => !a.IsArchived)
            .ToListAsync();

        return assets
            .Where(a => !string.IsNullOrWhiteSpace(a.AssetType))
            .GroupBy(a => a.AssetType!)
            .Select(g =>
            {
                var allLogs = g.SelectMany(a => a.ServiceLogs).ToList();
                var repairLogs = allLogs.Where(l =>
                    l.ServiceType != "Filter Change" && l.ServiceType != "Inspection").ToList();

                var topServiceType = repairLogs
                    .GroupBy(l => l.ServiceType)
                    .OrderByDescending(sg => sg.Count())
                    .FirstOrDefault()?.Key;

                var underWarranty = g.Count(a =>
                    a.WarrantyExpiry.HasValue && a.WarrantyExpiry.Value > now);
                var outOfWarranty = g.Count() - underWarranty;

                return new AssetRepairTrendItem
                {
                    AssetType = g.Key,
                    AssetCount = g.Count(),
                    RepairCount = repairLogs.Count,
                    TotalCost = repairLogs.Sum(l => l.Cost ?? 0),
                    TopServiceType = topServiceType,
                    UnderWarranty = underWarranty,
                    OutOfWarranty = outOfWarranty,
                };
            })
            .OrderByDescending(t => t.RepairCount)
            .ToList();
    }

    public async Task<List<InventoryUsageItem>> GetInventoryUsageAsync()
    {
        var items = await db.InventoryItems
            .Include(i => i.Product)
            .Where(i => !i.IsArchived)
            .OrderBy(i => i.Quantity <= i.MinThreshold ? 0 : 1)
            .ThenBy(i => i.Quantity)
            .Take(100)
            .ToListAsync();

        return items.Select(i => new InventoryUsageItem
        {
            ProductId = i.ProductId ?? 0,
            ProductName = i.Product?.Name ?? i.Name,
            SKU = i.SKU,
            CurrentStock = i.Quantity,
            MinThreshold = i.MinThreshold,
            Cost = i.Cost,
            PreferredSupplier = i.PreferredSupplier,
            LastRestockedDate = i.LastRestockedDate,
        }).ToList();
    }

    public async Task<List<AgreementRenewalItem>> GetAgreementRenewalsAsync()
    {
        var now = DateTime.UtcNow;

        var agreements = await db.ServiceAgreements
            .Include(sa => sa.Customer)
            .Where(sa => !sa.IsArchived &&
                (sa.Status == AgreementStatus.Active || sa.Status == AgreementStatus.Expiring))
            .OrderBy(sa => sa.EndDate)
            .Take(100)
            .ToListAsync();

        return agreements.Select(sa =>
        {
            var daysUntilExpiry = (int)(sa.EndDate - now).TotalDays;

            return new AgreementRenewalItem
            {
                AgreementId = sa.Id,
                AgreementNumber = sa.AgreementNumber,
                CustomerName = sa.Customer?.Name,
                TradeType = sa.TradeType,
                CoverageLevel = sa.CoverageLevel.ToString(),
                EndDate = sa.EndDate,
                DaysUntilExpiry = daysUntilExpiry,
                VisitsIncluded = sa.VisitsIncluded,
                VisitsUsed = sa.VisitsUsed,
                Fee = sa.Fee,
                AutoRenew = sa.AutoRenew,
                Status = sa.Status.ToString(),
            };
        }).ToList();
    }
}
