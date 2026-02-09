namespace OneManVanFSM.Shared.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Tech;
    public DateTime? LastLogin { get; set; }
    public int LoginAttempts { get; set; }
    public bool IsLocked { get; set; }
    public string? Preferences { get; set; } // JSON for user preferences
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}

public enum UserRole
{
    Owner,
    Admin,
    Dispatcher,
    Tech,
    Manager
}
