namespace OneManVanFSM.Shared.Models;

public class Document
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; } = DocumentCategory.Other;
    public string? FilePath { get; set; }
    public string? StoredFileName { get; set; } // Server-side unique filename for uploaded files
    public string? FileType { get; set; } // PDF, Image, DOCX, etc.
    public long? FileSize { get; set; }
    public int Version { get; set; } = 1;
    public DocumentAccessLevel AccessLevel { get; set; } = DocumentAccessLevel.Public;
    public string? CustomTags { get; set; } // JSON array
    public string? Notes { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Polymorphic relationships
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int? UploadedByEmployeeId { get; set; }
    public Employee? UploadedByEmployee { get; set; }
}

public enum DocumentCategory
{
    Manual,
    WarrantyPrintout,
    ProductBrochure,
    SetupGuide,
    PartsLiterature,
    EmployeeApplication,
    Certification,
    TaxDocument,
    Permit,
    SafetySheet,
    Photo,
    Other
}

public enum DocumentAccessLevel
{
    Public,
    Private,
    RoleBased,
    CustomerOnly
}
