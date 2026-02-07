using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileNoteService(AppDbContext db) : IMobileNoteService
{
    public async Task<List<MobileNoteItem>> GetNotesAsync(int? jobId = null)
    {
        var query = db.QuickNotes.AsQueryable();

        if (jobId.HasValue)
            query = query.Where(n => n.JobId == jobId.Value
                || (n.EntityType == "Job" && n.EntityId == jobId.Value));

        return await query
            .OrderByDescending(n => n.IsUrgent)
            .ThenByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new MobileNoteItem
            {
                Id = n.Id,
                Title = n.Title,
                Text = n.Text,
                Category = n.Category,
                IsUrgent = n.IsUrgent,
                EntityType = n.EntityType,
                EntityId = n.EntityId,
                JobId = n.JobId,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<QuickNote> CreateNoteAsync(MobileNoteCreate model)
    {
        var note = new QuickNote
        {
            Title = model.Title,
            Text = model.Text,
            Category = model.Category ?? "General",
            IsUrgent = model.IsUrgent,
            EntityType = model.EntityType,
            EntityId = model.EntityId,
            JobId = model.EntityType == "Job" ? model.EntityId : null,
            CreatedByEmployeeId = model.CreatedByEmployeeId,
            Status = QuickNoteStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.QuickNotes.Add(note);
        await db.SaveChangesAsync();
        return note;
    }

    public async Task<bool> DeleteNoteAsync(int id)
    {
        var note = await db.QuickNotes.FindAsync(id);
        if (note is null) return false;
        db.QuickNotes.Remove(note);
        await db.SaveChangesAsync();
        return true;
    }
}
