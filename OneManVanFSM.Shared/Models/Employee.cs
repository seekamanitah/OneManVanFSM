namespace OneManVanFSM.Shared.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public EmployeeRole Role { get; set; } = EmployeeRole.Tech;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public string? Territory { get; set; }
    public string? Certifications { get; set; } // JSON array of certs with expiry dates
    public string? LicenseNumber { get; set; } // Primary trade license (EPA 608, state HVAC, etc.)
    public DateTime? LicenseExpiry { get; set; }
    public string? VehicleAssigned { get; set; } // Truck number/plate
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public decimal? OvertimeRate { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<TimeEntry> TimeEntries { get; set; } = [];
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Job> AssignedJobs { get; set; } = [];
}

public enum EmployeeRole
{
    Owner,
    Admin,
    Dispatcher,
    Tech,
    Manager,
    Apprentice
}

public enum EmployeeStatus
{
    Active,
    OnLeave,
    Inactive,
    Terminated
}
