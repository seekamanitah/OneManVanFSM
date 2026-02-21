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

    // Business defaults
    public decimal DefaultTaxRate { get; set; } = 0m;
    public string DefaultPrintTemplate { get; set; } = "Classic";

    // Auto-backup settings
    public bool AutoBackupEnabled { get; set; }
    public string AutoBackupFrequency { get; set; } = "Daily"; // Daily, Weekly, Monthly
    public int AutoBackupMaxCount { get; set; } = 10;
    public int AutoBackupRetentionDays { get; set; } = 90;
    public DateTime? LastAutoBackup { get; set; }

    // SMTP configuration
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public bool SmtpUseSsl { get; set; } = true;
    public string? PublicBaseUrl { get; set; }
}
