using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Models.Api;

namespace OneManVanFSM.Web.Controllers;

[Route("api/expenses")]
public class ExpensesApiController : SyncApiController
{
    private readonly AppDbContext _db;
    public ExpensesApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SyncResponse<Expense>>> GetAll([FromQuery] DateTime? since)
    {
        var query = _db.Expenses.AsNoTracking().AsQueryable();
        if (since.HasValue)
            query = query.Where(e => e.UpdatedAt > since.Value);

        var data = await query
            .Include(e => e.Lines)
            .OrderByDescending(e => e.ExpenseDate).ToListAsync();
        return Ok(new SyncResponse<Expense> { Data = data, TotalCount = data.Count });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Expense>> Get(int id)
    {
        var expense = await _db.Expenses.AsNoTracking()
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id);
        return expense is not null ? Ok(expense) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<Expense>> Create([FromBody] Expense expense)
    {
        expense.Id = 0;
        foreach (var line in expense.Lines) line.Id = 0;

        expense.CreatedAt = DateTime.UtcNow;
        expense.UpdatedAt = DateTime.UtcNow;
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = expense.Id }, expense);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Expense>> Update(int id, [FromBody] Expense expense)
    {
        var existing = await _db.Expenses.Include(e => e.Lines).FirstOrDefaultAsync(e => e.Id == id);
        if (existing is null) return NotFound();

        if (expense.UpdatedAt < existing.UpdatedAt)
            return Conflict(new SyncConflict
            {
                EntityId = id, EntityType = "Expense",
                Message = "Server version is newer.",
                ServerUpdatedAt = existing.UpdatedAt, ClientUpdatedAt = expense.UpdatedAt
            });

        existing.Category = expense.Category;
        existing.Description = expense.Description;
        existing.Amount = expense.Amount;
        existing.TaxAmount = expense.TaxAmount;
        existing.Total = expense.Total;
        existing.PaymentMethod = expense.PaymentMethod;
        existing.IsBillable = expense.IsBillable;
        existing.Status = expense.Status;
        existing.VendorName = expense.VendorName;
        existing.ReceiptNumber = expense.ReceiptNumber;
        existing.ReceiptPath = expense.ReceiptPath;
        existing.Notes = expense.Notes;
        existing.ExpenseDate = expense.ExpenseDate;
        existing.EmployeeId = expense.EmployeeId;
        existing.JobId = expense.JobId;
        existing.CustomerId = expense.CustomerId;
        existing.CompanyId = expense.CompanyId;
        existing.InvoiceId = expense.InvoiceId;
        existing.UpdatedAt = DateTime.UtcNow;

        // Replace lines
        _db.RemoveRange(existing.Lines);
        foreach (var line in expense.Lines)
        {
            line.Id = 0;
            line.ExpenseId = existing.Id;
        }
        existing.Lines = expense.Lines;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return NotFound();

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
