namespace OneManVanFSM.Web.Services;

public interface IDataManagementService
{
    Task<byte[]> ExportAllXlsxAsync();
    Task<byte[]> ExportTableXlsxAsync(string tableName);
    Task<List<string>> GetExportableTablesAsync();
    Task<ImportResult> ImportCsvAsync(Stream csvStream, string fileName);
    Task<ImportResult> ImportXlsxAsync(Stream xlsxStream, string fileName);
    Task<ImportResult> ImportFileAsync(Stream fileStream, string fileName);
    Task<ImportPreview> PreviewImportAsync(Stream fileStream, string fileName);
    Task<ImportResult> CommitImportAsync(ImportPreview preview);
    Task<byte[]> BackupDatabaseAsync();
    Task RestoreDatabaseAsync(Stream backupStream);
    Task PurgeDatabaseAsync();
    Task<bool> HasDataAsync();
    Task<bool> SeedDemoDataAsync();
}

public class ImportResult
{
    public bool Success { get; set; }
    public int RecordsImported { get; set; }
    public List<string> Tables { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Holds parsed import data with duplicate detection results for user review before committing.
/// </summary>
public class ImportPreview
{
    public List<ImportPreviewTable> Tables { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public int TotalRows => Tables.Sum(t => t.Rows.Count);
    public int ConflictCount => Tables.Sum(t => t.Rows.Count(r => r.ConflictType != ImportConflictType.None));
}

public class ImportPreviewTable
{
    public string TableName { get; set; } = "";
    public string[] Headers { get; set; } = [];
    public List<ImportPreviewRow> Rows { get; set; } = [];
}

public class ImportPreviewRow
{
    public int RowNumber { get; set; }
    public Dictionary<string, string?> Values { get; set; } = new();
    public ImportConflictType ConflictType { get; set; } = ImportConflictType.None;
    public string? ConflictDetail { get; set; }
    public int? ExistingEntityId { get; set; }
    public ImportRowAction Action { get; set; } = ImportRowAction.Import;
}

public enum ImportConflictType
{
    None,
    ExactDuplicate,
    PartialMatch,
}

public enum ImportRowAction
{
    Import,
    Skip,
    Overwrite,
}
