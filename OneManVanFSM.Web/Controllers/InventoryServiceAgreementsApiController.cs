using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/inventory")]
public class InventoryApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public InventoryApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<InventoryItem>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.InventoryItems.AsNoTracking().Where(i => !i.IsArchived);
        if (since.HasValue)
            query = query.Where(i => i.UpdatedAt > since.Value);

        var data = await query.OrderBy(i => i.Name).ToListAsync();
        return Ok(new SyncResponse<InventoryItem> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryItem>> Get(int id)
    {
        var item = await _db.InventoryItems.AsNoTracking()
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id);
        return item is not null ? Ok(item) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<InventoryItem>> Create([FromBody] InventoryItem item)
    {
        item.Id = 0;
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InventoryItem>> Update(int id, [FromBody] InventoryItem item)
    {
        var existing = await _db.InventoryItems.FindAsync(id);
        if (existing is null) return NotFound();

        if (item.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "InventoryItem",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = item.UpdatedAt
            });

        existing.Name = item.Name;
        existing.SKU = item.SKU;
        existing.PartNumber = item.PartNumber;
        existing.Barcode = item.Barcode;
        existing.Category = item.Category;
        existing.Unit = item.Unit;
        existing.Description = item.Description;
        existing.ShelfBin = item.ShelfBin;
        existing.PreferredSupplier = item.PreferredSupplier;
        existing.Location = item.Location;
        existing.Quantity = item.Quantity;
        existing.MinThreshold = item.MinThreshold;
        existing.MaxCapacity = item.MaxCapacity;
        existing.LotNumber = item.LotNumber;
        existing.ExpiryDate = item.ExpiryDate;
        existing.Cost = item.Cost;
        existing.Price = item.Price;
        existing.MarkupPercent = item.MarkupPercent;
        existing.LastRestockedDate = item.LastRestockedDate;
        existing.Notes = item.Notes;
        existing.ProductId = item.ProductId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null) return NotFound();

        item.IsArchived = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[Route("api/serviceagreements")]
public class ServiceAgreementsApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public ServiceAgreementsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<ServiceAgreement>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.ServiceAgreements.AsNoTracking().Where(s => !s.IsArchived);
        if (since.HasValue)
            query = query.Where(s => s.UpdatedAt > since.Value);

        var data = await query.OrderByDescending(s => s.StartDate).ToListAsync();
        return Ok(new SyncResponse<ServiceAgreement> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceAgreement>> Get(int id)
    {
        var agreement = await _db.ServiceAgreements.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Site)
            .Include(s => s.ServiceAgreementAssets)
            .FirstOrDefaultAsync(s => s.Id == id);
        return agreement is not null ? Ok(agreement) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<ServiceAgreement>> Create([FromBody] ServiceAgreement agreement)
    {
        agreement.Id = 0;
        if (string.IsNullOrEmpty(agreement.AgreementNumber))
        {
            var count = await _db.ServiceAgreements.CountAsync();
            agreement.AgreementNumber = $"SA-{count + 1:D4}";
        }

        agreement.CreatedAt = DateTime.UtcNow;
        agreement.UpdatedAt = DateTime.UtcNow;
        _db.ServiceAgreements.Add(agreement);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = agreement.Id }, agreement);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceAgreement>> Update(int id, [FromBody] ServiceAgreement agreement)
    {
        var existing = await _db.ServiceAgreements.FindAsync(id);
        if (existing is null) return NotFound();

        if (agreement.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "ServiceAgreement",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = agreement.UpdatedAt
            });

        existing.Title = agreement.Title;
        existing.CoverageLevel = agreement.CoverageLevel;
        existing.StartDate = agreement.StartDate;
        existing.EndDate = agreement.EndDate;
        existing.VisitsIncluded = agreement.VisitsIncluded;
        existing.VisitsUsed = agreement.VisitsUsed;
        existing.Fee = agreement.Fee;
        existing.TradeType = agreement.TradeType;
        existing.BillingFrequency = agreement.BillingFrequency;
        existing.DiscountPercent = agreement.DiscountPercent;
        existing.RenewalDate = agreement.RenewalDate;
        existing.AutoRenew = agreement.AutoRenew;
        existing.AddOns = agreement.AddOns;
        existing.Status = agreement.Status;
        existing.Notes = agreement.Notes;
        existing.CustomerId = agreement.CustomerId;
        existing.CompanyId = agreement.CompanyId;
        existing.SiteId = agreement.SiteId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var agreement = await _db.ServiceAgreements.FindAsync(id);
        if (agreement is null) return NotFound();

        agreement.IsArchived = true;
        agreement.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[Route("api/materiallists")]
public class MaterialListsApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public MaterialListsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<MaterialList>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.MaterialLists.AsNoTracking().Where(m => !m.IsArchived);
        if (since.HasValue)
            query = query.Where(m => m.UpdatedAt > since.Value);

        var data = await query
            .Include(m => m.Items)
            .OrderByDescending(m => m.CreatedAt).ToListAsync();
        return Ok(new SyncResponse<MaterialList> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MaterialList>> Get(int id)
    {
        var list = await _db.MaterialLists.AsNoTracking()
            .Include(m => m.Items)
            .Include(m => m.Customer)
            .Include(m => m.Site)
            .FirstOrDefaultAsync(m => m.Id == id);
        return list is not null ? Ok(list) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<MaterialList>> Create([FromBody] MaterialList list)
    {
        list.Id = 0;
        foreach (var item in list.Items) item.Id = 0;

        list.CreatedAt = DateTime.UtcNow;
        list.UpdatedAt = DateTime.UtcNow;
        _db.MaterialLists.Add(list);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = list.Id }, list);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MaterialList>> Update(int id, [FromBody] MaterialList list)
    {
        var existing = await _db.MaterialLists.Include(m => m.Items).FirstOrDefaultAsync(m => m.Id == id);
        if (existing is null) return NotFound();

        if (list.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "MaterialList",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = list.UpdatedAt
            });

        existing.Name = list.Name;
        existing.IsTemplate = list.IsTemplate;
        existing.Status = list.Status;
        existing.TradeType = list.TradeType;
        existing.PricingMethod = list.PricingMethod;
        existing.Subtotal = list.Subtotal;
        existing.MarkupPercent = list.MarkupPercent;
        existing.TaxPercent = list.TaxPercent;
        existing.ContingencyPercent = list.ContingencyPercent;
        existing.Total = list.Total;
        existing.Notes = list.Notes;
        existing.InternalNotes = list.InternalNotes;
        existing.ExternalNotes = list.ExternalNotes;
        existing.PONumber = list.PONumber;
        existing.JobId = list.JobId;
        existing.CustomerId = list.CustomerId;
        existing.SiteId = list.SiteId;
        existing.UpdatedAt = DateTime.UtcNow;

        // Replace items
        _db.RemoveRange(existing.Items);
        foreach (var item in list.Items)
        {
            item.Id = 0;
            item.MaterialListId = existing.Id;
        }
        existing.Items = list.Items;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var list = await _db.MaterialLists.FindAsync(id);
        if (list is null) return NotFound();

        list.IsArchived = true;
        list.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
