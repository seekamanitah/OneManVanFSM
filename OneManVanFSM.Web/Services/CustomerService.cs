using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CustomerListItem>> GetCustomersAsync(CustomerFilter? filter = null)
    {
        var query = _db.Customers
            .Where(c => !c.IsArchived)
            .AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    (c.PrimaryEmail != null && c.PrimaryEmail.ToLower().Contains(term)) ||
                    (c.PrimaryPhone != null && c.PrimaryPhone.Contains(term)));
            }

            if (filter.Type.HasValue)
            {
                query = query.Where(c => c.Type == filter.Type.Value);
            }

            query = filter.SortBy?.ToLower() switch
            {
                "type" => filter.SortDescending ? query.OrderByDescending(c => c.Type) : query.OrderBy(c => c.Type),
                "email" => filter.SortDescending ? query.OrderByDescending(c => c.PrimaryEmail) : query.OrderBy(c => c.PrimaryEmail),
                "contact" => filter.SortDescending ? query.OrderByDescending(c => c.PrimaryPhone) : query.OrderBy(c => c.PrimaryPhone),
                "sites" => filter.SortDescending ? query.OrderByDescending(c => c.Sites.Count(s => !s.IsArchived)) : query.OrderBy(c => c.Sites.Count(s => !s.IsArchived)),
                "assets" => filter.SortDescending ? query.OrderByDescending(c => c.Assets.Count(a => !a.IsArchived)) : query.OrderBy(c => c.Assets.Count(a => !a.IsArchived)),
                "openjobs" => filter.SortDescending ? query.OrderByDescending(c => c.Jobs.Count(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)) : query.OrderBy(c => c.Jobs.Count(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)),
                "date" => filter.SortDescending ? query.OrderByDescending(c => c.SinceDate) : query.OrderBy(c => c.SinceDate),
                "balance" => filter.SortDescending
                    ? query.OrderByDescending(c => c.Invoices.Where(i => !i.IsArchived && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void).Sum(i => i.BalanceDue))
                    : query.OrderBy(c => c.Invoices.Where(i => !i.IsArchived && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void).Sum(i => i.BalanceDue)),
                _ => filter.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name)
            };
        }
        else
        {
            query = query.OrderBy(c => c.Name);
        }

        return await query.Select(c => new CustomerListItem
        {
            Id = c.Id,
            Name = c.Name,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Type = c.Type,
            PrimaryPhone = c.PrimaryPhone,
            PrimaryEmail = c.PrimaryEmail,
            CompanyName = c.Company != null ? c.Company.Name : null,
            SiteCount = c.Sites.Count(s => !s.IsArchived),
            AssetCount = c.Sites.SelectMany(s => s.Assets).Count(a => !a.IsArchived),
            OpenJobCount = c.Jobs.Count(j => !j.IsArchived && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled),
            OutstandingBalance = c.Invoices.Where(i => !i.IsArchived && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void).Sum(i => i.BalanceDue)
        }).ToListAsync();
    }

    public async Task<CustomerDetail?> GetCustomerAsync(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.Company)
            .Include(c => c.Sites).ThenInclude(s => s.Assets)
            .Include(c => c.Jobs).ThenInclude(j => j.AssignedEmployee)
            .Include(c => c.Jobs).ThenInclude(j => j.Site)
            .Include(c => c.Invoices)
            .Include(c => c.ServiceAgreements)
            .Include(c => c.QuickNotes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null) return null;

        var now = DateTime.UtcNow;

        return new CustomerDetail
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
            ReferralSource = customer.ReferralSource,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Zip = customer.Zip,
            SinceDate = customer.SinceDate,
            CreditLimit = customer.CreditLimit,
            TaxExempt = customer.TaxExempt,
            BalanceOwed = customer.BalanceOwed,
            Tags = customer.Tags,
            Notes = customer.Notes,
            CompanyId = customer.CompanyId,
            CompanyName = customer.Company?.Name,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,

            Sites = customer.Sites.Where(s => !s.IsArchived).Select(s => new SiteSummary
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                City = s.City,
                State = s.State,
                AssetCount = s.Assets.Count(a => !a.IsArchived)
            }).ToList(),

            Assets = customer.Sites
                .SelectMany(s => s.Assets.Where(a => !a.IsArchived).Select(a => new AssetSummary
                {
                    Id = a.Id,
                    Name = a.Name,
                    AssetType = a.AssetType,
                    SiteName = s.Name,
                    InstallDate = a.InstallDate,
                    WarrantyExpiry = a.WarrantyExpiry,
                    Status = a.Status
                })).ToList(),

            RecentJobs = customer.Jobs
                .Where(j => !j.IsArchived && j.Status is JobStatus.Completed or JobStatus.Cancelled)
                .OrderByDescending(j => j.CompletedDate ?? j.ScheduledDate)
                .Take(10)
                .Select(j => new JobSummary
                {
                    Id = j.Id,
                    JobNumber = j.JobNumber,
                    Title = j.Title,
                    SiteName = j.Site?.Name,
                    Status = j.Status,
                    ScheduledDate = j.ScheduledDate,
                    TechnicianName = j.AssignedEmployee?.Name
                }).ToList(),

            UpcomingJobs = customer.Jobs
                .Where(j => !j.IsArchived && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
                .OrderBy(j => j.ScheduledDate)
                .Take(10)
                .Select(j => new JobSummary
                {
                    Id = j.Id,
                    JobNumber = j.JobNumber,
                    Title = j.Title,
                    SiteName = j.Site?.Name,
                    Status = j.Status,
                    ScheduledDate = j.ScheduledDate,
                    TechnicianName = j.AssignedEmployee?.Name
                }).ToList(),

            OutstandingInvoices = customer.Invoices
                .Where(i => !i.IsArchived && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void)
                .OrderBy(i => i.DueDate)
                .Select(i => new InvoiceSummary
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    Status = i.Status,
                    Total = i.Total,
                    BalanceDue = i.BalanceDue,
                    DueDate = i.DueDate
                }).ToList(),

            ServiceAgreements = customer.ServiceAgreements
                .Where(sa => !sa.IsArchived)
                .OrderByDescending(sa => sa.StartDate)
                .Select(sa => new AgreementSummary
                {
                    Id = sa.Id,
                    Title = sa.Title ?? sa.AgreementNumber,
                    AgreementType = sa.CoverageLevel.ToString(),
                    StartDate = sa.StartDate,
                    EndDate = sa.EndDate,
                    IsActive = sa.Status == AgreementStatus.Active
                }).ToList(),

            RecentNotes = customer.QuickNotes
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new NoteSummary
                {
                    Id = n.Id,
                    Content = n.Text,
                    Category = n.Category,
                    CreatedAt = n.CreatedAt
                }).ToList()
        };
    }

    public async Task<Customer> CreateCustomerAsync(CustomerEditModel model)
    {
        var customer = new Customer
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Name = string.IsNullOrWhiteSpace(model.LastName) ? model.FirstName : $"{model.FirstName} {model.LastName}".Trim(),
            Type = model.Type,
            PrimaryPhone = model.PrimaryPhone,
            SecondaryPhone = model.SecondaryPhone,
            PrimaryEmail = model.PrimaryEmail,
            PreferredContactMethod = model.PreferredContactMethod,
            ReferralSource = model.ReferralSource,
            Address = model.Address,
            City = model.City,
            State = model.State,
            Zip = model.Zip,
            CreditLimit = model.CreditLimit,
            TaxExempt = model.TaxExempt,
            Tags = model.Tags,
            Notes = model.Notes,
            CompanyId = model.CompanyId,
            SinceDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        // Auto-create a primary site if address is filled
        if (!string.IsNullOrWhiteSpace(model.Address))
        {
            var site = new Site
            {
                Name = "Primary Residence",
                Address = model.Address,
                City = model.City,
                State = model.State,
                Zip = model.Zip,
                CustomerId = customer.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Sites.Add(site);
            await _db.SaveChangesAsync();
        }

        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(int id, CustomerEditModel model)
    {
        var customer = await _db.Customers.FindAsync(id)
            ?? throw new InvalidOperationException("Customer not found.");

        customer.FirstName = model.FirstName;
        customer.LastName = model.LastName;
        customer.Name = string.IsNullOrWhiteSpace(model.LastName) ? model.FirstName : $"{model.FirstName} {model.LastName}".Trim();
        customer.Type = model.Type;
        customer.PrimaryPhone = model.PrimaryPhone;
        customer.SecondaryPhone = model.SecondaryPhone;
        customer.PrimaryEmail = model.PrimaryEmail;
        customer.PreferredContactMethod = model.PreferredContactMethod;
        customer.ReferralSource = model.ReferralSource;
        customer.Address = model.Address;
        customer.City = model.City;
        customer.State = model.State;
        customer.Zip = model.Zip;
        customer.CreditLimit = model.CreditLimit;
        customer.TaxExempt = model.TaxExempt;
        customer.Tags = model.Tags;
        customer.Notes = model.Notes;
        customer.CompanyId = model.CompanyId;
        customer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> ArchiveCustomerAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return false;

        customer.IsArchived = true;
        customer.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
