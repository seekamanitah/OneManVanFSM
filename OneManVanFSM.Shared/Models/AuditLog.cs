namespace OneManVanFSM.Shared.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, Viewed, etc.
    public string EntityType { get; set; } = string.Empty; // Job, Customer, Invoice, etc.
    public int? EntityId { get; set; }
    public string? Details { get; set; } // JSON with change details
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? UserId { get; set; }
    public AppUser? User { get; set; }
}
