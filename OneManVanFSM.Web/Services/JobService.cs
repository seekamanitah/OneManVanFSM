using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class JobService : IJobService
{
    private readonly AppDbContext _db;
    public JobService(AppDbContext db) => _db = db;

    public async Task<List<JobListItem>> GetJobsAsync(JobFilter? filter = null)
    {
        var query = _db.Jobs.Where(j => !j.IsArchived).AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(j =>
                    j.JobNumber.ToLower().Contains(term) ||
                    (j.Title != null && j.Title.ToLower().Contains(term)) ||
                    (j.Customer != null && j.Customer.Name.ToLower().Contains(term)));
            }
            if (filter.Status.HasValue) query = query.Where(j => j.Status == filter.Status.Value);
            if (filter.Priority.HasValue) query = query.Where(j => j.Priority == filter.Priority.Value);
            if (filter.CustomerId.HasValue) query = query.Where(j => j.CustomerId == filter.CustomerId.Value);
            if (filter.SiteId.HasValue) query = query.Where(j => j.SiteId == filter.SiteId.Value);

            query = filter.SortBy?.ToLower() switch
            {
                "jobnumber" => filter.SortDescending ? query.OrderByDescending(j => j.JobNumber) : query.OrderBy(j => j.JobNumber),
                "status" => filter.SortDescending ? query.OrderByDescending(j => j.Status) : query.OrderBy(j => j.Status),
                "priority" => filter.SortDescending ? query.OrderByDescending(j => j.Priority) : query.OrderBy(j => j.Priority),
                _ => filter.SortDescending ? query.OrderByDescending(j => j.ScheduledDate) : query.OrderBy(j => j.ScheduledDate)
            };
        }
        else
        {
            query = query.OrderByDescending(j => j.ScheduledDate);
        }

        return await query.Select(j => new JobListItem
        {
            Id = j.Id,
            JobNumber = j.JobNumber,
            Title = j.Title,
            CustomerName = j.Customer != null ? j.Customer.Name : null,
            SiteName = j.Site != null ? j.Site.Name : null,
            Status = j.Status,
            Priority = j.Priority,
            ScheduledDate = j.ScheduledDate,
            TechnicianName = j.AssignedEmployee != null ? j.AssignedEmployee.Name : null,
            EstimatedTotal = j.EstimatedTotal
        }).ToListAsync();
    }

    public async Task<JobDetail?> GetJobAsync(int id)
    {
        var job = await _db.Jobs
            .Include(j => j.Customer).Include(j => j.Site)
            .Include(j => j.AssignedEmployee)
            .Include(j => j.Invoice).Include(j => j.Estimate)
            .Include(j => j.MaterialList).ThenInclude(ml => ml!.Items)
            .Include(j => j.TimeEntries).ThenInclude(t => t.Employee)
            .Include(j => j.QuickNotes)
            .Include(j => j.JobEmployees).ThenInclude(je => je.Employee)
            .Include(j => j.JobAssets).ThenInclude(ja => ja.Asset)
            .AsSplitQuery()
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return null;

        return new JobDetail
        {
            Id = job.Id, JobNumber = job.JobNumber, Title = job.Title, Description = job.Description,
            Status = job.Status, Priority = job.Priority,
            TradeType = job.TradeType, JobType = job.JobType, SystemType = job.SystemType,
            ScheduledDate = job.ScheduledDate, ScheduledTime = job.ScheduledTime,
            EstimatedDuration = job.EstimatedDuration, EstimatedTotal = job.EstimatedTotal,
            ActualDuration = job.ActualDuration, ActualTotal = job.ActualTotal,
            PermitRequired = job.PermitRequired, PermitNumber = job.PermitNumber,
            Notes = job.Notes, CompletedDate = job.CompletedDate,
            CreatedAt = job.CreatedAt, UpdatedAt = job.UpdatedAt,
            CustomerId = job.CustomerId, CustomerName = job.Customer?.Name,
            SiteId = job.SiteId, SiteName = job.Site?.Name,
            SiteAddress = job.Site != null ? string.Join(", ", new[] { job.Site.Address, job.Site.City, job.Site.State }.Where(p => !string.IsNullOrWhiteSpace(p))) : null,
            AssignedEmployeeId = job.AssignedEmployeeId, TechnicianName = job.AssignedEmployee?.Name,
            EstimateId = job.EstimateId, EstimateNumber = job.Estimate?.EstimateNumber,
            InvoiceId = job.InvoiceId, InvoiceNumber = job.Invoice?.InvoiceNumber,
            MaterialListId = job.MaterialListId,
            TimeEntries = job.TimeEntries.OrderByDescending(t => t.StartTime).Select(t => new TimeEntrySummary
            {
                Id = t.Id, EmployeeName = t.Employee?.Name,
                StartTime = t.StartTime, EndTime = t.EndTime, Hours = t.Hours, Notes = t.Notes
            }).ToList(),
            Notes2 = job.QuickNotes.OrderByDescending(n => n.CreatedAt).Select(n => new NoteSummary
            {
                Id = n.Id, Content = n.Text, Category = n.Category, CreatedAt = n.CreatedAt
            }).ToList(),
            AssignedEmployees = job.JobEmployees.Select(je => new JobEmployeeDto
            {
                EmployeeId = je.EmployeeId,
                Name = je.Employee?.Name ?? "—",
                Role = je.Role,
                PayType = je.PayType.ToString(),
                FlatRateAmount = je.FlatRateAmount,
                AssignedAt = je.AssignedAt
            }).ToList(),
            LinkedAssets = job.JobAssets.Select(ja => new JobLinkedAsset
            {
                Id = ja.Asset?.Id ?? 0,
                Name = ja.Asset?.Name ?? "Unknown",
                AssetType = ja.Asset?.AssetType,
                Status = ja.Asset?.Status ?? AssetStatus.Active,
                Role = ja.Role
            }).ToList(),
            MaterialSummary = job.MaterialList != null ? new JobMaterialSummary
            {
                Id = job.MaterialList.Id,
                Name = job.MaterialList.Name,
                ItemCount = job.MaterialList.Items.Count,
                Total = job.MaterialList.Total
            } : null
        };
    }

    public async Task<Job> CreateJobAsync(JobEditModel model)
    {
        var jobNum = model.JobNumber;
        if (string.IsNullOrWhiteSpace(jobNum))
        {
            var count = await _db.Jobs.CountAsync() + 1;
            jobNum = $"JOB-{count:D5}";
        }

        var job = new Job
        {
            JobNumber = jobNum, Title = model.Title, Description = model.Description,
            Status = model.Status, Priority = model.Priority,
            TradeType = model.TradeType, JobType = model.JobType, SystemType = model.SystemType,
            ScheduledDate = model.ScheduledDate, EstimatedDuration = model.EstimatedDuration,
            EstimatedTotal = model.EstimatedTotal, ActualDuration = model.ActualDuration,
            ActualTotal = model.ActualTotal, PermitRequired = model.PermitRequired,
            PermitNumber = model.PermitNumber, Notes = model.Notes,
            CustomerId = model.CustomerId, SiteId = model.SiteId,
            AssignedEmployeeId = model.AssignedEmployeeId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();
        return job;
    }

    public async Task<Job> UpdateJobAsync(int id, JobEditModel model)
    {
        var job = await _db.Jobs.FindAsync(id) ?? throw new InvalidOperationException("Job not found.");
        job.Title = model.Title; job.Description = model.Description;
        job.Status = model.Status; job.Priority = model.Priority;
        job.TradeType = model.TradeType; job.JobType = model.JobType; job.SystemType = model.SystemType;
        job.ScheduledDate = model.ScheduledDate; job.EstimatedDuration = model.EstimatedDuration;
        job.EstimatedTotal = model.EstimatedTotal; job.ActualDuration = model.ActualDuration;
        job.ActualTotal = model.ActualTotal; job.PermitRequired = model.PermitRequired;
        job.PermitNumber = model.PermitNumber; job.Notes = model.Notes;
        job.CustomerId = model.CustomerId; job.SiteId = model.SiteId;
        job.AssignedEmployeeId = model.AssignedEmployeeId;
        job.UpdatedAt = DateTime.UtcNow;
        if (model.Status == JobStatus.Completed && !job.CompletedDate.HasValue)
            job.CompletedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return job;
    }

    public async Task<bool> UpdateStatusAsync(int id, JobStatus status)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return false;
        job.Status = status; job.UpdatedAt = DateTime.UtcNow;
        if (status == JobStatus.Completed) job.CompletedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveJobAsync(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return false;
        job.IsArchived = true; job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<EmployeeOption>> GetTechniciansAsync()
    {
        return await _db.Employees
            .Where(e => !e.IsArchived && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeOption { Id = e.Id, Name = e.Name, Territory = e.Territory })
            .ToListAsync();
    }

    public async Task AddEmployeeToJobAsync(int jobId, int employeeId, string? role, JobEmployeePayType payType, decimal? flatRate)
    {
        var exists = await _db.JobEmployees.AnyAsync(je => je.JobId == jobId && je.EmployeeId == employeeId);
        if (exists) return;
        _db.JobEmployees.Add(new JobEmployee
        {
            JobId = jobId,
            EmployeeId = employeeId,
            Role = role,
            PayType = payType,
            FlatRateAmount = payType == JobEmployeePayType.FlatRate ? flatRate : null,
            AssignedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveEmployeeFromJobAsync(int jobId, int employeeId)
    {
        var je = await _db.JobEmployees.FirstOrDefaultAsync(j => j.JobId == jobId && j.EmployeeId == employeeId);
        if (je is not null)
        {
            _db.JobEmployees.Remove(je);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<JobEmployeeDto>> GetJobEmployeesAsync(int jobId)
    {
        return await _db.JobEmployees
            .Where(je => je.JobId == jobId)
            .Include(je => je.Employee)
            .Select(je => new JobEmployeeDto
            {
                EmployeeId = je.EmployeeId,
                Name = je.Employee != null ? je.Employee.Name : "Unknown",
                Role = je.Role,
                PayType = je.PayType.ToString(),
                FlatRateAmount = je.FlatRateAmount,
                AssignedAt = je.AssignedAt
            })
            .ToListAsync();
    }
}
