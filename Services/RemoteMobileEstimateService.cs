using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

/// <summary>
/// Remote-mode estimate service. Reads from local SQLite cache,
/// pushes mutations to the REST API with offline queue fallback.
/// </summary>
public class RemoteMobileEstimateService : IMobileEstimateService
{
    private readonly AppDbContext _db;
    private readonly ApiClient _api;
    private readonly IOfflineQueueService _offlineQueue;
    private readonly ILogger<RemoteMobileEstimateService> _logger;

    public RemoteMobileEstimateService(AppDbContext db, ApiClient api, IOfflineQueueService offlineQueue, ILogger<RemoteMobileEstimateService> logger)
    {
        _db = db;
        _api = api;
        _offlineQueue = offlineQueue;
        _logger = logger;
    }

    public async Task<List<MobileEstimateCard>> GetEstimatesAsync(MobileEstimateFilter? filter = null)
    {
        var query = _db.Estimates.AsNoTracking()
            .Include(e => e.Customer).Include(e => e.Company).Include(e => e.Site).Include(e => e.Lines)
            .Where(e => !e.IsArchived).AsQueryable();

        if (filter?.Status.HasValue == true)
            query = query.Where(e => e.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(e => e.EstimateNumber.ToLower().Contains(s)
                || (e.Title != null && e.Title.ToLower().Contains(s))
                || (e.Customer != null && e.Customer.Name.ToLower().Contains(s)));
        }

        var estimates = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        return estimates.Select(e => new MobileEstimateCard
        {
            Id = e.Id, EstimateNumber = e.EstimateNumber, Title = e.Title,
            Status = e.Status, Priority = e.Priority,
            CustomerName = e.Customer?.Name, CompanyName = e.Company?.Name,
            SiteAddress = e.Site?.Address, Total = e.Total,
            ExpiryDate = e.ExpiryDate, CreatedAt = e.CreatedAt,
            LineCount = e.Lines.Count,
        }).ToList();
    }

    public async Task<MobileEstimateDetail?> GetEstimateDetailAsync(int id)
    {
        var e = await _db.Estimates.AsNoTracking()
            .Include(e => e.Customer).Include(e => e.Company).Include(e => e.Site).Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (e is null) return null;

        var linkedJob = await _db.Jobs.AsNoTracking()
            .Where(j => j.EstimateId == id && !j.IsArchived)
            .Select(j => new { j.Id, j.JobNumber }).FirstOrDefaultAsync();

        return new MobileEstimateDetail
        {
            Id = e.Id, EstimateNumber = e.EstimateNumber, Title = e.Title,
            Status = e.Status, Priority = e.Priority, TradeType = e.TradeType,
            SystemType = e.SystemType, CustomerName = e.Customer?.Name,
            CustomerPhone = e.Customer?.PrimaryPhone,
            CompanyId = e.CompanyId, CompanyName = e.Company?.Name,
            SiteName = e.Site?.Name,
            SiteAddress = e.Site != null ? $"{e.Site.Address}, {e.Site.City}, {e.Site.State} {e.Site.Zip}" : null,
            SiteId = e.SiteId, CustomerId = e.CustomerId,
            Subtotal = e.Subtotal, MarkupPercent = e.MarkupPercent,
            TaxPercent = e.TaxPercent, Total = e.Total,
            DepositRequired = e.DepositRequired, ExpiryDate = e.ExpiryDate,
            Notes = e.Notes, NeedsReview = e.NeedsReview,
            LinkedJobId = linkedJob?.Id, LinkedJobNumber = linkedJob?.JobNumber,
            CreatedAt = e.CreatedAt,
            Lines = e.Lines.OrderBy(l => l.SortOrder).Select(l => new MobileEstimateLine
            {
                Description = l.Description, LineType = l.LineType, Section = l.Section,
                Quantity = l.Quantity, UnitPrice = l.UnitPrice, LineTotal = l.LineTotal, Unit = l.Unit,
            }).ToList(),
        };
    }

