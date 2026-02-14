namespace OneManVanFSM.Shared.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal Hours { get; set; }
    public decimal? OvertimeHours { get; set; }
    public bool IsBillable { get; set; } = true;
    public string? TimeCategory { get; set; } // Travel, On-Site, Diagnostic, Admin, Break, Training
    public TimeEntryType EntryType { get; set; } = TimeEntryType.Shift;
    public decimal? HourlyRate { get; set; } // Snapshot of rate at time of clock-in
    public double? ClockInLatitude { get; set; }
    public double? ClockInLongitude { get; set; }
    public double? ClockOutLatitude { get; set; }
    public double? ClockOutLongitude { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
}

public enum TimeEntryType
{
    Shift,    // Daily work shift for payroll
    JobClock  // Per-job time tracking
}
