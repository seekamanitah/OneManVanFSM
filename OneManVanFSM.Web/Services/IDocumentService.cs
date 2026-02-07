namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IDocumentService
{
    Task<List<DocumentListItem>> GetDocumentsAsync(DocumentFilter? filter = null);
    Task<DocumentDetail?> GetDocumentAsync(int id);
    Task<Document> CreateDocumentAsync(DocumentEditModel model);
    Task<Document> UploadDocumentAsync(DocumentEditModel model, Stream fileStream, string fileName, long fileSize);
    Task<(Stream stream, string contentType, string fileName)?> DownloadDocumentAsync(int id);
    Task<bool> DeleteDocumentAsync(int id);
}

public class DocumentFilter
{
    public string? Search { get; set; }
    public DocumentCategory? Category { get; set; }
    public string? EntityType { get; set; } // Customer, Job, Asset, etc.
    public int? EntityId { get; set; }
    public string? SortBy { get; set; } = "UploadDate";
    public bool SortDescending { get; set; } = true;
}

public class DocumentListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public int Version { get; set; }
    public DocumentAccessLevel AccessLevel { get; set; }
    public DateTime UploadDate { get; set; }
    public string? UploadedByName { get; set; }
    public string? CustomerName { get; set; }
    public string? JobTitle { get; set; }
    public string? LinkedTo { get; set; }
}

public class DocumentDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; }
    public string? FilePath { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public int Version { get; set; }
    public DocumentAccessLevel AccessLevel { get; set; }
    public string? CustomTags { get; set; }
    public string? Notes { get; set; }
    public DateTime UploadDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public int? JobId { get; set; }
    public string? JobTitle { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int? UploadedByEmployeeId { get; set; }
    public string? UploadedByName { get; set; }
}

public class DocumentEditModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Document name is required.")]
    public string Name { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; } = DocumentCategory.Other;
    public string? FilePath { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public DocumentAccessLevel AccessLevel { get; set; } = DocumentAccessLevel.Public;
    public string? CustomTags { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }
    public int? AssetId { get; set; }
    public int? JobId { get; set; }
    public int? EmployeeId { get; set; }
}
