using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileInvoiceService : IMobileInvoiceService
{
    private readonly AppDbContext _db;
    public MobileInvoiceService(AppDbContext db) => _db = db;

    public async Task<MobileInvoiceStats> GetStatsAsync()
    {
        var invoices = await _db.Invoices.AsNoTracking().Where(i => !i.IsArchived).ToListAsync();

        return new MobileInvoiceStats
        {
            TotalCount = invoices.Count,
            DraftCount = invoices.Count(i => i.Status == InvoiceStatus.Draft),
            OverdueCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue),
            TotalOutstanding = invoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void).Sum(i => i.BalanceDue),
        };
    }

    public async Task<List<MobileInvoiceCard>> GetInvoicesAsync(MobileInvoiceFilter? filter = null)
    {
        var query = _db.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Job)
            .Include(i => i.Lines)
            .Where(i => !i.IsArchived)
            .AsQueryable();

        if (filter?.Status.HasValue == true)
            query = query.Where(i => i.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(s)
                || (i.Customer != null && i.Customer.Name.ToLower().Contains(s)));
        }

        var invoices = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

        return invoices.Select(i => new MobileInvoiceCard
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            Status = i.Status,
            CustomerName = i.Customer?.Name,
            JobNumber = i.Job?.JobNumber,
            Total = i.Total,
            BalanceDue = i.BalanceDue,
            DueDate = i.DueDate,
            InvoiceDate = i.InvoiceDate,
            LineCount = i.Lines.Count,
        }).ToList();
    }

    public async Task<MobileInvoiceDetail?> GetInvoiceDetailAsync(int id)
    {
        var i = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Site)
            .Include(i => i.Job)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (i == null) return null;

        return new MobileInvoiceDetail
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            Status = i.Status,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            PaymentTerms = i.PaymentTerms,
            PricingType = i.PricingType,
            Subtotal = i.Subtotal,
            TaxAmount = i.TaxAmount,
            TaxRate = i.TaxRate,
            MarkupAmount = i.MarkupAmount,
            DiscountAmount = i.DiscountAmount,
            Total = i.Total,
            AmountPaid = i.AmountPaid,
            BalanceDue = i.BalanceDue,
            Notes = i.Notes,
            Terms = i.Terms,
            NeedsReview = i.NeedsReview,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.Name,
            CustomerPhone = i.Customer?.PrimaryPhone,
            CustomerEmail = i.Customer?.PrimaryEmail,
            SiteId = i.SiteId,
            SiteName = i.Site?.Name,
            SiteAddress = i.Site != null ? $"{i.Site.Address}, {i.Site.City}, {i.Site.State} {i.Site.Zip}" : null,
            JobId = i.JobId,
            JobNumber = i.Job?.JobNumber,
            JobTitle = i.Job?.Title,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
            Lines = i.Lines.OrderBy(l => l.SortOrder).Select(l => new MobileInvoiceLine
            {
                Description = l.Description,
                LineType = l.LineType,
                Unit = l.Unit,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
            }).ToList(),
            Payments = i.Payments.OrderByDescending(p => p.PaymentDate).Select(p => new MobileInvoicePayment
            {
                Amount = p.Amount,
                Method = p.Method.ToString(),
                Reference = p.Reference,
                PaymentDate = p.PaymentDate,
            }).ToList(),
        };
    }

    public async Task<Invoice> QuickCreateAsync(MobileInvoiceQuickCreate model)
    {
        var count = await _db.Invoices.CountAsync() + 1;
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{count:D5}",
            Status = InvoiceStatus.Draft,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            PaymentTerms = "Net 30",
            CustomerId = model.CustomerId,
            JobId = model.JobId,
            SiteId = model.SiteId,
            Notes = model.Notes,
            NeedsReview = true,
            CreatedFrom = "mobile",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    public async Task<Invoice> FullCreateAsync(MobileInvoiceFullCreate model)
    {
        var count = await _db.Invoices.CountAsync() + 1;
        var lines = model.Lines.Select((l, i) => new InvoiceLine
        {
            Description = l.Description,
            LineType = l.LineType,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LineTotal = l.Quantity * l.UnitPrice,
            Unit = l.Unit,
            SortOrder = i,
        }).ToList();

        var subtotal = lines.Sum(l => l.LineTotal);
        var markupAmount = subtotal * model.MarkupPercent / 100;
        var taxableAmount = subtotal + markupAmount;
        var taxAmount = taxableAmount * model.TaxRate / 100;

        decimal discountAmount = 0;
        if (model.DiscountType == "Percent" && model.DiscountValue > 0)
            discountAmount = (taxableAmount + taxAmount) * model.DiscountValue / 100;
        else if (model.DiscountType == "Dollar" && model.DiscountValue > 0)
            discountAmount = model.DiscountValue;

        var total = taxableAmount + taxAmount - discountAmount;

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{count:D5}",
            Status = InvoiceStatus.Draft,
            InvoiceDate = DateTime.UtcNow,
            DueDate = model.DueDate ?? DateTime.UtcNow.AddDays(30),
            PaymentTerms = model.PaymentTerms,
            PricingType = "Material & Labor (Itemized)",
            CustomerId = model.CustomerId,
            JobId = model.JobId,
            SiteId = model.SiteId,
            Notes = model.Notes,
            Terms = model.Terms,
            Subtotal = subtotal,
            MarkupAmount = markupAmount,
            TaxRate = model.TaxRate,
            TaxAmount = taxAmount,
            DiscountType = model.DiscountType,
            DiscountValue = model.DiscountValue,
            DiscountAmount = discountAmount,
            Total = total,
            BalanceDue = total,
            NeedsReview = false,
            CreatedFrom = "mobile",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lines = lines,
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    public async Task<bool> UpdateStatusAsync(int id, InvoiceStatus status)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return false;

        invoice.Status = status;
        invoice.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
