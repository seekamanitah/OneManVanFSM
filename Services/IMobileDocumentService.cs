using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileDocumentService
{
    Task<List<MobileDocumentItem>> GetDocumentsAsync(MobileDocumentFilter? filter = null);
    Task<MobileDocumentItem?> GetDocumentAsync(int id);
    Task<Document> CreateDocumentAsync(MobileDocumentCreate model);
    Task<bool> DeleteDocumentAsync(int id);
    Task<string?> GetCachedFilePathAsync(int docId, string? storedFileName);
    Task OpenDocumentAsync(int docId, string? storedFileName, string? fileType);
}

public class MobileDocumentItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public string? StoredFileName { get; set; }
    public string? Notes { get; set; }
    public string? LinkedEntity { get; set; }
    public int? LinkedEntityId { get; set; }
    public DateTime UploadDate { get; set; }
}

public class MobileDocumentCreate
{
    public string Name { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; } = DocumentCategory.Other;
    public string? FileType { get; set; }
    public string? Notes { get; set; }
    public int? JobId { get; set; }
    public int? SiteId { get; set; }
    public int? AssetId { get; set; }
    public int? UploadedByEmployeeId { get; set; }
}

public class MobileDocumentFilter
{
    public string? Search { get; set; }
    public DocumentCategory? Category { get; set; }
    public int? JobId { get; set; }
}
