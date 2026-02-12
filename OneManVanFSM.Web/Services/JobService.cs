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
        var showArchived = filter?.ShowArchived ?? false;
        var query = _db.Jobs.Where(j => j.IsArchived == showArchived).AsQueryable();

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
                "title" => filter.SortDescending ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
                "customer" => filter.SortDescending ? query.OrderByDescending(j => j.Customer != null ? j.Customer.Name : "") : query.OrderBy(j => j.Customer != null ? j.Customer.Name : ""),
                "site" => filter.SortDescending ? query.OrderByDescending(j => j.Site != null ? j.Site.Name : "") : query.OrderBy(j => j.Site != null ? j.Site.Name : ""),
                "status" => filter.SortDescending ? query.OrderByDescending(j => j.Status) : query.OrderBy(j => j.Status),
                "priority" => filter.SortDescending ? query.OrderByDescending(j => j.Priority) : query.OrderBy(j => j.Priority),
                "tech" => filter.SortDescending ? query.OrderByDescending(j => j.AssignedEmployee != null ? j.AssignedEmployee.Name : "") : query.OrderBy(j => j.AssignedEmployee != null ? j.AssignedEmployee.Name : ""),
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
            .Include(j => j.Customer).Include(j => j.Company).Include(j => j.Site)
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
            CompanyId = job.CompanyId, CompanyName = job.Company?.Name,
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
            CustomerId = model.CustomerId, CompanyId = model.CompanyId, SiteId = model.SiteId,
            AssignedEmployeeId = model.AssignedEmployeeId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        // Link assets
        if (model.AssetIds.Count > 0)
        {
            foreach (var assetId in model.AssetIds)
            {
                _db.JobAssets.Add(new JobAsset { JobId = job.Id, AssetId = assetId, CreatedAt = DateTime.UtcNow });
            }
            await _db.SaveChangesAsync();
        }

        return job;
    }

    public async Task<Job> UpdateJobAsync(int id, JobEditModel model)
    {
        var job = await _db.Jobs.FindAsync(id) ?? throw new InvalidOperationException("Job not found.");
        var wasCompleted = job.CompletedDate.HasValue;
        job.Title = model.Title; job.Description = model.Description;
        job.Status = model.Status; job.Priority = model.Priority;
        job.TradeType = model.TradeType; job.JobType = model.JobType; job.SystemType = model.SystemType;
        job.ScheduledDate = model.ScheduledDate; job.EstimatedDuration = model.EstimatedDuration;
        job.EstimatedTotal = model.EstimatedTotal; job.ActualDuration = model.ActualDuration;
        job.ActualTotal = model.ActualTotal; job.PermitRequired = model.PermitRequired;
        job.PermitNumber = model.PermitNumber; job.Notes = model.Notes;
        job.CustomerId = model.CustomerId; job.CompanyId = model.CompanyId; job.SiteId = model.SiteId;
        job.AssignedEmployeeId = model.AssignedEmployeeId;
        job.UpdatedAt = DateTime.UtcNow;
        if (model.Status == JobStatus.Completed && !job.CompletedDate.HasValue)
            job.CompletedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (model.Status == JobStatus.Completed && !wasCompleted)
            await OnJobCompletedAsync(job);

        // Sync linked assets
        var existingLinks = await _db.JobAssets.Where(ja => ja.JobId == id).ToListAsync();
        _db.JobAssets.RemoveRange(existingLinks);
        foreach (var assetId in model.AssetIds)
        {
            _db.JobAssets.Add(new JobAsset { JobId = id, AssetId = assetId, CreatedAt = DateTime.UtcNow });
        }
        await _db.SaveChangesAsync();

        return job;
    }

    public async Task<bool> UpdateStatusAsync(int id, JobStatus status)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return false;
        var wasCompleted = job.CompletedDate.HasValue;
        job.Status = status; job.UpdatedAt = DateTime.UtcNow;
        if (status == JobStatus.Completed) job.CompletedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (status == JobStatus.Completed && !wasCompleted)
            await OnJobCompletedAsync(job);

        return true;
    }

    private async Task OnJobCompletedAsync(Job job)
    {
        var linkedAssets = await _db.JobAssets
            .Include(ja => ja.Asset)
            .Where(ja => ja.JobId == job.Id)
            .ToListAsync();

        if (linkedAssets.Count == 0) return;

        var now = DateTime.Now;
        var recordCount = await _db.ServiceHistoryRecords.CountAsync() + 1;

        foreach (var ja in linkedAssets)
        {
            // Auto-create a ServiceHistoryRecord for each linked asset
            var alreadyLinked = await _db.ServiceHistoryRecords
                .AnyAsync(sh => sh.JobId == job.Id && sh.AssetId == ja.AssetId);
            if (!alreadyLinked)
            {
                var serviceType = (ja.Role?.ToLower()) switch
                {
                    "installed" => ServiceHistoryType.NonWarrantyRepair,
                    "replaced" => ServiceHistoryType.NonWarrantyRepair,
                    "inspected" => ServiceHistoryType.PreventiveMaintenance,
                    "serviced" => ServiceHistoryType.PreventiveMaintenance,
                    _ => ServiceHistoryType.NonWarrantyRepair
                };

                _db.ServiceHistoryRecords.Add(new ServiceHistoryRecord
                {
                    RecordNumber = $"SH-{recordCount:D5}",
                    Type = serviceType,
                    Status = ServiceHistoryStatus.Resolved,
                    ServiceDate = now,
                    Description = $"Auto-generated from completed job {job.JobNumber}: {job.Title}",
                    ResolutionNotes = $"Asset role: {ja.Role ?? "General"}. {job.Notes}",
                    Cost = job.ActualTotal,
                    CustomerId = job.CustomerId,
                    SiteId = job.SiteId,
                    AssetId = ja.AssetId,
                    JobId = job.Id,
                    TechId = job.AssignedEmployeeId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                recordCount++;
            }

            // Auto-update Asset.LastServiceDate
            if (ja.Asset is not null)
            {
                ja.Asset.LastServiceDate = now;
                ja.Asset.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync();

        // Auto-create a draft Invoice from the completed job (pipeline automation)
        await CreateInvoiceFromJobAsync(job);
    }

    /// <summary>
    /// Auto-creates a draft Invoice from a completed Job, pulling in estimate line items if available,
    /// otherwise creating a single line from the job total.
    /// </summary>
    private async Task CreateInvoiceFromJobAsync(Job job)
    {
        // Skip if job already has an invoice
        if (job.InvoiceId.HasValue) return;
        var existingInv = await _db.Invoices.AnyAsync(i => i.JobId == job.Id && !i.IsArchived);
        if (existingInv) return;

        var invCount = await _db.Invoices.CountAsync() + 1;
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{invCount:D5}",
            Status = InvoiceStatus.Draft,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            PaymentTerms = "Net 30",
            Notes = $"Auto-generated from completed job {job.JobNumber}",
            CustomerId = job.CustomerId,
            CompanyId = job.CompanyId,
            SiteId = job.SiteId,
            JobId = job.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // If the job has a linked estimate with line items, copy those to the invoice
        if (job.EstimateId.HasValue)
        {
            var estimateLines = await _db.EstimateLines
                .Where(l => l.EstimateId == job.EstimateId.Value)
                .OrderBy(l => l.SortOrder)
                .ToListAsync();

            if (estimateLines.Count > 0)
            {
                invoice.Subtotal = estimateLines.Sum(l => l.LineTotal);
                invoice.Total = invoice.Subtotal;
                invoice.BalanceDue = invoice.Total;
                _db.Invoices.Add(invoice);
                await _db.SaveChangesAsync();

                int order = 0;
                foreach (var el in estimateLines)
                {
                    _db.InvoiceLines.Add(new InvoiceLine
                    {
                        InvoiceId = invoice.Id,
                        ProductId = el.ProductId,
                        AssetId = el.AssetId,
                        Description = el.Description,
                        LineType = el.LineType,
                        Unit = el.Unit,
                        Quantity = el.Quantity,
                        UnitPrice = el.UnitPrice,
                        LineTotal = el.LineTotal,
                        SortOrder = order++
                    });
                }
                await _db.SaveChangesAsync();

                job.InvoiceId = invoice.Id;
                await _db.SaveChangesAsync();
                return;
            }
        }

        // Fallback: single line from job total
        var total = job.ActualTotal ?? job.EstimatedTotal ?? 0;
        invoice.Subtotal = total;
        invoice.Total = total;
        invoice.BalanceDue = total;
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        if (total > 0)
        {
            _db.InvoiceLines.Add(new InvoiceLine
            {
                InvoiceId = invoice.Id,
                Description = $"{job.Title} - {job.JobType ?? "Service"}",
                LineType = "Labor",
                Quantity = 1,
                UnitPrice = total,
                LineTotal = total,
                SortOrder = 0
            });
            await _db.SaveChangesAsync();
        }

        job.InvoiceId = invoice.Id;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ArchiveJobAsync(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return false;
        job.IsArchived = true; job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreJobAsync(int id)
    {
        var j = await _db.Jobs.FindAsync(id);
        if (j is null) return false;
        j.IsArchived = false; j.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteJobPermanentlyAsync(int id)
    {
        var j = await _db.Jobs.FindAsync(id);
        if (j is null) return false;
        _db.Jobs.Remove(j);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveJobsAsync(List<int> ids)
    {
        var items = await _db.Jobs.Where(j => ids.Contains(j.Id) && !j.IsArchived).ToListAsync();
        foreach (var j in items) { j.IsArchived = true; j.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkRestoreJobsAsync(List<int> ids)
    {
        var items = await _db.Jobs.Where(j => ids.Contains(j.Id) && j.IsArchived).ToListAsync();
        foreach (var j in items) { j.IsArchived = false; j.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkDeleteJobsPermanentlyAsync(List<int> ids)
    {
        var items = await _db.Jobs.Where(j => ids.Contains(j.Id)).ToListAsync();
        _db.Jobs.RemoveRange(items);
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<List<EmployeeOption>> GetTechniciansAsync()
    {
        return await _db.Employees
            .Where(e => !e.IsArchived && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeOption { Id = e.Id, Name = e.Name, Territory = e.Territory, HourlyRate = e.HourlyRate })
            .ToListAsync();
    }

    public async Task<List<JobOption>> GetJobOptionsAsync(int? customerId = null, int? siteId = null)
    {
        var query = _db.Jobs.Where(j => !j.IsArchived);
        if (siteId.HasValue)
            query = query.Where(j => j.SiteId == siteId.Value);
        else if (customerId.HasValue)
            query = query.Where(j => j.CustomerId == customerId.Value);
        return await query
            .OrderByDescending(j => j.ScheduledDate)
            .Select(j => new JobOption { Id = j.Id, JobNumber = j.JobNumber, Title = j.Title })
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
            FlatRateAmount = flatRate,
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
