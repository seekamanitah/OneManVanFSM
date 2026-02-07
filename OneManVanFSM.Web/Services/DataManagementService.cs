using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;

namespace OneManVanFSM.Web.Services;

public class DataManagementService : IDataManagementService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public DataManagementService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private static readonly Dictionary<string, Func<AppDbContext, IQueryable<object>>> TableMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customers"] = db => db.Customers,
        ["Companies"] = db => db.Companies,
        ["Sites"] = db => db.Sites,
        ["Assets"] = db => db.Assets,
        ["Products"] = db => db.Products,
        ["InventoryItems"] = db => db.InventoryItems,
        ["Employees"] = db => db.Employees,
        ["Jobs"] = db => db.Jobs,
        ["Estimates"] = db => db.Estimates,
        ["Invoices"] = db => db.Invoices,
        ["InvoiceLines"] = db => db.InvoiceLines,
        ["EstimateLines"] = db => db.EstimateLines,
        ["Payments"] = db => db.Payments,
        ["Expenses"] = db => db.Expenses,
        ["TimeEntries"] = db => db.TimeEntries,
        ["ServiceAgreements"] = db => db.ServiceAgreements,
        ["QuickNotes"] = db => db.QuickNotes,
        ["Documents"] = db => db.Documents,
        ["MaterialLists"] = db => db.MaterialLists,
        ["MaterialListItems"] = db => db.MaterialListItems,
        ["CalendarEvents"] = db => db.CalendarEvents,
        ["Templates"] = db => db.Templates,
        ["JobEmployees"] = db => db.JobEmployees,
        ["JobAssets"] = db => db.JobAssets,
        ["ServiceAgreementAssets"] = db => db.ServiceAgreementAssets,
        ["AssetServiceLogs"] = db => db.AssetServiceLogs,
        ["Suppliers"] = db => db.Suppliers,
        ["DropdownOptions"] = db => db.DropdownOptions,
    };

    public Task<List<string>> GetExportableTablesAsync()
        => Task.FromResult(TableMap.Keys.OrderBy(k => k).ToList());

    public async Task<byte[]> ExportAllCsvAsync()
    {
        var sb = new StringBuilder();
        foreach (var (tableName, queryFunc) in TableMap.OrderBy(t => t.Key))
        {
            sb.AppendLine($"### TABLE: {tableName} ###");
            var rows = await queryFunc(_db).ToListAsync();
            AppendCsvTable(sb, rows, tableName);
            sb.AppendLine();
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportTableCsvAsync(string tableName)
    {
        if (!TableMap.TryGetValue(tableName, out var queryFunc))
            throw new InvalidOperationException($"Table '{tableName}' is not exportable.");

        var rows = await queryFunc(_db).ToListAsync();
        var sb = new StringBuilder();
        AppendCsvTable(sb, rows, tableName);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void AppendCsvTable(StringBuilder sb, List<object> rows, string tableName)
    {
        if (rows.Count == 0)
        {
            sb.AppendLine("(empty)");
            return;
        }

        var type = rows[0].GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimpleType(p.PropertyType))
            .ToArray();

        // Header
        sb.AppendLine(string.Join(",", props.Select(p => EscapeCsv(p.Name))));

        // Rows
        foreach (var row in rows)
        {
            var values = props.Select(p =>
            {
                var val = p.GetValue(row);
                return EscapeCsv(val?.ToString() ?? "");
            });
            sb.AppendLine(string.Join(",", values));
        }
    }

    private static bool IsSimpleType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t == typeof(string) || t == typeof(decimal) ||
               t == typeof(DateTime) || t == typeof(DateOnly) || t == typeof(TimeSpan) ||
               t == typeof(Guid) || t.IsEnum;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    public async Task<ImportResult> ImportCsvAsync(Stream csvStream, string fileName)
    {
        var result = new ImportResult();
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();

        // Detect if this is a full export (multi-table) or single table
        if (content.Contains("### TABLE:"))
        {
            // Multi-table import
            var sections = content.Split("### TABLE:", StringSplitOptions.RemoveEmptyEntries);
            foreach (var section in sections)
            {
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2) continue;

                var tableHeader = lines[0].Trim().TrimEnd('#').Trim();
                var csvLines = lines.Skip(1).Where(l => l.Trim() != "(empty)" && !string.IsNullOrWhiteSpace(l)).ToArray();
                if (csvLines.Length < 2) continue;

                var imported = await ImportTableFromCsv(tableHeader, csvLines, result);
                if (imported > 0)
                {
                    result.Tables.Add(tableHeader);
                    result.RecordsImported += imported;
                }
            }
        }
        else
        {
            // Single table import - detect table from filename or header
            var tableName = DetectTableName(fileName, content);
            if (tableName is null)
            {
                result.Errors.Add("Could not determine target table. Name your file like 'Customers.csv' or use a full export format.");
                return result;
            }

            var csvLines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (csvLines.Length < 2)
            {
                result.Errors.Add("CSV file has no data rows.");
                return result;
            }

            var imported = await ImportTableFromCsv(tableName, csvLines, result);
            if (imported > 0)
            {
                result.Tables.Add(tableName);
                result.RecordsImported += imported;
            }
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    private string? DetectTableName(string fileName, string content)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (TableMap.ContainsKey(name)) return name;

        // Try matching header columns
        var firstLine = content.Split('\n').FirstOrDefault()?.Trim();
        if (firstLine is null) return null;

        foreach (var (tableName, queryFunc) in TableMap)
        {
            var sampleType = queryFunc(_db).ElementType;
            var props = sampleType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => IsSimpleType(p.PropertyType))
                .Select(p => p.Name.ToLower())
                .ToHashSet();

            var headers = ParseCsvLine(firstLine).Select(h => h.ToLower()).ToHashSet();
            var overlap = headers.Intersect(props).Count();
            if (overlap >= Math.Min(3, props.Count))
                return tableName;
        }
        return null;
    }

    private async Task<int> ImportTableFromCsv(string tableName, string[] csvLines, ImportResult result)
    {
        if (!TableMap.TryGetValue(tableName, out var queryFunc))
        {
            result.Warnings.Add($"Skipping unknown table: {tableName}");
            return 0;
        }

        var entityType = queryFunc(_db).ElementType;
        var props = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimpleType(p.PropertyType) && p.CanWrite)
            .ToDictionary(p => p.Name.ToLower(), p => p);

        var headers = ParseCsvLine(csvLines[0]).Select(h => h.Trim()).ToArray();
        int count = 0;

        for (int i = 1; i < csvLines.Length; i++)
        {
            try
            {
                var values = ParseCsvLine(csvLines[i]);
                if (values.Length == 0) continue;

                var entity = Activator.CreateInstance(entityType)!;
                for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
                {
                    var headerKey = headers[j].ToLower();
                    if (headerKey == "id") continue; // Skip ID column to let DB auto-assign
                    if (!props.TryGetValue(headerKey, out var prop)) continue;

                    var val = values[j].Trim();
                    if (string.IsNullOrEmpty(val)) continue;

                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        object? converted;
                        if (targetType.IsEnum)
                            converted = Enum.Parse(targetType, val, ignoreCase: true);
                        else if (targetType == typeof(DateTime))
                            converted = DateTime.Parse(val, CultureInfo.InvariantCulture);
                        else if (targetType == typeof(TimeSpan))
                            converted = TimeSpan.Parse(val, CultureInfo.InvariantCulture);
                        else
                            converted = Convert.ChangeType(val, targetType, CultureInfo.InvariantCulture);

                        prop.SetValue(entity, converted);
                    }
                    catch
                    {
                        // Skip unparseable values
                    }
                }

                _db.Add(entity);
                count++;
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Row {i + 1} in {tableName}: {ex.Message}");
            }
        }

        if (count > 0)
            await _db.SaveChangesAsync();

        return count;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else if (c == '\r')
            {
                continue;
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    public async Task<byte[]> BackupDatabaseAsync()
    {
        var connStr = _config.GetConnectionString("DefaultConnection") ?? "Data Source=OneManVanFSM.db";
        var dbPath = connStr.Replace("Data Source=", "").Trim();

        if (!File.Exists(dbPath))
            throw new InvalidOperationException("Database file not found.");

        // Flush pending changes
        await _db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");

        return await File.ReadAllBytesAsync(dbPath);
    }

    public async Task RestoreDatabaseAsync(Stream backupStream)
    {
        var connStr = _config.GetConnectionString("DefaultConnection") ?? "Data Source=OneManVanFSM.db";
        var dbPath = connStr.Replace("Data Source=", "").Trim();

        // Create backup of current DB first
        var backupPath = $"{dbPath}.bak.{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (File.Exists(dbPath))
            File.Copy(dbPath, backupPath, true);

        using var fs = new FileStream(dbPath, FileMode.Create, FileAccess.Write);
        await backupStream.CopyToAsync(fs);
    }

    public async Task PurgeDatabaseAsync()
    {
        var connStr = _config.GetConnectionString("DefaultConnection") ?? "Data Source=OneManVanFSM.db";
        var dbPath = connStr.Replace("Data Source=", "").Trim();

        // Create backup before purge
        if (File.Exists(dbPath))
        {
            var backupPath = $"{dbPath}.pre-purge.{DateTime.UtcNow:yyyyMMddHHmmss}";
            await _db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
            File.Copy(dbPath, backupPath, true);
        }

        // Disable FK constraints so tables can be deleted in any order
        await _db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");

        try
        {
            // Delete child/junction tables first, then parent tables
            string[] deleteOrder =
            [
                // Junction / child tables (no dependents)
                "JobAssets", "JobEmployees", "ServiceAgreementAssets", "AssetServiceLogs",
                "InvoiceLines", "EstimateLines", "MaterialListItems", "TemplateVersions", "ClaimActions",
                // Leaf tables
                "Payments", "Expenses", "TimeEntries", "CalendarEvents", "QuickNotes", "Documents",
                "DropdownOptions", "Suppliers",
                // Mid-level tables
                "Invoices", "MaterialLists", "Templates",
                // Core tables
                "Jobs", "Estimates", "ServiceAgreements",
                "Assets", "InventoryItems", "Products",
                "Sites", "Employees", "Companies", "Customers",
                // System tables
                "Users", "AuditLogs", "ServiceHistoryRecords",
            ];

            foreach (var tableName in deleteOrder)
            {
                try
                {
                    await _db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{tableName}\"");
                }
                catch
                {
                    // Table may not exist if schema evolved — skip silently
                }
            }

            // Reset SQLite auto-increment counters
            try
            {
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence");
            }
            catch
            {
                // sqlite_sequence may not exist if no AUTOINCREMENT columns are used
            }
        }
        finally
        {
            // Always re-enable FK constraints
            await _db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }

        // Re-seed the default admin account
        _db.Users.Add(new OneManVanFSM.Shared.Models.AppUser
        {
            Username = "admin",
            Email = "admin@onemanvan.local",
            PasswordHash = AuthService.HashPassword("admin123"),
            Role = OneManVanFSM.Shared.Models.UserRole.Owner,
            IsActive = true,
        });
        await _db.SaveChangesAsync();
    }
}
