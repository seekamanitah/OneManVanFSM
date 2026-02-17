using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class DashboardService(AppDbContext db, IServiceAgreementService agreementService) : IDashboardService
{
    public async Task<DashboardData> GetDashboardDataAsync(DashboardPeriod period = DashboardPeriod.Today)
    {
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Determine date range for scheduled jobs based on period
        var rangeStart = today;
        var rangeEnd = period switch
        {
            DashboardPeriod.Today => today.AddDays(1),
            DashboardPeriod.ThisWeek => weekStart.AddDays(7),
            DashboardPeriod.ThisMonth => monthStart.AddMonths(1),
            _ => today.AddDays(1)
        };
        var timeEntriesStart = period switch
        {
            DashboardPeriod.Today => today,
            DashboardPeriod.ThisWeek => weekStart,
            DashboardPeriod.ThisMonth => monthStart,
            _ => today
        };

        // Scheduled jobs in period (not completed/cancelled)
        var scheduledJobs = await db.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedEmployee)
            .Where(j => !j.IsArchived
                && j.ScheduledDate != null
                && j.ScheduledDate.Value.Date >= rangeStart
                && j.ScheduledDate.Value.Date < rangeEnd
                && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.ScheduledDate)
            .ThenBy(j => j.ScheduledTime)
            .Take(10)
            .ToListAsync();

        // Finished jobs with no invoice
        var finishedJobs = await db.Jobs
            .Include(j => j.Customer)
            .Where(j => !j.IsArchived
                && j.Status == JobStatus.Completed
                && j.InvoiceId == null)
            .OrderByDescending(j => j.CompletedDate)
            .Take(10)
            .ToListAsync();

        // Unpaid invoices
        var unpaidInvoices = await db.Invoices
            .Include(i => i.Customer)
            .Where(i => !i.IsArchived
                && i.BalanceDue > 0
                && i.Status != InvoiceStatus.Paid
                && i.Status != InvoiceStatus.Void)
            .OrderBy(i => i.DueDate)
            .Take(10)
            .ToListAsync();

        // Unscheduled jobs (approved/quoted but no date)
        var unscheduledJobs = await db.Jobs
            .Include(j => j.Customer)
            .Where(j => !j.IsArchived
                && j.ScheduledDate == null
                && (j.Status == JobStatus.Approved || j.Status == JobStatus.Quoted || j.Status == JobStatus.Lead))
            .OrderByDescending(j => j.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Pending estimates (Draft or Sent)
        var pendingEstimates = await db.Estimates
            .Include(e => e.Customer)
            .Where(e => !e.IsArchived
                && (e.Status == EstimateStatus.Draft || e.Status == EstimateStatus.Sent))
            .OrderBy(e => e.ExpiryDate)
            .Take(10)
            .ToListAsync();

        // Employee summaries from time entries
        var employees = await db.Employees
            .Where(e => !e.IsArchived && e.Status == EmployeeStatus.Active)
            .ToListAsync();

        var timeEntries = await db.TimeEntries
            .Include(t => t.Job)
            .Where(t => t.StartTime >= timeEntriesStart)
            .ToListAsync();

        var expenses = await db.Expenses
            .Where(e => e.Status == ExpenseStatus.Pending)
            .ToListAsync();

        var employeeSummaries = employees.Select(e =>
        {
            var empTimes = timeEntries.Where(t => t.EmployeeId == e.Id).ToList();
            var empExpenses = expenses.Where(x => x.EmployeeId == e.Id).ToList();
            var activeEntry = empTimes.FirstOrDefault(t => t.EndTime == null);
            return new EmployeeSummary
            {
                EmployeeId = e.Id,
                Name = e.Name,
                HoursToday = empTimes.Where(t => t.StartTime.Date == today).Sum(t => t.Hours),
                HoursThisWeek = empTimes.Sum(t => t.Hours),
                PendingExpenses = empExpenses.Sum(x => x.Amount),
                IsOnShift = activeEntry is not null,
                CurrentJobTitle = activeEntry?.Job?.Title,
            };
        }).ToList();

        // Recent activity from entity changes
        var recentActivities = new List<ActivityItem>();
        var recentJobs = await db.Jobs
            .Include(j => j.Customer)
            .OrderByDescending(j => j.UpdatedAt)
            .Take(4)
            .ToListAsync();
        foreach (var j in recentJobs)
        {
            recentActivities.Add(new ActivityItem
            {
                Description = $"Job {j.JobNumber} - {j.Status}" + (j.Customer != null ? $" ({j.Customer.Name})" : ""),
                Icon = j.Status == JobStatus.Completed ? "bi-check-circle-fill" : "bi-wrench",
                Timestamp = j.UpdatedAt,
                EntityType = "Job",
                EntityId = j.Id,
            });
        }

        var recentInvoices = await db.Invoices
            .Include(i => i.Customer)
            .OrderByDescending(i => i.UpdatedAt)
            .Take(2)
            .ToListAsync();
        foreach (var i in recentInvoices)
        {
            recentActivities.Add(new ActivityItem
            {
                Description = $"Invoice {i.InvoiceNumber} - {i.Status} ({i.Total:C})",
                Icon = i.Status == InvoiceStatus.Paid ? "bi-cash-coin" : "bi-receipt",
                Timestamp = i.UpdatedAt,
                EntityType = "Invoice",
                EntityId = i.Id,
            });
        }

        var recentEstimates = await db.Estimates
            .Include(e => e.Customer)
            .OrderByDescending(e => e.UpdatedAt)
            .Take(2)
            .ToListAsync();
        foreach (var e in recentEstimates)
        {
            recentActivities.Add(new ActivityItem
            {
                Description = $"Estimate {e.EstimateNumber} - {e.Status}" + (e.Customer != null ? $" ({e.Customer.Name})" : ""),
                Icon = "bi-file-earmark-plus",
                Timestamp = e.UpdatedAt,
                EntityType = "Estimate",
                EntityId = e.Id,
            });
        }

        recentActivities = recentActivities.OrderByDescending(a => a.Timestamp).Take(8).ToList();

        // Low stock items (quantity below min threshold)
        var lowStockItems = await db.InventoryItems
            .Where(i => !i.IsArchived && i.Quantity <= i.MinThreshold && i.MinThreshold > 0)
            .OrderBy(i => i.Quantity)
            .Take(10)
            .Select(i => new LowStockItem
            {
                Id = i.Id,
                Name = i.Name,
                Quantity = i.Quantity,
                MinThreshold = i.MinThreshold,
                Location = i.Location.ToString(),
            })
            .ToListAsync();

        // Expiring service agreements (within 30 days)
        var expiringAgreements = await db.ServiceAgreements
            .Include(a => a.Customer)
            .Where(a => !a.IsArchived
                && (a.Status == AgreementStatus.Active || a.Status == AgreementStatus.Expiring)
                && a.EndDate <= today.AddDays(30)
                && a.EndDate >= today)
            .OrderBy(a => a.EndDate)
            .Take(10)
            .Select(a => new ExpiringAgreement
            {
                Id = a.Id,
                AgreementNumber = a.AgreementNumber,
                CustomerName = a.Customer != null ? a.Customer.Name : null,
                EndDate = a.EndDate,
                DaysUntilExpiry = (int)(a.EndDate - today).TotalDays,
                CoverageLevel = a.CoverageLevel.ToString(),
            })
            .ToListAsync();

        // Urgent quick notes (recent, not archived)
        var urgentNotes = await db.QuickNotes
            .Where(n => n.IsUrgent && n.Status != QuickNoteStatus.Archived)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new UrgentNote
            {
                Id = n.Id,
                Title = n.Title,
                Text = n.Text,
                Category = n.Category,
                EntityType = n.EntityType,
                EntityId = n.EntityId,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync();

        // Maintenance due assets (overdue or due within 14 days)
        var maintenanceDueAssets = await db.Assets
            .Include(a => a.Customer)
            .Include(a => a.Site)
            .Where(a => !a.IsArchived
                && a.Status == AssetStatus.Active
                && a.NextServiceDue != null
                && a.NextServiceDue.Value <= today.AddDays(14))
            .OrderBy(a => a.NextServiceDue)
            .Take(10)
            .Select(a => new MaintenanceDueAsset
            {
                Id = a.Id,
                Name = a.Name,
                AssetType = a.AssetType,
                CustomerName = a.Customer != null ? a.Customer.Name : null,
                SiteName = a.Site != null ? a.Site.Name : null,
                NextServiceDue = a.NextServiceDue,
                LastServiceDate = a.LastServiceDate,
                DaysOverdue = a.NextServiceDue != null ? (int)(today - a.NextServiceDue.Value.Date).TotalDays : 0,
            })
            .ToListAsync();

        // Expiring warranty assets (any warranty expiring within 90 days or already expired within last 30 days)
        var warrantyWindowStart = today.AddDays(-30);
        var warrantyWindowEnd = today.AddDays(90);
        var expiringWarrantyAssets = await db.Assets
            .Include(a => a.Customer)
            .Where(a => !a.IsArchived
                && !a.NoWarranty
                && a.Status == AssetStatus.Active
                && (
                    (a.LaborWarrantyExpiry != null && a.LaborWarrantyTermYears > 0 && a.LaborWarrantyExpiry.Value >= warrantyWindowStart && a.LaborWarrantyExpiry.Value <= warrantyWindowEnd)
                    || (a.PartsWarrantyExpiry != null && a.PartsWarrantyTermYears > 0 && a.PartsWarrantyExpiry.Value >= warrantyWindowStart && a.PartsWarrantyExpiry.Value <= warrantyWindowEnd)
                    || (a.CompressorWarrantyExpiry != null && a.CompressorWarrantyTermYears > 0 && a.CompressorWarrantyExpiry.Value >= warrantyWindowStart && a.CompressorWarrantyExpiry.Value <= warrantyWindowEnd)
                ))
            .OrderBy(a => a.WarrantyExpiry)
            .Take(10)
            .ToListAsync();

        var expiringWarranties = expiringWarrantyAssets.Select(a =>
        {
            // Only consider warranty categories with term years > 0
            var validExpiries = new (DateTime? Expiry, int? Term)[]
            {
                (a.LaborWarrantyExpiry, a.LaborWarrantyTermYears),
                (a.PartsWarrantyExpiry, a.PartsWarrantyTermYears),
                (a.CompressorWarrantyExpiry, a.CompressorWarrantyTermYears),
            };
            var earliest = validExpiries
                .Where(x => x.Expiry.HasValue && (x.Term ?? 0) > 0)
                .Select(x => x.Expiry!.Value)
                .OrderBy(d => d)
                .FirstOrDefault();
            var daysUntil = earliest != default ? (int)(earliest - today).TotalDays : 0;

            var alerts = new List<string>();
            if (a.LaborWarrantyExpiry.HasValue && (a.LaborWarrantyTermYears ?? 0) > 0 && a.LaborWarrantyExpiry.Value <= warrantyWindowEnd)
                alerts.Add(a.LaborWarrantyExpiry.Value < today ? "Labor expired" : $"Labor in {(int)(a.LaborWarrantyExpiry.Value - today).TotalDays}d");
            if (a.PartsWarrantyExpiry.HasValue && (a.PartsWarrantyTermYears ?? 0) > 0 && a.PartsWarrantyExpiry.Value <= warrantyWindowEnd)
                alerts.Add(a.PartsWarrantyExpiry.Value < today ? "Parts expired" : $"Parts in {(int)(a.PartsWarrantyExpiry.Value - today).TotalDays}d");
            if (a.CompressorWarrantyExpiry.HasValue && (a.CompressorWarrantyTermYears ?? 0) > 0 && a.CompressorWarrantyExpiry.Value <= warrantyWindowEnd)
                alerts.Add(a.CompressorWarrantyExpiry.Value < today ? "Compressor expired" : $"Compressor in {(int)(a.CompressorWarrantyExpiry.Value - today).TotalDays}d");

            return new ExpiringWarrantyAsset
            {
                Id = a.Id,
                Name = a.Name,
                AssetType = a.AssetType,
                CustomerName = a.Customer?.Name,
                LaborWarrantyExpiry = a.LaborWarrantyExpiry,
                PartsWarrantyExpiry = a.PartsWarrantyExpiry,
                CompressorWarrantyExpiry = a.CompressorWarrantyExpiry,
                WarrantyAlert = string.Join(", ", alerts),
                DaysUntilExpiry = daysUntil,
            };
        }).ToList();

        // KPI aggregates
        var allJobsInPeriod = await db.Jobs
            .Where(j => !j.IsArchived && j.CreatedAt >= timeEntriesStart)
            .ToListAsync();
        var paymentsInPeriod = await db.Payments
            .Where(p => p.PaymentDate >= timeEntriesStart)
            .SumAsync(p => p.Amount);
        var totalOutstanding = await db.Invoices
            .Where(i => !i.IsArchived && i.BalanceDue > 0 && i.Status != InvoiceStatus.Void)
            .SumAsync(i => i.BalanceDue);
        var activeCustomerCount = await db.Customers.CountAsync(c => !c.IsArchived);
        var activeAgreementCount = await db.ServiceAgreements
            .CountAsync(a => !a.IsArchived && a.Status == AgreementStatus.Active);

        var kpis = new DashboardKPIs
        {
            TotalJobsInPeriod = allJobsInPeriod.Count,
            CompletedJobsInPeriod = allJobsInPeriod.Count(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Closed),
            RevenueCollected = paymentsInPeriod,
            OutstandingBalance = totalOutstanding,
            ActiveCustomers = activeCustomerCount,
            ActiveAgreements = activeAgreementCount,
        };

        return new DashboardData
        {
            ScheduledJobs = scheduledJobs,
            FinishedJobsWaitingInvoice = finishedJobs,
            UnpaidInvoices = unpaidInvoices,
            UnscheduledJobs = unscheduledJobs,
            PendingEstimates = pendingEstimates,
            EmployeeSummaries = employeeSummaries,
            TotalPendingExpenses = employeeSummaries.Sum(e => e.PendingExpenses),
            RecentActivities = recentActivities,
            LowStockItems = lowStockItems,
            ExpiringAgreements = expiringAgreements,
            UrgentNotes = urgentNotes,
            MaintenanceDueAssets = maintenanceDueAssets,
            ExpiringWarranties = expiringWarranties,
            KPIs = kpis,
        };
    }

    public async Task<AgreementAutomationResult> ProcessAgreementAutomationAsync()
    {
        var statusesUpdated = await agreementService.UpdateAgreementStatusesAsync();
        var renewed = await agreementService.ProcessAutoRenewalsAsync();
        var jobsGenerated = await agreementService.GenerateAgreementJobsAsync();

        return new AgreementAutomationResult
        {
            StatusesUpdated = statusesUpdated,
            AgreementsRenewed = renewed,
            JobsGenerated = jobsGenerated,
        };
    }
}
