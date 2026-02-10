using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;

namespace OneManVanFSM.Services;

public class MobileSettingsService : IMobileSettingsService
{
    private readonly AppDbContext _db;
    private MobileAppSettings? _cachedSettings;

    // Preferences keys
    private const string PrefTheme = "settings_theme";
    private const string PrefNotificationsEnabled = "settings_notifications_enabled";
    private const string PrefJobAlerts = "settings_job_alerts";
    private const string PrefInventoryAlerts = "settings_inventory_alerts";
    private const string PrefEstimateAlerts = "settings_estimate_alerts";
    private const string PrefDefaultJobFilter = "settings_default_job_filter";
    private const string PrefDefaultCalendarView = "settings_default_calendar_view";
    private const string PrefAutoClockOnEnRoute = "settings_auto_clock_en_route";
    private const string PrefShowCompletedJobs = "settings_show_completed_jobs";
    private const string PrefSyncIntervalMinutes = "sync_interval_minutes";

    public MobileSettingsService(AppDbContext db)
    {
        _db = db;
    }

    public Task<MobileAppSettings> GetSettingsAsync()
    {
        _cachedSettings ??= LoadFromPreferences();
        return Task.FromResult(_cachedSettings);
    }

    public Task SaveSettingsAsync(MobileAppSettings settings)
    {
        _cachedSettings = settings;
        SaveToPreferences(settings);
        return Task.CompletedTask;
    }

    private static MobileAppSettings LoadFromPreferences()
    {
        return new MobileAppSettings
        {
            Theme = Preferences.Default.Get(PrefTheme, "Light"),
            NotificationsEnabled = Preferences.Default.Get(PrefNotificationsEnabled, true),
            JobAlerts = Preferences.Default.Get(PrefJobAlerts, true),
            InventoryAlerts = Preferences.Default.Get(PrefInventoryAlerts, true),
            EstimateAlerts = Preferences.Default.Get(PrefEstimateAlerts, true),
            DefaultJobFilter = Preferences.Default.Get(PrefDefaultJobFilter, "All"),
            DefaultCalendarView = Preferences.Default.Get(PrefDefaultCalendarView, "Day"),
            AutoClockOnEnRoute = Preferences.Default.Get(PrefAutoClockOnEnRoute, true),
            ShowCompletedJobs = Preferences.Default.Get(PrefShowCompletedJobs, true),
            SyncIntervalMinutes = Preferences.Default.Get(PrefSyncIntervalMinutes, 15),
        };
    }

    private static void SaveToPreferences(MobileAppSettings settings)
    {
        Preferences.Default.Set(PrefTheme, settings.Theme);
        Preferences.Default.Set(PrefNotificationsEnabled, settings.NotificationsEnabled);
        Preferences.Default.Set(PrefJobAlerts, settings.JobAlerts);
        Preferences.Default.Set(PrefInventoryAlerts, settings.InventoryAlerts);
        Preferences.Default.Set(PrefEstimateAlerts, settings.EstimateAlerts);
        Preferences.Default.Set(PrefDefaultJobFilter, settings.DefaultJobFilter);
        Preferences.Default.Set(PrefDefaultCalendarView, settings.DefaultCalendarView);
        Preferences.Default.Set(PrefAutoClockOnEnRoute, settings.AutoClockOnEnRoute);
        Preferences.Default.Set(PrefShowCompletedJobs, settings.ShowCompletedJobs);
        Preferences.Default.Set(PrefSyncIntervalMinutes, settings.SyncIntervalMinutes);
    }

    public async Task<MobileSyncStatus> GetSyncStatusAsync()
    {
        var jobCount = await _db.Jobs.CountAsync();
        var customerCount = await _db.Customers.CountAsync();
        var assetCount = await _db.Assets.CountAsync();

        long cacheSizeBytes = 0;
        try
        {
            var dbFile = new FileInfo(Path.Combine(FileSystem.AppDataDirectory, "OneManVanFSM.db"));
            if (dbFile.Exists)
                cacheSizeBytes = dbFile.Length;
        }
        catch
        {
            // File may be locked or inaccessible — report 0
        }

        return new MobileSyncStatus
        {
            LastSyncTime = DateTime.Now,
            PendingChanges = 0,
            SyncState = "Synced",
            CacheSizeBytes = cacheSizeBytes,
            CachedJobs = jobCount,
            CachedCustomers = customerCount,
            CachedAssets = assetCount,
        };
    }

    public Task<MobileAppInfo> GetAppInfoAsync()
    {
        var dbMode = Preferences.Default.Get("db_mode", "Local");
        var serverUrl = Preferences.Default.Get("db_server_url", "");

        return Task.FromResult(new MobileAppInfo
        {
            AppVersion = AppInfo.Current.VersionString,
            BuildNumber = AppInfo.Current.BuildString,
            Framework = ".NET MAUI Blazor Hybrid",
            DatabaseEngine = "SQLite (EF Core)",
            ApiEndpoint = dbMode == "Remote" && !string.IsNullOrWhiteSpace(serverUrl)
                ? $"Remote ({serverUrl})"
                : "Local (Offline)",
        });
    }

    public async Task<bool> HasDataAsync()
    {
        return await _db.Employees.AnyAsync() || await _db.Customers.AnyAsync();
    }

    public Task<bool> SeedDemoDataAsync()
    {
        try
        {
            MauiProgram.SeedMobileData(_db);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