    public async Task<Estimate> QuickCreateAsync(MobileEstimateQuickCreate model)
    {
        var count = await _db.Estimates.CountAsync() + 1;
        var estimate = new Estimate
        {
            EstimateNumber = $"EST-{count:D5}",
            Title = model.Title ?? "Untitled Estimate",
            Status = EstimateStatus.Draft, Priority = model.Priority,
            TradeType = model.TradeType, CustomerId = model.CustomerId,
            SiteId = model.SiteId, Notes = model.Notes,
            NeedsReview = true, CreatedFrom = "mobile",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };

        try
        {
            var created = await _api.PostAsync<Estimate>("api/estimates", estimate);
            if (created is not null)
            {
                _db.Estimates.Add(created);
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
                return created;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Estimate create failed (offline), saving locally and queueing.");
        }

        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        _offlineQueue.Enqueue(new OfflineQueueItem
        {
            HttpMethod = "POST", Endpoint = "api/estimates",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(estimate),
            Description = $"Create estimate: {estimate.EstimateNumber}"
        });
        return estimate;
    }

    public async Task<Estimate> FullCreateAsync(MobileEstimateFullCreate model)
    {
        var count = await _db.Estimates.CountAsync() + 1;
        var lines = model.Lines.Select((l, i) => new EstimateLine
        {
            Description = l.Description, LineType = l.LineType, Section = l.Section,
            Quantity = l.Quantity, UnitPrice = l.UnitPrice,
            LineTotal = l.Quantity * l.UnitPrice, Unit = l.Unit, SortOrder = i,
        }).ToList();

        var subtotal = lines.Sum(l => l.LineTotal);
        var markupAmount = subtotal * model.MarkupPercent / 100;
        var taxableAmount = subtotal + markupAmount;
        var taxAmount = taxableAmount * model.TaxPercent / 100;

        var estimate = new Estimate
        {
            EstimateNumber = $"EST-{count:D5}",
            Title = model.Title ?? "Untitled Estimate",
            Status = EstimateStatus.Draft, Priority = model.Priority,
            TradeType = model.TradeType, SystemType = model.SystemType,
            CustomerId = model.CustomerId, SiteId = model.SiteId,
            Notes = model.Notes, ExpiryDate = model.ExpiryDate,
            MarkupPercent = model.MarkupPercent, TaxPercent = model.TaxPercent,
            Subtotal = subtotal, Total = taxableAmount + taxAmount,
            DepositRequired = model.DepositRequired,
            NeedsReview = false, CreatedFrom = "mobile",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            Lines = lines,
        };

        try
        {
            var created = await _api.PostAsync<Estimate>("api/estimates", estimate);
            if (created is not null)
            {
                _db.Estimates.Add(created);
                await _db.SaveChangesAsync();
                _db.ChangeTracker.Clear();
                return created;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Estimate full-create failed (offline), saving locally and queueing.");
        }

        _db.Estimates.Add(estimate);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        _offlineQueue.Enqueue(new OfflineQueueItem
        {
            HttpMethod = "POST", Endpoint = "api/estimates",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(estimate),
            Description = $"Create estimate (full): {estimate.EstimateNumber}"
        });
        return estimate;
    }

    public async Task<bool> UpdateStatusAsync(int id, EstimateStatus status)
    {
        var e = await _db.Estimates.FindAsync(id);
        if (e is null) return false;

        e.Status = status;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        try
        {
            await _api.PutAsync<Estimate>($"api/estimates/{id}", e);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Estimate {Id} status update failed (offline), queueing.", id);
            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "PUT", Endpoint = $"api/estimates/{id}",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(e),
                Description = $"Update estimate #{id} status → {status}"
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Estimate {Id} status update failed.", id);
            return false;
        }
    }

    public async Task<bool> DeleteEstimateAsync(int id)
    {
        var estimate = await _db.Estimates.FindAsync(id);
        if (estimate is null) return false;

        estimate.IsArchived = true;
        estimate.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        try
        {
            await _api.DeleteAsync($"api/estimates/{id}");
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Estimate {Id} delete failed (offline), queueing.", id);
            _offlineQueue.Enqueue(new OfflineQueueItem
            {
                HttpMethod = "DELETE", Endpoint = $"api/estimates/{id}",
                Description = $"Delete estimate #{id}"
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Estimate {Id} delete failed.", id);
            return false;
        }
    }
}
