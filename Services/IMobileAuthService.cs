namespace OneManVanFSM.Services;

public interface IMobileAuthService
{
    /// <summary>
    /// Authenticate against the AppUser table using PBKDF2 password verification.
    /// Stores session in MAUI SecureStorage when rememberMe is true.
    /// </summary>
    Task<MobileAuthResult> LoginAsync(string username, string password, bool rememberMe = false);

    /// <summary>
    /// Clears session state and SecureStorage credentials.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Returns the currently authenticated user session, or null if not logged in.
    /// On first call, attempts to restore a remembered session from SecureStorage.
    /// </summary>
    Task<MobileUserSession?> GetCurrentUserAsync();

    /// <summary>
    /// Returns true if a valid session exists.
    /// </summary>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// Returns the EmployeeId of the currently logged-in user, or null if not authenticated
    /// or the user has no linked Employee record.
    /// </summary>
    Task<int?> GetEmployeeIdAsync();

    /// <summary>
    /// Complete the first-time setup by changing the default password.
    /// Optionally update username and email.
    /// </summary>
    Task<MobileAuthResult> CompleteFirstTimeSetupAsync(string currentPassword, string newPassword, string? newUsername = null, string? newEmail = null);
}

public class MobileAuthResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public MobileUserSession? Session { get; set; }

    public static MobileAuthResult Success(MobileUserSession session) =>
        new() { Succeeded = true, Session = session };

    public static MobileAuthResult Failure(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

public class MobileUserSession
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? Territory { get; set; }
    public bool MustChangePassword { get; set; }
}
