using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileEstimateService : IMobileEstimateService
{
    private readonly AppDbContext _db;
    public MobileEstimateService(AppDbContext db) => _db = db;

    public async Task<List<MobileEstimateCard>> GetEstimatesAsync(MobileEstimateFilter? filter = null)
    {
        var query = _db.Estimates
            .Include(e => e.Customer)
            .Include(e => e.Company)
            .Include(e => e.Site)
            .Include(e => e.Lines)
            .Where(e => !e.IsArchived)
            .AsQueryable();

        if (filter?.Status.HasValue == true)
            query = query.Where(e => e.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(e => e.EstimateNumber.ToLower().Contains(s)
                || (e.Title != null && e.Title.ToLower().Contains(s))
                || (e.Customer != null && e.Customer.Name.ToLower().Contains(s)));
        }

        var estimates = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();

        return estimates.Select(e => new MobileEstimateCard
        {
            Id = e.Id,
            EstimateNumber = e.EstimateNumber,
            Title = e.Title,
            Status = e.Status,
            Priority = e.Priority,
            CustomerName = e.Customer?.Name,
            CompanyName = e.Company?.Name,
            SiteAddress = e.Site?.Address,
            Total = e.Total,
            ExpiryDate = e.ExpiryDate,
            CreatedAt = e.CreatedAt,
            LineCount = e.Lines.Count,
        }).ToList();
    }

    public async Task<MobileEstimateDetail?> GetEstimateDetailAsync(int id)
    {
        var e = await _db.Estimates
            .Include(e => e.Customer)
            .Include(e => e.Company)
            .Include(e => e.Site)
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (e == null) return null;

        // Check for a linked job
        var linkedJob = await _db.Jobs.AsNoTracking()
            .Where(j => j.EstimateId == id && !j.IsArchived)
            .Select(j => new { j.Id, j.JobNumber })
            .FirstOrDefaultAsync();

        return new MobileEstimateDetail
        {
            Id = e.Id,
            EstimateNumber = e.EstimateNumber,
            Title = e.Title,
            Status = e.Status,
            Priority = e.Priority,
            TradeType = e.TradeType,
            SystemType = e.SystemType,
            CustomerName = e.Customer?.Name,
            CustomerPhone = e.Customer?.PrimaryPhone,
            CompanyId = e.CompanyId,
            CompanyName = e.Company?.Name,
            SiteName = e.Site?.Name,
            SiteAddress = e.Site != null ? $"{e.Site.Address}, {e.Site.City}, {e.Site.State} {e.Site.Zip}" : null,
            SiteId = e.SiteId,
            CustomerId = e.CustomerId,
            Subtotal = e.Subtotal,
            MarkupPercent = e.MarkupPercent,
            TaxPercent = e.TaxPercent,
            Total = e.Total,
            DepositRequired = e.DepositRequired,
            ExpiryDate = e.ExpiryDate,
            Notes = e.Notes,
            NeedsReview = e.NeedsReview,
            LinkedJobId = linkedJob?.Id,
            LinkedJobNumber = linkedJob?.JobNumber,
            CreatedAt = e.CreatedAt,
            Lines = e.Lines.OrderBy(l => l.SortOrder).Select(l => new MobileEstimateLine
            {
                Description = l.Description,
                LineType = l.LineType,
                Section = l.Section,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                Unit = l.Unit,
            }).ToList(),
        };
    }

    public async Task<Estimate> QuickCreateAsync(MobileEstimateQuickCreate model)
    {
        var count = await _db.Estimates.CountAsync() + 1;
        var estimate = new Estimate
        {
            EstimateNumber = $"EST-{count:D5}",
            Title = model.Title ?? "Untitled Estimate",
            Status = EstimateStatus.Draft,
            Priority = model.Priority,
            TradeType = model.TradeType,
            CustomerId = model.CustomerId,
            SiteId = model.SiteId,
            Notes = model.Notes,
            NeedsReview = true,
            CreatedFrom = "mobile",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync();
        return estimate;
    }

    public async Task<Estimate> FullCreateAsync(MobileEstimateFullCreate model)
    {
        var count = await _db.Estimates.CountAsync() + 1;
        var lines = model.Lines.Select((l, i) => new EstimateLine
        {
            Description = l.Description,
            LineType = l.LineType,
            Section = l.Section,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LineTotal = l.Quantity * l.UnitPrice,
            Unit = l.Unit,
            SortOrder = i,
        }).ToList();

        var subtotal = lines.Sum(l => l.LineTotal);
        var markupAmount = subtotal * model.MarkupPercent / 100;
        var taxableAmount = subtotal + markupAmount;
        var taxAmount = taxableAmount * model.TaxPercent / 100;
        var total = taxableAmount + taxAmount;

        var estimate = new Estimate
        {
            EstimateNumber = $"EST-{count:D5}",
            Title = model.Title ?? "Untitled Estimate",
            Status = EstimateStatus.Draft,
            Priority = model.Priority,
            TradeType = model.TradeType,
            SystemType = model.SystemType,
            CustomerId = model.CustomerId,
            SiteId = model.SiteId,
            Notes = model.Notes,
            ExpiryDate = model.ExpiryDate,
            MarkupPercent = model.MarkupPercent,
            TaxPercent = model.TaxPercent,
            Subtotal = subtotal,
            Total = total,
            DepositRequired = model.DepositRequired,
            NeedsReview = false,
            CreatedFrom = "mobile",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lines = lines,
        };
        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync();
        return estimate;
    }

    public async Task<bool> UpdateStatusAsync(int id, EstimateStatus status)
    {
        var e = await _db.Estimates.FindAsync(id);
        if (e is null) return false;

        var wasNotApproved = e.Status != EstimateStatus.Approved;
        e.Status = status;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Auto-create a Job when Estimate is approved (pipeline automation)
        if (status == EstimateStatus.Approved && wasNotApproved)
            await CreateJobFromEstimateAsync(e);

        return true;
    }

    public async Task<bool> DeleteEstimateAsync(int id)
    {
        var estimate = await _db.Estimates.FindAsync(id);
        if (estimate is null) return false;
        estimate.IsArchived = true;
        estimate.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Auto-creates a Job from an approved Estimate, carrying over customer, site,
    /// trade, material list, and pricing data. Matches web app behavior.
    /// </summary>
    private async Task CreateJobFromEstimateAsync(Estimate estimate)
    {
        var existing = await _db.Jobs.AnyAsync(j => j.EstimateId == estimate.Id && !j.IsArchived);
        if (existing) return;

        var jobCount = await _db.Jobs.CountAsync() + 1;
        var job = new Job
        {
            JobNumber = $"JOB-{jobCount:D5}",
            Title = estimate.Title,
            Description = $"Auto-created from Estimate {estimate.EstimateNumber}",
            Status = JobStatus.Approved,
            Priority = estimate.Priority,
            TradeType = estimate.TradeType,
            SystemType = estimate.SystemType,
            EstimatedTotal = estimate.Total,
            Notes = estimate.Notes,
            CustomerId = estimate.CustomerId,
            CompanyId = estimate.CompanyId,
            SiteId = estimate.SiteId,
            EstimateId = estimate.Id,
            MaterialListId = estimate.MaterialListId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        estimate.Job = job;
        await _db.SaveChangesAsync();
    }
}
