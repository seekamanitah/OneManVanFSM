namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IEmployeeService
{
    Task<List<EmployeeListItem>> GetEmployeesAsync(EmployeeFilter? filter = null);
    Task<EmployeeDetail?> GetEmployeeAsync(int id);
    Task<Employee> CreateEmployeeAsync(EmployeeEditModel model);
    Task<Employee> UpdateEmployeeAsync(int id, EmployeeEditModel model);
    Task<bool> ArchiveEmployeeAsync(int id);
    Task<bool> RestoreEmployeeAsync(int id);
    Task<bool> DeleteEmployeePermanentlyAsync(int id);
    Task<int> BulkArchiveEmployeesAsync(List<int> ids);
    Task<int> BulkRestoreEmployeesAsync(List<int> ids);
    Task<int> BulkDeleteEmployeesPermanentlyAsync(List<int> ids);
    // Time clock
    Task<TimeEntry> ClockInAsync(int employeeId, int? jobId, string? notes = null);
    Task<TimeEntry?> ClockOutAsync(int employeeId);
    Task<TimeEntry?> GetActiveClockAsync(int employeeId);
    Task<List<TimesheetDay>> GetWeeklyTimesheetAsync(int employeeId, DateTime weekStart);
    Task<PayPeriodSummary> GetPaySummaryAsync(int employeeId, DateTime periodStart, DateTime periodEnd);
    Task<TimeEntry> AddManualTimeEntryAsync(int employeeId, int? jobId, DateTime start, DateTime end, bool isBillable, string? notes, TimeEntryType entryType = TimeEntryType.Shift, string? timeCategory = null);
    Task<bool> UpdateTimeEntryAsync(int entryId, DateTime start, DateTime end, bool isBillable, string? notes, TimeEntryType entryType, string? timeCategory);
    Task<bool> DeleteTimeEntryAsync(int entryId);
}

public class EmployeeFilter
{
    public string? Search { get; set; }
    public EmployeeRole? Role { get; set; }
    public EmployeeStatus? Status { get; set; }
    public string? Territory { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
    public bool ShowArchived { get; set; }
}

public class EmployeeListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public EmployeeRole Role { get; set; }
    public EmployeeStatus Status { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Territory { get; set; }
    public decimal HourlyRate { get; set; }
    public int ActiveJobCount { get; set; }
}

public class EmployeeDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public EmployeeRole Role { get; set; }
    public EmployeeStatus Status { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime HireDate { get; set; }
    public string? Territory { get; set; }
    public string? Certifications { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public string? VehicleAssigned { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public decimal? OvertimeRate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<EmployeeJobSummary> RecentJobs { get; set; } = [];
    public List<EmployeeTimeEntry> RecentTimeEntries { get; set; } = [];
}

public class EmployeeJobSummary
{
    public int Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public JobStatus Status { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public class EmployeeTimeEntry
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal Hours { get; set; }
    public string? JobTitle { get; set; }
    public bool IsBillable { get; set; }
    public TimeEntryType EntryType { get; set; }
    public string? TimeCategory { get; set; }
}

public class EmployeeEditModel
{
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public EmployeeRole Role { get; set; } = EmployeeRole.Tech;
    public string? Phone { get; set; }

    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string? Email { get; set; }

    public string? Address { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public string? Territory { get; set; }
    public string? Certifications { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public string? VehicleAssigned { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public decimal? OvertimeRate { get; set; }
    public string? Notes { get; set; }
}

public class TimesheetDay
{
    public DateTime Date { get; set; }
    public decimal TotalHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public List<EmployeeTimeEntry> Entries { get; set; } = [];
}

public class PayPeriodSummary
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalRegularHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal RegularPay { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal FlatRateTotal { get; set; }
    public decimal TotalPay { get; set; }
    public List<PayPeriodJobEntry> Jobs { get; set; } = [];
}

public class PayPeriodJobEntry
{
    public int JobId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public decimal Hours { get; set; }
    public string PayType { get; set; } = "Hourly";
    public decimal? FlatRateAmount { get; set; }
    public decimal Amount { get; set; }
}
