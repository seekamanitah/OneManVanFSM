using System.Text.Json;

namespace OneManVanFSM.Web.Services;

public class CompanyProfileService : ICompanyProfileService
{
    private static string ProfilePath => Path.Combine(AppContext.BaseDirectory, "companyprofile.json");

    public async Task<CompanyProfile> GetProfileAsync()
    {
        try
        {
            if (File.Exists(ProfilePath))
            {
                var json = await File.ReadAllTextAsync(ProfilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict is not null)
                {
                    return new CompanyProfile
                    {
                        Name = dict.GetValueOrDefault("Name", "OneManVanFSM"),
                        Phone = dict.GetValueOrDefault("Phone", ""),
                        Email = dict.GetValueOrDefault("Email", ""),
                        TaxId = dict.GetValueOrDefault("TaxId", ""),
                        Address = dict.GetValueOrDefault("Address", ""),
                        SmtpHost = dict.GetValueOrDefault("SmtpHost", ""),
                        SmtpPort = int.TryParse(dict.GetValueOrDefault("SmtpPort", "587"), out var port) ? port : 587,
                        SmtpUsername = dict.GetValueOrDefault("SmtpUsername", ""),
                        SmtpPassword = dict.GetValueOrDefault("SmtpPassword", ""),
                        SmtpUseSsl = !dict.TryGetValue("SmtpUseSsl", out var ssl) || ssl != "false",
                        PublicBaseUrl = dict.GetValueOrDefault("PublicBaseUrl", null),
                    };
                }
            }
        }
        catch { /* Return defaults on any error */ }

        return new CompanyProfile();
    }
}
