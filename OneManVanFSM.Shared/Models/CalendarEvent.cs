namespace OneManVanFSM.Shared.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal? Duration { get; set; } // in hours
    public CalendarEventStatus Status { get; set; } = CalendarEventStatus.Tentative;
    public string? EventType { get; set; } // Job, Personal, Meeting, Reminder, Block-Off, Training
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; } // iCal RRULE format
    public string? Color { get; set; } // Hex color for visual categorization
    public string? Notes { get; set; }
    public string? Checklist { get; set; } // JSON for sub-tasks
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int? ServiceAgreementId { get; set; }
    public ServiceAgreement? ServiceAgreement { get; set; }
}

public enum CalendarEventStatus
{
    Tentative,
    Confirmed,
    Completed,
    Cancelled
}
