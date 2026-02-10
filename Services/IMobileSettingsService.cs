using OneManVanFSM.Shared.Data;

namespace OneManVanFSM.Services;

public interface IMobileSettingsService
{
    Task<MobileAppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(MobileAppSettings settings);
    Task<MobileSyncStatus> GetSyncStatusAsync();
    Task<MobileAppInfo> GetAppInfoAsync();
    Task<bool> HasDataAsync();
    Task<bool> SeedDemoDataAsync();
}

public class MobileAppSettings
{
    public string Theme { get; set; } = "Light";
    public bool NotificationsEnabled { get; set; } = true;
    public bool JobAlerts { get; set; } = true;
    public bool InventoryAlerts { get; set; } = true;
    public bool EstimateAlerts { get; set; } = true;
    public string DefaultJobFilter { get; set; } = "All";
    public string DefaultCalendarView { get; set; } = "Day";
    public bool AutoClockOnEnRoute { get; set; } = true;
    public bool ShowCompletedJobs { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 15;
}

public class MobileSyncStatus
{
    public DateTime LastSyncTime { get; set; }
    public int PendingChanges { get; set; }
    public string SyncState { get; set; } = "Synced";
    public long CacheSizeBytes { get; set; }
    public int CachedJobs { get; set; }
    public int CachedCustomers { get; set; }
    public int CachedAssets { get; set; }
}

public class MobileAppInfo
{
    public string AppVersion { get; set; } = "1.0.0";
    public string BuildNumber { get; set; } = "2025.06";
    public string Framework { get; set; } = ".NET MAUI Blazor Hybrid";
    public string DatabaseEngine { get; set; } = "SQLite";
    public string ApiEndpoint { get; set; } = "Local (Offline)";
}
