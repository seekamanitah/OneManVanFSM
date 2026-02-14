using Microsoft.Extensions.Logging;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Services;

/// <summary>
/// Remote-mode auth service that authenticates via the REST API instead of
/// the local SQLite database. On successful login, stores the JWT token
/// in SecureStorage and triggers an initial data sync.
/// </summary>
public class RemoteMobileAuthService : IMobileAuthService
{
    private readonly ApiClient _api;
    private readonly ISyncService _sync;
    private readonly ILogger<RemoteMobileAuthService> _logger;
    private MobileUserSession? _cachedSession;
    private bool _restoredFromStorage;

    private const string StorageKeyUserId = "auth_user_id";
    private const string StorageKeyUsername = "auth_username";
    private const string StorageKeyEmail = "auth_email";
    private const string StorageKeyRole = "auth_role";
    private const string StorageKeyEmployeeId = "auth_employee_id";
    private const string StorageKeyEmployeeName = "auth_employee_name";
    private const string StorageKeyTerritory = "auth_territory";

    public RemoteMobileAuthService(ApiClient api, ISyncService sync, ILogger<RemoteMobileAuthService> logger)
    {
        _api = api;
        _sync = sync;
        _logger = logger;
    }

    public async Task<MobileAuthResult> LoginAsync(string username, string password, bool rememberMe = false)
    {
        try
        {
            _logger.LogInformation("[RemoteAuth] LoginAsync called for user '{Username}', BaseUrl='{Url}'", username, _api.BaseUrl);

            if (string.IsNullOrWhiteSpace(_api.BaseUrl))
            {
                _logger.LogWarning("[RemoteAuth] No server URL configured.");
                return MobileAuthResult.Failure("No server URL configured. Go to 'Configure Remote Server' and enter the server URL.");
            }

            var response = await _api.LoginAsync(username, password);

            if (!response.Succeeded)
            {
                _logger.LogWarning("[RemoteAuth] Server returned login failure: {Error}", response.ErrorMessage);
                return MobileAuthResult.Failure(response.ErrorMessage ?? "Login failed.");
            }

            var session = new MobileUserSession
            {
                UserId = response.UserId ?? 0,
                Username = response.Username ?? username,
                Email = "",
                Role = response.Role ?? "Tech",
                EmployeeId = response.EmployeeId,
                EmployeeName = null,
                Territory = null,
                MustChangePassword = false
            };

            _cachedSession = session;

            if (rememberMe)
                await PersistSessionAsync(session);

            // Trigger background sync after login
            _ = Task.Run(async () =>
            {
                try { await _sync.SyncAllAsync(); }
                catch (Exception ex) { _logger.LogWarning(ex, "Post-login sync failed."); }
            });

            return MobileAuthResult.Success(session);
        }
        catch (HttpRequestException ex)
        {
            var inner = ex.InnerException?.Message;
            var detail = !string.IsNullOrEmpty(inner) ? $"{ex.Message} -> {inner}" : ex.Message;
            var statusInfo = ex.StatusCode.HasValue ? $" (HTTP {(int)ex.StatusCode.Value})" : "";
            _logger.LogWarning(ex, "[RemoteAuth] HTTP error during login.");
            return MobileAuthResult.Failure($"Cannot reach server{statusInfo}: {detail}");
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("[RemoteAuth] Login request timed out.");
            return MobileAuthResult.Failure("Connection timed out after 30 seconds. Check server URL, port, and firewall settings.");
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException is not null ? $" -> {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : "";
            _logger.LogError(ex, "[RemoteAuth] Unexpected login error.");
            return MobileAuthResult.Failure($"Login error ({ex.GetType().Name}): {ex.Message}{inner}");
        }
    }

