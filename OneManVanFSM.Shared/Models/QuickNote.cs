namespace OneManVanFSM.Shared.Models;

public class QuickNote
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Category { get; set; } // Material, Customer, Equipment, Safety, etc.
    public string? EntityType { get; set; } // Job, Customer, Asset, Invoice, etc.
    public int? EntityId { get; set; }
    public bool IsUrgent { get; set; }
    public QuickNoteStatus Status { get; set; } = QuickNoteStatus.Draft;
    public string? Tags { get; set; } // JSON array
    public string? PhotoPath { get; set; }
    public string? AudioPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? CreatedByEmployeeId { get; set; }
    public Employee? CreatedByEmployee { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? JobId { get; set; }
    public Job? Job { get; set; }
}

public enum QuickNoteStatus
{
    Draft,
    Active,
    Imported,
    Archived
}
