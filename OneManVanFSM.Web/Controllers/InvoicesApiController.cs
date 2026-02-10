using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/invoices")]
public class InvoicesApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public InvoicesApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Invoice>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Invoices.AsNoTracking().Where(i => !i.IsArchived);
        if (since.HasValue)
            query = query.Where(i => i.UpdatedAt > since.Value);

        var data = await query
            .Include(i => i.Lines)
            .OrderByDescending(i => i.CreatedAt).ToListAsync();
        return Ok(new SyncResponse<Invoice> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Invoice>> Get(int id)
    {
        var invoice = await _db.Invoices.AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Include(i => i.Customer)
            .Include(i => i.Site)
            .FirstOrDefaultAsync(i => i.Id == id);
        return invoice is not null ? Ok(invoice) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Invoice>> Create([FromBody] Invoice invoice)
    {
        invoice.Id = 0;
        foreach (var line in invoice.Lines) line.Id = 0;

        if (string.IsNullOrEmpty(invoice.InvoiceNumber))
        {
            var count = await _db.Invoices.CountAsync();
            invoice.InvoiceNumber = $"INV-{count + 1:D4}";
        }

        invoice.CreatedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = invoice.Id }, invoice);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Invoice>> Update(int id, [FromBody] Invoice invoice)
    {
        var existing = await _db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id);
        if (existing is null) return NotFound();

        if (invoice.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "Invoice",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = invoice.UpdatedAt
            });

        existing.InvoiceNumber = invoice.InvoiceNumber;
        existing.Status = invoice.Status;
        existing.InvoiceDate = invoice.InvoiceDate;
        existing.DueDate = invoice.DueDate;
        existing.PaymentTerms = invoice.PaymentTerms;
        existing.Subtotal = invoice.Subtotal;
        existing.TaxAmount = invoice.TaxAmount;
        existing.TaxRate = invoice.TaxRate;
        existing.TaxIncludedInPrice = invoice.TaxIncludedInPrice;
        existing.PricingType = invoice.PricingType;
        existing.MarkupAmount = invoice.MarkupAmount;
        existing.Total = invoice.Total;
        existing.DiscountAmount = invoice.DiscountAmount;
        existing.DepositApplied = invoice.DepositApplied;
        existing.AmountPaid = invoice.AmountPaid;
        existing.BalanceDue = invoice.BalanceDue;
        existing.Notes = invoice.Notes;
        existing.Terms = invoice.Terms;
        existing.IncludeSiteLocation = invoice.IncludeSiteLocation;
        existing.IncludeAssetInfo = invoice.IncludeAssetInfo;
        existing.CustomerId = invoice.CustomerId;
        existing.CompanyId = invoice.CompanyId;
        existing.JobId = invoice.JobId;
        existing.SiteId = invoice.SiteId;
        existing.UpdatedAt = DateTime.UtcNow;

        // Replace lines
        _db.RemoveRange(existing.Lines);
        foreach (var line in invoice.Lines)
        {
            line.Id = 0;
            line.InvoiceId = existing.Id;
        }
        existing.Lines = invoice.Lines;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();

        invoice.IsArchived = true;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
