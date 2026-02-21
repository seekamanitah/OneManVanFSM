namespace OneManVanFSM.Services;

public interface IMobileDataManagementService
{
    Task<MobileDataStats> GetDataStatsAsync();
    Task<bool> HasDataAsync();
    Task<bool> SeedDemoDataAsync();
    Task PurgeDatabaseAsync();
    Task<string> BackupDatabaseAsync();
    Task<bool> RestoreDatabaseAsync(string backupPath);
    Task<List<string>> GetAvailableBackupsAsync();
}

public class MobileDataStats
{
    public int CustomerCount { get; set; }
    public int JobCount { get; set; }
    public int InvoiceCount { get; set; }
    public int EstimateCount { get; set; }
    public int InventoryItemCount { get; set; }
    public int ProductCount { get; set; }
    public int AssetCount { get; set; }
    public int EmployeeCount { get; set; }
    public int SupplierCount { get; set; }
    public int ExpenseCount { get; set; }
    public int TimeEntryCount { get; set; }
    public int NoteCount { get; set; }
    public int DocumentCount { get; set; }
    public int AgreementCount { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public string DatabaseSizeFormatted => DatabaseSizeBytes switch
    {
        < 1024 => $"{DatabaseSizeBytes} B",
        < 1048576 => $"{DatabaseSizeBytes / 1024.0:F1} KB",
        _ => $"{DatabaseSizeBytes / 1048576.0:F1} MB",
    };
}
