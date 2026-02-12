using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class ServiceAgreementService : IServiceAgreementService
{
    private readonly AppDbContext _db;
    public ServiceAgreementService(AppDbContext db) => _db = db;

    public async Task<List<AgreementListItem>> GetAgreementsAsync(AgreementFilter? filter = null)
    {
        var showArchived = filter?.ShowArchived ?? false;
        var query = _db.ServiceAgreements.Where(a => a.IsArchived == showArchived).AsQueryable();
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(a => a.AgreementNumber.ToLower().Contains(term) ||
                    (a.Title != null && a.Title.ToLower().Contains(term)) ||
                    (a.Customer != null && a.Customer.Name.ToLower().Contains(term)));
            }
            if (filter.Status.HasValue) query = query.Where(a => a.Status == filter.Status.Value);
            if (filter.CoverageLevel.HasValue) query = query.Where(a => a.CoverageLevel == filter.CoverageLevel.Value);
            query = filter.SortBy?.ToLower() switch
            {
                "title" => filter.SortDescending ? query.OrderByDescending(a => a.Title) : query.OrderBy(a => a.Title),
                "status" => filter.SortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
                "fee" => filter.SortDescending ? query.OrderByDescending(a => a.Fee) : query.OrderBy(a => a.Fee),
                _ => filter.SortDescending ? query.OrderByDescending(a => a.EndDate) : query.OrderBy(a => a.EndDate)
            };
        }
        else query = query.OrderBy(a => a.EndDate);

        return await query.Select(a => new AgreementListItem
        {
            Id = a.Id, AgreementNumber = a.AgreementNumber, Title = a.Title,
            CustomerName = a.Customer != null ? a.Customer.Name : null,
            CompanyName = a.Company != null ? a.Company.Name : null,
            TradeType = a.TradeType, BillingFrequency = a.BillingFrequency,
            CoverageLevel = a.CoverageLevel, Status = a.Status,
            StartDate = a.StartDate, EndDate = a.EndDate,
            VisitsIncluded = a.VisitsIncluded, VisitsUsed = a.VisitsUsed, Fee = a.Fee
        }).ToListAsync();
    }

    public async Task<AgreementFullDetail?> GetAgreementAsync(int id)
    {
        return await _db.ServiceAgreements.Where(a => a.Id == id && !a.IsArchived)
            .Select(a => new AgreementFullDetail
            {
                Id = a.Id, AgreementNumber = a.AgreementNumber, Title = a.Title,
                CoverageLevel = a.CoverageLevel, Status = a.Status,
                StartDate = a.StartDate, EndDate = a.EndDate,
                VisitsIncluded = a.VisitsIncluded, VisitsUsed = a.VisitsUsed,
                Fee = a.Fee, TradeType = a.TradeType, BillingFrequency = a.BillingFrequency,
                DiscountPercent = a.DiscountPercent, RenewalDate = a.RenewalDate, AutoRenew = a.AutoRenew,
                AddOns = a.AddOns, Notes = a.Notes,
                CustomerId = a.CustomerId, CustomerName = a.Customer != null ? a.Customer.Name : null,
                CompanyId = a.CompanyId, CompanyName = a.Company != null ? a.Company.Name : null,
                SiteId = a.SiteId, SiteName = a.Site != null ? a.Site.Name : null,
                CoveredAssets = a.ServiceAgreementAssets.Select(sa => new CoveredAssetDto
                {
                    Id = sa.Id, AssetId = sa.AssetId,
                    AssetName = sa.Asset != null ? sa.Asset.Name : null,
                    CoverageNotes = sa.CoverageNotes
                }).ToList(),
                CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt
            }).FirstOrDefaultAsync();
    }

    public async Task<ServiceAgreement> CreateAgreementAsync(AgreementEditModel model)
    {
        var num = model.AgreementNumber;
        if (string.IsNullOrWhiteSpace(num))
        {
            var count = await _db.ServiceAgreements.CountAsync() + 1;
            num = $"SA-{count:D5}";
        }
        var sa = new ServiceAgreement
        {
            AgreementNumber = num, Title = model.Title, CoverageLevel = model.CoverageLevel,
            Status = model.Status, StartDate = model.StartDate, EndDate = model.EndDate,
            VisitsIncluded = model.VisitsIncluded, VisitsUsed = model.VisitsUsed,
            Fee = model.Fee, TradeType = model.TradeType, BillingFrequency = model.BillingFrequency,
            DiscountPercent = model.DiscountPercent, RenewalDate = model.RenewalDate, AutoRenew = model.AutoRenew,
            AddOns = model.AddOns, Notes = model.Notes,
            CustomerId = model.CustomerId, CompanyId = model.CompanyId, SiteId = model.SiteId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.ServiceAgreements.Add(sa);
        await _db.SaveChangesAsync();
        return sa;
    }

    public async Task<ServiceAgreement> UpdateAgreementAsync(int id, AgreementEditModel model)
    {
        var a = await _db.ServiceAgreements.FindAsync(id) ?? throw new InvalidOperationException("Agreement not found.");
        a.Title = model.Title; a.CoverageLevel = model.CoverageLevel; a.Status = model.Status;
        a.StartDate = model.StartDate; a.EndDate = model.EndDate;
        a.VisitsIncluded = model.VisitsIncluded; a.VisitsUsed = model.VisitsUsed;
        a.Fee = model.Fee; a.TradeType = model.TradeType; a.BillingFrequency = model.BillingFrequency;
        a.DiscountPercent = model.DiscountPercent; a.RenewalDate = model.RenewalDate; a.AutoRenew = model.AutoRenew;
        a.AddOns = model.AddOns; a.Notes = model.Notes;
        a.CustomerId = model.CustomerId; a.CompanyId = model.CompanyId; a.SiteId = model.SiteId;
        a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return a;
    }

    public async Task<bool> ArchiveAgreementAsync(int id)
    {
        var a = await _db.ServiceAgreements.FindAsync(id);
        if (a is null) return false;
        a.IsArchived = true; a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAgreementAsync(int id)
    {
        var a = await _db.ServiceAgreements.FindAsync(id);
        if (a is null) return false;
        a.IsArchived = false; a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAgreementPermanentlyAsync(int id)
    {
        var a = await _db.ServiceAgreements.FindAsync(id);
        if (a is null) return false;
        _db.ServiceAgreements.Remove(a);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveAgreementsAsync(List<int> ids)
    {
        var items = await _db.ServiceAgreements.Where(a => ids.Contains(a.Id) && !a.IsArchived).ToListAsync();
        foreach (var a in items) { a.IsArchived = true; a.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkRestoreAgreementsAsync(List<int> ids)
    {
        var items = await _db.ServiceAgreements.Where(a => ids.Contains(a.Id) && a.IsArchived).ToListAsync();
        foreach (var a in items) { a.IsArchived = false; a.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkDeleteAgreementsPermanentlyAsync(List<int> ids)
    {
        var items = await _db.ServiceAgreements.Where(a => ids.Contains(a.Id)).ToListAsync();
        _db.ServiceAgreements.RemoveRange(items);
        await _db.SaveChangesAsync();
        return items.Count;
    }

    /// <summary>
    /// Scans active service agreements that have remaining visits and creates scheduled
    /// maintenance jobs if one doesn't already exist for the current service window.
    /// Returns the number of jobs generated.
    /// </summary>
    public async Task<int> GenerateAgreementJobsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var activeAgreements = await _db.ServiceAgreements
            .Include(a => a.ServiceAgreementAssets).ThenInclude(sa => sa.Asset)
            .Include(a => a.Customer)
            .Where(a => !a.IsArchived
                && a.Status == AgreementStatus.Active
                && a.VisitsUsed < a.VisitsIncluded
                && a.EndDate >= today)
            .ToListAsync();

        int jobsCreated = 0;

        foreach (var agreement in activeAgreements)
        {
            // Calculate next visit window: divide agreement period evenly by visits included
            var totalDays = (agreement.EndDate - agreement.StartDate).TotalDays;
            if (totalDays <= 0) continue;
            var intervalDays = totalDays / agreement.VisitsIncluded;
            var nextVisitDate = agreement.StartDate.AddDays(intervalDays * agreement.VisitsUsed);

            // Only create if the next visit is within 30 days from now
            if (nextVisitDate > today.AddDays(30)) continue;

            // Check if a maintenance job already exists for this agreement in this window
            var windowStart = nextVisitDate.AddDays(-7);
            var windowEnd = nextVisitDate.AddDays(30);
            var existingJob = await _db.Jobs.AnyAsync(j =>
                !j.IsArchived
                && j.Description != null && j.Description.Contains($"SA:{agreement.Id}")
                && j.ScheduledDate >= windowStart
                && j.ScheduledDate <= windowEnd);

            if (existingJob) continue;

            var jobCount = await _db.Jobs.CountAsync() + 1;
            var job = new Job
            {
                JobNumber = $"JOB-{jobCount:D5}",
                Title = $"Scheduled Maintenance - {agreement.Title ?? agreement.AgreementNumber}",
                Description = $"Auto-generated from Service Agreement {agreement.AgreementNumber} (SA:{agreement.Id}). Visit {agreement.VisitsUsed + 1} of {agreement.VisitsIncluded}.",
                Status = JobStatus.Scheduled,
                Priority = JobPriority.Standard,
                TradeType = agreement.TradeType,
                JobType = "Maintenance",
                ScheduledDate = nextVisitDate < today ? today : nextVisitDate,
                EstimatedDuration = 2,
                CustomerId = agreement.CustomerId,
                CompanyId = agreement.CompanyId,
                SiteId = agreement.SiteId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();

            // Link covered assets to the job
            foreach (var coveredAsset in agreement.ServiceAgreementAssets)
            {
                _db.JobAssets.Add(new JobAsset
                {
                    JobId = job.Id,
                    AssetId = coveredAsset.AssetId,
                    Role = "Serviced",
                    Notes = coveredAsset.CoverageNotes
                });
            }
            await _db.SaveChangesAsync();
            jobsCreated++;
        }

        return jobsCreated;
    }

    /// <summary>
    /// Updates agreement statuses: marks past-EndDate as Expired, within-30-days as Expiring.
    /// Returns the number of agreements updated.
    /// </summary>
    public async Task<int> UpdateAgreementStatusesAsync()
    {
        var today = DateTime.UtcNow.Date;
        int updated = 0;

        // Mark expired agreements
        var expired = await _db.ServiceAgreements
            .Where(a => !a.IsArchived
                && (a.Status == AgreementStatus.Active || a.Status == AgreementStatus.Expiring))
            .Where(a => a.EndDate < today)
            .ToListAsync();

        foreach (var a in expired.Where(a => a.Status != AgreementStatus.Expired))
        {
            a.Status = AgreementStatus.Expired;
            a.UpdatedAt = DateTime.UtcNow;
            updated++;
        }

        // Mark expiring-soon agreements (within 30 days)
        var expiringSoon = await _db.ServiceAgreements
            .Where(a => !a.IsArchived
                && a.Status == AgreementStatus.Active
                && a.EndDate >= today
                && a.EndDate <= today.AddDays(30))
            .ToListAsync();

        foreach (var a in expiringSoon)
        {
            a.Status = AgreementStatus.Expiring;
            a.UpdatedAt = DateTime.UtcNow;
            updated++;
        }

        if (updated > 0) await _db.SaveChangesAsync();
        return updated;
    }

    /// <summary>
    /// Processes auto-renewal for expired agreements that have AutoRenew enabled.
    /// Creates a new term with same duration, resets visits, and sets status to Active.
    /// Returns the number of agreements renewed.
    /// </summary>
    public async Task<int> ProcessAutoRenewalsAsync()
    {
        var today = DateTime.UtcNow.Date;
        int renewed = 0;

        var autoRenewable = await _db.ServiceAgreements
            .Where(a => !a.IsArchived
                && a.AutoRenew
                && a.Status == AgreementStatus.Expired
                && a.EndDate < today)
            .ToListAsync();

        foreach (var a in autoRenewable)
        {
            // Calculate original term length and create a new period
            var termDays = (int)(a.EndDate - a.StartDate).TotalDays;
            if (termDays <= 0) termDays = 365; // default to 1 year

            a.StartDate = a.EndDate; // new term starts where old one ended
            a.EndDate = a.StartDate.AddDays(termDays);
            a.RenewalDate = today;
            a.VisitsUsed = 0;
            a.Status = AgreementStatus.Active;
            a.UpdatedAt = DateTime.UtcNow;
            renewed++;
        }

        if (renewed > 0) await _db.SaveChangesAsync();
        return renewed;
    }
}
