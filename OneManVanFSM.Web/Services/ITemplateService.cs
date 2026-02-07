namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface ITemplateService
{
    Task<List<TemplateListItem>> GetTemplatesAsync(TemplateFilter? filter = null);
    Task<TemplateDetail?> GetTemplateAsync(int id);
    Task<Template> CreateTemplateAsync(TemplateEditModel model);
    Task<bool> UpdateTemplateAsync(int id, TemplateEditModel model);
    Task<bool> DeleteTemplateAsync(int id);
    Task<bool> CloneTemplateAsync(int id, string newName);
    Task<bool> IncrementUsageAsync(int id);
}

public class TemplateFilter
{
    public string? Search { get; set; }
    public TemplateType? Type { get; set; }
    public bool? IsCompanyDefault { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
}

public class TemplateListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TemplateType Type { get; set; }
    public bool IsCompanyDefault { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsed { get; set; }
    public string? CustomerName { get; set; }
    public string? CompanyName { get; set; }
}

public class TemplateDetail
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
    public List<TemplateVersionItem> Versions { get; set; } = [];
}

public class TemplateVersionItem
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string? ChangeNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TemplateEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Template name is required.")]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TemplateType Type { get; set; } = TemplateType.MaterialList;
    public string? Data { get; set; }
    public bool IsCompanyDefault { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? CompanyId { get; set; }
}
