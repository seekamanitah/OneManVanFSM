using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Services;

/// <summary>
/// HTTP client wrapper for communicating with the OneManVanFSM REST API.
/// Manages base URL, JWT token attachment, automatic token refresh,
/// transient fault retry with exponential backoff, and connectivity checking.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;
    private string? _token;
    private DateTime? _tokenExpiry;

    private const int MaxTransientRetries = 3;
    private static readonly TimeSpan[] RetryDelays = [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    ];

    private static readonly HashSet<HttpStatusCode> TransientStatusCodes =
    [
        HttpStatusCode.BadGateway,          // 502
        HttpStatusCode.ServiceUnavailable,  // 503
        HttpStatusCode.GatewayTimeout       // 504
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient(ILogger<ApiClient> logger)
    {
        _logger = logger;

        var handler = new HttpClientHandler
        {
            // Accept self-signed certs in dev/local network
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// The server base URL (e.g. "http://192.168.1.100:5002").
    /// Reloaded from Preferences on every call to allow runtime changes.
    /// </summary>
    public string BaseUrl
    {
        get
        {
            var url = Preferences.Default.Get("db_server_url", "").TrimEnd('/');
            return url;
        }
    }

    /// <summary>
    /// Whether the client has a valid (non-expired) JWT token.
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token) && _tokenExpiry > DateTime.UtcNow;

    /// <summary>
    /// Authenticate and store the JWT token. Returns the full login response.
    /// </summary>
    public async Task<ApiLoginResponse> LoginAsync(string username, string password)
    {
        _logger.LogInformation("Authenticating user {Username}...", username);
        var request = new ApiLoginRequest { Username = username, Password = password };
        var response = await PostAsync<ApiLoginResponse>("api/authapi/login", request, skipAuth: true);

        if (response is { Succeeded: true, Token: not null })
        {
            _token = response.Token;
            _tokenExpiry = response.ExpiresAt ?? DateTime.UtcNow.AddHours(23);
            await SecureStorage.Default.SetAsync("api_jwt_token", _token);
            Preferences.Default.Set("api_token_expiry", _tokenExpiry.Value.ToString("O"));
            _logger.LogInformation("Authentication succeeded for {Username}.", username);
        }
        else
        {
            _logger.LogWarning("Authentication failed for {Username}: {Error}", username, response?.ErrorMessage ?? "No response");
        }

        return response ?? ApiLoginResponse.Failure("No response from server.");
    }

    /// <summary>
    /// Attempt to restore a previously saved JWT token from SecureStorage.
    /// </summary>
    public async Task<bool> TryRestoreTokenAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("api_jwt_token");
            var expiryStr = Preferences.Default.Get("api_token_expiry", "");

            if (!string.IsNullOrEmpty(token) && DateTime.TryParse(expiryStr, out var expiry) && expiry > DateTime.UtcNow)
            {
                _token = token;
                _tokenExpiry = expiry;
                _logger.LogDebug("Restored JWT token from SecureStorage (expires {Expiry}).", expiry);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore JWT token from SecureStorage.");
        }

        return false;
    }

    /// <summary>
    /// Clear stored credentials.
    /// </summary>
    public void ClearToken()
    {
        _token = null;
        _tokenExpiry = null;
        SecureStorage.Default.Remove("api_jwt_token");
        Preferences.Default.Remove("api_token_expiry");
    }

    /// <summary>
    /// Refresh the JWT token using the existing valid token.
    /// </summary>
    public async Task<bool> TryRefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_token)) return false;

        try
        {
            _logger.LogDebug("Attempting JWT token refresh...");
            var response = await PostAsync<ApiLoginResponse>("api/authapi/refresh", new { }, skipAuth: false);
            if (response is { Succeeded: true, Token: not null })
            {
                _token = response.Token;
                _tokenExpiry = response.ExpiresAt ?? DateTime.UtcNow.AddHours(23);
                await SecureStorage.Default.SetAsync("api_jwt_token", _token);
                Preferences.Default.Set("api_token_expiry", _tokenExpiry.Value.ToString("O"));
                _logger.LogDebug("JWT token refreshed successfully.");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT token refresh failed.");
        }

        return false;
    }

    /// <summary>
    /// Test connectivity to the server's health endpoint with detailed diagnostics.
    /// </summary>
    public async Task<(bool IsReachable, string Message)> TestConnectionAsync()
    {
        try
        {
            var url = BaseUrl;
            if (string.IsNullOrWhiteSpace(url))
                return (false, "No server URL configured. Enter a URL like http://192.168.1.100:6000");

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return (false, $"URL must start with http:// or https:// — got: {url}");

            _logger.LogInformation("Testing connection to {Url}/health...", url);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _http.GetAsync($"{url}/health", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Health check succeeded: {Body}", body);
                return (true, $"Server is reachable and healthy (HTTP {(int)response.StatusCode}).");
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Health check returned {StatusCode}: {Body}", (int)response.StatusCode, errorBody);
            return (false, $"Server returned HTTP {(int)response.StatusCode} ({response.StatusCode}). Response: {Truncate(errorBody, 200)}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Connection test failed with HttpRequestException.");
            var inner = ex.InnerException?.Message;
            var detail = !string.IsNullOrEmpty(inner) ? $"{ex.Message} ? {inner}" : ex.Message;
            return (false, $"Connection failed: {detail}");
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Connection test timed out after 10 seconds.");
            return (false, "Connection timed out after 10 seconds. Server may be unreachable or the port may be blocked.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during connection test.");
            return (false, $"Unexpected error: {ex.GetType().Name} — {ex.Message}");
        }
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";

    // ?????????????????????? HTTP helpers ??????????????????????

    public async Task<T?> GetAsync<T>(string path)
    {
        var request = CreateRequest(HttpMethod.Get, path);
        var response = await SendWithRetryAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task<T?> GetAsync<T>(string path, Dictionary<string, string?> queryParams)
    {
        var query = string.Join("&", queryParams
            .Where(kv => kv.Value != null)
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

        var fullPath = string.IsNullOrEmpty(query) ? path : $"{path}?{query}";
        return await GetAsync<T>(fullPath);
    }

    public async Task<T?> PostAsync<T>(string path, object body, bool skipAuth = false)
    {
        var request = CreateRequest(HttpMethod.Post, path, skipAuth);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        var response = await SendWithRetryAsync(request, skipAuth);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task<T?> PutAsync<T>(string path, object body)
    {
        var request = CreateRequest(HttpMethod.Put, path);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        var response = await SendWithRetryAsync(request);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await response.Content.ReadFromJsonAsync<SyncConflict>(JsonOptions);
            throw new SyncConflictException(conflict);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task DeleteAsync(string path)
    {
        var request = CreateRequest(HttpMethod.Delete, path);
        var response = await SendWithRetryAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Stream?> GetStreamAsync(string path)
    {
        var request = CreateRequest(HttpMethod.Get, path);
        var response = await SendWithRetryAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsStreamAsync();
    }

    // ?????????????????????? Internal ??????????????????????

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, bool skipAuth = false)
    {
        var url = $"{BaseUrl}/{path.TrimStart('/')}";
        var request = new HttpRequestMessage(method, url);

        if (!skipAuth && !string.IsNullOrEmpty(_token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        return request;
    }

    /// <summary>
    /// Send request with transient fault retry (exponential backoff) and
    /// automatic 401 token refresh. Retries on network errors, timeouts,
    /// and 502/503/504 up to 3 times before falling through.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, bool skipAuth = false)
    {
        // Capture the original content so we can rebuild on retry
        string? bodyContent = null;
        string? contentType = null;
        if (request.Content != null)
        {
            bodyContent = await request.Content.ReadAsStringAsync();
            contentType = request.Content.Headers.ContentType?.MediaType ?? "application/json";
        }

        HttpResponseMessage? response = null;

        for (var attempt = 0; attempt <= MaxTransientRetries; attempt++)
        {
            // Build a fresh request for each attempt (HttpRequestMessage can't be reused after send)
            var msg = new HttpRequestMessage(request.Method, request.RequestUri);
            if (!skipAuth && !string.IsNullOrEmpty(_token))
                msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            if (bodyContent != null)
                msg.Content = new StringContent(bodyContent, Encoding.UTF8, contentType!);

            try
            {
                response = await _http.SendAsync(msg);

                // Non-transient failure — don't retry
                if (!TransientStatusCodes.Contains(response.StatusCode))
                    break;

                _logger.LogWarning("Transient HTTP {StatusCode} on {Method} {Uri} (attempt {Attempt}/{Max}).",
                    (int)response.StatusCode, request.Method, request.RequestUri, attempt + 1, MaxTransientRetries + 1);
            }
            catch (HttpRequestException ex) when (attempt < MaxTransientRetries)
            {
                _logger.LogWarning(ex, "Transient network error on {Method} {Uri} (attempt {Attempt}/{Max}).",
                    request.Method, request.RequestUri, attempt + 1, MaxTransientRetries + 1);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested && attempt < MaxTransientRetries)
            {
                _logger.LogWarning("Request timeout on {Method} {Uri} (attempt {Attempt}/{Max}).",
                    request.Method, request.RequestUri, attempt + 1, MaxTransientRetries + 1);
            }

            if (attempt < MaxTransientRetries)
                await Task.Delay(RetryDelays[attempt]);
        }

        // response will be null only if all retries threw — let the final attempt propagate naturally
        response ??= await _http.SendAsync(RebuildRequest(request.Method, request.RequestUri!, bodyContent, contentType, skipAuth));

        // Handle 401 — attempt token refresh then one final retry
        if (response.StatusCode == HttpStatusCode.Unauthorized && !skipAuth)
        {
            if (await TryRefreshTokenAsync())
            {
                _logger.LogDebug("Retrying request after token refresh: {Method} {Uri}", request.Method, request.RequestUri);
                var retry = RebuildRequest(request.Method, request.RequestUri!, bodyContent, contentType, skipAuth: false);
                response = await _http.SendAsync(retry);
            }
        }

        return response;
    }

    /// <summary>Builds a fresh HttpRequestMessage for retry (original can't be reused).</summary>
    private HttpRequestMessage RebuildRequest(HttpMethod method, Uri uri, string? body, string? contentType, bool skipAuth)
    {
        var msg = new HttpRequestMessage(method, uri);
        if (!skipAuth && !string.IsNullOrEmpty(_token))
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (body != null)
            msg.Content = new StringContent(body, Encoding.UTF8, contentType ?? "application/json");
        return msg;
    }
}

/// <summary>
/// Thrown when a PUT/POST results in a 409 Conflict due to a newer server version.
/// </summary>
public class SyncConflictException : Exception
{
    public SyncConflict? Conflict { get; }

    public SyncConflictException(SyncConflict? conflict)
        : base(conflict?.Message ?? "Sync conflict detected.")
    {
        Conflict = conflict;
    }
}
