namespace OneManVanFSM.Web.Services;

public interface ICompanyProfileService
{
    Task<CompanyProfile> GetProfileAsync();
}

public class CompanyProfile
{
    public string Name { get; set; } = "OneManVanFSM";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string TaxId { get; set; } = "";
    public string Address { get; set; } = "";
}
