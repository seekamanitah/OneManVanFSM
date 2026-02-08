namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface ICustomerService
{
    Task<List<CustomerListItem>> GetCustomersAsync(CustomerFilter? filter = null);
    Task<CustomerDetail?> GetCustomerAsync(int id);
    Task<Customer> CreateCustomerAsync(CustomerEditModel model);
    Task<Customer> UpdateCustomerAsync(int id, CustomerEditModel model);
    Task<bool> ArchiveCustomerAsync(int id);
}

// --- DTOs ---

public class CustomerFilter
{
    public string? Search { get; set; }
    public CustomerType? Type { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
}

public class CustomerListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public CustomerType Type { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? CompanyName { get; set; }
    public int SiteCount { get; set; }
    public int AssetCount { get; set; }
    public int OpenJobCount { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class CustomerDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public CustomerType Type { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? ReferralSource { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public DateTime SinceDate { get; set; }
    public decimal CreditLimit { get; set; }
    public bool TaxExempt { get; set; }
    public decimal BalanceOwed { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<SiteSummary> Sites { get; set; } = [];
    public List<AssetSummary> Assets { get; set; } = [];
    public List<JobSummary> RecentJobs { get; set; } = [];
    public List<JobSummary> UpcomingJobs { get; set; } = [];
    public List<InvoiceSummary> OutstandingInvoices { get; set; } = [];
    public List<AgreementSummary> ServiceAgreements { get; set; } = [];
    public List<NoteSummary> RecentNotes { get; set; } = [];
}

public class SiteSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public int AssetCount { get; set; }
}

public class AssetSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetType { get; set; }
    public string? SiteName { get; set; }
    public DateTime? InstallDate { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public AssetStatus Status { get; set; }
}

public class JobSummary
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? SiteName { get; set; }
    public JobStatus Status { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? TechnicianName { get; set; }
}

public class InvoiceSummary
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public decimal Total { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime? DueDate { get; set; }
}

public class AgreementSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AgreementType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class NoteSummary
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerEditModel
{
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public CustomerType Type { get; set; } = CustomerType.Individual;

    public string? PrimaryPhone { get; set; }

    public string? SecondaryPhone { get; set; }

    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string? PrimaryEmail { get; set; }

    public string? PreferredContactMethod { get; set; }
    public string? ReferralSource { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public decimal CreditLimit { get; set; }
    public bool TaxExempt { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public int? CompanyId { get; set; }
}
