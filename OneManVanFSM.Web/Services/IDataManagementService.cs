namespace OneManVanFSM.Web.Services;

public interface IDataManagementService
{
    Task<byte[]> ExportAllCsvAsync();
    Task<byte[]> ExportTableCsvAsync(string tableName);
    Task<List<string>> GetExportableTablesAsync();
    Task<ImportResult> ImportCsvAsync(Stream csvStream, string fileName);
    Task<byte[]> BackupDatabaseAsync();
    Task RestoreDatabaseAsync(Stream backupStream);
}

public class ImportResult
{
    public bool Success { get; set; }
    public int RecordsImported { get; set; }
    public List<string> Tables { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
