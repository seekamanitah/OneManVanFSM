using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/customers")]
public class CustomersApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public CustomersApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Customer>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Customers.AsNoTracking().Where(c => !c.IsArchived);
        if (since.HasValue)
            query = query.Where(c => c.UpdatedAt > since.Value);

        var data = await query.OrderBy(c => c.Name).ToListAsync();
        return Ok(new SyncResponse<Customer> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> Get(int id)
    {
        var customer = await _db.Customers.AsNoTracking()
            .Include(c => c.Company)
            .Include(c => c.Sites.Where(s => !s.IsArchived))
            .FirstOrDefaultAsync(c => c.Id == id);
        return customer is not null ? Ok(customer) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create([FromBody] Customer customer)
    {
        customer.Id = 0;
        customer.CreatedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Customer>> Update(int id, [FromBody] Customer customer)
    {
        var existing = await _db.Customers.FindAsync(id);
        if (existing is null) return NotFound();

        if (customer.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "Customer",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = customer.UpdatedAt
            });

        _db.Entry(existing).CurrentValues.SetValues(customer);
        existing.Id = id;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();
        customer.IsArchived = true;
        customer.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
