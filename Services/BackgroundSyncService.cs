using Microsoft.Extensions.Logging;

namespace OneManVanFSM.Services;

/// <summary>
/// Runs a periodic background sync at a configurable interval (default 15 minutes).
/// Only active in remote mode. Uses IServiceProvider to create scoped ISyncService
/// instances since the timer outlives any single Blazor scope.
/// </summary>
public class BackgroundSyncService : IDisposable
{
    private readonly IServiceProvider _sp;
    private readonly ApiClient _api;
    private readonly ILogger<BackgroundSyncService> _logger;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private int _intervalMinutes;

    public bool IsRunning => _loop is not null && !_loop.IsCompleted;
    public int IntervalMinutes => _intervalMinutes;

    public BackgroundSyncService(IServiceProvider sp, ApiClient api, ILogger<BackgroundSyncService> logger)
    {
        _sp = sp;
        _api = api;
        _logger = logger;
        _intervalMinutes = Preferences.Default.Get("sync_interval_minutes", 15);
    }

    /// <summary>Start the background sync loop.</summary>
    public void Start()
    {
        if (IsRunning) return;
        if (_intervalMinutes <= 0) return;

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(_intervalMinutes));
        _loop = RunLoopAsync(_cts.Token);

        _logger.LogInformation("Background sync started — interval: {Interval}m.", _intervalMinutes);
    }

    /// <summary>Stop the background sync loop.</summary>
    public void Stop()
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _cts = null;
        _logger.LogInformation("Background sync stopped.");
    }

    /// <summary>Update the interval and restart if running.</summary>
    public void SetInterval(int minutes)
    {
        _intervalMinutes = Math.Max(0, minutes);
        Preferences.Default.Set("sync_interval_minutes", _intervalMinutes);

        if (IsRunning)
        {
            Stop();
            if (_intervalMinutes > 0) Start();
        }

        _logger.LogInformation("Background sync interval set to {Interval}m.", _intervalMinutes);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(ct))
            {
                if (!_api.IsAuthenticated)
                {
                    _logger.LogDebug("Background sync skipped — not authenticated.");
                    continue;
                }

                try
                {
                    using var scope = _sp.CreateScope();
                    var syncService = scope.ServiceProvider.GetService<ISyncService>();
                    if (syncService is null) continue;

                    _logger.LogDebug("Starting automatic sync...");
                    var result = await syncService.SyncAllAsync();
                    if (result.Succeeded)
                        _logger.LogInformation("Automatic sync completed — {Count} records synced.", result.EntitiesSynced);
                    else
                        _logger.LogWarning("Automatic sync failed: {Error}", result.ErrorMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync error.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop/dispose
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
