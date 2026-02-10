using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/estimates")]
public class EstimatesApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public EstimatesApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Estimate>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Estimates.AsNoTracking().Where(e => !e.IsArchived);
        if (since.HasValue)
            query = query.Where(e => e.UpdatedAt > since.Value);

        var data = await query
            .Include(e => e.Lines)
            .OrderByDescending(e => e.CreatedAt).ToListAsync();
        return Ok(new SyncResponse<Estimate> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Estimate>> Get(int id)
    {
        var estimate = await _db.Estimates.AsNoTracking()
            .Include(e => e.Lines)
            .Include(e => e.Customer)
            .Include(e => e.Site)
            .FirstOrDefaultAsync(e => e.Id == id);
        return estimate is not null ? Ok(estimate) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Estimate>> Create([FromBody] Estimate estimate)
    {
        estimate.Id = 0;
        foreach (var line in estimate.Lines) line.Id = 0;

        if (string.IsNullOrEmpty(estimate.EstimateNumber))
        {
            var count = await _db.Estimates.CountAsync();
            estimate.EstimateNumber = $"EST-{count + 1:D4}";
        }

        estimate.CreatedAt = DateTime.UtcNow;
        estimate.UpdatedAt = DateTime.UtcNow;
        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = estimate.Id }, estimate);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Estimate>> Update(int id, [FromBody] Estimate estimate)
    {
        var existing = await _db.Estimates.Include(e => e.Lines).FirstOrDefaultAsync(e => e.Id == id);
        if (existing is null) return NotFound();

        if (estimate.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "Estimate",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = estimate.UpdatedAt
            });

        existing.EstimateNumber = estimate.EstimateNumber;
        existing.Title = estimate.Title;
        existing.Status = estimate.Status;
        existing.Priority = estimate.Priority;
        existing.TradeType = estimate.TradeType;
        existing.VersionNumber = estimate.VersionNumber;
        existing.SystemType = estimate.SystemType;
        existing.SqFt = estimate.SqFt;
        existing.Zones = estimate.Zones;
        existing.Stories = estimate.Stories;
        existing.ExpiryDate = estimate.ExpiryDate;
        existing.PricingMethod = estimate.PricingMethod;
        existing.Subtotal = estimate.Subtotal;
        existing.MarkupPercent = estimate.MarkupPercent;
        existing.TaxPercent = estimate.TaxPercent;
        existing.ContingencyPercent = estimate.ContingencyPercent;
        existing.Total = estimate.Total;
        existing.DepositRequired = estimate.DepositRequired;
        existing.DepositReceived = estimate.DepositReceived;
        existing.Notes = estimate.Notes;
        existing.CustomerId = estimate.CustomerId;
        existing.CompanyId = estimate.CompanyId;
        existing.SiteId = estimate.SiteId;
        existing.MaterialListId = estimate.MaterialListId;
        existing.UpdatedAt = DateTime.UtcNow;

        // Replace lines
        _db.RemoveRange(existing.Lines);
        foreach (var line in estimate.Lines)
        {
            line.Id = 0;
            line.EstimateId = existing.Id;
        }
        existing.Lines = estimate.Lines;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var estimate = await _db.Estimates.FindAsync(id);
        if (estimate is null) return NotFound();

        estimate.IsArchived = true;
        estimate.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
