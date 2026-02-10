using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OneManVanFSM.Services;

/// <summary>
/// Persists queued mutations in MAUI Preferences as JSON so they survive app restarts.
/// When connectivity returns the queue is drained in FIFO order.
/// </summary>
public class OfflineQueueService : IOfflineQueueService
{
    private const string PrefsKey = "offline_queue";
    private const int MaxRetries = 3;
    private readonly ApiClient _api;
    private readonly ILogger<OfflineQueueService> _logger;
    private readonly List<OfflineQueueItem> _queue;
    private readonly object _lock = new();

    public int PendingCount
    {
        get { lock (_lock) return _queue.Count; }
    }

    public OfflineQueueService(ApiClient api, ILogger<OfflineQueueService> logger)
    {
        _api = api;
        _logger = logger;
        _queue = LoadFromPrefs();
    }

    public void Enqueue(OfflineQueueItem item)
    {
        lock (_lock)
        {
            _queue.Add(item);
            Persist();
        }
        _logger.LogInformation("Enqueued offline mutation: {Method} {Endpoint} — {Description}", item.HttpMethod, item.Endpoint, item.Description);
    }

    public List<OfflineQueueItem> GetPending()
    {
        lock (_lock) return [.. _queue];
    }

    public async Task<int> ProcessQueueAsync()
    {
        List<OfflineQueueItem> snapshot;
        lock (_lock) snapshot = [.. _queue];

        if (snapshot.Count == 0) return 0;

        _logger.LogInformation("Processing {Count} queued offline mutation(s)...", snapshot.Count);
        int processed = 0;
        var failed = new List<OfflineQueueItem>();

        foreach (var item in snapshot)
        {
            try
            {
                var success = await ReplayAsync(item);
                if (success)
                {
                    processed++;
                    _logger.LogDebug("Replayed: {Description}", item.Description);
                }
                else
                {
                    item.RetryCount++;
                    if (item.RetryCount < MaxRetries)
                        failed.Add(item);
                    else
                        _logger.LogWarning("Dropped after {MaxRetries} retries: {Description}", MaxRetries, item.Description);
                }
            }
            catch (Exception ex)
            {
                item.RetryCount++;
                if (item.RetryCount < MaxRetries)
                    failed.Add(item);
                _logger.LogWarning(ex, "Replay failed: {Description}", item.Description);
            }
        }

        lock (_lock)
        {
            _queue.Clear();
            _queue.AddRange(failed);
            Persist();
        }

        _logger.LogInformation("Offline queue processed. Succeeded: {Processed}, Remaining: {Remaining}", processed, failed.Count);
        return processed;
    }

    private async Task<bool> ReplayAsync(OfflineQueueItem item)
    {
        return item.HttpMethod.ToUpperInvariant() switch
        {
            "POST" => await ReplayPostAsync(item),
            "PUT" => await ReplayPutAsync(item),
            "DELETE" => await ReplayDeleteAsync(item),
            _ => false
        };
    }

    private async Task<bool> ReplayPostAsync(OfflineQueueItem item)
    {
        if (item.PayloadJson is null) return false;
        var payload = JsonSerializer.Deserialize<JsonElement>(item.PayloadJson);
        var result = await _api.PostAsync<JsonElement>(item.Endpoint, payload);
        return true;
    }

    private async Task<bool> ReplayPutAsync(OfflineQueueItem item)
    {
        if (item.PayloadJson is null) return false;
        var payload = JsonSerializer.Deserialize<JsonElement>(item.Endpoint.Contains("/status") ? item.PayloadJson : item.PayloadJson);
        await _api.PutAsync<JsonElement>(item.Endpoint, payload);
        return true;
    }

    private async Task<bool> ReplayDeleteAsync(OfflineQueueItem item)
    {
        await _api.DeleteAsync(item.Endpoint);
        return true;
    }

    private List<OfflineQueueItem> LoadFromPrefs()
    {
        var json = Preferences.Default.Get(PrefsKey, "");
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<OfflineQueueItem>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void Persist()
    {
        try
        {
            var json = JsonSerializer.Serialize(_queue);
            Preferences.Default.Set(PrefsKey, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist offline queue to Preferences.");
        }
    }
}
