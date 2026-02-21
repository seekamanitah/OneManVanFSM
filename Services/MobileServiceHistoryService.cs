using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileServiceHistoryService(AppDbContext db) : IMobileServiceHistoryService
{
    public async Task<List<MobileServiceHistoryCard>> GetRecordsAsync(MobileServiceHistoryFilter? filter = null)
    {
        var query = db.ServiceHistoryRecords
            .Include(r => r.Customer)
            .Include(r => r.Asset)
            .Include(r => r.Tech)
            .AsQueryable();

        if (filter?.Type.HasValue == true)
            query = query.Where(r => r.Type == filter.Type.Value);

        if (filter?.Status.HasValue == true)
            query = query.Where(r => r.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(r => r.RecordNumber.ToLower().Contains(s)
                || (r.Description != null && r.Description.ToLower().Contains(s))
                || (r.Customer != null && r.Customer.Name.ToLower().Contains(s))
                || (r.VendorName != null && r.VendorName.ToLower().Contains(s)));
        }

        var records = await query.OrderByDescending(r => r.ServiceDate).ToListAsync();

        return records.Select(r => new MobileServiceHistoryCard
        {
            Id = r.Id,
            RecordNumber = r.RecordNumber,
            Type = r.Type,
            Status = r.Status,
            ServiceDate = r.ServiceDate,
            Description = r.Description,
            Cost = r.Cost,
            CustomerName = r.Customer?.Name,
            AssetName = r.Asset?.Name,
            TechName = r.Tech?.Name,
            IssueType = r.IssueType,
        }).ToList();
    }

    public async Task<MobileServiceHistoryDetail?> GetRecordDetailAsync(int id)
    {
        var r = await db.ServiceHistoryRecords
            .Include(rec => rec.Customer)
            .Include(rec => rec.Site)
            .Include(rec => rec.Asset)
            .Include(rec => rec.Job)
            .Include(rec => rec.Tech)
            .Include(rec => rec.ClaimActions)
            .FirstOrDefaultAsync(rec => rec.Id == id);

        if (r is null) return null;

        return new MobileServiceHistoryDetail
        {
            Id = r.Id,
            RecordNumber = r.RecordNumber,
            Type = r.Type,
            Status = r.Status,
            ServiceDate = r.ServiceDate,
            Description = r.Description,
            ResolutionNotes = r.ResolutionNotes,
            Cost = r.Cost,
            Reimbursement = r.Reimbursement,
            VendorName = r.VendorName,
            IssueType = r.IssueType,
            Notes = r.Notes,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.Name,
            SiteId = r.SiteId,
            SiteName = r.Site?.Name,
            AssetId = r.AssetId,
            AssetName = r.Asset?.Name,
            JobId = r.JobId,
            JobNumber = r.Job?.JobNumber,
            TechId = r.TechId,
            TechName = r.Tech?.Name,
            CreatedAt = r.CreatedAt,
            ClaimActions = r.ClaimActions.OrderBy(a => a.ActionDate).Select(a => new MobileClaimActionItem
            {
                Id = a.Id,
                Action = a.Action,
                Response = a.Response,
                ActionDate = a.ActionDate,
                PerformedBy = a.PerformedBy,
            }).ToList(),
        };
    }

    public async Task<int> CreateRecordAsync(MobileServiceHistoryCreate model)
    {
        var nextNum = await db.ServiceHistoryRecords.CountAsync() + 1;

        var record = new ServiceHistoryRecord
        {
            RecordNumber = $"SH-{nextNum:D4}",
            Type = model.Type,
            Status = ServiceHistoryStatus.Open,
            ServiceDate = model.ServiceDate,
            Description = model.Description,
            Cost = model.Cost,
            VendorName = model.VendorName,
            IssueType = model.IssueType,
            Notes = model.Notes,
            CustomerId = model.CustomerId,
            SiteId = model.SiteId,
            AssetId = model.AssetId,
            JobId = model.JobId,
            TechId = model.TechId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.ServiceHistoryRecords.Add(record);
        await db.SaveChangesAsync();
        return record.Id;
    }

    public async Task<bool> UpdateRecordAsync(int id, MobileServiceHistoryUpdate model)
    {
        var record = await db.ServiceHistoryRecords.FindAsync(id);
        if (record is null) return false;

        record.Type = model.Type;
        record.Status = model.Status;
        record.ServiceDate = model.ServiceDate;
        record.Description = model.Description;
        record.ResolutionNotes = model.ResolutionNotes;
        record.Cost = model.Cost;
        record.Reimbursement = model.Reimbursement;
        record.VendorName = model.VendorName;
        record.IssueType = model.IssueType;
        record.Notes = model.Notes;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, ServiceHistoryStatus status)
    {
        var record = await db.ServiceHistoryRecords.FindAsync(id);
        if (record is null) return false;
        record.Status = status;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRecordAsync(int id)
    {
        var record = await db.ServiceHistoryRecords.FindAsync(id);
        if (record is null) return false;
        db.ServiceHistoryRecords.Remove(record);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddClaimActionAsync(int recordId, MobileClaimActionCreate action)
    {
        var record = await db.ServiceHistoryRecords.FindAsync(recordId);
        if (record is null) return false;

        db.Set<ClaimAction>().Add(new ClaimAction
        {
            ServiceHistoryRecordId = recordId,
            Action = action.Action,
            Response = action.Response,
            ActionDate = action.ActionDate,
            PerformedBy = action.PerformedBy,
        });
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<MobileServiceHistoryStats> GetStatsAsync()
    {
        var records = await db.ServiceHistoryRecords.ToListAsync();
        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        return new MobileServiceHistoryStats
        {
            TotalRecords = records.Count,
            OpenClaims = records.Count(r => r.Status == ServiceHistoryStatus.Open || r.Status == ServiceHistoryStatus.Submitted || r.Status == ServiceHistoryStatus.InProgress),
            ResolvedThisMonth = records.Count(r => r.Status == ServiceHistoryStatus.Resolved && r.UpdatedAt >= monthStart),
            TotalCost = records.Sum(r => r.Cost ?? 0),
            TotalReimbursed = records.Sum(r => r.Reimbursement ?? 0),
        };
    }
}
