using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string usernameOrEmail, string password);
    Task LogoutAsync();
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AppUser?> GetCurrentUserAsync();
    Task<bool> RequestPasswordResetAsync(string email);
    Task<List<UserListItem>> GetUsersAsync();
    Task<bool> ToggleUserActiveAsync(int userId);
    Task<bool> UpdateUserRoleAsync(int userId, UserRole role);
    Task<bool> ToggleLockAsync(int userId);
    Task<AuthResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> AdminResetPasswordAsync(int userId, string newPassword);
    Task<AuthResult> AdminCreateUserAsync(string username, string email, string password, UserRole role);
    Task<AuthResult> CompleteFirstTimeSetupAsync(string currentPassword, string newPassword, string? newUsername = null, string? newEmail = null);
    Task<bool> LinkUserToEmployeeAsync(int userId, int? employeeId);
    Task<List<EmployeeOption>> GetEmployeeOptionsAsync();
}

public class AuthResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public AppUser? User { get; set; }

    public static AuthResult Success(AppUser user) => new() { Succeeded = true, User = user };
    public static AuthResult Failure(string message) => new() { Succeeded = false, ErrorMessage = message };
}

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public UserRole RequestedRole { get; set; } = UserRole.Tech;
}

public class UserListItem
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LastLogin { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
}
