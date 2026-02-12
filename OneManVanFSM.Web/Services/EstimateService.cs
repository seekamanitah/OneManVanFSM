using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class EstimateService : IEstimateService
{
    private readonly AppDbContext _db;
    public EstimateService(AppDbContext db) => _db = db;

    public async Task<List<EstimateListItem>> GetEstimatesAsync(EstimateFilter? filter = null)
    {
        var showArchived = filter?.ShowArchived ?? false;
        var query = _db.Estimates.Where(e => e.IsArchived == showArchived).AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(e => e.EstimateNumber.ToLower().Contains(term) ||
                    (e.Title != null && e.Title.ToLower().Contains(term)) ||
                    (e.Customer != null && e.Customer.Name.ToLower().Contains(term)));
            }
            if (filter.Status.HasValue)
                query = query.Where(e => e.Status == filter.Status.Value);

            query = filter.SortBy?.ToLower() switch
            {
                "title" => filter.SortDescending ? query.OrderByDescending(e => e.Title) : query.OrderBy(e => e.Title),
                "total" => filter.SortDescending ? query.OrderByDescending(e => e.Total) : query.OrderBy(e => e.Total),
                "status" => filter.SortDescending ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
                _ => filter.SortDescending ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt)
            };
        }
        else query = query.OrderByDescending(e => e.CreatedAt);

        return await query.Select(e => new EstimateListItem
        {
            Id = e.Id, EstimateNumber = e.EstimateNumber, Title = e.Title,
            CustomerName = e.Customer != null ? e.Customer.Name : null,
            SiteName = e.Site != null ? e.Site.Name : null,
            Status = e.Status, Priority = e.Priority, PricingMethod = e.PricingMethod,
            Total = e.Total, ExpiryDate = e.ExpiryDate, CreatedAt = e.CreatedAt
        }).ToListAsync();
    }

    public async Task<EstimateDetail?> GetEstimateAsync(int id)
    {
        return await _db.Estimates.Where(e => e.Id == id && !e.IsArchived)
            .Select(e => new EstimateDetail
            {
            Id = e.Id, EstimateNumber = e.EstimateNumber, Title = e.Title,
                Status = e.Status, Priority = e.Priority, TradeType = e.TradeType,
                VersionNumber = e.VersionNumber, PricingMethod = e.PricingMethod,
                SqFt = e.SqFt, Zones = e.Zones, Stories = e.Stories, SystemType = e.SystemType,
                ExpiryDate = e.ExpiryDate,
                Subtotal = e.Subtotal, MarkupPercent = e.MarkupPercent,
                TaxPercent = e.TaxPercent, ContingencyPercent = e.ContingencyPercent,
                Total = e.Total, DepositRequired = e.DepositRequired,
                DepositReceived = e.DepositReceived,
                DepositAmountPaid = e.DepositAmountPaid,
                DepositPaymentMethod = e.DepositPaymentMethod,
                DepositPaymentReference = e.DepositPaymentReference,
                DepositReceivedDate = e.DepositReceivedDate,
                Notes = e.Notes,
                CustomerId = e.CustomerId, CustomerName = e.Customer != null ? e.Customer.Name : null,
                CustomerEmail = e.Customer != null ? e.Customer.PrimaryEmail : null,
                CustomerPhone = e.Customer != null ? e.Customer.PrimaryPhone : null,
                CustomerAddress = e.Customer != null ? e.Customer.Address : null,
                CustomerCity = e.Customer != null ? e.Customer.City : null,
                CustomerState = e.Customer != null ? e.Customer.State : null,
                CustomerZip = e.Customer != null ? e.Customer.Zip : null,
                CompanyId = e.CompanyId, CompanyName = e.Company != null ? e.Company.Name : null,
                SiteId = e.SiteId,
                SiteName = e.Site != null ? e.Site.Name : null,
                SiteAddress = e.Site != null ? e.Site.Address : null,
                MaterialListId = e.MaterialListId,
                MaterialListName = e.MaterialList != null ? e.MaterialList.Name : null,
                CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
                LinkedJobId = _db.Jobs.Where(j => j.EstimateId == e.Id && !j.IsArchived).Select(j => (int?)j.Id).FirstOrDefault(),
                LinkedJobNumber = _db.Jobs.Where(j => j.EstimateId == e.Id && !j.IsArchived).Select(j => j.JobNumber).FirstOrDefault(),
                Lines = e.Lines.OrderBy(l => l.SortOrder).Select(l => new EstimateLineDto
                {
                    Id = l.Id, ProductId = l.ProductId,
                    ProductName = l.Product != null ? l.Product.Name : null,
                    AssetId = l.AssetId,
                    AssetName = l.Asset != null ? l.Asset.Name : null,
                    Description = l.Description, LineType = l.LineType,
                    Unit = l.Unit, Section = l.Section,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice, LineTotal = l.LineTotal, SortOrder = l.SortOrder
                }).ToList()
            }).FirstOrDefaultAsync();
    }

    public async Task<Estimate> CreateEstimateAsync(EstimateEditModel model)
    {
        var num = model.EstimateNumber;
        if (string.IsNullOrWhiteSpace(num))
        {
            var count = await _db.Estimates.CountAsync() + 1;
            num = $"EST-{count:D5}";
        }

        if (model.Lines.Count > 0)
        {
            model.Subtotal = model.Lines.Sum(l => l.Quantity * l.UnitPrice);
            var markup = model.Subtotal * model.MarkupPercent / 100;
            var tax = model.Subtotal * model.TaxPercent / 100;
            var contingency = model.Subtotal * model.ContingencyPercent / 100;
            model.Total = model.Subtotal + markup + tax + contingency;
        }

        var estimate = new Estimate
        {
            EstimateNumber = num, Title = model.Title,
            Status = model.Status, Priority = model.Priority,
            TradeType = model.TradeType, PricingMethod = model.PricingMethod,
            ExpiryDate = model.ExpiryDate, DepositRequired = model.DepositRequired,
            DepositReceived = model.DepositReceived,
            DepositAmountPaid = model.DepositAmountPaid,
            DepositPaymentMethod = model.DepositPaymentMethod,
            DepositPaymentReference = model.DepositPaymentReference,
            DepositReceivedDate = model.DepositReceivedDate,
            SqFt = model.SqFt, Zones = model.Zones, Stories = model.Stories,
            SystemType = model.SystemType, Subtotal = model.Subtotal,
            MarkupPercent = model.MarkupPercent, TaxPercent = model.TaxPercent,
            ContingencyPercent = model.ContingencyPercent, Total = model.Total,
            Notes = model.Notes, CustomerId = model.CustomerId, CompanyId = model.CompanyId,
            SiteId = model.SiteId, MaterialListId = model.MaterialListId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync();

        int order = 0;
        foreach (var line in model.Lines)
        {
            _db.EstimateLines.Add(new OneManVanFSM.Shared.Models.EstimateLine
            {
                EstimateId = estimate.Id, ProductId = line.ProductId,
                AssetId = line.AssetId,
                Description = line.Description, LineType = line.LineType,
                Unit = line.Unit, Section = line.Section,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice, LineTotal = line.Quantity * line.UnitPrice,
                SortOrder = order++
            });
        }
        if (model.Lines.Count > 0) await _db.SaveChangesAsync();

        return estimate;
    }

    public async Task<Estimate> UpdateEstimateAsync(int id, EstimateEditModel model)
    {
        var e = await _db.Estimates.Include(est => est.Lines).FirstOrDefaultAsync(est => est.Id == id)
            ?? throw new InvalidOperationException("Estimate not found.");

        var wasNotApproved = e.Status != EstimateStatus.Approved;

        if (model.Lines.Count > 0)
        {
            model.Subtotal = model.Lines.Sum(l => l.Quantity * l.UnitPrice);
            var markup = model.Subtotal * model.MarkupPercent / 100;
            var tax = model.Subtotal * model.TaxPercent / 100;
            var contingency = model.Subtotal * model.ContingencyPercent / 100;
            model.Total = model.Subtotal + markup + tax + contingency;
        }

        e.Title = model.Title;
        e.Status = model.Status; e.Priority = model.Priority;
        e.TradeType = model.TradeType; e.PricingMethod = model.PricingMethod;
        e.ExpiryDate = model.ExpiryDate; e.DepositRequired = model.DepositRequired;
        e.DepositReceived = model.DepositReceived;
        e.DepositAmountPaid = model.DepositAmountPaid;
        e.DepositPaymentMethod = model.DepositPaymentMethod;
        e.DepositPaymentReference = model.DepositPaymentReference;
        e.DepositReceivedDate = model.DepositReceivedDate;
        e.SqFt = model.SqFt; e.Zones = model.Zones; e.Stories = model.Stories;
        e.SystemType = model.SystemType; e.Subtotal = model.Subtotal;
        e.MarkupPercent = model.MarkupPercent; e.TaxPercent = model.TaxPercent;
        e.ContingencyPercent = model.ContingencyPercent; e.Total = model.Total;
        e.Notes = model.Notes; e.CustomerId = model.CustomerId; e.CompanyId = model.CompanyId;
        e.SiteId = model.SiteId; e.MaterialListId = model.MaterialListId;
        e.UpdatedAt = DateTime.UtcNow;

        _db.EstimateLines.RemoveRange(e.Lines);
        int order = 0;
        foreach (var line in model.Lines)
        {
            _db.EstimateLines.Add(new OneManVanFSM.Shared.Models.EstimateLine
            {
                EstimateId = e.Id, ProductId = line.ProductId,
                AssetId = line.AssetId,
                Description = line.Description, LineType = line.LineType,
                Unit = line.Unit, Section = line.Section,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice, LineTotal = line.Quantity * line.UnitPrice,
                SortOrder = order++
            });
        }

        await _db.SaveChangesAsync();

        // Auto-create a Job when Estimate transitions to Approved
        if (model.Status == EstimateStatus.Approved && wasNotApproved)
            await CreateJobFromEstimateAsync(e);

        return e;
    }

    public async Task<bool> UpdateStatusAsync(int id, EstimateStatus status)
    {
        var e = await _db.Estimates.FindAsync(id);
        if (e is null) return false;
        var wasNotApproved = e.Status != EstimateStatus.Approved;
        e.Status = status; e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Auto-create a Job when Estimate is approved (pipeline automation)
        if (status == EstimateStatus.Approved && wasNotApproved)
            await CreateJobFromEstimateAsync(e);

        return true;
    }

    /// <summary>
    /// Auto-creates a Job from an approved Estimate, carrying over customer, site, trade, and line item data.
    /// </summary>
    private async Task CreateJobFromEstimateAsync(Estimate estimate)
    {
        // Check if a job already exists for this estimate
        var existing = await _db.Jobs.AnyAsync(j => j.EstimateId == estimate.Id && !j.IsArchived);
        if (existing) return;

        var jobCount = await _db.Jobs.CountAsync() + 1;
        var job = new Job
        {
            JobNumber = $"JOB-{jobCount:D5}",
            Title = $"{estimate.Title}",
            Description = $"Auto-created from Estimate {estimate.EstimateNumber}",
            Status = JobStatus.Approved,
            Priority = estimate.Priority,
            TradeType = estimate.TradeType,
            SystemType = estimate.SystemType,
            EstimatedTotal = estimate.Total,
            Notes = estimate.Notes,
            CustomerId = estimate.CustomerId,
            CompanyId = estimate.CompanyId,
            SiteId = estimate.SiteId,
            EstimateId = estimate.Id,
            MaterialListId = estimate.MaterialListId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        // Link estimate to the job
        estimate.Job = job;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ArchiveEstimateAsync(int id)
    {
        var e = await _db.Estimates.FindAsync(id);
        if (e is null) return false;
        e.IsArchived = true; e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreEstimateAsync(int id)
    {
        var e = await _db.Estimates.FindAsync(id);
        if (e is null) return false;
        e.IsArchived = false; e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteEstimatePermanentlyAsync(int id)
    {
        var e = await _db.Estimates.FindAsync(id);
        if (e is null) return false;
        _db.Estimates.Remove(e);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveEstimatesAsync(List<int> ids)
    {
        var items = await _db.Estimates.Where(e => ids.Contains(e.Id) && !e.IsArchived).ToListAsync();
        foreach (var e in items) { e.IsArchived = true; e.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkRestoreEstimatesAsync(List<int> ids)
    {
        var items = await _db.Estimates.Where(e => ids.Contains(e.Id) && e.IsArchived).ToListAsync();
        foreach (var e in items) { e.IsArchived = false; e.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkDeleteEstimatesPermanentlyAsync(List<int> ids)
    {
        var items = await _db.Estimates.Where(e => ids.Contains(e.Id)).ToListAsync();
        _db.Estimates.RemoveRange(items);
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<List<ProductOption>> GetProductOptionsAsync()
    {
        return await _db.Products
            .OrderBy(p => p.Name)
            .Select(p => new ProductOption
            {
                Id = p.Id, Name = p.Name, Category = p.Category,
                Price = p.Price, Unit = p.Unit
            }).ToListAsync();
    }
}
