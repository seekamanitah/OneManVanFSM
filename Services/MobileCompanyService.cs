using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileCompanyService : IMobileCompanyService
{
    private readonly AppDbContext _db;

    public MobileCompanyService(AppDbContext db) => _db = db;

    public async Task<List<MobileCompanyCard>> GetCompaniesAsync(string? search = null)
    {
        var query = _db.Companies
            .Where(c => !c.IsArchived)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.LegalName != null && c.LegalName.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)) ||
                (c.Email != null && c.Email.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new MobileCompanyCard
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                Phone = c.Phone,
                Email = c.Email,
                City = c.City,
                State = c.State,
                IsActive = c.IsActive,
                ContactCount = c.Contacts.Count(ct => !ct.IsArchived),
                SiteCount = c.Sites.Count(s => !s.IsArchived),
                OpenJobCount = c.Jobs.Count(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled),
            })
            .ToListAsync();
    }

    public async Task<MobileCompanyDetail?> GetCompanyDetailAsync(int id)
    {
        var c = await _db.Companies
            .Include(co => co.PrimaryContact)
            .Include(co => co.Contacts.Where(ct => !ct.IsArchived))
            .Include(co => co.Sites.Where(s => !s.IsArchived))
                .ThenInclude(s => s.Assets)
            .Include(co => co.Jobs.Where(j => !j.IsArchived).OrderByDescending(j => j.ScheduledDate).Take(10))
            .Include(co => co.ServiceAgreements.Where(sa => !sa.IsArchived))
            .FirstOrDefaultAsync(co => co.Id == id && !co.IsArchived);

        if (c is null) return null;

        return new MobileCompanyDetail
        {
            Id = c.Id,
            Name = c.Name,
            LegalName = c.LegalName,
            Type = c.Type,
            TaxId = c.TaxId,
            Industry = c.Industry,
            Website = c.Website,
            IsActive = c.IsActive,
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Zip = c.Zip,
            Notes = c.Notes,
            PrimaryContactId = c.PrimaryContactId,
            PrimaryContactName = c.PrimaryContact?.Name,
            Contacts = c.Contacts.Select(ct => new MobileCompanyContact
            {
                Id = ct.Id,
                Name = ct.Name,
                Type = ct.Type,
                Phone = ct.PrimaryPhone,
                Email = ct.PrimaryEmail,
            }).ToList(),
            Sites = c.Sites.Select(s => new MobileCompanySite
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address != null
                    ? s.Address + (s.City != null ? ", " + s.City : "")
                    : null,
                PropertyType = s.PropertyType,
                AssetCount = s.Assets.Count(a => !a.IsArchived),
            }).ToList(),
            RecentJobs = c.Jobs.Select(j => new MobileCompanyJob
            {
                Id = j.Id,
                JobNumber = j.JobNumber,
                Title = j.Title,
                Status = j.Status,
                ScheduledDate = j.ScheduledDate,
            }).ToList(),
            Agreements = c.ServiceAgreements
                .OrderByDescending(sa => sa.EndDate)
                .Select(sa => new MobileCompanyAgreement
                {
                    Id = sa.Id,
                    AgreementNumber = sa.AgreementNumber,
                    Title = sa.Title,
                    Status = sa.Status,
                    EndDate = sa.EndDate,
                }).ToList(),
        };
    }
}
