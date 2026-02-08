using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileCompanyService
{
    Task<List<MobileCompanyCard>> GetCompaniesAsync(string? search = null);
    Task<MobileCompanyDetail?> GetCompanyDetailAsync(int id);
}

public class MobileCompanyCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CompanyType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; }
    public int ContactCount { get; set; }
    public int SiteCount { get; set; }
    public int OpenJobCount { get; set; }
}

public class MobileCompanyDetail
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
    public string? PrimaryContactName { get; set; }
    public int? PrimaryContactId { get; set; }

    public List<MobileCompanyContact> Contacts { get; set; } = [];
    public List<MobileCompanySite> Sites { get; set; } = [];
    public List<MobileCompanyJob> RecentJobs { get; set; } = [];
    public List<MobileCompanyAgreement> Agreements { get; set; } = [];
}

public class MobileCompanyContact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CustomerType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class MobileCompanySite
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public PropertyType PropertyType { get; set; }
    public int AssetCount { get; set; }
}

public class MobileCompanyJob
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public JobStatus Status { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public class MobileCompanyAgreement
{
    public int Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public AgreementStatus Status { get; set; }
    public DateTime EndDate { get; set; }
}
