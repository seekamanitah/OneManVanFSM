namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface ICalendarService
{
    Task<List<CalendarEventItem>> GetEventsAsync(DateTime? start = null, DateTime? end = null, int? employeeId = null);
    Task<CalendarEventDetail?> GetEventDetailAsync(int id);
    Task<CalendarEvent> CreateEventAsync(CalendarEventEditModel model);
    Task<CalendarEvent> UpdateEventAsync(int id, CalendarEventEditModel model);
    Task<bool> UpdateStatusAsync(int id, CalendarEventStatus status);
    Task<bool> DeleteEventAsync(int id);
}

public class CalendarEventItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal? Duration { get; set; }
    public CalendarEventStatus Status { get; set; }
    public string? EventType { get; set; }
    public string? Color { get; set; }
    public bool IsRecurring { get; set; }
    public string? JobNumber { get; set; }
    public string? EmployeeName { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
    public string? Notes { get; set; }
}

public class CalendarEventDetail
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal? Duration { get; set; }
    public CalendarEventStatus Status { get; set; }
    public string? EventType { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public string? Color { get; set; }
    public string? Notes { get; set; }
    public string? Checklist { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? JobId { get; set; }
    public string? JobNumber { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int? ServiceAgreementId { get; set; }
    public string? AgreementNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? SiteName { get; set; }
}

public class CalendarEventEditModel
{
    public string? Title { get; set; }
    public DateTime StartDateTime { get; set; } = DateTime.UtcNow;
    public DateTime EndDateTime { get; set; } = DateTime.UtcNow.AddHours(2);
    public decimal? Duration { get; set; }
    public CalendarEventStatus Status { get; set; } = CalendarEventStatus.Tentative;
    public string? EventType { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public string? Color { get; set; }
    public string? Notes { get; set; }
    public string? Checklist { get; set; }
    public int? JobId { get; set; }
    public int? EmployeeId { get; set; }
    public int? ServiceAgreementId { get; set; }
}
