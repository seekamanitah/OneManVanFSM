using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileTemplateService
{
    Task<List<MobileTemplateCard>> GetTemplatesAsync(MobileTemplateFilter? filter = null);
    Task<MobileTemplateDetail?> GetTemplateDetailAsync(int id);
    Task<int> CreateTemplateAsync(MobileTemplateCreate model);
    Task<bool> UpdateTemplateAsync(int id, MobileTemplateUpdate model);
    Task<bool> ArchiveTemplateAsync(int id);
    Task<bool> RestoreTemplateAsync(int id);
    Task<bool> DeleteTemplateAsync(int id);
    Task<bool> CloneTemplateAsync(int id, string newName);
    Task<MobileTemplateStats> GetStatsAsync();
}

public class MobileTemplateFilter
{
    public string? Search { get; set; }
    public TemplateType? Type { get; set; }
    public bool ShowArchived { get; set; }
}

public class MobileTemplateCard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TemplateType Type { get; set; }
    public bool IsCompanyDefault { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsed { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MobileTemplateDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TemplateType Type { get; set; }
    public string? Data { get; set; }
    public bool IsCompanyDefault { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsed { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MobileTemplateVersionItem> Versions { get; set; } = [];
}

public class MobileTemplateVersionItem
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string? ChangeNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MobileTemplateCreate
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TemplateType Type { get; set; } = TemplateType.MaterialList;
    public string? Data { get; set; }
    public bool IsCompanyDefault { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
}

public class MobileTemplateUpdate
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TemplateType Type { get; set; }
    public string? Data { get; set; }
    public bool IsCompanyDefault { get; set; }
    public string? Notes { get; set; }
}

public class MobileTemplateStats
{
    public int TotalTemplates { get; set; }
    public int DefaultCount { get; set; }
    public int ArchivedCount { get; set; }
    public int TotalUsages { get; set; }
    public Dictionary<TemplateType, int> CountByType { get; set; } = [];
}
