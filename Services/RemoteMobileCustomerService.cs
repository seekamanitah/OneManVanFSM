using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

/// <summary>
/// Remote-mode customer service. Reads from the local SQLite cache populated by SyncService.
/// Customer data is read-only from the mobile side.
/// </summary>
public class RemoteMobileCustomerService : IMobileCustomerService
{
    private readonly AppDbContext _db;

    public RemoteMobileCustomerService(AppDbContext db) => _db = db;

    public async Task<List<MobileCustomerCard>> GetCustomersAsync(string? search = null)
    {
        var query = _db.Customers.AsNoTracking()
            .Include(c => c.Company)
            .Where(c => !c.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search)
                || (c.PrimaryEmail ?? "").Contains(search)
                || (c.PrimaryPhone ?? "").Contains(search));

        return await query.OrderBy(c => c.Name)
            .Select(c => new MobileCustomerCard
            {
                Id = c.Id, Name = c.Name, FirstName = c.FirstName, LastName = c.LastName,
                Type = c.Type, Phone = c.PrimaryPhone, Email = c.PrimaryEmail,
                Address = c.Address, CompanyId = c.CompanyId,
                CompanyName = c.Company != null ? c.Company.Name : null,
                SiteCount = c.Sites.Count(s => !s.IsArchived),
                OpenJobCount = c.Jobs.Count(j => !j.IsArchived
                    && j.Status != JobStatus.Completed && j.Status != JobStatus.Closed && j.Status != JobStatus.Cancelled),
                HasActiveAgreement = c.ServiceAgreements.Any(sa => sa.Status == AgreementStatus.Active),
                Tags = c.Tags
            }).ToListAsync();
    }

    public async Task<MobileCustomerDetail?> GetCustomerDetailAsync(int id)
    {
        var customer = await _db.Customers.AsNoTracking()
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null) return null;

        var sites = await _db.Sites.AsNoTracking()
            .Where(s => s.CustomerId == id && !s.IsArchived)
            .Select(s => new MobileCustomerSite
            {
                Id = s.Id, Name = s.Name, Address = s.Address,
                PropertyType = s.PropertyType,
                AssetCount = s.Assets.Count(a => !a.IsArchived)
            }).ToListAsync();

        var recentJobs = await _db.Jobs.AsNoTracking()
            .Include(j => j.Site)
            .Where(j => j.CustomerId == id && !j.IsArchived)
            .OrderByDescending(j => j.ScheduledDate).Take(10)
            .Select(j => new MobileCustomerJob
            {
                Id = j.Id, JobNumber = j.JobNumber, Title = j.Title,
                Status = j.Status, Priority = j.Priority,
                ScheduledDate = j.ScheduledDate,
                SiteAddress = j.Site != null ? j.Site.Address : null
            }).ToListAsync();

        var agreements = await _db.ServiceAgreements.AsNoTracking()
            .Where(sa => sa.CustomerId == id && !sa.IsArchived)
            .Select(sa => new MobileCustomerAgreement
            {
                Id = sa.Id, AgreementNumber = sa.AgreementNumber, Title = sa.Title,
                CoverageLevel = sa.CoverageLevel, Status = sa.Status,
                EndDate = sa.EndDate, VisitsIncluded = sa.VisitsIncluded, VisitsUsed = sa.VisitsUsed
            }).ToListAsync();

        return new MobileCustomerDetail
        {
            Id = customer.Id, Name = customer.Name, FirstName = customer.FirstName,
            LastName = customer.LastName, Type = customer.Type,
            PrimaryPhone = customer.PrimaryPhone, SecondaryPhone = customer.SecondaryPhone,
            PrimaryEmail = customer.PrimaryEmail, PreferredContactMethod = customer.PreferredContactMethod,
            Address = customer.Address, City = customer.City, State = customer.State, Zip = customer.Zip,
            CompanyId = customer.CompanyId, CompanyName = customer.Company?.Name,
            SinceDate = customer.SinceDate, BalanceOwed = customer.BalanceOwed,
            Tags = customer.Tags, Notes = customer.Notes,
            NeedsReview = customer.NeedsReview,
            Sites = sites, RecentJobs = recentJobs, Agreements = agreements
        };
    }

    public async Task<Customer> QuickCreateAsync(MobileCustomerQuickCreate model)
    {
        var customer = new Customer
        {
            Name = model.Name,
            Type = model.Type,
            PrimaryPhone = model.PrimaryPhone,
            PrimaryEmail = model.PrimaryEmail,
            Address = model.Address,
            City = model.City,
            State = model.State,
            Zip = model.Zip,
            Notes = model.Notes,
            NeedsReview = true,
            CreatedFrom = "mobile",
            SinceDate = DateTime.Now,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }
}
