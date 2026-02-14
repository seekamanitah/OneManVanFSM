using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileCalendarService
{
    Task<List<MobileCalendarEvent>> GetEventsAsync(DateTime date, int employeeId, bool isElevated = false);
    Task<List<MobileCalendarEvent>> GetWeekEventsAsync(DateTime weekStart, int employeeId, bool isElevated = false);
}

public class MobileCalendarEvent
{
    public int Id { get; set; }
    public int? JobId { get; set; }
    public string? Title { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public CalendarEventStatus Status { get; set; }
    public string? JobNumber { get; set; }
    public string? SiteAddress { get; set; }
    public string EventType { get; set; } = "Job";
    public string? Color { get; set; }
}
