namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IShiftService
{
    Task<List<EmployeeShiftStatus>> GetTeamBoardAsync();
    Task ClockInEmployeeAsync(int employeeId, string? notes = null);
    Task ClockOutEmployeeAsync(int employeeId);
    Task AssignEmployeeToJobAsync(int employeeId, int jobId);
    Task UnassignEmployeeFromJobAsync(int employeeId);
    Task<List<ShiftJobOption>> GetAssignableJobsAsync();
}

public class EmployeeShiftStatus
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public EmployeeRole Role { get; set; }
    public string? Territory { get; set; }
    public string? VehicleAssigned { get; set; }
    public bool IsOnShift { get; set; }
    public DateTime? ShiftStartTime { get; set; }
    public TimeSpan? ShiftDuration { get; set; }
    public int? ActiveTimeEntryId { get; set; }
    public int? CurrentJobId { get; set; }
    public string? CurrentJobNumber { get; set; }
    public string? CurrentJobTitle { get; set; }
    public JobPriority? CurrentJobPriority { get; set; }
    public string? CurrentCustomerName { get; set; }
    public decimal HoursToday { get; set; }
}

public class ShiftJobOption
{
    public int Id { get; set; }
    public string Display { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public JobPriority Priority { get; set; }
    public JobStatus Status { get; set; }
}
