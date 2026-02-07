namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IQuickNoteService
{
    Task<List<QuickNoteListItem>> GetNotesAsync(QuickNoteFilter? filter = null);
    Task<QuickNoteDetail?> GetNoteAsync(int id);
    Task<QuickNote> CreateNoteAsync(QuickNoteEditModel model);
    Task<QuickNote> UpdateNoteAsync(int id, QuickNoteEditModel model);
    Task<bool> DeleteNoteAsync(int id);
}

public class QuickNoteFilter
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public bool? IsUrgent { get; set; }
    public QuickNoteStatus? Status { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class QuickNoteListItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public bool IsUrgent { get; set; }
    public QuickNoteStatus Status { get; set; }
    public string? Tags { get; set; }
    public string? CreatedByName { get; set; }
    public string? CustomerName { get; set; }
    public string? JobTitle { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QuickNoteDetail
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public bool IsUrgent { get; set; }
    public QuickNoteStatus Status { get; set; }
    public string? Tags { get; set; }
    public string? PhotoPath { get; set; }
    public string? AudioPath { get; set; }
    public int? CreatedByEmployeeId { get; set; }
    public string? CreatedByName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? JobId { get; set; }
    public string? JobTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class QuickNoteEditModel
{
    public string? Title { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Note text is required.")]
    public string Text { get; set; } = string.Empty;

    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public bool IsUrgent { get; set; }
    public QuickNoteStatus Status { get; set; } = QuickNoteStatus.Active;
    public string? Tags { get; set; }
    public int? CreatedByEmployeeId { get; set; }
    public int? CustomerId { get; set; }
    public int? JobId { get; set; }
}
