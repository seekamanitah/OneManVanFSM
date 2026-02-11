using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileServiceAgreementService : IMobileServiceAgreementService
{
    private readonly AppDbContext _db;

    public MobileServiceAgreementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MobileAgreementCard>> GetAgreementsAsync(string? statusFilter = null, string? search = null)
    {
        var query = _db.ServiceAgreements
            .Include(a => a.Customer)
            .Include(a => a.Site)
            .Where(a => !a.IsArchived)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            if (Enum.TryParse<AgreementStatus>(statusFilter, out var status))
            {
                query = query.Where(a => a.Status == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(a =>
                a.AgreementNumber.ToLower().Contains(term) ||
                (a.Title != null && a.Title.ToLower().Contains(term)) ||
                (a.Customer != null && a.Customer.Name.ToLower().Contains(term)) ||
                (a.Site != null && a.Site.Name.ToLower().Contains(term)));
        }

        return await query
            .OrderByDescending(a => a.Status == AgreementStatus.Active)
            .ThenBy(a => a.EndDate)
            .Select(a => new MobileAgreementCard
            {
                Id = a.Id,
                AgreementNumber = a.AgreementNumber,
                Title = a.Title,
                CustomerName = a.Customer != null ? a.Customer.Name : null,
                SiteName = a.Site != null ? a.Site.Name : null,
                CoverageLevel = a.CoverageLevel,
                Status = a.Status,
                EndDate = a.EndDate,
                VisitsIncluded = a.VisitsIncluded,
                VisitsUsed = a.VisitsUsed,
                Fee = a.Fee,
            })
            .ToListAsync();
    }

    public async Task<MobileAgreementDetail?> GetAgreementDetailAsync(int id)
    {
        var agreement = await _db.ServiceAgreements
            .Include(a => a.Customer)
            .Include(a => a.Site)
            .Include(a => a.ServiceAgreementAssets)
                .ThenInclude(sa => sa.Asset)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agreement == null) return null;

        return new MobileAgreementDetail
        {
            Id = agreement.Id,
            AgreementNumber = agreement.AgreementNumber,
            Title = agreement.Title,
            CoverageLevel = agreement.CoverageLevel,
            Status = agreement.Status,
            StartDate = agreement.StartDate,
            EndDate = agreement.EndDate,
            VisitsIncluded = agreement.VisitsIncluded,
            VisitsUsed = agreement.VisitsUsed,
            Fee = agreement.Fee,
            TradeType = agreement.TradeType,
            BillingFrequency = agreement.BillingFrequency,
            DiscountPercent = agreement.DiscountPercent,
            RenewalDate = agreement.RenewalDate,
            AutoRenew = agreement.AutoRenew,
            Notes = agreement.Notes,
            CustomerId = agreement.CustomerId,
            CustomerName = agreement.Customer?.Name,
            CustomerPhone = agreement.Customer?.PrimaryPhone,
            SiteId = agreement.SiteId,
            SiteName = agreement.Site?.Name,
            SiteAddress = agreement.Site != null
                ? agreement.Site.Address + ", " + agreement.Site.City + ", " + agreement.Site.State + " " + agreement.Site.Zip
                : null,
            Assets = agreement.ServiceAgreementAssets.Select(sa => new MobileAgreementAsset
            {
                AssetId = sa.AssetId,
                AssetName = sa.Asset?.Name ?? "Unknown",
                AssetType = sa.Asset?.AssetType,
                CoverageNotes = sa.CoverageNotes,
            }).ToList(),
        };
    }

    public async Task<ServiceAgreement> QuickCreateAsync(MobileAgreementQuickCreate model)
    {
        var count = await _db.ServiceAgreements.CountAsync() + 1;
        var agreement = new ServiceAgreement
        {
            AgreementNumber = $"SA-{count:D5}",
            Title = model.Title ?? "Untitled Agreement",
            CoverageLevel = model.CoverageLevel,
            TradeType = model.TradeType,
            CustomerId = model.CustomerId,
            SiteId = model.SiteId,
            VisitsIncluded = model.VisitsIncluded,
            Fee = model.Fee,
            BillingFrequency = model.BillingFrequency,
            Notes = model.Notes,
            Status = AgreementStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            NeedsReview = true,
            CreatedFrom = "mobile",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.ServiceAgreements.Add(agreement);
        await _db.SaveChangesAsync();
        return agreement;
    }
}
