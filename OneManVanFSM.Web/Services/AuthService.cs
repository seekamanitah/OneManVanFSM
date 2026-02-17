using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const int MaxLoginAttempts = 5;

    public AuthService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password)
    {
        var user = await _db.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u =>
                u.Username == usernameOrEmail || u.Email == usernameOrEmail);

        if (user is null)
            return AuthResult.Failure("Invalid username or password.");

        if (user.IsLocked)
            return AuthResult.Failure("Account is locked. Contact your administrator.");

        if (!user.IsActive)
            return AuthResult.Failure("Account is inactive. Contact your administrator.");

        if (!VerifyPassword(password, user.PasswordHash))
        {
            user.LoginAttempts++;
            if (user.LoginAttempts >= MaxLoginAttempts)
                user.IsLocked = true;

            await _db.SaveChangesAsync();
            return AuthResult.Failure("Invalid username or password.");
        }

        // Successful login — reset attempts, update last login
        user.LoginAttempts = 0;
        user.LastLogin = DateTime.Now;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Create claims and sign in with cookie
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        if (user.Employee is not null)
        {
            claims.Add(new Claim("EmployeeId", user.Employee.Id.ToString()));
            claims.Add(new Claim("EmployeeName", user.Employee.Name));
            if (user.Employee.Territory is not null)
                claims.Add(new Claim("Territory", user.Employee.Territory));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = _httpContextAccessor.HttpContext!;
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12),
            });

        return AuthResult.Success(user);
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return AuthResult.Failure("Passwords do not match.");

        if (request.Password.Length < 6)
            return AuthResult.Failure("Password must be at least 6 characters.");

        var exists = await _db.Users.AnyAsync(u =>
            u.Username == request.Username || u.Email == request.Email);

        if (exists)
            return AuthResult.Failure("Username or email already exists.");

        var user = new AppUser
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = request.RequestedRole,
            IsActive = false, // Requires admin approval per guideline
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return AuthResult.Success(user);
    }

    public async Task<AppUser?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userIdClaim = httpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return null;

        return await _db.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        // Always return true to prevent email enumeration
        // In production, send reset email here
        return true;
    }

    public async Task<List<UserListItem>> GetUsersAsync()
    {
        return await _db.Users
            .Include(u => u.Employee)
            .OrderBy(u => u.Username)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                IsLocked = u.IsLocked,
                LastLogin = u.LastLogin,
                EmployeeId = u.EmployeeId,
                EmployeeName = u.Employee != null ? u.Employee.Name : null,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> ToggleUserActiveAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return false;
        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, UserRole role)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return false;
        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleLockAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return false;
        user.IsLocked = !user.IsLocked;
        if (!user.IsLocked) user.LoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<AuthResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        if (newPassword.Length < 6)
            return AuthResult.Failure("New password must be at least 6 characters.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return AuthResult.Failure("User not found.");

        if (!VerifyPassword(currentPassword, user.PasswordHash))
            return AuthResult.Failure("Current password is incorrect.");

        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return AuthResult.Success(user);
    }

    public async Task<bool> AdminResetPasswordAsync(int userId, string newPassword)
    {
        if (newPassword.Length < 6) return false;

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return false;

        user.PasswordHash = HashPassword(newPassword);
        user.LoginAttempts = 0;
        user.IsLocked = false;
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<AuthResult> AdminCreateUserAsync(string username, string email, string password, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return AuthResult.Failure("Username must be at least 3 characters.");

        if (string.IsNullOrWhiteSpace(email))
            return AuthResult.Failure("Email is required.");

        if (password.Length < 6)
            return AuthResult.Failure("Password must be at least 6 characters.");

        var exists = await _db.Users.AnyAsync(u =>
            u.Username == username || u.Email == email);

        if (exists)
            return AuthResult.Failure("Username or email already exists.");

        var user = new AppUser
        {
            Username = username.Trim(),
            Email = email.Trim(),
            PasswordHash = HashPassword(password),
            Role = role,
            IsActive = true, // Admin-created users are active immediately
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return AuthResult.Success(user);
    }

    public async Task<AuthResult> CompleteFirstTimeSetupAsync(string currentPassword, string newPassword, string? newUsername = null, string? newEmail = null)
    {
        if (newPassword.Length < 6)
            return AuthResult.Failure("New password must be at least 6 characters.");

        // Find the user who must change their password (typically the seeded admin)
        var httpContext = _httpContextAccessor.HttpContext;
        var userIdClaim = httpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        AppUser? user = null;

        if (userIdClaim is not null && int.TryParse(userIdClaim.Value, out var userId))
        {
            user = await _db.Users.FindAsync(userId);
        }

        // Fallback: find any user with MustChangePassword=true that matches the current password
        if (user is null || !user.MustChangePassword)
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.MustChangePassword && u.IsActive);
        }

        if (user is null)
            return AuthResult.Failure("No setup-pending account found.");

        if (!VerifyPassword(currentPassword, user.PasswordHash))
            return AuthResult.Failure("Current password is incorrect.");

        // Update credentials
        user.PasswordHash = HashPassword(newPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(newUsername) && newUsername != user.Username)
        {
            var usernameTaken = await _db.Users.AnyAsync(u => u.Username == newUsername && u.Id != user.Id);
            if (usernameTaken)
                return AuthResult.Failure("That username is already taken.");
            user.Username = newUsername.Trim();
        }

        if (!string.IsNullOrWhiteSpace(newEmail) && newEmail != user.Email)
        {
            var emailTaken = await _db.Users.AnyAsync(u => u.Email == newEmail && u.Id != user.Id);
            if (emailTaken)
                return AuthResult.Failure("That email is already in use.");
            user.Email = newEmail.Trim();
        }

        await _db.SaveChangesAsync();

        // NOTE: Do NOT call LogoutAsync() here — this method runs inside a
        // Blazor Server interactive circuit where HttpContext.SignOutAsync()
        // cannot modify response headers. The caller (Setup.razor) navigates
        // to the /auth/logout HTTP endpoint which properly clears the cookie.

        return AuthResult.Success(user);
    }

    public async Task<bool> LinkUserToEmployeeAsync(int userId, int? employeeId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return false;

        // Validate the employee exists (if linking, not unlinking)
        if (employeeId.HasValue)
        {
            var empExists = await _db.Employees.AnyAsync(e => e.Id == employeeId.Value);
            if (!empExists) return false;
        }

        user.EmployeeId = employeeId;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<EmployeeOption>> GetEmployeeOptionsAsync()
    {
        return await _db.Employees
            .Where(e => !e.IsArchived)
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeOption { Id = e.Id, Name = e.Name })
            .ToListAsync();
    }

    // --- Password hashing with PBKDF2 ---
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var combined = new byte[48];
        salt.CopyTo(combined, 0);
        hash.CopyTo(combined, 16);
        return Convert.ToBase64String(combined);
    }

    internal static bool VerifyPassword(string password, string storedHash)
    {
        var combined = Convert.FromBase64String(storedHash);
        var salt = combined[..16];
        var storedKey = combined[16..];
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, storedKey);
    }
}
