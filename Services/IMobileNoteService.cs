using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileNoteService
{
    Task<List<MobileNoteItem>> GetNotesAsync(int? jobId = null);
    Task<QuickNote> CreateNoteAsync(MobileNoteCreate model);
    Task<bool> DeleteNoteAsync(int id);
}

public class MobileNoteItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsUrgent { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public int? JobId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MobileNoteCreate
{
    public string? Title { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsUrgent { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public int? CreatedByEmployeeId { get; set; }
}
