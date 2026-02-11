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
}
