namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface ICompanyService
{
    Task<List<CompanyListItem>> GetCompaniesAsync(CompanyFilter? filter = null);
    Task<CompanyDetail?> GetCompanyAsync(int id);
    Task<Company> CreateCompanyAsync(CompanyEditModel model);
    Task<Company> UpdateCompanyAsync(int id, CompanyEditModel model);
    Task<bool> ArchiveCompanyAsync(int id);
    Task<List<CompanyDropdownItem>> GetCompanyDropdownAsync();
}

// --- DTOs ---

public class CompanyFilter
{
    public string? Search { get; set; }
    public CompanyType? Type { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
}

public class CompanyListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CompanyType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; }
    public string? PrimaryContactName { get; set; }
    public int ContactCount { get; set; }
    public int SiteCount { get; set; }
    public int JobCount { get; set; }
}

public class CompanyDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public CompanyType Type { get; set; }
    public string? TaxId { get; set; }
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Notes { get; set; }
    public int? PrimaryContactId { get; set; }
    public string? PrimaryContactName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<CompanyContactItem> Contacts { get; set; } = [];
    public List<CompanySiteItem> Sites { get; set; } = [];
    public List<CompanyJobItem> RecentJobs { get; set; } = [];
    public List<CompanyInvoiceItem> OutstandingInvoices { get; set; } = [];
    public List<CompanyAgreementItem> ServiceAgreements { get; set; } = [];
}

public class CompanyContactItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CustomerType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class CompanySiteItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public int AssetCount { get; set; }
}

public class CompanyJobItem
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public JobStatus Status { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public class CompanyInvoiceItem
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public decimal Total { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime? DueDate { get; set; }
}

public class CompanyAgreementItem
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public AgreementStatus Status { get; set; }
    public DateTime EndDate { get; set; }
}

public class CompanyEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Company name is required.")]
    public string Name { get; set; } = string.Empty;

    public string? LegalName { get; set; }
    public CompanyType Type { get; set; } = CompanyType.Customer;
    public string? TaxId { get; set; }
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Phone { get; set; }

    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string? Email { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Notes { get; set; }
    public int? PrimaryContactId { get; set; }
}

public class CompanyDropdownItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