    public async Task LogoutAsync()
    {
        _cachedSession = null;
        _restoredFromStorage = true;
        _api.ClearToken();

        SecureStorage.Default.Remove(StorageKeyUserId);
        SecureStorage.Default.Remove(StorageKeyUsername);
        SecureStorage.Default.Remove(StorageKeyEmail);
        SecureStorage.Default.Remove(StorageKeyRole);
        SecureStorage.Default.Remove(StorageKeyEmployeeId);
        SecureStorage.Default.Remove(StorageKeyEmployeeName);
        SecureStorage.Default.Remove(StorageKeyTerritory);

        await Task.CompletedTask;
    }

    public async Task<MobileUserSession?> GetCurrentUserAsync()
    {
        if (_cachedSession is not null)
            return _cachedSession;

        if (!_restoredFromStorage)
        {
            _restoredFromStorage = true;
            _cachedSession = await RestoreSessionAsync();
        }

        return _cachedSession;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        return await GetCurrentUserAsync() is not null;
    }

    public async Task<int?> GetEmployeeIdAsync()
    {
        var session = await GetCurrentUserAsync();
        return session?.EmployeeId;
    }

    public Task<MobileAuthResult> CompleteFirstTimeSetupAsync(string currentPassword, string newPassword, string? newUsername = null, string? newEmail = null)
    {
        // First-time setup is not supported in remote mode — must be done on the server
        return Task.FromResult(MobileAuthResult.Failure(
            "Password change must be completed on the web server. Please log in at the server URL to change your password."));
    }

    public async Task RefreshSessionAsync()
    {
        if (_cachedSession is null) return;
        try
        {
            var me = await _api.GetAsync<ApiLoginResponse>("api/authapi/me");
            if (me is null || !me.Succeeded) return;

            _cachedSession.EmployeeId = me.EmployeeId;
            _cachedSession.Role = me.Role ?? _cachedSession.Role;
            _cachedSession.Username = me.Username ?? _cachedSession.Username;

            await PersistSessionAsync(_cachedSession);
            _logger.LogInformation("[RemoteAuth] Session refreshed — EmployeeId={Eid}", me.EmployeeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RemoteAuth] Failed to refresh session from server.");
        }
    }

    private async Task PersistSessionAsync(MobileUserSession session)
    {
        await SecureStorage.Default.SetAsync(StorageKeyUserId, session.UserId.ToString());
        await SecureStorage.Default.SetAsync(StorageKeyUsername, session.Username);
        await SecureStorage.Default.SetAsync(StorageKeyEmail, session.Email);
        await SecureStorage.Default.SetAsync(StorageKeyRole, session.Role);

        if (session.EmployeeId.HasValue)
            await SecureStorage.Default.SetAsync(StorageKeyEmployeeId, session.EmployeeId.Value.ToString());
        if (session.EmployeeName is not null)
            await SecureStorage.Default.SetAsync(StorageKeyEmployeeName, session.EmployeeName);
        if (session.Territory is not null)
            await SecureStorage.Default.SetAsync(StorageKeyTerritory, session.Territory);
    }

    private async Task<MobileUserSession?> RestoreSessionAsync()
    {
        var userIdStr = await SecureStorage.Default.GetAsync(StorageKeyUserId);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return null;

        // Try to restore JWT token too
        if (!await _api.TryRestoreTokenAsync())
            return null;

        var username = await SecureStorage.Default.GetAsync(StorageKeyUsername) ?? "";
        var role = await SecureStorage.Default.GetAsync(StorageKeyRole) ?? "Tech";
        var eidStr = await SecureStorage.Default.GetAsync(StorageKeyEmployeeId);
        int? eid = int.TryParse(eidStr, out var e) ? e : null;

        return new MobileUserSession
        {
            UserId = userId,
            Username = username,
            Email = await SecureStorage.Default.GetAsync(StorageKeyEmail) ?? "",
            Role = role,
            EmployeeId = eid,
            EmployeeName = await SecureStorage.Default.GetAsync(StorageKeyEmployeeName),
            Territory = await SecureStorage.Default.GetAsync(StorageKeyTerritory),
            MustChangePassword = false
        };
    }
}
