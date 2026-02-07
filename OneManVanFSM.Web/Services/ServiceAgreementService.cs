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
        var query = _db.ServiceAgreements.Where(a => !a.IsArchived).AsQueryable();
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
}
