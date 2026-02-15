namespace OneManVanFSM.Shared.Models;

/// <summary>
/// Tracks mobile app installations and their versions for fleet management.
/// Updated automatically during each sync operation.
/// </summary>
public class MobileDevice
{
    public int Id { get; set; }

    /// <summary>
    /// Unique device identifier (from DeviceInfo.Current.Id or similar).
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Device name (e.g., "Tech's Pixel 8", "John's Galaxy S24").
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Platform (e.g., "Android", "iOS", "Windows").
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// OS version (e.g., "Android 14", "iOS 17.2").
    /// </summary>
    public string? OsVersion { get; set; }

    /// <summary>
    /// App version string (e.g., "1.0.0").
    /// </summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// Build number (e.g., "461713" = 46 days since Jan 2025 + 17:13).
    /// </summary>
    public string BuildNumber { get; set; } = string.Empty;

    /// <summary>
    /// Parsed build timestamp for age calculation.
    /// </summary>
    public DateTime? BuildTimestamp { get; set; }

    /// <summary>
    /// Employee linked to this device (if logged in).
    /// </summary>
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>
    /// User account linked to this device.
    /// </summary>
    public int? UserId { get; set; }
    public AppUser? User { get; set; }

    /// <summary>
    /// Last time this device synced with the server.
    /// </summary>
    public DateTime LastSyncTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// First time this device was registered.
    /// </summary>
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this device is actively used (auto-set to false if no sync for 30+ days).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Notes about this device (e.g., "Primary tech device", "Testing only").
    /// </summary>
    public string? Notes { get; set; }
}
