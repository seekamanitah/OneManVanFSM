using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

/// <summary>
/// Remote-mode note service. Reads from local SQLite cache,
/// pushes mutations to the REST API with offline queue fallback.
/// </summary>
public class RemoteMobileNoteService : IMobileNoteService
{
    private readonly AppDbContext _db;
    private readonly ApiClient _api;
    private readonly IOfflineQueueService _offlineQueue;
    private readonly ILogger<RemoteMobileNoteService> _logger;

    public RemoteMobileNoteService(AppDbContext db, ApiClient api, IOfflineQueueService offlineQueue, ILogger<RemoteMobileNoteService> logger)
    {
        _db = db;
        _api = api;
        _offlineQueue = offlineQueue;
        _logger = logger;
    }

    public async Task<List<MobileNoteItem>> GetNotesAsync(int? jobId = null)
    {
        var query = _db.QuickNotes.Where(n => n.Status != QuickNoteStatus.Archived).AsQueryable();
        if (jobId.HasValue)
            query = query.Where(n => n.JobId == jobId.Value
                || (n.EntityType == "Job" && n.EntityId == jobId.Value));

        return await query
            .OrderByDescending(n => n.IsUrgent)
            .ThenByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new MobileNoteItem
            {
                Id = n.Id, Title = n.Title, Text = n.Text,
                Category = n.Category, IsUrgent = n.IsUrgent,
                EntityType = n.EntityType, EntityId = n.EntityId,
                JobId = n.JobId, CreatedAt = n.CreatedAt,
            }).ToListAsync();
    }

    public async Task<QuickNote> CreateNoteAsync(MobileNoteCreate model)
    {
        var note = new QuickNote
        {
            Title = model.Title, Text = model.Text,
            Category = model.Category ?? "General",
            IsUrgent = model.IsUrgent,
            EntityType = model.EntityType, EntityId = model.EntityId,
            JobId = model.EntityType == "Job" ? model.EntityId : null,
            CreatedByEmployeeId = model.CreatedByEmployeeId,
            Status = QuickNoteStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };

        try
        {
            var created = await _api.PostAsync<QuickNote>("api/notes", note);
            if (created is not null)
            {
                _db.QuickNotes.Add(created);
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
                return created;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Note create failed (offline), saving locally and queueing.");
        }

        _db.QuickNotes.Add(note);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        _offlineQueue.Enqueue(new OfflineQueueItem
        {
            HttpMethod = "POST", Endpoint = "api/notes",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(note),
            Description = $"Create note: {note.Title ?? note.Text?[..Math.Min(30, note.Text.Length)] ?? "note"}"
        });
        return note;
    }

    public async Task<bool> UpdateNoteAsync(int id, MobileNoteUpdate model)
    {
        var note = await _db.QuickNotes.FindAsync(id);
        if (note is null) return false;

        note.Title = model.Title;
        note.Text = model.Text;
        note.Category = model.Category;
        note.IsUrgent = model.IsUrgent;
        note.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        try
        {
            await _api.PutAsync<QuickNote>($"api/notes/{id}", note);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Note {NoteId} update failed (offline), queueing.", id);
            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "PUT", Endpoint = $"api/notes/{id}",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(note),
                Description = $"Update note #{id}"
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Note {NoteId} update failed.", id);
            return false;
        }
    }

    public async Task<bool> DeleteNoteAsync(int id)
    {
        var note = await _db.QuickNotes.FindAsync(id);
        if (note is null) return false;

        note.Status = QuickNoteStatus.Archived;
        note.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        try
        {
            await _api.DeleteAsync($"api/notes/{id}");
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Note {NoteId} delete failed (offline), queueing.", id);
            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "DELETE", Endpoint = $"api/notes/{id}",
                Description = $"Delete note #{id}"
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Note {NoteId} delete failed.", id);
            return false;
        }
    }
}
