using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Services;

/// <summary>
/// Pulls delta changes from the server and merges them into the local SQLite DB.
/// Each entity type tracks its last sync timestamp independently so partial syncs
/// can resume where they left off.
/// </summary>
public class SyncService : ISyncService
{
    private readonly ApiClient _api;
    private readonly AppDbContext _db;
    private readonly IOfflineQueueService _offlineQueue;
    private readonly ILogger<SyncService> _logger;
    private static readonly string[] EntityTypes =
    [
        "Customers", "Companies", "Sites", "Jobs", "Employees",
        "Assets", "Products", "Estimates", "Invoices", "Expenses", "Inventory",
        "QuickNotes", "Documents", "TimeEntries", "ServiceAgreements", "MaterialLists"
    ];

    public bool IsSyncing { get; private set; }
    public DateTime? LastSyncTime { get; private set; }
    public event Action<SyncProgressInfo>? OnSyncProgress;

    public SyncService(ApiClient api, AppDbContext db, IOfflineQueueService offlineQueue, ILogger<SyncService> logger)
    {
        _api = api;
        _db = db;
        _offlineQueue = offlineQueue;
        _logger = logger;

        var lastStr = Preferences.Default.Get("sync_last_full", "");
        if (DateTime.TryParse(lastStr, out var last))
            LastSyncTime = last;
    }

    public async Task<SyncResult> SyncAllAsync()
    {
        if (IsSyncing) return SyncResult.Failure("Sync already in progress.");
        if (!_api.IsAuthenticated) return SyncResult.Failure("Not authenticated.");

        IsSyncing = true;
        int totalSynced = 0, errors = 0;

        try
        {
            // Drain offline queue first — push pending local mutations to the server
            if (_offlineQueue.PendingCount > 0)
            {
                RaiseProgress(true, null, 0, EntityTypes.Length, $"Pushing {_offlineQueue.PendingCount} offline change(s)...");
                var pushed = await _offlineQueue.ProcessQueueAsync();
                _logger.LogInformation("Offline queue: pushed {Pushed} item(s), {Remaining} remaining.", pushed, _offlineQueue.PendingCount);
            }

            for (var i = 0; i < EntityTypes.Length; i++)
            {
                RaiseProgress(true, EntityTypes[i], i, EntityTypes.Length, $"Syncing {EntityTypes[i]}...");
                try
                {
                    var result = await SyncEntityAsync(EntityTypes[i]);
                    totalSynced += result.EntitiesSynced;
                    if (!result.Succeeded) errors++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error syncing {EntityType}.", EntityTypes[i]);
                    errors++;
                }
            }

            LastSyncTime = DateTime.UtcNow;
            Preferences.Default.Set("sync_last_full", LastSyncTime.Value.ToString("O"));

            RaiseProgress(false, null, EntityTypes.Length, EntityTypes.Length,
                errors == 0 ? $"Sync complete — {totalSynced} records." : $"Sync finished with {errors} error(s).");

            return errors == 0
                ? SyncResult.Success(totalSynced)
                : new SyncResult { Succeeded = false, EntitiesSynced = totalSynced, Errors = errors, ErrorMessage = $"{errors} entity type(s) failed." };
        }
        finally
        {
            IsSyncing = false;
        }
    }

