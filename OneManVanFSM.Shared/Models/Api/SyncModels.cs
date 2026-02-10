namespace OneManVanFSM.Shared.Models.Api;

/// <summary>
/// Login request for the sync API. Accepts either regular user credentials
/// or the dedicated sync account credentials from environment variables.
/// </summary>
public class ApiLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ApiLoginResponse
{
    public bool Succeeded { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int? UserId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }

    public static ApiLoginResponse Success(string token, DateTime expiresAt, int userId, int? employeeId, string username, string role) =>
        new() { Succeeded = true, Token = token, ExpiresAt = expiresAt, UserId = userId, EmployeeId = employeeId, Username = username, Role = role };

    public static ApiLoginResponse Failure(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

/// <summary>
/// Generic sync response wrapping a list of entities changed since a given timestamp.
/// </summary>
public class SyncResponse<T>
{
    public List<T> Data { get; set; } = [];
    public DateTime ServerTimestamp { get; set; } = DateTime.UtcNow;
    public int TotalCount { get; set; }
}

/// <summary>
/// Request for delta sync — "give me everything changed since this timestamp."
/// </summary>
public class SyncRequest
{
    public DateTime? Since { get; set; }
    public int? PageSize { get; set; }
    public int? Page { get; set; }
}

/// <summary>
/// Envelope for pushing local changes to the server.
/// </summary>
public class SyncPushRequest<T>
{
    public List<T> Created { get; set; } = [];
    public List<T> Updated { get; set; } = [];
    public List<int> DeletedIds { get; set; } = [];
}

/// <summary>
/// Result of a push sync operation.
/// </summary>
public class SyncPushResponse
{
    public bool Succeeded { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public List<SyncConflict> Conflicts { get; set; } = [];
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Maps client-side temporary IDs to server-assigned IDs for newly created records.
    /// </summary>
    public Dictionary<int, int> IdMappings { get; set; } = new();
}

/// <summary>
/// Represents a sync conflict where the server version is newer than the client's.
/// </summary>
public class SyncConflict
{
    public int EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ServerUpdatedAt { get; set; }
    public DateTime ClientUpdatedAt { get; set; }
}

/// <summary>
/// A comprehensive sync status response for the mobile app.
/// </summary>
public class SyncStatusResponse
{
    public DateTime ServerTime { get; set; }
    public string ServerVersion { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public Dictionary<string, int> EntityCounts { get; set; } = new();
}
