using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileCustomerService : IMobileCustomerService
{
    private readonly AppDbContext _db;

    public MobileCustomerService(AppDbContext db) => _db = db;

    public async Task<List<MobileCustomerCard>> GetCustomersAsync(string? search = null)
    {
        var query = _db.Customers
            .Include(c => c.Company)
            .Where(c => !c.IsArchived)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.FirstName != null && c.FirstName.ToLower().Contains(term)) ||
                (c.LastName != null && c.LastName.ToLower().Contains(term)) ||
                (c.PrimaryPhone != null && c.PrimaryPhone.Contains(term)) ||
                (c.PrimaryEmail != null && c.PrimaryEmail.ToLower().Contains(term)) ||
                (c.Address != null && c.Address.ToLower().Contains(term)));
        }

        var now = DateTime.UtcNow;
        return await query
            .OrderBy(c => c.Name)
            .Select(c => new MobileCustomerCard
            {
                Id = c.Id,
                Name = c.Name,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Type = c.Type,
                Phone = c.PrimaryPhone,
                Email = c.PrimaryEmail,
                Address = c.Address != null
                    ? c.Address + (c.City != null ? ", " + c.City : "")
                    : null,
                CompanyId = c.CompanyId,
                CompanyName = c.Company != null ? c.Company.Name : null,
                SiteCount = c.Sites.Count(s => !s.IsArchived),
                OpenJobCount = c.Jobs.Count(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled),
                HasActiveAgreement = c.ServiceAgreements.Any(sa => sa.Status == AgreementStatus.Active && sa.EndDate > now),
                Tags = c.Tags,
            })
            .ToListAsync();
    }

    public async Task<MobileCustomerDetail?> GetCustomerDetailAsync(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.Company)
            .Include(c => c.Sites.Where(s => !s.IsArchived))
                .ThenInclude(s => s.Assets)
            .Include(c => c.Jobs.OrderByDescending(j => j.ScheduledDate).Take(5))
                .ThenInclude(j => j.Site)
            .Include(c => c.ServiceAgreements.Where(sa => !sa.IsArchived))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null) return null;

        return new MobileCustomerDetail
        {
            Id = customer.Id,
            Name = customer.Name,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Type = customer.Type,
            PrimaryPhone = customer.PrimaryPhone,
            SecondaryPhone = customer.SecondaryPhone,
            PrimaryEmail = customer.PrimaryEmail,
            PreferredContactMethod = customer.PreferredContactMethod,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Zip = customer.Zip,
            CompanyId = customer.CompanyId,
            CompanyName = customer.Company?.Name,
            SinceDate = customer.SinceDate,
            BalanceOwed = customer.BalanceOwed,
            Tags = customer.Tags,
            Notes = customer.Notes,
            Sites = customer.Sites.Select(s => new MobileCustomerSite
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address != null
                    ? s.Address + (s.City != null ? ", " + s.City : "")
                    : null,
                PropertyType = s.PropertyType,
                AssetCount = s.Assets.Count(a => a.Status != AssetStatus.Decommissioned),
            }).ToList(),
            RecentJobs = customer.Jobs.Select(j => new MobileCustomerJob
            {
                Id = j.Id,
                JobNumber = j.JobNumber,
                Title = j.Title,
                Status = j.Status,
                Priority = j.Priority,
                ScheduledDate = j.ScheduledDate,
                SiteAddress = j.Site?.Address,
            }).ToList(),
            Agreements = customer.ServiceAgreements.Select(sa => new MobileCustomerAgreement
            {
                Id = sa.Id,
                AgreementNumber = sa.AgreementNumber,
                Title = sa.Title,
                CoverageLevel = sa.CoverageLevel,
                Status = sa.Status,
                EndDate = sa.EndDate,
                VisitsIncluded = sa.VisitsIncluded,
                VisitsUsed = sa.VisitsUsed,
            }).ToList(),
        };
    }
}
