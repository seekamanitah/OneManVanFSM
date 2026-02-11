using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/products")]
public class ProductsApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public ProductsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Product>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Products.AsNoTracking().Where(p => !p.IsArchived);
        if (since.HasValue)
            query = query.Where(p => p.UpdatedAt > since.Value);
        var data = await query.OrderBy(p => p.Name).ToListAsync();
        return Ok(new SyncResponse<Product> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> Get(int id)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product is not null ? Ok(product) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        product.Id = 0;
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }
}
