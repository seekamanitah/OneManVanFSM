using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/jobs")]
public class JobsApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public JobsApiController(AppDbContext db) => _db = db;

    /// <summary>GET /api/jobs?since=2024-01-01T00:00:00Z</summary>
    [HttpGet]
    public async Task<ActionResult<SyncResponse<Job>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Jobs.AsNoTracking().Where(j => !j.IsArchived);
        if (since.HasValue)
            query = query.Where(j => j.UpdatedAt > since.Value);

        var data = await query.OrderByDescending(j => j.UpdatedAt).ToListAsync();
        return Ok(new SyncResponse<Job> { Data = data, TotalCount = data.Count });
    }

    /// <summary>GET /api/jobs/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Job>> Get(int id)
    {
        var job = await _db.Jobs.AsNoTracking()
            .Include(j => j.Customer)
            .Include(j => j.Site)
            .Include(j => j.Company)
            .Include(j => j.TimeEntries)
            .Include(j => j.QuickNotes)
            .Include(j => j.Documents)
            .Include(j => j.Expenses)
            .Include(j => j.JobAssets)
            .FirstOrDefaultAsync(j => j.Id == id);

        return job is not null ? Ok(job) : NotFound();
    }

    /// <summary>GET /api/jobs/assigned/{employeeId}?since=...</summary>
    [HttpGet("assigned/{employeeId:int}")]
    public async Task<ActionResult<SyncResponse<Job>>> GetAssigned(int employeeId, [FromQuery] DateTime? since)
    {
        var query = _db.Jobs.AsNoTracking()
            .Where(j => !j.IsArchived && j.AssignedEmployeeId == employeeId);
        if (since.HasValue)
            query = query.Where(j => j.UpdatedAt > since.Value);

        var data = await query.OrderBy(j => j.ScheduledDate).ThenBy(j => j.ScheduledTime).ToListAsync();
        return Ok(new SyncResponse<Job> { Data = data, TotalCount = data.Count });
    }

    /// <summary>POST /api/jobs</summary>
    [HttpPost]
    public async Task<ActionResult<Job>> Create([FromBody] Job job)
    {
        job.Id = 0;
        job.CreatedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        if (string.IsNullOrEmpty(job.JobNumber))
            job.JobNumber = await GenerateJobNumber();

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = job.Id }, job);
    }

    /// <summary>PUT /api/jobs/{id}</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Job>> Update(int id, [FromBody] Job job)
    {
        var existing = await _db.Jobs.FindAsync(id);
        if (existing is null) return NotFound();

        // Conflict check: if server version is newer, reject
        if (job.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id,
                EntityType = "Job",
                Message = "Server version is newer than client version.",
                ServerUpdatedAt = existing.UpdatedAt,
                ClientUpdatedAt = job.UpdatedAt
            });

        _db.Entry(existing).CurrentValues.SetValues(job);
        existing.Id = id; // Preserve ID
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    /// <summary>PUT /api/jobs/{id}/status</summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] JobStatus status)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound();

        job.Status = status;
        job.UpdatedAt = DateTime.UtcNow;
        if (status == JobStatus.Completed)
            job.CompletedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(job);
    }

    /// <summary>DELETE /api/jobs/{id} (soft delete)</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound();

        job.IsArchived = true;
        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string> GenerateJobNumber()
    {
        var count = await _db.Jobs.CountAsync();
        return $"JOB-{count + 1:D5}";
    }
}
