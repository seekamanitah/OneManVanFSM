namespace OneManVanFSM.Web.Services;

public interface IDataManagementService
{
    Task<byte[]> ExportAllXlsxAsync();
    Task<byte[]> ExportTableXlsxAsync(string tableName);
    Task<List<string>> GetExportableTablesAsync();
    Task<ImportResult> ImportCsvAsync(Stream csvStream, string fileName);
    Task<ImportResult> ImportXlsxAsync(Stream xlsxStream, string fileName);
    Task<ImportResult> ImportFileAsync(Stream fileStream, string fileName);
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
