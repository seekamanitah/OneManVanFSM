using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class CompanyService : ICompanyService
{
    private readonly AppDbContext _db;

    public CompanyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CompanyListItem>> GetCompaniesAsync(CompanyFilter? filter = null)
    {
        var query = _db.Companies
            .Where(c => (filter != null && filter.ShowArchived) ? c.IsArchived : !c.IsArchived)
            .AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    (c.LegalName != null && c.LegalName.ToLower().Contains(term)) ||
                    (c.Email != null && c.Email.ToLower().Contains(term)) ||
                    (c.Phone != null && c.Phone.Contains(term)));
            }

            if (filter.Type.HasValue)
                query = query.Where(c => c.Type == filter.Type.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(c => c.IsActive == filter.IsActive.Value);

            query = filter.SortBy?.ToLower() switch
            {
                "type" => filter.SortDescending ? query.OrderByDescending(c => c.Type) : query.OrderBy(c => c.Type),
                "city" => filter.SortDescending ? query.OrderByDescending(c => c.City) : query.OrderBy(c => c.City),
                "date" => filter.SortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
                _ => filter.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name)
            };
        }
        else
        {
            query = query.OrderBy(c => c.Name);
        }

        return await query.Select(c => new CompanyListItem
        {
            Id = c.Id,
            Name = c.Name,
            Type = c.Type,
            Phone = c.Phone,
            Email = c.Email,
            City = c.City,
            State = c.State,
            IsActive = c.IsActive,
            PrimaryContactName = c.PrimaryContact != null ? c.PrimaryContact.Name : null,
            ContactCount = c.Contacts.Count(ct => !ct.IsArchived),
            SiteCount = c.Sites.Count(s => !s.IsArchived),
            JobCount = c.Jobs.Count(j => !j.IsArchived)
        }).ToListAsync();
    }

    public async Task<CompanyDetail?> GetCompanyAsync(int id)
    {
        var c = await _db.Companies
            .Include(co => co.PrimaryContact)
            .Include(co => co.Contacts.Where(ct => !ct.IsArchived))
            .Include(co => co.Sites.Where(s => !s.IsArchived))
                .ThenInclude(s => s.Assets)
            .Include(co => co.Jobs.Where(j => !j.IsArchived))
            .Include(co => co.Invoices.Where(i => !i.IsArchived))
            .Include(co => co.ServiceAgreements.Where(sa => !sa.IsArchived))
            .FirstOrDefaultAsync(co => co.Id == id && !co.IsArchived);

        if (c is null) return null;

        return new CompanyDetail
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
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Contacts = c.Contacts.Select(ct => new CompanyContactItem
            {
                Id = ct.Id,
                Name = ct.Name,
                Type = ct.Type,
                Phone = ct.PrimaryPhone,
                Email = ct.PrimaryEmail
            }).ToList(),
            Sites = c.Sites.Select(s => new CompanySiteItem
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                City = s.City,
                AssetCount = s.Assets.Count(a => !a.IsArchived)
            }).ToList(),
            RecentJobs = c.Jobs
                .OrderByDescending(j => j.ScheduledDate ?? j.CreatedAt)
                .Take(10)
                .Select(j => new CompanyJobItem
                {
                    Id = j.Id,
                    JobNumber = j.JobNumber,
                    Title = j.Title,
                    Status = j.Status,
                    ScheduledDate = j.ScheduledDate
                }).ToList(),
            OutstandingInvoices = c.Invoices
                .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void)
                .OrderByDescending(i => i.DueDate)
                .Select(i => new CompanyInvoiceItem
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    Status = i.Status,
                    Total = i.Total,
                    BalanceDue = i.BalanceDue,
                    DueDate = i.DueDate
                }).ToList(),
            ServiceAgreements = c.ServiceAgreements
                .OrderByDescending(sa => sa.EndDate)
                .Select(sa => new CompanyAgreementItem
                {
                    Id = sa.Id,
                    AgreementNumber = sa.AgreementNumber,
                    Title = sa.Title,
                    Status = sa.Status,
                    EndDate = sa.EndDate
                }).ToList()
        };
    }

    public async Task<Company> CreateCompanyAsync(CompanyEditModel model)
    {
        var company = new Company
        {
            Name = model.Name,
            LegalName = model.LegalName,
            Type = model.Type,
            TaxId = model.TaxId,
            Industry = model.Industry,
            Website = model.Website,
            IsActive = model.IsActive,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address,
            City = model.City,
            State = model.State,
            Zip = model.Zip,
            Notes = model.Notes,
            PrimaryContactId = model.PrimaryContactId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        // Auto-create a primary site if address is filled
        if (!string.IsNullOrWhiteSpace(model.Address))
        {
            var site = new Site
            {
                Name = "Primary Location",
                Address = model.Address,
                City = model.City,
                State = model.State,
                Zip = model.Zip,
                CompanyId = company.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Sites.Add(site);
            await _db.SaveChangesAsync();
        }

        return company;
    }

    public async Task<Company> UpdateCompanyAsync(int id, CompanyEditModel model)
    {
        var company = await _db.Companies.FindAsync(id)
            ?? throw new InvalidOperationException("Company not found.");

        company.Name = model.Name;
        company.LegalName = model.LegalName;
        company.Type = model.Type;
        company.TaxId = model.TaxId;
        company.Industry = model.Industry;
        company.Website = model.Website;
        company.IsActive = model.IsActive;
        company.Phone = model.Phone;
        company.Email = model.Email;
        company.Address = model.Address;
        company.City = model.City;
        company.State = model.State;
        company.Zip = model.Zip;
        company.Notes = model.Notes;
        company.PrimaryContactId = model.PrimaryContactId;
        company.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return company;
    }

    public async Task<bool> ArchiveCompanyAsync(int id)
    {
        var company = await _db.Companies.FindAsync(id);
        if (company is null) return false;

        company.IsArchived = true;
        company.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreCompanyAsync(int id)
    {
        var company = await _db.Companies.FindAsync(id);
        if (company is null) return false;
        company.IsArchived = false;
        company.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCompanyPermanentlyAsync(int id)
    {
        var company = await _db.Companies.FindAsync(id);
        if (company is null) return false;
        _db.Companies.Remove(company);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveCompaniesAsync(List<int> ids)
    {
        var companies = await _db.Companies.Where(c => ids.Contains(c.Id) && !c.IsArchived).ToListAsync();
        foreach (var c in companies) { c.IsArchived = true; c.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return companies.Count;
    }

    public async Task<int> BulkRestoreCompaniesAsync(List<int> ids)
    {
        var companies = await _db.Companies.Where(c => ids.Contains(c.Id) && c.IsArchived).ToListAsync();
        foreach (var c in companies) { c.IsArchived = false; c.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return companies.Count;
    }

    public async Task<int> BulkDeleteCompaniesPermanentlyAsync(List<int> ids)
    {
        var companies = await _db.Companies.Where(c => ids.Contains(c.Id)).ToListAsync();
        _db.Companies.RemoveRange(companies);
        await _db.SaveChangesAsync();
        return companies.Count;
    }

    public async Task<List<CompanyDropdownItem>> GetCompanyDropdownAsync()
    {
        return await _db.Companies
            .Where(c => !c.IsArchived && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDropdownItem { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }
}
