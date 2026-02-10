namespace OneManVanFSM.Services;

/// <summary>
/// Orchestrates background data sync between the mobile local DB and the
/// remote REST API server.
/// </summary>
public interface ISyncService
{
    /// <summary>True when a sync operation is running.</summary>
    bool IsSyncing { get; }

    /// <summary>Last time a full sync completed successfully.</summary>
    DateTime? LastSyncTime { get; }

    /// <summary>Run a full delta-sync for all entity types.</summary>
    Task<SyncResult> SyncAllAsync();

    /// <summary>Sync a single entity type by name.</summary>
    Task<SyncResult> SyncEntityAsync(string entityType);

    /// <summary>Raised when sync state changes (started / finished / progress).</summary>
    event Action<SyncProgressInfo>? OnSyncProgress;
}

public class SyncResult
{
    public bool Succeeded { get; set; }
    public int EntitiesSynced { get; set; }
    public int Errors { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static SyncResult Success(int count) =>
        new() { Succeeded = true, EntitiesSynced = count };

    public static SyncResult Failure(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

public class SyncProgressInfo
{
    public bool IsRunning { get; set; }
    public string? CurrentEntity { get; set; }
    public int CompletedEntities { get; set; }
    public int TotalEntities { get; set; }
    public string? StatusMessage { get; set; }
}
