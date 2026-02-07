namespace OneManVanFSM.Shared.Models;

public class Template
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TemplateType Type { get; set; } = TemplateType.MaterialList;
    public string? Data { get; set; } // JSON structured content (sections, items, defaults)
    public bool IsCompanyDefault { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsed { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public ICollection<TemplateVersion> Versions { get; set; } = [];
}

public class TemplateVersion
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public Template Template { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string? Data { get; set; } // JSON snapshot
    public string? ChangeNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TemplateType
{
    MaterialList,
    JobChecklist,
    EstimateFormat,
    ProductVariant,
    ServiceAgreementStructure,
    Other
}
