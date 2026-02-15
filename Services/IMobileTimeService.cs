using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileTimeService
{
    // Legacy single-layer (kept for backward compat)
    Task<TimeEntry?> ClockInAsync(int employeeId, int? jobId = null, string? timeCategory = null);
    Task<TimeEntry?> ClockOutAsync(int employeeId);
    Task<TimeEntry?> GetActiveClockAsync(int employeeId);

    // Dual-layer: Shift (daily payroll clock)
    Task<TimeEntry?> ShiftClockInAsync(int employeeId);
    Task<TimeEntry?> ShiftClockOutAsync(int employeeId);
    Task<TimeEntry?> GetActiveShiftAsync(int employeeId);

    // Dual-layer: Job clock (per-job, runs within a shift)
    Task<TimeEntry?> JobClockInAsync(int employeeId, int jobId, decimal? rateOverride = null);
    Task<TimeEntry?> JobClockOutAsync(int employeeId, int jobId);
    Task<List<TimeEntry>> GetActiveJobClocksAsync(int employeeId);

    // Break/Pause (within a shift)
    Task<TimeEntry?> ShiftPauseAsync(int employeeId, string? reason = null);
    Task<TimeEntry?> ShiftResumeAsync(int employeeId);
    Task<TimeEntry?> GetActiveBreakAsync(int employeeId);

    Task<List<MobileTimeEntrySummary>> GetRecentEntriesAsync(int employeeId, int count = 10);
    Task<MobileTimeSummary> GetTimeSummaryAsync(int employeeId);
    Task<MobileEmployeeProfile?> GetEmployeeProfileAsync(int employeeId);
    Task<MobilePayrollSummary> GetPayrollSummaryAsync(int employeeId, DateTime weekStart);
}

public class MobileTimeSummary
{
    public decimal HoursToday { get; set; }
    public decimal HoursThisWeek { get; set; }
    public decimal HoursThisMonth { get; set; }
    public int JobsCompletedThisWeek { get; set; }
    public bool IsClockedIn { get; set; }
    public DateTime? CurrentClockInTime { get; set; }
    public decimal ShiftHoursToday { get; set; }
    public decimal ShiftHoursThisWeek { get; set; }
    public decimal JobHoursToday { get; set; }
    public int ActiveJobClockCount { get; set; }
    public string? ActiveJobName { get; set; }
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
    public decimal HourlyRate { get; set; }
    public decimal? OvertimeRate { get; set; }
}

public class MobilePayrollSummary
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal TotalShiftHours { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal? OvertimeRate { get; set; }
    public decimal RegularPay { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal TotalGrossPay { get; set; }
    public List<MobilePayrollDaySummary> Days { get; set; } = [];
    public List<MobilePayrollJobSummary> Jobs { get; set; } = [];
}

public class MobilePayrollDaySummary
{
    public DateTime Date { get; set; }
    public decimal ShiftHours { get; set; }
    public decimal JobHours { get; set; }
    public int JobCount { get; set; }
}

public class MobilePayrollJobSummary
{
    public int JobId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public decimal Hours { get; set; }
    public decimal? RateUsed { get; set; }
    public decimal Amount { get; set; }
}
