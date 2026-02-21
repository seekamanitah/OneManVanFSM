using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace OneManVanFSM.Web.Services;

/// <summary>
/// Background service that runs automatic scheduled backups based on
/// the settings in companyprofile.json. Supports Daily, Weekly, Monthly
/// frequencies and auto-deletes old backups beyond the configured
/// retention period or max backup count.
/// </summary>
public class BackupHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<BackupHostedService> _logger;
    private static readonly string BackupDir = Path.Combine(AppContext.BaseDirectory, "Backups");
    private static readonly string ProfilePath = Path.Combine(AppContext.BaseDirectory, "companyprofile.json");

    public BackupHostedService(IServiceProvider sp, ILogger<BackupHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit on startup for the app to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var profile = await LoadProfileAsync();
                if (profile.AutoBackupEnabled && ShouldRunBackup(profile))
                {
                    _logger.LogInformation("Auto-backup triggered ({Frequency}).", profile.AutoBackupFrequency);
                    await RunBackupAsync(stoppingToken);
                    await CleanupOldBackups(profile.AutoBackupMaxCount, profile.AutoBackupRetentionDays);
                    await UpdateLastBackupTime();
                    _logger.LogInformation("Auto-backup completed successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-backup failed.");
            }

            // Check every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private static bool ShouldRunBackup(CompanyProfile profile)
    {
        if (!profile.LastAutoBackup.HasValue)
            return true;

        var elapsed = DateTime.UtcNow - profile.LastAutoBackup.Value;
        return profile.AutoBackupFrequency switch
        {
            "Daily" => elapsed.TotalHours >= 24,
            "Weekly" => elapsed.TotalDays >= 7,
            "Monthly" => elapsed.TotalDays >= 30,
            _ => elapsed.TotalHours >= 24
        };
    }

    private async Task RunBackupAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connStr = config.GetConnectionString("DefaultConnection") ?? "Data Source=OneManVanFSM.db";
        var dbPath = connStr.Replace("Data Source=", "").Trim();

        if (!File.Exists(dbPath))
        {
            _logger.LogWarning("Auto-backup skipped — database file not found: {Path}", dbPath);
            return;
        }

        Directory.CreateDirectory(BackupDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(BackupDir, $"auto_backup_{timestamp}.zip");

        // Flush WAL using a scoped DbContext
        var db = scope.ServiceProvider.GetRequiredService<OneManVanFSM.Shared.Data.AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);", ct);

        using var fs = new FileStream(backupPath, FileMode.Create);
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
        {
            var dbEntry = archive.CreateEntry("OneManVanFSM.db", CompressionLevel.Optimal);
            await using (var entryStream = dbEntry.Open())
            {
                var dbBytes = await File.ReadAllBytesAsync(dbPath, ct);
                await entryStream.WriteAsync(dbBytes, ct);
            }

            if (File.Exists(ProfilePath))
            {
                var profileEntry = archive.CreateEntry("companyprofile.json", CompressionLevel.Optimal);
                await using var entryStream = profileEntry.Open();
                var profileBytes = await File.ReadAllBytesAsync(ProfilePath, ct);
                await entryStream.WriteAsync(profileBytes, ct);
            }
        }

        _logger.LogInformation("Auto-backup saved: {Path} ({Size:F1} KB)", backupPath, new FileInfo(backupPath).Length / 1024.0);
    }

    private Task CleanupOldBackups(int maxCount, int retentionDays)
    {
        if (!Directory.Exists(BackupDir))
            return Task.CompletedTask;

        var backupFiles = Directory.GetFiles(BackupDir, "auto_backup_*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var deleted = 0;

        for (int i = 0; i < backupFiles.Count; i++)
        {
            var file = backupFiles[i];
            // Delete if beyond max count OR older than retention period (always keep at least 1)
            if (i > 0 && (i >= maxCount || file.CreationTimeUtc < cutoffDate))
            {
                try
                {
                    file.Delete();
                    deleted++;
                    _logger.LogInformation("Auto-backup cleanup: deleted {File}", file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old backup: {File}", file.Name);
                }
            }
        }

        if (deleted > 0)
            _logger.LogInformation("Auto-backup cleanup: removed {Count} old backup(s).", deleted);

        return Task.CompletedTask;
    }

    private static async Task<CompanyProfile> LoadProfileAsync()
    {
        try
        {
            if (File.Exists(ProfilePath))
            {
                var json = await File.ReadAllTextAsync(ProfilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict is not null)
                {
                    return new CompanyProfile
                    {
                        AutoBackupEnabled = dict.GetValueOrDefault("AutoBackupEnabled", "false") == "true",
                        AutoBackupFrequency = dict.GetValueOrDefault("AutoBackupFrequency", "Daily"),
                        AutoBackupMaxCount = int.TryParse(dict.GetValueOrDefault("AutoBackupMaxCount", "10"), out var mc) ? mc : 10,
                        AutoBackupRetentionDays = int.TryParse(dict.GetValueOrDefault("AutoBackupRetentionDays", "90"), out var rd) ? rd : 90,
                        LastAutoBackup = DateTime.TryParse(dict.GetValueOrDefault("LastAutoBackup", ""), out var lb) ? lb : null,
                    };
                }
            }
        }
        catch { /* Return defaults */ }
        return new CompanyProfile();
    }

    private static async Task UpdateLastBackupTime()
    {
        try
        {
            Dictionary<string, string> dict = new();
            if (File.Exists(ProfilePath))
            {
                var json = await File.ReadAllTextAsync(ProfilePath);
                dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            dict["LastAutoBackup"] = DateTime.UtcNow.ToString("O");
            var updatedJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(ProfilePath, updatedJson);
        }
        catch { /* Non-critical — will retry next cycle */ }
    }
}