    public async Task<SyncResult> SyncEntityAsync(string entityType)
    {
        var since = GetLastSync(entityType);
        var sinceParam = since.HasValue ? $"?since={since.Value:O}" : "";

        try
        {
            var count = entityType switch
            {
                "Customers" => await PullAndMerge<Customer>($"api/customers{sinceParam}", e => e.Id, (db, e) => db.Customers),
                "Companies" => await PullAndMerge<Company>($"api/companies{sinceParam}", e => e.Id, (db, e) => db.Companies),
                "Sites" => await PullAndMerge<Site>($"api/sites{sinceParam}", e => e.Id, (db, e) => db.Sites),
                "Jobs" => await PullAndMerge<Job>($"api/jobs{sinceParam}", e => e.Id, (db, e) => db.Jobs),
                "Employees" => await PullAndMerge<Employee>($"api/employees{sinceParam}", e => e.Id, (db, e) => db.Employees),
                "Assets" => await PullAndMerge<Asset>($"api/assets{sinceParam}", e => e.Id, (db, e) => db.Assets),
                "Products" => await PullAndMerge<Product>($"api/products{sinceParam}", e => e.Id, (db, e) => db.Products),
                "Estimates" => await PullAndMerge<Estimate>($"api/estimates{sinceParam}", e => e.Id, (db, e) => db.Estimates),
                "Invoices" => await PullAndMerge<Invoice>($"api/invoices{sinceParam}", e => e.Id, (db, e) => db.Invoices),
                "Expenses" => await PullAndMerge<Expense>($"api/expenses{sinceParam}", e => e.Id, (db, e) => db.Expenses),
                "Inventory" => await PullAndMerge<InventoryItem>($"api/inventory{sinceParam}", e => e.Id, (db, e) => db.InventoryItems),
                "QuickNotes" => await PullAndMerge<QuickNote>($"api/notes{sinceParam}", e => e.Id, (db, e) => db.QuickNotes),
                "Documents" => await PullAndMerge<Document>($"api/documents{sinceParam}", e => e.Id, (db, e) => db.Documents),
                "TimeEntries" => await PullAndMerge<TimeEntry>($"api/timeentries{sinceParam}", e => e.Id, (db, e) => db.TimeEntries),
                "ServiceAgreements" => await PullAndMerge<ServiceAgreement>($"api/serviceagreements{sinceParam}", e => e.Id, (db, e) => db.ServiceAgreements),
                "MaterialLists" => await PullAndMerge<MaterialList>($"api/materiallists{sinceParam}", e => e.Id, (db, e) => db.MaterialLists),
                _ => 0
            };

            SetLastSync(entityType, DateTime.UtcNow);
            return SyncResult.Success(count);
        }
        catch (HttpRequestException ex)
        {
            return SyncResult.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return SyncResult.Failure(ex.Message);
        }
    }

    // ?????????????????????? Pull & Merge ??????????????????????

    /// <summary>
    /// Fetches entities from the API and upserts them into the local database.
    /// Server is the source of truth — server data always wins on conflict.
    /// </summary>
    private async Task<int> PullAndMerge<T>(
        string apiPath,
        Func<T, int> getId,
        Func<AppDbContext, T, DbSet<T>> getDbSet) where T : class
    {
        var response = await _api.GetAsync<SyncResponse<T>>(apiPath);
        if (response is null || response.Data.Count == 0) return 0;

        var dbSet = getDbSet(_db, response.Data[0]);

        foreach (var entity in response.Data)
        {
            var id = getId(entity);
            var existing = await dbSet.FindAsync(id);

            if (existing is not null)
            {
                // Detach existing and attach updated version
                _db.Entry(existing).State = EntityState.Detached;
                _db.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                // New entity from server — add to local DB
                dbSet.Add(entity);
            }
        }

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        return response.Data.Count;
    }

    // ?????????????????????? Timestamp Tracking ??????????????????????

    private DateTime? GetLastSync(string entityType)
    {
        var val = Preferences.Default.Get($"sync_last_{entityType}", "");
        return DateTime.TryParse(val, out var dt) ? dt : null;
    }

    private void SetLastSync(string entityType, DateTime time)
    {
        Preferences.Default.Set($"sync_last_{entityType}", time.ToString("O"));
    }

    private void RaiseProgress(bool running, string? entity, int completed, int total, string? message)
    {
        OnSyncProgress?.Invoke(new SyncProgressInfo
        {
            IsRunning = running,
            CurrentEntity = entity,
            CompletedEntities = completed,
            TotalEntities = total,
            StatusMessage = message
        });
    }
}
