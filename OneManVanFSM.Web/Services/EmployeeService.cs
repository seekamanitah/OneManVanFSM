using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;
    public EmployeeService(AppDbContext db) => _db = db;

    public async Task<List<EmployeeListItem>> GetEmployeesAsync(EmployeeFilter? filter = null)
    {
        var showArchived = filter?.ShowArchived ?? false;
        var query = _db.Employees.Where(e => e.IsArchived == showArchived).AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(term) ||
                    (e.Email != null && e.Email.ToLower().Contains(term)) ||
                    (e.Territory != null && e.Territory.ToLower().Contains(term)));
            }
            if (filter.Role.HasValue) query = query.Where(e => e.Role == filter.Role.Value);
            if (filter.Status.HasValue) query = query.Where(e => e.Status == filter.Status.Value);
            if (!string.IsNullOrWhiteSpace(filter.Territory))
                query = query.Where(e => e.Territory == filter.Territory);

            query = filter.SortBy?.ToLower() switch
            {
                "role" => filter.SortDescending ? query.OrderByDescending(e => e.Role) : query.OrderBy(e => e.Role),
                "status" => filter.SortDescending ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
                "rate" => filter.SortDescending ? query.OrderByDescending(e => e.HourlyRate) : query.OrderBy(e => e.HourlyRate),
                _ => filter.SortDescending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name)
            };
        }
        else query = query.OrderBy(e => e.Name);

        return await query.Select(e => new EmployeeListItem
        {
            Id = e.Id, Name = e.Name, FirstName = e.FirstName, LastName = e.LastName,
            Role = e.Role, Status = e.Status,
            Phone = e.Phone, Email = e.Email, Territory = e.Territory,
            HourlyRate = e.HourlyRate,
            ActiveJobCount = e.AssignedJobs.Count(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
        }).ToListAsync();
    }

    public async Task<EmployeeDetail?> GetEmployeeAsync(int id)
    {
        var emp = await _db.Employees
            .Include(e => e.AssignedJobs)
            .Include(e => e.TimeEntries).ThenInclude(t => t.Job)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsArchived);
        if (emp is null) return null;

        return new EmployeeDetail
        {
            Id = emp.Id, Name = emp.Name, FirstName = emp.FirstName, LastName = emp.LastName,
            Role = emp.Role, Status = emp.Status,
            Phone = emp.Phone, Email = emp.Email, Address = emp.Address,
            HourlyRate = emp.HourlyRate, HireDate = emp.HireDate,
            Territory = emp.Territory, Certifications = emp.Certifications,
            LicenseNumber = emp.LicenseNumber, LicenseExpiry = emp.LicenseExpiry,
            VehicleAssigned = emp.VehicleAssigned,
            EmergencyContactName = emp.EmergencyContactName,
            EmergencyContactPhone = emp.EmergencyContactPhone,
            OvertimeRate = emp.OvertimeRate,
            Notes = emp.Notes, CreatedAt = emp.CreatedAt, UpdatedAt = emp.UpdatedAt,
            RecentJobs = emp.AssignedJobs.OrderByDescending(j => j.ScheduledDate).Take(10)
                .Select(j => new EmployeeJobSummary
                {
                    Id = j.Id, JobNumber = j.JobNumber, Title = j.Title,
                    Status = j.Status, ScheduledDate = j.ScheduledDate
                }).ToList(),
            RecentTimeEntries = emp.TimeEntries.OrderByDescending(t => t.StartTime).Take(20)
                .Select(t => new EmployeeTimeEntry
                {
                    Id = t.Id, StartTime = t.StartTime, EndTime = t.EndTime,
                    Hours = t.Hours, JobTitle = t.Job?.Title, IsBillable = t.IsBillable,
                    EntryType = t.EntryType, TimeCategory = t.TimeCategory
                }).ToList()
        };
    }

    public async Task<Employee> CreateEmployeeAsync(EmployeeEditModel model)
    {
        var emp = new Employee
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Name = string.IsNullOrWhiteSpace(model.LastName) ? model.FirstName : $"{model.FirstName} {model.LastName}".Trim(),
            Role = model.Role, Phone = model.Phone,
            Email = model.Email, Address = model.Address, HourlyRate = model.HourlyRate,
            HireDate = model.HireDate, Status = model.Status, Territory = model.Territory,
            Certifications = model.Certifications,
            LicenseNumber = model.LicenseNumber, LicenseExpiry = model.LicenseExpiry,
            VehicleAssigned = model.VehicleAssigned,
            EmergencyContactName = model.EmergencyContactName,
            EmergencyContactPhone = model.EmergencyContactPhone,
            OvertimeRate = model.OvertimeRate,
            Notes = model.Notes,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();
        return emp;
    }

    public async Task<Employee> UpdateEmployeeAsync(int id, EmployeeEditModel model)
    {
        var e = await _db.Employees.FindAsync(id) ?? throw new InvalidOperationException("Employee not found.");
        e.FirstName = model.FirstName;
        e.LastName = model.LastName;
        e.Name = string.IsNullOrWhiteSpace(model.LastName) ? model.FirstName : $"{model.FirstName} {model.LastName}".Trim();
        e.Role = model.Role; e.Phone = model.Phone;
        e.Email = model.Email; e.Address = model.Address; e.HourlyRate = model.HourlyRate;
        e.HireDate = model.HireDate; e.Status = model.Status; e.Territory = model.Territory;
        e.Certifications = model.Certifications;
        e.LicenseNumber = model.LicenseNumber; e.LicenseExpiry = model.LicenseExpiry;
        e.VehicleAssigned = model.VehicleAssigned;
        e.EmergencyContactName = model.EmergencyContactName;
        e.EmergencyContactPhone = model.EmergencyContactPhone;
        e.OvertimeRate = model.OvertimeRate;
        e.Notes = model.Notes;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return e;
    }

    public async Task<bool> ArchiveEmployeeAsync(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e is null) return false;
        e.IsArchived = true; e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreEmployeeAsync(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e is null) return false;
        e.IsArchived = false; e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteEmployeePermanentlyAsync(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e is null) return false;
        _db.Employees.Remove(e);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkArchiveEmployeesAsync(List<int> ids)
    {
        var items = await _db.Employees.Where(e => ids.Contains(e.Id) && !e.IsArchived).ToListAsync();
        foreach (var e in items) { e.IsArchived = true; e.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkRestoreEmployeesAsync(List<int> ids)
    {
        var items = await _db.Employees.Where(e => ids.Contains(e.Id) && e.IsArchived).ToListAsync();
        foreach (var e in items) { e.IsArchived = false; e.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> BulkDeleteEmployeesPermanentlyAsync(List<int> ids)
    {
        var items = await _db.Employees.Where(e => ids.Contains(e.Id)).ToListAsync();
        _db.Employees.RemoveRange(items);
        await _db.SaveChangesAsync();
        return items.Count;
    }

    // Time Clock
    public async Task<TimeEntry> ClockInAsync(int employeeId, int? jobId, string? notes = null)
    {
        // Check for existing active clock
        var active = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);
        if (active is not null)
            throw new InvalidOperationException("Employee is already clocked in. Clock out first.");

        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            JobId = jobId,
            StartTime = DateTime.UtcNow,
            IsBillable = jobId.HasValue,
            Notes = notes
        };
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<TimeEntry?> ClockOutAsync(int employeeId)
    {
        var active = await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);
        if (active is null) return null;

        active.EndTime = DateTime.UtcNow;
        var totalHours = (decimal)(active.EndTime.Value - active.StartTime).TotalHours;
        active.Hours = Math.Round(totalHours, 2);
        active.OvertimeHours = 0; // Will be calculated in pay summary
        await _db.SaveChangesAsync();
        return active;
    }

    public async Task<TimeEntry?> GetActiveClockAsync(int employeeId)
    {
        return await _db.TimeEntries
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.EndTime == null);
    }

    public async Task<List<TimesheetDay>> GetWeeklyTimesheetAsync(int employeeId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        var entries = await _db.TimeEntries
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= weekStart && t.StartTime < weekEnd)
            .OrderBy(t => t.StartTime)
            .ToListAsync();

        var days = new List<TimesheetDay>();
        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i).Date;
            var dayEntries = entries.Where(e => e.StartTime.Date == date).ToList();
            days.Add(new TimesheetDay
            {
                Date = date,
                TotalHours = dayEntries.Sum(e => e.Hours),
                OvertimeHours = dayEntries.Sum(e => e.OvertimeHours) ?? 0,
                Entries = dayEntries.Select(e => new EmployeeTimeEntry
                {
                    Id = e.Id, StartTime = e.StartTime, EndTime = e.EndTime,
                    Hours = e.Hours, JobTitle = e.Job?.Title, IsBillable = e.IsBillable,
                    EntryType = e.EntryType, TimeCategory = e.TimeCategory
                }).ToList()
            });
        }
        return days;
    }

    public async Task<PayPeriodSummary> GetPaySummaryAsync(int employeeId, DateTime periodStart, DateTime periodEnd)
    {
        var emp = await _db.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found.");

        var entries = await _db.TimeEntries
            .Include(t => t.Job)
            .Where(t => t.EmployeeId == employeeId && t.StartTime >= periodStart && t.StartTime < periodEnd)
            .ToListAsync();

        var jobEmployees = await _db.JobEmployees
            .Include(je => je.Job)
            .Where(je => je.EmployeeId == employeeId && je.PayType == JobEmployeePayType.FlatRate)
            .ToListAsync();

        var totalHours = entries.Sum(e => e.Hours);
        var regularHours = Math.Min(totalHours, 40);
        var overtimeHours = Math.Max(totalHours - 40, 0);

        var regularPay = regularHours * emp.HourlyRate;
        var overtimePay = overtimeHours * emp.HourlyRate * 1.5m;
        var flatRateTotal = jobEmployees.Sum(je => je.FlatRateAmount ?? 0);

        var jobHours = entries.GroupBy(e => e.JobId).Where(g => g.Key.HasValue).Select(g =>
        {
            var job = g.First().Job;
            return new PayPeriodJobEntry
            {
                JobId = g.Key!.Value,
                JobNumber = job?.JobNumber ?? "—",
                Title = job?.Title,
                Hours = g.Sum(e => e.Hours),
                PayType = "Hourly",
                Amount = g.Sum(e => e.Hours) * emp.HourlyRate
            };
        }).ToList();

        foreach (var je in jobEmployees.Where(je => je.FlatRateAmount > 0))
        {
            jobHours.Add(new PayPeriodJobEntry
            {
                JobId = je.JobId,
                JobNumber = je.Job?.JobNumber ?? "—",
                Title = je.Job?.Title,
                PayType = "Flat Rate",
                FlatRateAmount = je.FlatRateAmount,
                Amount = je.FlatRateAmount ?? 0
            });
        }

        return new PayPeriodSummary
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalRegularHours = regularHours,
            TotalOvertimeHours = overtimeHours,
            HourlyRate = emp.HourlyRate,
            RegularPay = regularPay,
            OvertimePay = overtimePay,
            FlatRateTotal = flatRateTotal,
            TotalPay = regularPay + overtimePay + flatRateTotal,
            Jobs = jobHours
        };
    }

    public async Task<TimeEntry> AddManualTimeEntryAsync(int employeeId, int? jobId, DateTime start, DateTime end, bool isBillable, string? notes, TimeEntryType entryType = TimeEntryType.Shift, string? timeCategory = null)
    {
        var hours = Math.Round((decimal)(end - start).TotalHours, 2);
        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            JobId = jobId,
            StartTime = start,
            EndTime = end,
            Hours = hours,
            IsBillable = isBillable,
            Notes = notes,
            EntryType = entryType,
            TimeCategory = timeCategory
        };
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<bool> UpdateTimeEntryAsync(int entryId, DateTime start, DateTime end, bool isBillable, string? notes, TimeEntryType entryType, string? timeCategory)
    {
        var entry = await _db.TimeEntries.FindAsync(entryId);
        if (entry is null) return false;
        entry.StartTime = start;
        entry.EndTime = end;
        entry.Hours = Math.Round((decimal)(end - start).TotalHours, 2);
        entry.IsBillable = isBillable;
        entry.Notes = notes;
        entry.EntryType = entryType;
        entry.TimeCategory = timeCategory;
        entry.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTimeEntryAsync(int entryId)
    {
        var entry = await _db.TimeEntries.FindAsync(entryId);
        if (entry is null) return false;
        _db.TimeEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
    }
}
