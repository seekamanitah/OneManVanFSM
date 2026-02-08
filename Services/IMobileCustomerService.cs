using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileCustomerService
{
    Task<List<MobileCustomerCard>> GetCustomersAsync(string? search = null);
    Task<MobileCustomerDetail?> GetCustomerDetailAsync(int id);
}

public class MobileCustomerCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public CustomerType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int SiteCount { get; set; }
    public int OpenJobCount { get; set; }
    public bool HasActiveAgreement { get; set; }
    public string? Tags { get; set; }
}

public class MobileCustomerDetail
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
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTime SinceDate { get; set; }
    public decimal BalanceOwed { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }
    public List<MobileCustomerSite> Sites { get; set; } = [];
    public List<MobileCustomerJob> RecentJobs { get; set; } = [];
    public List<MobileCustomerAgreement> Agreements { get; set; } = [];
}

public class MobileCustomerSite
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public PropertyType PropertyType { get; set; }
    public int AssetCount { get; set; }
}

public class MobileCustomerJob
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? SiteAddress { get; set; }
}

public class MobileCustomerAgreement
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public CoverageLevel CoverageLevel { get; set; }
    public AgreementStatus Status { get; set; }
    public DateTime EndDate { get; set; }
    public int VisitsIncluded { get; set; }
    public int VisitsUsed { get; set; }
}
