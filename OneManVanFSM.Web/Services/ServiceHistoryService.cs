using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class ServiceHistoryService : IServiceHistoryService
{
    private readonly AppDbContext _db;
    public ServiceHistoryService(AppDbContext db) => _db = db;

    public async Task<List<ServiceHistoryListItem>> GetRecordsAsync(ServiceHistoryFilter? filter = null)
    {
        var query = _db.ServiceHistoryRecords.AsQueryable();
        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(r => r.RecordNumber.ToLower().Contains(term) ||
                    (r.Description != null && r.Description.ToLower().Contains(term)) ||
                    (r.IssueType != null && r.IssueType.ToLower().Contains(term)));
            }
            if (filter.Type.HasValue) query = query.Where(r => r.Type == filter.Type.Value);
            if (filter.Status.HasValue) query = query.Where(r => r.Status == filter.Status.Value);
            if (filter.CustomerId.HasValue) query = query.Where(r => r.CustomerId == filter.CustomerId);
            if (filter.SiteId.HasValue) query = query.Where(r => r.SiteId == filter.SiteId);
            if (filter.AssetId.HasValue) query = query.Where(r => r.AssetId == filter.AssetId);

            query = filter.SortBy?.ToLower() switch
            {
                "recordnumber" => filter.SortDescending ? query.OrderByDescending(r => r.RecordNumber) : query.OrderBy(r => r.RecordNumber),
                "type" => filter.SortDescending ? query.OrderByDescending(r => r.Type) : query.OrderBy(r => r.Type),
                "status" => filter.SortDescending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
                "cost" => filter.SortDescending ? query.OrderByDescending(r => r.Cost) : query.OrderBy(r => r.Cost),
                _ => filter.SortDescending ? query.OrderByDescending(r => r.ServiceDate) : query.OrderBy(r => r.ServiceDate)
            };
        }
        else query = query.OrderByDescending(r => r.ServiceDate);

        return await query.Select(r => new ServiceHistoryListItem
        {
            Id = r.Id,
            RecordNumber = r.RecordNumber,
            Type = r.Type,
            Status = r.Status,
            ServiceDate = r.ServiceDate,
            Description = r.Description,
            Cost = r.Cost,
            CustomerName = r.Customer != null ? r.Customer.Name : null,
            SiteName = r.Site != null ? r.Site.Name : null,
            AssetName = r.Asset != null ? r.Asset.Name : null,
            TechName = r.Tech != null ? r.Tech.Name : null,
            IssueType = r.IssueType
        }).ToListAsync();
    }

    public async Task<ServiceHistoryDetail?> GetRecordAsync(int id)
    {
        return await _db.ServiceHistoryRecords
            .Include(r => r.Customer)
            .Include(r => r.Company)
            .Include(r => r.Site)
            .Include(r => r.Asset)
            .Include(r => r.Job)
            .Include(r => r.Tech)
            .Include(r => r.ClaimActions.OrderByDescending(ca => ca.ActionDate))
            .Where(r => r.Id == id)
            .Select(r => new ServiceHistoryDetail
            {
                Id = r.Id,
                RecordNumber = r.RecordNumber,
                Type = r.Type,
                Status = r.Status,
                ServiceDate = r.ServiceDate,
                Description = r.Description,
                ResolutionNotes = r.ResolutionNotes,
                Evidence = r.Evidence,
                Cost = r.Cost,
                Reimbursement = r.Reimbursement,
                VendorName = r.VendorName,
                IssueType = r.IssueType,
                Notes = r.Notes,
                CustomerId = r.CustomerId,
                CustomerName = r.Customer != null ? r.Customer.Name : null,
                CompanyId = r.CompanyId,
                CompanyName = r.Company != null ? r.Company.Name : null,
                SiteId = r.SiteId,
                SiteName = r.Site != null ? r.Site.Name : null,
                AssetId = r.AssetId,
                AssetName = r.Asset != null ? r.Asset.Name : null,
                JobId = r.JobId,
                JobNumber = r.Job != null ? r.Job.JobNumber : null,
                TechId = r.TechId,
                TechName = r.Tech != null ? r.Tech.Name : null,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ClaimActions = r.ClaimActions.Select(ca => new ClaimActionItem
                {
                    Id = ca.Id,
                    Action = ca.Action,
                    Response = ca.Response,
                    ActionDate = ca.ActionDate,
                    PerformedBy = ca.PerformedBy
                }).ToList()
            }).FirstOrDefaultAsync();
    }

    public async Task<ServiceHistoryRecord> CreateRecordAsync(ServiceHistoryEditModel model)
    {
        var record = new ServiceHistoryRecord
        {
            RecordNumber = model.RecordNumber,
            Type = model.Type,
            Status = model.Status,
            ServiceDate = model.ServiceDate,
            Description = model.Description,
            ResolutionNotes = model.ResolutionNotes,
            Evidence = model.Evidence,
            Cost = model.Cost,
            Reimbursement = model.Reimbursement,
            VendorName = model.VendorName,
            IssueType = model.IssueType,
            Notes = model.Notes,
            CustomerId = model.CustomerId,
            CompanyId = model.CompanyId,
            SiteId = model.SiteId,
            AssetId = model.AssetId,
            JobId = model.JobId,
            TechId = model.TechId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ServiceHistoryRecords.Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    public async Task<bool> UpdateRecordAsync(int id, ServiceHistoryEditModel model)
    {
        var record = await _db.ServiceHistoryRecords.FindAsync(id);
        if (record is null) return false;

        record.RecordNumber = model.RecordNumber;
        record.Type = model.Type;
        record.Status = model.Status;
        record.ServiceDate = model.ServiceDate;
        record.Description = model.Description;
        record.ResolutionNotes = model.ResolutionNotes;
        record.Evidence = model.Evidence;
        record.Cost = model.Cost;
        record.Reimbursement = model.Reimbursement;
        record.VendorName = model.VendorName;
        record.IssueType = model.IssueType;
        record.Notes = model.Notes;
        record.CustomerId = model.CustomerId;
        record.CompanyId = model.CompanyId;
        record.SiteId = model.SiteId;
        record.AssetId = model.AssetId;
        record.JobId = model.JobId;
        record.TechId = model.TechId;
        record.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, ServiceHistoryStatus newStatus, string? notes = null)
    {
        var record = await _db.ServiceHistoryRecords.FindAsync(id);
        if (record is null) return false;
        record.Status = newStatus;
        if (notes is not null) record.ResolutionNotes = notes;
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRecordAsync(int id)
    {
        var record = await _db.ServiceHistoryRecords.FindAsync(id);
        if (record is null) return false;
        _db.ServiceHistoryRecords.Remove(record);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddClaimActionAsync(int recordId, ClaimActionEditModel action)
    {
        var record = await _db.ServiceHistoryRecords.FindAsync(recordId);
        if (record is null) return false;

        _db.ClaimActions.Add(new ClaimAction
        {
            ServiceHistoryRecordId = recordId,
            Action = action.Action,
            Response = action.Response,
            ActionDate = action.ActionDate,
            PerformedBy = action.PerformedBy
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ServiceHistoryKpis> GetKpisAsync()
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var records = _db.ServiceHistoryRecords.AsQueryable();

        return new ServiceHistoryKpis
        {
            TotalRecords = await records.CountAsync(),
            OpenClaims = await records.CountAsync(r => r.Type == ServiceHistoryType.WarrantyClaim &&
                (r.Status == ServiceHistoryStatus.Open || r.Status == ServiceHistoryStatus.Submitted || r.Status == ServiceHistoryStatus.InProgress)),
            ResolvedThisMonth = await records.CountAsync(r => r.Status == ServiceHistoryStatus.Resolved && r.UpdatedAt >= monthStart),
            DeniedClaims = await records.CountAsync(r => r.Status == ServiceHistoryStatus.Denied),
            TotalCost = await records.SumAsync(r => r.Cost ?? 0m),
            TotalReimbursed = await records.SumAsync(r => r.Reimbursement ?? 0m)
        };
    }
}
