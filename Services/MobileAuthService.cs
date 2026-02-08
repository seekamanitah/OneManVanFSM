using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;

namespace OneManVanFSM.Services;

public class MobileAuthService : IMobileAuthService
{
    private readonly AppDbContext _db;
    private MobileUserSession? _cachedSession;
    private bool _restoredFromStorage;
    private const int MaxLoginAttempts = 5;

    // SecureStorage keys
    private const string StorageKeyUserId = "auth_user_id";
    private const string StorageKeyUsername = "auth_username";
    private const string StorageKeyEmail = "auth_email";
    private const string StorageKeyRole = "auth_role";
    private const string StorageKeyEmployeeId = "auth_employee_id";
    private const string StorageKeyEmployeeName = "auth_employee_name";
    private const string StorageKeyTerritory = "auth_territory";

    public MobileAuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MobileAuthResult> LoginAsync(string username, string password, bool rememberMe = false)
    {
        System.Diagnostics.Debug.WriteLine($"[AUTH] LoginAsync called — username='{username}', password length={password.Length}");

        var user = await _db.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u =>
                u.Username == username || u.Email == username);

        if (user is null)
        {
            System.Diagnostics.Debug.WriteLine("[AUTH] User NOT found in database.");
            return MobileAuthResult.Failure("Invalid username or password.");
        }

        System.Diagnostics.Debug.WriteLine($"[AUTH] User found: Id={user.Id}, Username={user.Username}, IsActive={user.IsActive}, IsLocked={user.IsLocked}, Hash={user.PasswordHash?[..Math.Min(20, user.PasswordHash?.Length ?? 0)]}...");

        if (user.IsLocked)
            return MobileAuthResult.Failure("Account is locked. Contact your administrator.");

        if (!user.IsActive)
            return MobileAuthResult.Failure("Account is inactive. Contact your administrator.");

        if (string.IsNullOrEmpty(user.PasswordHash))
            return MobileAuthResult.Failure("Invalid username or password.");

        var verifyResult = VerifyPassword(password, user.PasswordHash);
        System.Diagnostics.Debug.WriteLine($"[AUTH] VerifyPassword result: {verifyResult}");

        if (!verifyResult)
        {
            user.LoginAttempts++;
            if (user.LoginAttempts >= MaxLoginAttempts)
                user.IsLocked = true;

            await _db.SaveChangesAsync();
            return MobileAuthResult.Failure("Invalid username or password.");
        }

        // Successful login
        user.LoginAttempts = 0;
        user.LastLogin = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var session = new MobileUserSession
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            EmployeeId = user.EmployeeId,
            EmployeeName = user.Employee?.Name,
            Territory = user.Employee?.Territory,
        };

        _cachedSession = session;

        if (rememberMe)
        {
            await PersistSessionAsync(session);
        }

        return MobileAuthResult.Success(session);
    }

    public async Task LogoutAsync()
    {
        _cachedSession = null;
        _restoredFromStorage = true; // Prevent re-restore

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

    // --- Session persistence via MAUI SecureStorage ---

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

        // Verify user still exists and is active
        var user = await _db.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsLocked);

        if (user is null)
        {
            // User deleted, locked, or deactivated — clear stored session
            await LogoutAsync();
            return null;
        }

        return new MobileUserSession
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            EmployeeId = user.EmployeeId,
            EmployeeName = user.Employee?.Name,
            Territory = user.Employee?.Territory,
        };
    }

    // --- PBKDF2 password verification (matches Web AuthService) ---

    private static bool VerifyPassword(string password, string storedHash)
    {
        var combined = Convert.FromBase64String(storedHash);
        var salt = combined[..16];
        var storedKey = combined[16..];
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, storedKey);
    }

    /// <summary>
    /// Hash a password using the same PBKDF2 scheme as the web AuthService.
    /// Used for seeding AppUser records.
    /// </summary>
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var combined = new byte[48];
        salt.CopyTo(combined, 0);
        hash.CopyTo(combined, 16);
        return Convert.ToBase64String(combined);
    }
}
