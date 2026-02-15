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

    // Mobile device version tracking
    Task<List<MobileDeviceInfo>> GetMobileDevicesAsync();
    Task UpdateDeviceNotesAsync(int deviceId, string? notes);
    Task SetDeviceActiveAsync(int deviceId, bool isActive);
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

/// <summary>
/// DTO for displaying mobile device info in Settings page.
/// </summary>
public class MobileDeviceInfo
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? OsVersion { get; set; }
    public string AppVersion { get; set; } = string.Empty;
    public string BuildNumber { get; set; } = string.Empty;
    public DateTime? BuildTimestamp { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public DateTime LastSyncTime { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Human-readable build age (e.g., "2 hours ago", "3 days ago").
    /// </summary>
    public string BuildAge
    {
        get
        {
            if (BuildTimestamp is null) return "Unknown";
            var age = DateTime.UtcNow - BuildTimestamp.Value;
            if (age.TotalHours < 24)
                return $"{(int)age.TotalHours} hours ago";
            if (age.TotalDays < 7)
                return $"{(int)age.TotalDays} days ago";
            if (age.TotalDays < 30)
                return $"{(int)(age.TotalDays / 7)} weeks ago";
            return $"{(int)(age.TotalDays / 30)} months ago";
        }
    }

    /// <summary>
    /// Whether this build is outdated (> 48 hours old).
    /// </summary>
    public bool IsOutdated
    {
        get
        {
            if (BuildTimestamp is null) return false;
            return (DateTime.UtcNow - BuildTimestamp.Value).TotalHours > 48;
        }
    }
}
