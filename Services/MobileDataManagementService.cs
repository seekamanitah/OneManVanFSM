using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileDataManagementService(AppDbContext db) : IMobileDataManagementService
{
    public async Task<MobileDataStats> GetDataStatsAsync()
    {
        var stats = new MobileDataStats
        {
            CustomerCount = await db.Customers.CountAsync(),
            JobCount = await db.Jobs.CountAsync(),
            InvoiceCount = await db.Invoices.CountAsync(),
            EstimateCount = await db.Estimates.CountAsync(),
            InventoryItemCount = await db.InventoryItems.CountAsync(),
            ProductCount = await db.Products.CountAsync(),
            AssetCount = await db.Assets.CountAsync(),
            EmployeeCount = await db.Employees.CountAsync(),
            SupplierCount = await db.Suppliers.CountAsync(),
            ExpenseCount = await db.Expenses.CountAsync(),
            TimeEntryCount = await db.TimeEntries.CountAsync(),
            NoteCount = await db.QuickNotes.CountAsync(),
            DocumentCount = await db.Documents.CountAsync(),
            AgreementCount = await db.ServiceAgreements.CountAsync(),
        };

        try
        {
            var dbPath = db.Database.GetConnectionString()?.Replace("Data Source=", "").Trim() ?? "";
            if (File.Exists(dbPath))
                stats.DatabaseSizeBytes = new FileInfo(dbPath).Length;
        }
        catch { /* Ignore file access errors */ }

        return stats;
    }

    public async Task<bool> HasDataAsync()
    {
        return await db.Customers.AnyAsync();
    }

    public async Task<bool> SeedDemoDataAsync()
    {
        if (await db.Customers.AnyAsync()) return false;

        db.ChangeTracker.Clear();
        var today = DateTime.Now.Date;

        // Employees
        var emp1 = new Employee { Name = "Mike Johnson", Role = EmployeeRole.Tech, Phone = "(555) 234-5678", Email = "mike@onemanvan.local", HourlyRate = 32m, OvertimeRate = 48m, Territory = "East County", HireDate = today.AddYears(-3), Status = EmployeeStatus.Active };
        var emp2 = new Employee { Name = "Carlos Rivera", Role = EmployeeRole.Tech, Phone = "(555) 345-6789", Email = "carlos@onemanvan.local", HourlyRate = 30m, OvertimeRate = 45m, Territory = "West County", HireDate = today.AddYears(-2), Status = EmployeeStatus.Active };
        db.Employees.AddRange(emp1, emp2);

        // Customers
        var cust1 = new Customer { Name = "Martha Chen", Type = CustomerType.Individual, PrimaryPhone = "(555) 111-2222", PrimaryEmail = "martha.chen@email.com", PreferredContactMethod = "Email", Address = "123 Oak St", City = "Springfield", State = "IL", Zip = "62704", SinceDate = today.AddYears(-2) };
        var cust2 = new Customer { Name = "Bob Reynolds", Type = CustomerType.Individual, PrimaryPhone = "(555) 222-3333", PrimaryEmail = "breynolds@email.com", PreferredContactMethod = "Phone", Address = "456 Maple Ave", City = "Springfield", State = "IL", Zip = "62701", SinceDate = today.AddYears(-1) };
        var cust3 = new Customer { Name = "Heritage Oaks HOA", Type = CustomerType.Company, PrimaryPhone = "(555) 333-4444", PrimaryEmail = "board@heritageoaks.org", PreferredContactMethod = "Email", Address = "100 Heritage Blvd", City = "Springfield", State = "IL", Zip = "62711", SinceDate = today.AddYears(-3) };
        var cust4 = new Customer { Name = "Linda Parker", Type = CustomerType.Landlord, PrimaryPhone = "(555) 555-6666", PrimaryEmail = "lparker@rentals.com", PreferredContactMethod = "Text", Address = "200 Pine St", City = "Springfield", State = "IL", Zip = "62703" };
        db.Customers.AddRange(cust1, cust2, cust3, cust4);

        // Sites
        var site1 = new Site { Name = "Chen Residence", Address = "123 Oak St", City = "Springfield", State = "IL", Zip = "62704", PropertyType = PropertyType.Residential, SqFt = 2200, Customer = cust1, HeatingFuelSource = "Natural Gas" };
        var site2 = new Site { Name = "Reynolds Home", Address = "456 Maple Ave", City = "Springfield", State = "IL", Zip = "62701", PropertyType = PropertyType.Residential, SqFt = 1800, Customer = cust2, HeatingFuelSource = "Natural Gas" };
        var site3 = new Site { Name = "Heritage Oaks Clubhouse", Address = "100 Heritage Blvd", City = "Springfield", State = "IL", Zip = "62711", PropertyType = PropertyType.Commercial, SqFt = 5000, Customer = cust3 };
        db.Sites.AddRange(site1, site2, site3);

        // Products
        var prod1 = new Product { Name = "14\" Flex Duct (25ft)", Category = "Ductwork", Cost = 45m, Price = 72m, MarkupPercent = 60m, Unit = "Roll" };
        var prod2 = new Product { Name = "R-410A Refrigerant (25lb)", Category = "Refrigerant", Cost = 125m, Price = 200m, MarkupPercent = 60m, Unit = "Tank" };
        var prod3 = new Product { Name = "Honeywell T6 Pro Thermostat", Category = "Controls", Cost = 85m, Price = 145m, MarkupPercent = 70m, Unit = "Each" };
        var prod4 = new Product { Name = "1\" Pleated Air Filter (6pk)", Category = "Filters", Cost = 18m, Price = 35m, MarkupPercent = 94m, Unit = "Pack" };
        db.Products.AddRange(prod1, prod2, prod3, prod4);

        // Inventory
        var inv1 = new InventoryItem { Name = "14\" Flex Duct", SKU = "FD-14-25", Quantity = 8, MinThreshold = 3, Cost = 45m, Price = 72m, Unit = "Roll", Location = InventoryLocation.Warehouse };
        var inv2 = new InventoryItem { Name = "R-410A Refrigerant", SKU = "REF-410A", Quantity = 4, MinThreshold = 2, Cost = 125m, Price = 200m, Unit = "Tank", Location = InventoryLocation.Warehouse };
        var inv3 = new InventoryItem { Name = "Air Filter 16x25x1", SKU = "FLT-16251", Quantity = 24, MinThreshold = 6, Cost = 3m, Price = 8m, Unit = "Each", Location = InventoryLocation.Truck };
        db.InventoryItems.AddRange(inv1, inv2, inv3);

        // Jobs
        var job1 = new Job { JobNumber = "JOB-0001", Title = "AC Not Cooling", Description = "Customer reports AC running but not cooling. Check refrigerant and compressor.", Status = JobStatus.Scheduled, Priority = JobPriority.High, ScheduledDate = today.AddDays(1), ScheduledTime = new TimeSpan(9, 0, 0), EstimatedDuration = 2m, Customer = cust1, Site = site1, AssignedEmployee = emp1 };
        var job2 = new Job { JobNumber = "JOB-0002", Title = "Annual Furnace Tune-Up", Description = "Routine annual maintenance — clean burners, check heat exchanger, replace filter.", Status = JobStatus.Scheduled, Priority = JobPriority.Standard, ScheduledDate = today.AddDays(2), ScheduledTime = new TimeSpan(10, 0, 0), EstimatedDuration = 1.5m, Customer = cust2, Site = site2, AssignedEmployee = emp1 };
        var job3 = new Job { JobNumber = "JOB-0003", Title = "RTU Maintenance", Description = "Quarterly rooftop unit maintenance for clubhouse.", Status = JobStatus.Scheduled, Priority = JobPriority.Standard, ScheduledDate = today.AddDays(3), ScheduledTime = new TimeSpan(8, 0, 0), EstimatedDuration = 3m, Customer = cust3, Site = site3, AssignedEmployee = emp2 };
        var job4 = new Job { JobNumber = "JOB-0004", Title = "Thermostat Replacement", Description = "Replace old mercury thermostat with smart thermostat.", Status = JobStatus.Completed, Priority = JobPriority.Low, ScheduledDate = today.AddDays(-5), CompletedDate = today.AddDays(-5), Customer = cust1, Site = site1, AssignedEmployee = emp1 };
        db.Jobs.AddRange(job1, job2, job3, job4);

        // Suppliers
        var sup1 = new Supplier { Name = "HVAC Supply Co", ContactName = "Tom Harris", Phone = "(555) 999-1111", Email = "orders@hvacsupply.com", PaymentTerms = "Net 30", IsActive = true };
        var sup2 = new Supplier { Name = "FilterBuy", ContactName = "Support", Phone = "(800) 555-0199", Email = "wholesale@filterbuy.com", Website = "https://filterbuy.com", PaymentTerms = "Due on Receipt", IsActive = true };
        db.Suppliers.AddRange(sup1, sup2);

        await db.SaveChangesAsync();
        return true;
    }

    public async Task PurgeDatabaseAsync()
    {
        // Delete all data in dependency order
        db.ChangeTracker.Clear();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AssetServiceLogs\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"ServiceAgreementAssets\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"JobAssets\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"JobEmployees\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AssetLinks\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"ClaimActions\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"ServiceHistoryRecords\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"MaterialListItems\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"MaterialLists\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"InvoiceLines\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"EstimateLines\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"ExpenseLines\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Payments\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Invoices\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Estimates\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Expenses\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"TimeEntries\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"QuickNotes\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Documents\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"CalendarEvents\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"ServiceAgreements\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Assets\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Jobs\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Sites\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"InventoryItems\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Products\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Suppliers\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Customers\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Companies\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Employees\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"TemplateVersions\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"Templates\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"DropdownOptions\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"ItemAssociations\"");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AuditLogs\"");

        db.ChangeTracker.Clear();
    }

    public async Task<string> BackupDatabaseAsync()
    {
        var dbPath = db.Database.GetConnectionString()?.Replace("Data Source=", "").Trim() ?? "";
        if (!File.Exists(dbPath))
            throw new InvalidOperationException("Database file not found.");

        var backupDir = Path.Combine(FileSystem.AppDataDirectory, "Backups");
        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(backupDir, $"onemanvan_backup_{timestamp}.db");

        // Force a WAL checkpoint before copying
        await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL)");

        File.Copy(dbPath, backupPath, overwrite: true);
        return backupPath;
    }

    public async Task<bool> RestoreDatabaseAsync(string backupPath)
    {
        if (!File.Exists(backupPath))
            return false;

        var dbPath = db.Database.GetConnectionString()?.Replace("Data Source=", "").Trim() ?? "";
        if (string.IsNullOrEmpty(dbPath))
            return false;

        // Close connections
        await db.Database.CloseConnectionAsync();
        db.ChangeTracker.Clear();

        File.Copy(backupPath, dbPath, overwrite: true);

        // Reopen
        await db.Database.OpenConnectionAsync();
        return true;
    }

    public Task<List<string>> GetAvailableBackupsAsync()
    {
        var backupDir = Path.Combine(FileSystem.AppDataDirectory, "Backups");
        if (!Directory.Exists(backupDir))
            return Task.FromResult(new List<string>());

        var backups = Directory.GetFiles(backupDir, "*.db")
            .OrderByDescending(f => f)
            .ToList();

        return Task.FromResult(backups);
    }

    public Task<bool> DeleteBackupAsync(string backupPath)
    {
        if (!File.Exists(backupPath))
            return Task.FromResult(false);

        try
        {
            File.Delete(backupPath);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<int> CleanupOldBackupsAsync(int maxCount, int retentionDays)
    {
        var backupDir = Path.Combine(FileSystem.AppDataDirectory, "Backups");
        if (!Directory.Exists(backupDir))
            return Task.FromResult(0);

        var backupFiles = Directory.GetFiles(backupDir, "*.db")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var deleted = 0;

        for (int i = 0; i < backupFiles.Count; i++)
        {
            var file = backupFiles[i];
            if (i > 0 && (i >= maxCount || file.CreationTimeUtc < cutoffDate))
            {
                try { file.Delete(); deleted++; } catch { }
            }
        }

        return Task.FromResult(deleted);
    }
}
