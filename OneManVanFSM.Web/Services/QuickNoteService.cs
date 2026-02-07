using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class QuickNoteService : IQuickNoteService
{
    private readonly AppDbContext _db;
    public QuickNoteService(AppDbContext db) => _db = db;

    public async Task<List<QuickNoteListItem>> GetNotesAsync(QuickNoteFilter? filter = null)
    {
        var query = _db.QuickNotes.AsQueryable();
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(n => n.Text.ToLower().Contains(term) ||
                    (n.Title != null && n.Title.ToLower().Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(n => n.Category == filter.Category);
            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                query = query.Where(n => n.EntityType == filter.EntityType);
            if (filter.EntityId.HasValue)
                query = query.Where(n => n.EntityId == filter.EntityId);
            if (filter.IsUrgent.HasValue)
                query = query.Where(n => n.IsUrgent == filter.IsUrgent.Value);
            if (filter.Status.HasValue)
                query = query.Where(n => n.Status == filter.Status.Value);

            query = filter.SortBy?.ToLower() switch
            {
                "title" => filter.SortDescending ? query.OrderByDescending(n => n.Title) : query.OrderBy(n => n.Title),
                "category" => filter.SortDescending ? query.OrderByDescending(n => n.Category) : query.OrderBy(n => n.Category),
                _ => filter.SortDescending ? query.OrderByDescending(n => n.CreatedAt) : query.OrderBy(n => n.CreatedAt)
            };
        }
        else query = query.OrderByDescending(n => n.CreatedAt);

        return await query.Select(n => new QuickNoteListItem
        {
            Id = n.Id, Title = n.Title, Text = n.Text, Category = n.Category,
            EntityType = n.EntityType, EntityId = n.EntityId,
            IsUrgent = n.IsUrgent, Status = n.Status, Tags = n.Tags,
            CreatedByName = n.CreatedByEmployee != null ? n.CreatedByEmployee.Name : null,
            CustomerName = n.Customer != null ? n.Customer.Name : null,
            JobTitle = n.Job != null ? n.Job.Title : null,
            CreatedAt = n.CreatedAt
        }).ToListAsync();
    }

    public async Task<QuickNoteDetail?> GetNoteAsync(int id)
    {
        return await _db.QuickNotes
            .Include(n => n.CreatedByEmployee)
            .Include(n => n.Customer)
            .Include(n => n.Job)
            .Where(n => n.Id == id)
            .Select(n => new QuickNoteDetail
            {
                Id = n.Id, Title = n.Title, Text = n.Text, Category = n.Category,
                EntityType = n.EntityType, EntityId = n.EntityId,
                IsUrgent = n.IsUrgent, Status = n.Status, Tags = n.Tags,
                PhotoPath = n.PhotoPath, AudioPath = n.AudioPath,
                CreatedByEmployeeId = n.CreatedByEmployeeId,
                CreatedByName = n.CreatedByEmployee != null ? n.CreatedByEmployee.Name : null,
                CustomerId = n.CustomerId,
                CustomerName = n.Customer != null ? n.Customer.Name : null,
                JobId = n.JobId,
                JobTitle = n.Job != null ? n.Job.Title : null,
                CreatedAt = n.CreatedAt, UpdatedAt = n.UpdatedAt
            }).FirstOrDefaultAsync();
    }

    public async Task<QuickNote> CreateNoteAsync(QuickNoteEditModel model)
    {
        var note = new QuickNote
        {
            Title = model.Title, Text = model.Text, Category = model.Category,
            EntityType = model.EntityType, EntityId = model.EntityId,
            IsUrgent = model.IsUrgent, Status = model.Status, Tags = model.Tags,
            CreatedByEmployeeId = model.CreatedByEmployeeId,
            CustomerId = model.CustomerId, JobId = model.JobId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.QuickNotes.Add(note);
        await _db.SaveChangesAsync();
        return note;
    }

    public async Task<QuickNote> UpdateNoteAsync(int id, QuickNoteEditModel model)
    {
        var n = await _db.QuickNotes.FindAsync(id) ?? throw new InvalidOperationException("Note not found.");
        n.Title = model.Title; n.Text = model.Text; n.Category = model.Category;
        n.EntityType = model.EntityType; n.EntityId = model.EntityId;
        n.IsUrgent = model.IsUrgent; n.Status = model.Status; n.Tags = model.Tags;
        n.CreatedByEmployeeId = model.CreatedByEmployeeId;
        n.CustomerId = model.CustomerId; n.JobId = model.JobId;
        n.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return n;
    }

    public async Task<bool> DeleteNoteAsync(int id)
    {
        var n = await _db.QuickNotes.FindAsync(id);
        if (n is null) return false;
        _db.QuickNotes.Remove(n);
        await _db.SaveChangesAsync();
        return true;
    }
}
