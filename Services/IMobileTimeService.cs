using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileTimeService
{
    Task<TimeEntry?> ClockInAsync(int employeeId, int? jobId = null, string? timeCategory = null);
    Task<TimeEntry?> ClockOutAsync(int employeeId);
    Task<TimeEntry?> GetActiveClockAsync(int employeeId);
    Task<List<MobileTimeEntrySummary>> GetRecentEntriesAsync(int employeeId, int count = 10);
    Task<MobileTimeSummary> GetTimeSummaryAsync(int employeeId);
    Task<MobileEmployeeProfile?> GetEmployeeProfileAsync(int employeeId);
}

public class MobileTimeSummary
{
    public decimal HoursToday { get; set; }
    public decimal HoursThisWeek { get; set; }
    public decimal HoursThisMonth { get; set; }
    public int JobsCompletedThisWeek { get; set; }
    public bool IsClockedIn { get; set; }
    public DateTime? CurrentClockInTime { get; set; }
}

public class MobileEmployeeProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Territory { get; set; }
    public List<string> Certifications { get; set; } = [];
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public string? VehicleAssigned { get; set; }
    public DateTime HireDate { get; set; }
}
