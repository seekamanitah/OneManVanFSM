using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;

namespace OneManVanFSM.Web.Services;

public class DataManagementService : IDataManagementService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public DataManagementService(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
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

    public async Task<byte[]> ExportAllXlsxAsync()
    {
        using var workbook = new XLWorkbook();

        foreach (var (tableName, queryFunc) in TableMap.OrderBy(t => t.Key))
        {
            var query = queryFunc(_db);
            var entityType = query.ElementType;
            var rows = await query.ToListAsync();
            AddWorksheet(workbook, tableName, rows, entityType);
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportTableXlsxAsync(string tableName)
    {
        if (!TableMap.TryGetValue(tableName, out var queryFunc))
            throw new InvalidOperationException($"Table '{tableName}' is not exportable.");

        var query = queryFunc(_db);
        var entityType = query.ElementType;
        var rows = await query.ToListAsync();

        using var workbook = new XLWorkbook();
        AddWorksheet(workbook, tableName, rows, entityType);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void AddWorksheet(XLWorkbook workbook, string sheetName, List<object> rows, Type entityType)
    {
        // Excel sheet names max 31 chars, no special chars
        var safeName = sheetName.Length > 31 ? sheetName[..31] : sheetName;
        var ws = workbook.Worksheets.Add(safeName);

        // Resolve properties from the entity type (works even with zero rows)
        var props = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimpleType(p.PropertyType))
            .ToArray();

        // Header row — always written, even when empty
        for (int col = 0; col < props.Length; col++)
        {
            var cell = ws.Cell(1, col + 1);
            cell.Value = props[col].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        if (rows.Count > 0)
        {
            for (int row = 0; row < rows.Count; row++)
            {
                for (int col = 0; col < props.Length; col++)
                {
                    var val = props[col].GetValue(rows[row]);
                    var cell = ws.Cell(row + 2, col + 1);

                    if (val is null)
                        continue;

                    switch (val)
                    {
                        case DateTime dt:
                            cell.Value = dt;
                            cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                            break;
                        case DateOnly d:
                            cell.Value = d.ToDateTime(TimeOnly.MinValue);
                            cell.Style.DateFormat.Format = "yyyy-MM-dd";
                            break;
                        case TimeSpan ts:
                            cell.Value = ts.ToString();
                            break;
                        case bool b:
                            cell.Value = b;
                            break;
                        case int i:
                            cell.Value = i;
                            break;
                        case long l:
                            cell.Value = l;
                            break;
                        case decimal dec:
                            cell.Value = dec;
                            cell.Style.NumberFormat.Format = "#,##0.00";
                            break;
                        case double dbl:
                            cell.Value = dbl;
                            break;
                        case float f:
                            cell.Value = (double)f;
                            break;
                        case Enum e:
                            cell.Value = e.ToString();
                            break;
                        default:
                            cell.Value = val.ToString();
                            break;
                    }
                }
            }
        }

        // Auto-filter on header row
        if (props.Length > 0)
            ws.RangeUsed()?.SetAutoFilter();

        // Auto-fit columns (cap at 50 to avoid absurdly wide columns)
        ws.Columns().AdjustToContents(1, Math.Max(rows.Count + 1, 2));
        foreach (var col in ws.ColumnsUsed())
        {
            if (col.Width > 50)
                col.Width = 50;
        }
    }

    private static bool IsSimpleType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t == typeof(string) || t == typeof(decimal) ||
               t == typeof(DateTime) || t == typeof(DateOnly) || t == typeof(TimeSpan) ||
               t == typeof(Guid) || t.IsEnum;
    }

    public async Task<ImportResult> ImportFileAsync(Stream fileStream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is ".xlsx" or ".xls")
            return await ImportXlsxAsync(fileStream, fileName);
        return await ImportCsvAsync(fileStream, fileName);
    }

    public async Task<ImportResult> ImportXlsxAsync(Stream xlsxStream, string fileName)
    {
        var result = new ImportResult();

        try
        {
            // Copy to MemoryStream so ClosedXML can seek
            using var ms = new MemoryStream();
            await xlsxStream.CopyToAsync(ms);
            ms.Position = 0;

            using var workbook = new XLWorkbook(ms);

            foreach (var ws in workbook.Worksheets)
            {
                var sheetName = ws.Name.Trim();
                if (!TableMap.ContainsKey(sheetName))
                {
                    // Try detecting table name from header row
                    var detectedName = DetectTableNameFromHeaders(ws);
                    if (detectedName is not null)
                        sheetName = detectedName;
                    else
                    {
                        // For single-sheet files try the filename
                        var fileBaseName = Path.GetFileNameWithoutExtension(fileName);
                        if (TableMap.ContainsKey(fileBaseName))
                            sheetName = fileBaseName;
                        else
                        {
                            result.Warnings.Add($"Skipping sheet '{ws.Name}': could not match to a known table.");
                            continue;
                        }
                    }
                }

                var imported = await ImportTableFromWorksheet(sheetName, ws, result);
                if (imported > 0)
                {
                    result.Tables.Add(sheetName);
                    result.RecordsImported += imported;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to read Excel file: {ex.Message}");
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    private string? DetectTableNameFromHeaders(IXLWorksheet ws)
    {
        var headerRow = ws.Row(1);
        if (headerRow.IsEmpty()) return null;

        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastCol == 0) return null;

        var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int col = 1; col <= lastCol; col++)
        {
            var val = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(val)) headers.Add(val);
        }

        foreach (var (tableName, queryFunc) in TableMap)
        {
            var sampleType = queryFunc(_db).ElementType;
            var props = sampleType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => IsSimpleType(p.PropertyType))
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var overlap = headers.Intersect(props, StringComparer.OrdinalIgnoreCase).Count();
            if (overlap >= Math.Min(3, props.Count))
                return tableName;
        }
        return null;
    }

    private async Task<int> ImportTableFromWorksheet(string tableName, IXLWorksheet ws, ImportResult result)
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

        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
        if (lastCol == 0 || lastRow < 2) return 0;

        // Read headers from row 1
        var headers = new string[lastCol];
        for (int col = 1; col <= lastCol; col++)
            headers[col - 1] = ws.Cell(1, col).GetString().Trim();

        int count = 0;
        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var entity = Activator.CreateInstance(entityType)!;
                bool hasAnyValue = false;

                for (int col = 0; col < headers.Length; col++)
                {
                    var headerKey = headers[col].ToLower();
                    if (headerKey == "id") continue;
                    if (!props.TryGetValue(headerKey, out var prop)) continue;

                    var cell = ws.Cell(row, col + 1);
                    if (cell.IsEmpty()) continue;

                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        object? converted = null;

                        if (targetType.IsEnum)
                        {
                            converted = Enum.Parse(targetType, cell.GetString().Trim(), ignoreCase: true);
                        }
                        else if (targetType == typeof(DateTime))
                        {
                            converted = cell.DataType == XLDataType.DateTime
                                ? cell.GetDateTime()
                                : DateTime.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
                        }
                        else if (targetType == typeof(bool))
                        {
                            converted = cell.DataType == XLDataType.Boolean
                                ? cell.GetBoolean()
                                : bool.Parse(cell.GetString().Trim());
                        }
                        else if (targetType == typeof(decimal))
                        {
                            converted = cell.DataType == XLDataType.Number
                                ? (decimal)cell.GetDouble()
                                : decimal.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
                        }
                        else if (targetType == typeof(int))
                        {
                            converted = cell.DataType == XLDataType.Number
                                ? (int)cell.GetDouble()
                                : int.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
                        }
                        else if (targetType == typeof(long))
                        {
                            converted = cell.DataType == XLDataType.Number
                                ? (long)cell.GetDouble()
                                : long.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
                        }
                        else if (targetType == typeof(double))
                        {
                            converted = cell.DataType == XLDataType.Number
                                ? cell.GetDouble()
                                : double.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
                        }
                        else if (targetType == typeof(float))
                        {
                            converted = cell.DataType == XLDataType.Number
                                ? (float)cell.GetDouble()
                                : float.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
                        }
                        else if (targetType == typeof(TimeSpan))
                        {
                            converted = TimeSpan.Parse(cell.GetString().Trim(), CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            var strVal = cell.GetString().Trim();
                            if (!string.IsNullOrEmpty(strVal))
                                converted = Convert.ChangeType(strVal, targetType, CultureInfo.InvariantCulture);
                        }

                        if (converted is not null)
                        {
                            prop.SetValue(entity, converted);
                            hasAnyValue = true;
                        }
                    }
                    catch
                    {
                        // Skip unparseable cell values
                    }
                }

                if (hasAnyValue)
                {
                    _db.Add(entity);
                    count++;
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Row {row} in {tableName}: {ex.Message}");
            }
        }

        if (count > 0)
            await _db.SaveChangesAsync();

        return count;
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

    // --- Duplicate-detection key fields per table ---
    // These are the fields used to detect if an imported row already exists in the database.
    // Multiple fields are checked with OR logic (any match = potential conflict).
    private static readonly Dictionary<string, string[]> DuplicateKeyFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customers"] = ["Name"],
        ["Companies"] = ["Name"],
        ["Sites"] = ["Name"],
        ["Assets"] = ["Name", "SerialNumber"],
        ["Products"] = ["Name", "ModelNumber", "PartNumber", "ProductNumber"],
        ["InventoryItems"] = ["Name", "SKU", "PartNumber"],
        ["Employees"] = ["Name", "Email"],
        ["Jobs"] = ["JobNumber"],
        ["Estimates"] = ["EstimateNumber"],
        ["Invoices"] = ["InvoiceNumber"],
        ["Payments"] = ["Id"],
        ["Expenses"] = ["Id"],
        ["TimeEntries"] = ["Id"],
        ["ServiceAgreements"] = ["AgreementNumber"],
        ["QuickNotes"] = ["Title"],
        ["Documents"] = ["Name"],
        ["MaterialLists"] = ["Name"],
        ["MaterialListItems"] = ["Id"],
        ["CalendarEvents"] = ["Title"],
        ["Templates"] = ["Name"],
        ["Suppliers"] = ["Name", "Email", "AccountNumber"],
        ["DropdownOptions"] = ["Category", "Value"],
        ["InvoiceLines"] = ["Id"],
        ["EstimateLines"] = ["Id"],
        ["JobEmployees"] = ["Id"],
        ["JobAssets"] = ["Id"],
        ["ServiceAgreementAssets"] = ["Id"],
        ["AssetServiceLogs"] = ["Id"],
    };

    public async Task<ImportPreview> PreviewImportAsync(Stream fileStream, string fileName)
    {
        var preview = new ImportPreview();

        // Copy stream to memory so we can seek
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        ms.Position = 0;

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is ".xlsx" or ".xls")
            await PreviewXlsx(ms, fileName, preview);
        else
            await PreviewCsv(ms, fileName, preview);

        return preview;
    }

    private async Task PreviewXlsx(MemoryStream ms, string fileName, ImportPreview preview)
    {
        try
        {
            using var workbook = new XLWorkbook(ms);
            foreach (var ws in workbook.Worksheets)
            {
                var sheetName = ws.Name.Trim();
                if (!TableMap.ContainsKey(sheetName))
                {
                    var detectedName = DetectTableNameFromHeaders(ws);
                    if (detectedName is not null)
                        sheetName = detectedName;
                    else
                    {
                        var fileBaseName = Path.GetFileNameWithoutExtension(fileName);
                        if (TableMap.ContainsKey(fileBaseName))
                            sheetName = fileBaseName;
                        else
                        {
                            preview.Warnings.Add($"Skipping sheet '{ws.Name}': could not match to a known table.");
                            continue;
                        }
                    }
                }
                await PreviewTableFromWorksheet(sheetName, ws, preview);
            }
        }
        catch (Exception ex)
        {
            preview.Errors.Add($"Failed to read Excel file: {ex.Message}");
        }
    }

    private async Task PreviewCsv(MemoryStream ms, string fileName, ImportPreview preview)
    {
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();

        if (content.Contains("### TABLE:"))
        {
            var sections = content.Split("### TABLE:", StringSplitOptions.RemoveEmptyEntries);
            foreach (var section in sections)
            {
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2) continue;
                var tableHeader = lines[0].Trim().TrimEnd('#').Trim();
                var csvLines = lines.Skip(1).Where(l => l.Trim() != "(empty)" && !string.IsNullOrWhiteSpace(l)).ToArray();
                if (csvLines.Length < 2) continue;
                await PreviewTableFromCsvLines(tableHeader, csvLines, preview);
            }
        }
        else
        {
            var tableName = DetectTableName(fileName, content);
            if (tableName is null)
            {
                preview.Errors.Add("Could not determine target table. Name your file like 'Customers.csv' or use a full export format.");
                return;
            }
            var csvLines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (csvLines.Length < 2) { preview.Errors.Add("CSV file has no data rows."); return; }
            await PreviewTableFromCsvLines(tableName, csvLines, preview);
        }
    }

    private async Task PreviewTableFromWorksheet(string tableName, IXLWorksheet ws, ImportPreview preview)
    {
        if (!TableMap.TryGetValue(tableName, out var queryFunc))
        {
            preview.Warnings.Add($"Skipping unknown table: {tableName}");
            return;
        }

        var entityType = queryFunc(_db).ElementType;
        var props = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimpleType(p.PropertyType))
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
        if (lastCol == 0 || lastRow < 2) return;

        var headers = new string[lastCol];
        for (int col = 1; col <= lastCol; col++)
            headers[col - 1] = ws.Cell(1, col).GetString().Trim();

        var table = new ImportPreviewTable { TableName = tableName, Headers = headers.Where(h => !string.IsNullOrEmpty(h)).ToArray() };

        // Load existing key values for duplicate detection
        var existingKeys = await LoadExistingKeyValues(tableName, queryFunc);

        for (int row = 2; row <= lastRow; row++)
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int col = 0; col < headers.Length; col++)
            {
                var h = headers[col];
                if (string.IsNullOrEmpty(h)) continue;
                var cell = ws.Cell(row, col + 1);
                values[h] = cell.IsEmpty() ? null : cell.GetString().Trim();
            }

            var previewRow = new ImportPreviewRow { RowNumber = row, Values = values };
            DetectConflict(tableName, values, existingKeys, previewRow);
            table.Rows.Add(previewRow);
        }

        if (table.Rows.Count > 0)
            preview.Tables.Add(table);
    }

    private async Task PreviewTableFromCsvLines(string tableName, string[] csvLines, ImportPreview preview)
    {
        if (!TableMap.TryGetValue(tableName, out var queryFunc))
        {
            preview.Warnings.Add($"Skipping unknown table: {tableName}");
            return;
        }

        var headers = ParseCsvLine(csvLines[0]).Select(h => h.Trim()).ToArray();
        var table = new ImportPreviewTable { TableName = tableName, Headers = headers.Where(h => !string.IsNullOrEmpty(h)).ToArray() };

        var existingKeys = await LoadExistingKeyValues(tableName, queryFunc);

        for (int i = 1; i < csvLines.Length; i++)
        {
            var cols = ParseCsvLine(csvLines[i]);
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < Math.Min(headers.Length, cols.Length); j++)
            {
                if (!string.IsNullOrEmpty(headers[j]))
                    values[headers[j]] = string.IsNullOrEmpty(cols[j]) ? null : cols[j].Trim();
            }
            var previewRow = new ImportPreviewRow { RowNumber = i + 1, Values = values };
            DetectConflict(tableName, values, existingKeys, previewRow);
            table.Rows.Add(previewRow);
        }

        if (table.Rows.Count > 0)
            preview.Tables.Add(table);
    }

    /// <summary>
    /// Loads existing key-field values from the database for duplicate detection.
    /// Returns a list of (Id, Dictionary of keyField->value) for each existing entity.
    /// </summary>
    private async Task<List<(int Id, Dictionary<string, string?> Keys)>> LoadExistingKeyValues(
        string tableName, Func<AppDbContext, IQueryable<object>> queryFunc)
    {
        var result = new List<(int, Dictionary<string, string?>)>();
        if (!DuplicateKeyFields.TryGetValue(tableName, out var keyFields)) return result;

        var entityType = queryFunc(_db).ElementType;
        var idProp = entityType.GetProperty("Id");
        var keyProps = keyFields
            .Select(k => (Name: k, Prop: entityType.GetProperty(k)))
            .Where(x => x.Prop is not null)
            .ToArray();

        if (idProp is null || keyProps.Length == 0) return result;

        var entities = await queryFunc(_db).ToListAsync();
        foreach (var entity in entities)
        {
            var id = (int)(idProp.GetValue(entity) ?? 0);
            var keys = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, prop) in keyProps)
            {
                var val = prop!.GetValue(entity);
                keys[name] = val?.ToString();
            }
            result.Add((id, keys));
        }
        return result;
    }

    private static void DetectConflict(string tableName, Dictionary<string, string?> importValues,
        List<(int Id, Dictionary<string, string?> Keys)> existingKeys, ImportPreviewRow row)
    {
        if (!DuplicateKeyFields.TryGetValue(tableName, out var keyFields)) return;

        foreach (var (existingId, existingKeyValues) in existingKeys)
        {
            var matchedFields = new List<string>();
            foreach (var keyField in keyFields)
            {
                if (keyField == "Id") continue; // Never match on Id for duplicate detection
                if (!importValues.TryGetValue(keyField, out var importVal)) continue;
                if (string.IsNullOrWhiteSpace(importVal)) continue;
                if (!existingKeyValues.TryGetValue(keyField, out var existingVal)) continue;
                if (string.IsNullOrWhiteSpace(existingVal)) continue;

                if (string.Equals(importVal, existingVal, StringComparison.OrdinalIgnoreCase))
                    matchedFields.Add(keyField);
            }

            if (matchedFields.Count > 0)
            {
                // Check if all non-null key fields match = exact duplicate, else partial
                var importKeyCount = keyFields.Count(k => k != "Id" && importValues.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v));
                row.ConflictType = matchedFields.Count >= importKeyCount
                    ? ImportConflictType.ExactDuplicate
                    : ImportConflictType.PartialMatch;
                row.ExistingEntityId = existingId;
                row.ConflictDetail = $"Matches existing record (ID {existingId}) on: {string.Join(", ", matchedFields)}";
                row.Action = ImportRowAction.Skip; // Default to skip for conflicts
                return; // First match is enough
            }
        }
    }

    public async Task<ImportResult> CommitImportAsync(ImportPreview preview)
    {
        var result = new ImportResult();

        foreach (var table in preview.Tables)
        {
            if (!TableMap.TryGetValue(table.TableName, out var queryFunc))
            {
                result.Warnings.Add($"Skipping unknown table: {table.TableName}");
                continue;
            }

            var entityType = queryFunc(_db).ElementType;
            var props = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => IsSimpleType(p.PropertyType) && p.CanWrite)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            var idProp = entityType.GetProperty("Id");

            int tableCount = 0;
            foreach (var row in table.Rows)
            {
                if (row.Action == ImportRowAction.Skip) continue;

                try
                {
                    if (row.Action == ImportRowAction.Overwrite && row.ExistingEntityId.HasValue && idProp is not null)
                    {
                        // Update existing entity
                        var existing = await _db.FindAsync(entityType, row.ExistingEntityId.Value);
                        if (existing is not null)
                        {
                            ApplyValues(existing, row.Values, props);
                            tableCount++;
                        }
                        else
                        {
                            result.Warnings.Add($"Row {row.RowNumber} in {table.TableName}: existing record ID {row.ExistingEntityId} not found, importing as new.");
                            var entity = Activator.CreateInstance(entityType)!;
                            ApplyValues(entity, row.Values, props);
                            _db.Add(entity);
                            tableCount++;
                        }
                    }
                    else // Import as new
                    {
                        var entity = Activator.CreateInstance(entityType)!;
                        ApplyValues(entity, row.Values, props);
                        _db.Add(entity);
                        tableCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Row {row.RowNumber} in {table.TableName}: {ex.Message}");
                }
            }

            if (tableCount > 0)
            {
                await _db.SaveChangesAsync();
                result.Tables.Add(table.TableName);
                result.RecordsImported += tableCount;
            }
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    private static void ApplyValues(object entity, Dictionary<string, string?> values, Dictionary<string, PropertyInfo> props)
    {
        foreach (var (header, val) in values)
        {
            if (string.Equals(header, "Id", StringComparison.OrdinalIgnoreCase)) continue;
            if (!props.TryGetValue(header, out var prop)) continue;
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
                else if (targetType == typeof(bool))
                    converted = bool.Parse(val);
                else if (targetType == typeof(decimal))
                    converted = decimal.Parse(val, CultureInfo.InvariantCulture);
                else if (targetType == typeof(int))
                    converted = int.Parse(val, CultureInfo.InvariantCulture);
                else if (targetType == typeof(long))
                    converted = long.Parse(val, CultureInfo.InvariantCulture);
                else if (targetType == typeof(double))
                    converted = double.Parse(val, CultureInfo.InvariantCulture);
                else if (targetType == typeof(float))
                    converted = float.Parse(val, CultureInfo.InvariantCulture);
                else
                    converted = Convert.ChangeType(val, targetType, CultureInfo.InvariantCulture);

                prop.SetValue(entity, converted);
            }
            catch
            {
                // Skip unparseable values
            }
        }
    }

    public async Task<byte[]> BackupDatabaseAsync()
    {
        var connStr = _config.GetConnectionString("DefaultConnection") ?? "Data Source=OneManVanFSM.db";
        var dbPath = connStr.Replace("Data Source=", "").Trim();

        if (!File.Exists(dbPath))
            throw new InvalidOperationException("Database file not found.");

        // Flush pending changes
        await _db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");

        // Create ZIP containing the database and company profile
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Add database file
            var dbEntry = archive.CreateEntry("OneManVanFSM.db", CompressionLevel.Optimal);
            await using (var entryStream = dbEntry.Open())
            {
                var dbBytes = await File.ReadAllBytesAsync(dbPath);
                await entryStream.WriteAsync(dbBytes);
            }

            // Add company profile if it exists
            var profilePath = Path.Combine(AppContext.BaseDirectory, "companyprofile.json");
            if (File.Exists(profilePath))
            {
                var profileEntry = archive.CreateEntry("companyprofile.json", CompressionLevel.Optimal);
                await using var entryStream = profileEntry.Open();
                var profileBytes = await File.ReadAllBytesAsync(profilePath);
                await entryStream.WriteAsync(profileBytes);
            }

            // Add document uploads if the directory exists
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            if (Directory.Exists(uploadsDir))
            {
                foreach (var filePath in Directory.EnumerateFiles(uploadsDir))
                {
                    var fileName = Path.GetFileName(filePath);
                    var uploadEntry = archive.CreateEntry($"uploads/{fileName}", CompressionLevel.Optimal);
                    await using var entryStream = uploadEntry.Open();
                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    await entryStream.WriteAsync(fileBytes);
                }
            }
        }

        return ms.ToArray();
    }

    public async Task RestoreDatabaseAsync(Stream backupStream)
    {
        var connStr = _config.GetConnectionString("DefaultConnection") ?? "Data Source=OneManVanFSM.db";
        var dbPath = connStr.Replace("Data Source=", "").Trim();

        // Read the uploaded stream into memory so we can inspect it
        using var memStream = new MemoryStream();
        await backupStream.CopyToAsync(memStream);
        memStream.Position = 0;

        // Close the current DbContext connection before file operations
        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Closed)
            await connection.CloseAsync();

        // Clear all SQLite connection pools so stale handles are released
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        // Create backup of current DB first
        var backupPath = $"{dbPath}.bak.{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (File.Exists(dbPath))
            File.Copy(dbPath, backupPath, true);

        // Delete WAL/SHM journal files from the old database to prevent corruption
        var walPath = dbPath + "-wal";
        var shmPath = dbPath + "-shm";
        if (File.Exists(walPath)) File.Delete(walPath);
        if (File.Exists(shmPath)) File.Delete(shmPath);

        // Detect if the upload is a ZIP archive (starts with PK signature 0x504B)
        var isZip = memStream.Length >= 4;
        if (isZip)
        {
            var header = new byte[4];
            memStream.Read(header, 0, 4);
            memStream.Position = 0;
            isZip = header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04;
        }

        if (isZip)
        {
            using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

            // Restore database file
            var dbEntry = archive.GetEntry("OneManVanFSM.db");
            if (dbEntry is not null)
            {
                await using var entryStream = dbEntry.Open();
                await using var fs = new FileStream(dbPath, FileMode.Create, FileAccess.Write);
                await entryStream.CopyToAsync(fs);
                await fs.FlushAsync();
            }

            // Restore company profile if present
            var profileEntry = archive.GetEntry("companyprofile.json");
            if (profileEntry is not null)
            {
                var profilePath = Path.Combine(AppContext.BaseDirectory, "companyprofile.json");
                await using var entryStream = profileEntry.Open();
                await using var fs = new FileStream(profilePath, FileMode.Create, FileAccess.Write);
                await entryStream.CopyToAsync(fs);
                await fs.FlushAsync();
            }

            // Restore document uploads if present in backup
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            var uploadEntries = archive.Entries
                .Where(e => e.FullName.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
                         && !string.IsNullOrEmpty(e.Name))
                .ToList();
            if (uploadEntries.Count > 0)
            {
                Directory.CreateDirectory(uploadsDir);
                foreach (var uploadEntry in uploadEntries)
                {
                    var destPath = Path.Combine(uploadsDir, uploadEntry.Name);
                    await using var entryStream = uploadEntry.Open();
                    await using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write);
                    await entryStream.CopyToAsync(fs);
                    await fs.FlushAsync();
                }
            }
        }
        else
        {
            // Legacy: raw .db file restore
            await using var fs = new FileStream(dbPath, FileMode.Create, FileAccess.Write);
            await memStream.CopyToAsync(fs);
            await fs.FlushAsync();
        }

        // Clear pools again so new connections open against the restored file
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        // Clear EF change tracker — the database file was swapped underneath this context.
        _db.ChangeTracker.Clear();

        // Non-destructively add any tables/columns the current EF model expects but the
        // older backup is missing, so pages referencing newer schema elements don't 500.
        DatabaseInitializer.MigrateSchemaPreservingData(_db);
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

        // Clear the EF change tracker so stale entities (from prior queries in this
        // Blazor circuit) don't conflict with the freshly-reset auto-increment IDs.
        _db.ChangeTracker.Clear();

        // Re-seed the default admin account using environment variables (matches Program.cs startup seed)
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "chris.eikel@bledsoe.net";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "!1235aSdf12sadf5!";

        var adminEmployee = new OneManVanFSM.Shared.Models.Employee
        {
            Name = "Admin",
            Role = OneManVanFSM.Shared.Models.EmployeeRole.Owner,
            Status = OneManVanFSM.Shared.Models.EmployeeStatus.Active,
            Email = adminEmail,
        };
        _db.Employees.Add(adminEmployee);

        _db.Users.Add(new OneManVanFSM.Shared.Models.AppUser
        {
            Username = "admin",
            Email = adminEmail,
            PasswordHash = AuthService.HashPassword(adminPassword),
            Role = OneManVanFSM.Shared.Models.UserRole.Owner,
            IsActive = true,
            MustChangePassword = true,
            Employee = adminEmployee,
        });
        await _db.SaveChangesAsync();
    }

    public async Task<bool> HasDataAsync()
    {
        // Only check for Customers (actual demo/business data).
        // The admin Employee created during purge should NOT count as "demo data loaded".
        return await _db.Customers.AnyAsync();
    }

    public async Task<bool> SeedDemoDataAsync()
    {
        try
        {
            // Clear stale tracked entities from prior operations in this Blazor circuit
            // to prevent identity-map conflicts when the DB assigns auto-increment IDs.
            _db.ChangeTracker.Clear();

            if (await _db.Customers.AnyAsync()) return false;

            var today = DateTime.Now.Date;

            // Employees
            var emp1 = new OneManVanFSM.Shared.Models.Employee { Name = "Mike Johnson", Role = OneManVanFSM.Shared.Models.EmployeeRole.Tech, Phone = "(555) 234-5678", Email = "mike@onemanvan.local", HourlyRate = 32m, OvertimeRate = 48m, Territory = "East County", HireDate = today.AddYears(-3), Certifications = "[\"EPA 608 Universal\",\"NATE HVAC\"]", Status = OneManVanFSM.Shared.Models.EmployeeStatus.Active, LicenseNumber = "EPA-608-U-44210", LicenseExpiry = today.AddYears(2), VehicleAssigned = "Van #1 — 2022 Ford Transit", EmergencyContactName = "Sarah Johnson", EmergencyContactPhone = "(555) 234-0001" };
            var emp2 = new OneManVanFSM.Shared.Models.Employee { Name = "Carlos Rivera", Role = OneManVanFSM.Shared.Models.EmployeeRole.Tech, Phone = "(555) 345-6789", Email = "carlos@onemanvan.local", HourlyRate = 30m, OvertimeRate = 45m, Territory = "West County", HireDate = today.AddYears(-2), Certifications = "[\"EPA 608 Type II\"]", Status = OneManVanFSM.Shared.Models.EmployeeStatus.Active, LicenseNumber = "EPA-608-II-55320", LicenseExpiry = today.AddMonths(14), VehicleAssigned = "Van #2 — 2021 Chevy Express", EmergencyContactName = "Maria Rivera", EmergencyContactPhone = "(555) 345-0002" };
            var emp3 = new OneManVanFSM.Shared.Models.Employee { Name = "Jake Miller", Role = OneManVanFSM.Shared.Models.EmployeeRole.Apprentice, Phone = "(555) 456-7890", Email = "jake@onemanvan.local", HourlyRate = 22m, Territory = "East County", HireDate = today.AddMonths(-6), Status = OneManVanFSM.Shared.Models.EmployeeStatus.Active, EmergencyContactName = "Tom Miller", EmergencyContactPhone = "(555) 456-0003" };
            _db.Employees.AddRange(emp1, emp2, emp3);

            // Customers
            var cust1 = new OneManVanFSM.Shared.Models.Customer { Name = "Martha Chen", Type = OneManVanFSM.Shared.Models.CustomerType.Individual, PrimaryPhone = "(555) 111-2222", SecondaryPhone = "(555) 111-2223", PrimaryEmail = "martha.chen@email.com", PreferredContactMethod = "Email", ReferralSource = "Word of Mouth", Address = "123 Oak St", City = "Springfield", State = "IL", Zip = "62704", SinceDate = today.AddYears(-2), Tags = "[\"VIP\",\"Warranty Customer\"]", BalanceOwed = 324m };
            var cust2 = new OneManVanFSM.Shared.Models.Customer { Name = "Bob Reynolds", Type = OneManVanFSM.Shared.Models.CustomerType.Individual, PrimaryPhone = "(555) 222-3333", PrimaryEmail = "breynolds@email.com", PreferredContactMethod = "Phone", ReferralSource = "Google", Address = "456 Maple Ave", City = "Springfield", State = "IL", Zip = "62701", SinceDate = today.AddYears(-1), BalanceOwed = 450m };
            var cust3 = new OneManVanFSM.Shared.Models.Customer { Name = "Heritage Oaks HOA", Type = OneManVanFSM.Shared.Models.CustomerType.Company, PrimaryPhone = "(555) 333-4444", PrimaryEmail = "board@heritageoaks.org", PreferredContactMethod = "Email", ReferralSource = "Angi", Address = "100 Heritage Blvd", City = "Springfield", State = "IL", Zip = "62711", SinceDate = today.AddYears(-3), Tags = "[\"Commercial\",\"Service Agreement\"]", TaxExempt = true };
            var cust4 = new OneManVanFSM.Shared.Models.Customer { Name = "First Baptist Church", Type = OneManVanFSM.Shared.Models.CustomerType.Company, PrimaryPhone = "(555) 444-5555", PrimaryEmail = "office@firstbaptist.org", PreferredContactMethod = "Phone", Address = "900 Church Ln", City = "Springfield", State = "IL", Zip = "62702", SinceDate = today.AddMonths(-8), TaxExempt = true, BalanceOwed = 725m, Tags = "[\"Commercial\"]" };
            var cust5 = new OneManVanFSM.Shared.Models.Customer { Name = "Linda Parker", Type = OneManVanFSM.Shared.Models.CustomerType.Landlord, PrimaryPhone = "(555) 555-6666", SecondaryPhone = "(555) 555-6667", PrimaryEmail = "lparker@rentals.com", PreferredContactMethod = "Text", ReferralSource = "Repeat Customer", Address = "200 Pine St", City = "Springfield", State = "IL", Zip = "62703", SinceDate = today.AddMonths(-3), Notes = "Multi-site landlord – 3 rental properties", Tags = "[\"Landlord\",\"Multi-Site\"]" };
            var cust6 = new OneManVanFSM.Shared.Models.Customer { Name = "Sunrise Senior Living", Type = OneManVanFSM.Shared.Models.CustomerType.Company, PrimaryPhone = "(555) 666-7777", PrimaryEmail = "facilities@sunrisesl.com", PreferredContactMethod = "Email", ReferralSource = "Referral", Address = "500 Sunrise Dr", City = "Springfield", State = "IL", Zip = "62705", Tags = "[\"Commercial\",\"Priority\"]" };
            var cust7 = new OneManVanFSM.Shared.Models.Customer { Name = "David Kim", Type = OneManVanFSM.Shared.Models.CustomerType.Individual, PrimaryPhone = "(555) 777-8888", PrimaryEmail = "dkim@email.com", PreferredContactMethod = "No Preference", Address = "789 Elm St", City = "Springfield", State = "IL", Zip = "62706" };
            var cust8 = new OneManVanFSM.Shared.Models.Customer { Name = "Oakwood Dental", Type = OneManVanFSM.Shared.Models.CustomerType.Company, PrimaryPhone = "(555) 888-9999", PrimaryEmail = "admin@oakwooddental.com", PreferredContactMethod = "Email", ReferralSource = "Google", Address = "321 Business Park Dr", City = "Springfield", State = "IL", Zip = "62707", BalanceOwed = 290m, Tags = "[\"Commercial\"]" };
            _db.Customers.AddRange(cust1, cust2, cust3, cust4, cust5, cust6, cust7, cust8);

            // Sites
            var site1 = new OneManVanFSM.Shared.Models.Site { Name = "Chen Residence", Address = "123 Oak St", City = "Springfield", State = "IL", Zip = "62704", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Residential, SqFt = 2200, Zones = 2, Stories = 2, EquipmentLocation = "Basement", Customer = cust1, GasLineLocation = "Left side of house near meter", ElectricalPanelLocation = "Basement — east wall", WaterShutoffLocation = "Basement — near hot water heater", HeatingFuelSource = "Natural Gas", YearBuilt = 1998, HasAtticAccess = true, HasCrawlSpace = false, HasBasement = true };
            var site2 = new OneManVanFSM.Shared.Models.Site { Name = "Reynolds Home", Address = "456 Maple Ave", City = "Springfield", State = "IL", Zip = "62701", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Residential, SqFt = 1800, Zones = 1, Stories = 1, EquipmentLocation = "Attic", Customer = cust2, GasLineLocation = "Rear of house", ElectricalPanelLocation = "Garage", WaterShutoffLocation = "Under kitchen sink", HeatingFuelSource = "Natural Gas", YearBuilt = 2005, HasAtticAccess = true, HasCrawlSpace = true, HasBasement = false, Notes = "Attic ladder loose — safety concern" };
            var site3 = new OneManVanFSM.Shared.Models.Site { Name = "Heritage Oaks Clubhouse", Address = "100 Heritage Blvd", City = "Springfield", State = "IL", Zip = "62711", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Commercial, SqFt = 5000, Zones = 4, Stories = 1, Customer = cust3, AccessCodes = "Gate: 4521#", EquipmentLocation = "Rooftop", ElectricalPanelLocation = "Utility closet — main hall", HeatingFuelSource = "Electric", YearBuilt = 2010 };
            var site4 = new OneManVanFSM.Shared.Models.Site { Name = "First Baptist Main Hall", Address = "900 Church Ln", City = "Springfield", State = "IL", Zip = "62702", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Commercial, SqFt = 8000, Zones = 6, Stories = 2, Customer = cust4, EquipmentLocation = "Rooftop", GasLineLocation = "West side of building", ElectricalPanelLocation = "Basement electrical room", HeatingFuelSource = "Natural Gas", YearBuilt = 1985, HasBasement = true };
            var site5 = new OneManVanFSM.Shared.Models.Site { Name = "Parker Rental Unit A", Address = "201 Pine St", City = "Springfield", State = "IL", Zip = "62703", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Residential, SqFt = 1100, Zones = 1, Stories = 1, Customer = cust5, AccessCodes = "Lock box: 9876", HeatingFuelSource = "Propane", YearBuilt = 1992, HasCrawlSpace = true, HasBasement = false };
            var site6 = new OneManVanFSM.Shared.Models.Site { Name = "Parker Rental Unit B", Address = "203 Pine St", City = "Springfield", State = "IL", Zip = "62703", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Residential, SqFt = 1200, Zones = 1, Stories = 1, Customer = cust5, AccessCodes = "Lock box: 5432", HeatingFuelSource = "Propane", YearBuilt = 1992, HasCrawlSpace = true, HasBasement = false };
            var site7 = new OneManVanFSM.Shared.Models.Site { Name = "Sunrise Senior Living Facility", Address = "500 Sunrise Dr", City = "Springfield", State = "IL", Zip = "62705", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Commercial, SqFt = 15000, Zones = 8, Stories = 3, Customer = cust6, EquipmentLocation = "Mechanical Room B1", GasLineLocation = "Loading dock utility chase", ElectricalPanelLocation = "Mechanical Room B1", WaterShutoffLocation = "Mechanical Room B1", HeatingFuelSource = "Natural Gas", YearBuilt = 2015 };
            _db.Sites.AddRange(site1, site2, site3, site4, site5, site6, site7);

            // Products
            var prod1 = new OneManVanFSM.Shared.Models.Product { Name = "14\" Flex Duct (25ft)", Category = "Ductwork", Cost = 45m, Price = 72m, MarkupPercent = 60m, Unit = "Roll", SupplierName = "HVAC Supply Co" };
            var prod2 = new OneManVanFSM.Shared.Models.Product { Name = "R-410A Refrigerant (25lb)", Category = "Refrigerant", Cost = 125m, Price = 200m, MarkupPercent = 60m, Unit = "Tank", SupplierName = "CoolGas Direct" };
            var prod3 = new OneManVanFSM.Shared.Models.Product { Name = "Honeywell T6 Pro Thermostat", Category = "Controls", Cost = 85m, Price = 145m, MarkupPercent = 70m, Unit = "Each", SupplierName = "?" };
            var prod4 = new OneManVanFSM.Shared.Models.Product { Name = "1\" Pleated Air Filter (6pk)", Category = "Filters", Cost = 18m, Price = 35m, MarkupPercent = 94m, Unit = "Pack", SupplierName = "FilterBuy" };
            var prod5 = new OneManVanFSM.Shared.Models.Product { Name = "Condensate Drain Pan", Category = "Parts", Cost = 22m, Price = 40m, MarkupPercent = 82m, Unit = "Each", SupplierName = "HVAC Supply Co" };
            var prod6 = new OneManVanFSM.Shared.Models.Product { Name = "Carrier 3-Ton AC Unit", Category = "Equipment", Cost = 2800m, Price = 4200m, MarkupPercent = 50m, Unit = "Each", SupplierName = "Carrier Distributor", IsTemplate = true };
            _db.Products.AddRange(prod1, prod2, prod3, prod4, prod5, prod6);

            // Inventory
            _db.InventoryItems.AddRange(
                new OneManVanFSM.Shared.Models.InventoryItem { Name = "14\" Flex Duct", Location = OneManVanFSM.Shared.Models.InventoryLocation.Warehouse, Quantity = 12, MinThreshold = 3, MaxCapacity = 30, Cost = 45m, Price = 72m, Product = prod1 },
                new OneManVanFSM.Shared.Models.InventoryItem { Name = "R-410A Refrigerant", Location = OneManVanFSM.Shared.Models.InventoryLocation.Truck, Quantity = 2, MinThreshold = 1, MaxCapacity = 5, Cost = 125m, Price = 200m, Product = prod2, Notes = "Mike's truck" },
                new OneManVanFSM.Shared.Models.InventoryItem { Name = "T6 Pro Thermostat", Location = OneManVanFSM.Shared.Models.InventoryLocation.Warehouse, Quantity = 5, MinThreshold = 2, MaxCapacity = 20, Cost = 85m, Price = 145m, Product = prod3 },
                new OneManVanFSM.Shared.Models.InventoryItem { Name = "1\" Pleated Filters (6pk)", Location = OneManVanFSM.Shared.Models.InventoryLocation.Warehouse, Quantity = 8, MinThreshold = 4, MaxCapacity = 50, Cost = 18m, Price = 35m, Product = prod4 }
            );

            // Assets
            var asset1 = new OneManVanFSM.Shared.Models.Asset { Name = "Carrier 3-Ton AC", AssetType = "AC Unit", Brand = "Carrier", Model = "24ACC636A003", SerialNumber = "SN-AC-4421", Tonnage = 3m, SEER = 16m, BTURating = 36000, FuelType = "Electric", UnitConfiguration = "Split", FilterSize = "20x25x4", Voltage = "240V", Phase = "Single Phase", LocationOnSite = "Side Yard — South", RefrigerantType = "R-410A", RefrigerantQuantity = 6.2m, ManufactureDate = today.AddYears(-5), InstallDate = today.AddYears(-4), WarrantyStartDate = today.AddYears(-4), WarrantyTermYears = 5, WarrantyExpiry = today.AddYears(1), LastServiceDate = today.AddDays(-1), NextServiceDue = today, Status = OneManVanFSM.Shared.Models.AssetStatus.Active, Value = 4200m, Customer = cust1, Site = site1, Product = prod6 };
            var asset2 = new OneManVanFSM.Shared.Models.Asset { Name = "Trane XV80 Furnace", AssetType = "Furnace", Brand = "Trane", Model = "TUD2B060A9V3VB", SerialNumber = "SN-FURN-7782", BTURating = 80000, AFUE = 80m, FuelType = "Natural Gas", UnitConfiguration = "Split", FilterSize = "16x25x1", Voltage = "120V", Phase = "Single Phase", LocationOnSite = "Basement", ManufactureDate = today.AddYears(-6), InstallDate = today.AddYears(-5), WarrantyStartDate = today.AddYears(-5), WarrantyTermYears = 5, WarrantyExpiry = today.AddMonths(-2), LastServiceDate = today.AddDays(-1), NextServiceDue = today.AddMonths(1), Status = OneManVanFSM.Shared.Models.AssetStatus.Active, Value = 2800m, Customer = cust1, Site = site1, Notes = "Warranty recently expired" };
            var asset3 = new OneManVanFSM.Shared.Models.Asset { Name = "Lennox XC21 AC", AssetType = "AC Unit", Brand = "Lennox", Model = "XC21-036-230", SerialNumber = "SN-AC-9931", Tonnage = 3m, SEER = 21m, BTURating = 36000, FuelType = "Electric", UnitConfiguration = "Split", FilterSize = "20x20x4", Voltage = "240V", Phase = "Single Phase", LocationOnSite = "Backyard — Concrete Pad", RefrigerantType = "R-410A", RefrigerantQuantity = 7.1m, ManufactureDate = today.AddYears(-2).AddMonths(-3), InstallDate = today.AddYears(-2), WarrantyStartDate = today.AddYears(-2), WarrantyTermYears = 5, WarrantyExpiry = today.AddYears(3), LastServiceDate = today.AddMonths(-4), NextServiceDue = today.AddMonths(8), Status = OneManVanFSM.Shared.Models.AssetStatus.Active, Value = 5500m, Customer = cust2, Site = site2 };
            var asset4 = new OneManVanFSM.Shared.Models.Asset { Name = "Rooftop Unit #1", AssetType = "RTU", Brand = "Carrier", Model = "48HCDD08A2A6", SerialNumber = "SN-RTU-3301", Tonnage = 7.5m, SEER = 14m, BTURating = 90000, FuelType = "Natural Gas", UnitConfiguration = "Packaged", Voltage = "208V", Phase = "Three Phase", LocationOnSite = "Rooftop — NW Corner", ManufactureDate = today.AddYears(-9), InstallDate = today.AddYears(-8), WarrantyStartDate = today.AddYears(-8), WarrantyTermYears = 5, WarrantyExpiry = today.AddYears(-3), Status = OneManVanFSM.Shared.Models.AssetStatus.MaintenanceNeeded, Value = 8500m, Customer = cust4, Site = site4, Notes = "Needs condenser coil cleaning" };
            var asset5 = new OneManVanFSM.Shared.Models.Asset { Name = "Mini-Split Unit A", AssetType = "Ductless Mini-Split", Brand = "Mitsubishi", Model = "MSZ-GL12NA", SerialNumber = "SN-MS-5501", Tonnage = 1m, SEER = 23m, HSPF = 10.6m, BTURating = 12000, FuelType = "Electric", UnitConfiguration = "Mini-Split", Voltage = "240V", Phase = "Single Phase", LocationOnSite = "Living Room — Wall Mount", RefrigerantType = "R-410A", ManufactureDate = today.AddYears(-1).AddMonths(-2), InstallDate = today.AddYears(-1), WarrantyStartDate = today.AddYears(-1), WarrantyTermYears = 5, WarrantyExpiry = today.AddYears(4), Status = OneManVanFSM.Shared.Models.AssetStatus.Active, Value = 3200m, Customer = cust5, Site = site5 };
            _db.Assets.AddRange(asset1, asset2, asset3, asset4, asset5);

            // Jobs
            var job1 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0041", Title = "AC Repair – Low Refrigerant", Description = "Customer reports warm air from vents. Check refrigerant levels and inspect for leaks.", Status = OneManVanFSM.Shared.Models.JobStatus.Completed, Priority = OneManVanFSM.Shared.Models.JobPriority.High, TradeType = "HVAC", JobType = "Repair", SystemType = "Split System", ScheduledDate = today.AddDays(-1), ScheduledTime = new TimeSpan(9, 0, 0), EstimatedDuration = 2.5m, EstimatedTotal = 350m, ActualDuration = 2.5m, ActualTotal = 324m, CompletedDate = today.AddDays(-1), Customer = cust1, Site = site1, AssignedEmployee = emp1 };
            var job2 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0042", Title = "Thermostat Replacement", Description = "Replace old mercury thermostat with Honeywell T6 Pro.", Status = OneManVanFSM.Shared.Models.JobStatus.Scheduled, Priority = OneManVanFSM.Shared.Models.JobPriority.Standard, TradeType = "HVAC", JobType = "Install", ScheduledDate = today, ScheduledTime = new TimeSpan(10, 30, 0), EstimatedDuration = 1m, EstimatedTotal = 245m, Customer = cust2, Site = site2, AssignedEmployee = emp2 };
            var job3 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0043", Title = "Seasonal Maintenance – Clubhouse", Description = "Spring maintenance: inspect HVAC system, replace filters, check refrigerant.", Status = OneManVanFSM.Shared.Models.JobStatus.Scheduled, Priority = OneManVanFSM.Shared.Models.JobPriority.Standard, TradeType = "HVAC", JobType = "Maintenance", SystemType = "Commercial RTU", ScheduledDate = today.AddDays(1), ScheduledTime = new TimeSpan(8, 0, 0), EstimatedDuration = 3m, EstimatedTotal = 450m, Customer = cust3, Site = site3, AssignedEmployee = emp1 };
            var job4 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0044", Title = "Emergency – No Heat", Description = "Furnace not igniting. Tenant reports no heat since last night.", Status = OneManVanFSM.Shared.Models.JobStatus.EnRoute, Priority = OneManVanFSM.Shared.Models.JobPriority.Emergency, TradeType = "HVAC", JobType = "Repair", ScheduledDate = today, ScheduledTime = new TimeSpan(7, 0, 0), EstimatedDuration = 2m, EstimatedTotal = 500m, Customer = cust5, Site = site5, AssignedEmployee = emp1 };
            var job5 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0045", Title = "Duct Leak Inspection", Description = "Annual duct leak inspection for commercial facility.", Status = OneManVanFSM.Shared.Models.JobStatus.Scheduled, Priority = OneManVanFSM.Shared.Models.JobPriority.Standard, TradeType = "HVAC", JobType = "Diagnostic", SystemType = "Trunk Duct", ScheduledDate = today.AddDays(3), ScheduledTime = new TimeSpan(13, 0, 0), EstimatedDuration = 4m, EstimatedTotal = 600m, PermitRequired = false, Customer = cust4, Site = site4, AssignedEmployee = emp2 };
            var job6 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0046", Title = "New AC Install – Bldg B", Status = OneManVanFSM.Shared.Models.JobStatus.Approved, Priority = OneManVanFSM.Shared.Models.JobPriority.Standard, TradeType = "HVAC", JobType = "Install", EstimatedTotal = 5500m, PermitRequired = true, PermitNumber = "MECH-2025-0891", Customer = cust6, Site = site7 };
            var job7 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0047", Title = "Mini-Split Install", Status = OneManVanFSM.Shared.Models.JobStatus.Quoted, Priority = OneManVanFSM.Shared.Models.JobPriority.Low, TradeType = "HVAC", JobType = "Install", EstimatedTotal = 3200m, PermitRequired = true, Customer = cust5, Site = site6 };
            _db.Jobs.AddRange(job1, job2, job3, job4, job5, job6, job7);

            // Estimates, Invoices, Time Entries, Calendar Events, Notes, Documents
            var est1 = new OneManVanFSM.Shared.Models.Estimate { EstimateNumber = "EST-2025-0015", Title = "Full HVAC Replacement", Status = OneManVanFSM.Shared.Models.EstimateStatus.Sent, TradeType = "HVAC", VersionNumber = 2, ExpiryDate = today.AddDays(3), SqFt = 2200, Zones = 2, Stories = 2, SystemType = "Split System", PricingMethod = OneManVanFSM.Shared.Models.PricingMethod.FlatRate, Subtotal = 7000m, MarkupPercent = 15m, TaxPercent = 8m, Total = 8500m, DepositRequired = 2500m, DepositReceived = false, Customer = cust1, Site = site1 };
            var est2 = new OneManVanFSM.Shared.Models.Estimate { EstimateNumber = "EST-2025-0016", Title = "Ductwork Redesign", Status = OneManVanFSM.Shared.Models.EstimateStatus.Sent, TradeType = "HVAC", VersionNumber = 1, ExpiryDate = today.AddDays(12), SqFt = 8000, Zones = 6, Stories = 2, SystemType = "Trunk Duct", PricingMethod = OneManVanFSM.Shared.Models.PricingMethod.Hybrid, Subtotal = 3400m, MarkupPercent = 15m, TaxPercent = 8m, Total = 4200m, DepositRequired = 1200m, DepositReceived = true, Customer = cust4, Site = site4 };
            var est3 = new OneManVanFSM.Shared.Models.Estimate { EstimateNumber = "EST-2025-0017", Title = "Commercial Rooftop Unit", Status = OneManVanFSM.Shared.Models.EstimateStatus.Draft, TradeType = "HVAC", VersionNumber = 1, SqFt = 15000, Zones = 8, Stories = 3, SystemType = "Packaged RTU", PricingMethod = OneManVanFSM.Shared.Models.PricingMethod.FlatRate, Subtotal = 9800m, MarkupPercent = 15m, TaxPercent = 8m, Total = 12000m, DepositRequired = 3500m, Customer = cust6, Site = site7 };
            _db.Estimates.AddRange(est1, est2, est3);

            var inv_1 = new OneManVanFSM.Shared.Models.Invoice { InvoiceNumber = "INV-2025-0024", Status = OneManVanFSM.Shared.Models.InvoiceStatus.Overdue, InvoiceDate = today.AddDays(-15), DueDate = today.AddDays(-10), PaymentTerms = "Net 5", Subtotal = 300m, TaxAmount = 24m, Total = 324m, BalanceDue = 324m, Customer = cust1, Job = job1, Notes = "AC Repair – overdue" };
            var inv_2 = new OneManVanFSM.Shared.Models.Invoice { InvoiceNumber = "INV-2025-0025", Status = OneManVanFSM.Shared.Models.InvoiceStatus.Sent, InvoiceDate = today.AddDays(-7), DueDate = today.AddDays(-5), PaymentTerms = "Due on Receipt", Subtotal = 400m, TaxAmount = 50m, Total = 450m, BalanceDue = 450m, Customer = cust2 };
            var inv_3 = new OneManVanFSM.Shared.Models.Invoice { InvoiceNumber = "INV-2025-0027", Status = OneManVanFSM.Shared.Models.InvoiceStatus.Sent, InvoiceDate = today.AddDays(-5), DueDate = today.AddDays(2), PaymentTerms = "Net 7", Subtotal = 650m, TaxAmount = 75m, Total = 725m, BalanceDue = 725m, Customer = cust4, Site = site4 };
            var inv_4 = new OneManVanFSM.Shared.Models.Invoice { InvoiceNumber = "INV-2025-0028", Status = OneManVanFSM.Shared.Models.InvoiceStatus.Invoiced, InvoiceDate = today.AddDays(-3), DueDate = today.AddDays(10), PaymentTerms = "Net 15", Subtotal = 260m, TaxAmount = 30m, Total = 290m, BalanceDue = 290m, Customer = cust8, DiscountAmount = 0m };
            _db.Invoices.AddRange(inv_1, inv_2, inv_3, inv_4);

            _db.TimeEntries.AddRange(
                new OneManVanFSM.Shared.Models.TimeEntry { Employee = emp1, Job = job1, StartTime = today.AddDays(-1).AddHours(8).AddMinutes(30), EndTime = today.AddDays(-1).AddHours(9), Hours = 0.5m, IsBillable = false, TimeCategory = "Travel", Notes = "Drive to Chen residence" },
                new OneManVanFSM.Shared.Models.TimeEntry { Employee = emp1, Job = job1, StartTime = today.AddDays(-1).AddHours(9), EndTime = today.AddDays(-1).AddHours(11).AddMinutes(30), Hours = 2.5m, IsBillable = true, TimeCategory = "On-Site", Notes = "Diagnosed low refrigerant, recharged system" },
                new OneManVanFSM.Shared.Models.TimeEntry { Employee = emp1, Job = job4, StartTime = today.AddHours(7), EndTime = today.AddHours(8).AddMinutes(45), Hours = 1.75m, IsBillable = true, TimeCategory = "On-Site", Notes = "Emergency furnace no-heat call" },
                new OneManVanFSM.Shared.Models.TimeEntry { Employee = emp2, StartTime = today.AddDays(-2).AddHours(8), EndTime = today.AddDays(-2).AddHours(16).AddMinutes(30), Hours = 8.5m, IsBillable = true, TimeCategory = "On-Site", Notes = "Full day — maintenance rounds" },
                new OneManVanFSM.Shared.Models.TimeEntry { Employee = emp1, StartTime = today.AddDays(-3).AddHours(9), EndTime = today.AddDays(-3).AddHours(17), Hours = 8m, IsBillable = true, TimeCategory = "On-Site", Notes = "Service calls (3 jobs)" }
            );

            _db.CalendarEvents.AddRange(
                new OneManVanFSM.Shared.Models.CalendarEvent { Title = "Team Safety Meeting", StartDateTime = today.AddDays(2).AddHours(8), EndDateTime = today.AddDays(2).AddHours(9), Duration = 1m, Status = OneManVanFSM.Shared.Models.CalendarEventStatus.Confirmed, EventType = "Meeting", Color = "#6f42c1", Employee = emp1, Notes = "Monthly safety briefing — PPE review" },
                new OneManVanFSM.Shared.Models.CalendarEvent { Title = "NATE Certification Renewal", StartDateTime = today.AddDays(14).AddHours(9), EndDateTime = today.AddDays(14).AddHours(12), Duration = 3m, Status = OneManVanFSM.Shared.Models.CalendarEventStatus.Tentative, EventType = "Training", Color = "#fd7e14", Employee = emp1, Notes = "Online renewal exam" },
                new OneManVanFSM.Shared.Models.CalendarEvent { Title = "Van #2 Service Appointment", StartDateTime = today.AddDays(5).AddHours(7), EndDateTime = today.AddDays(5).AddHours(9), Duration = 2m, Status = OneManVanFSM.Shared.Models.CalendarEventStatus.Confirmed, EventType = "Personal", Color = "#20c997", Employee = emp2, Notes = "Oil change + tire rotation" },
                new OneManVanFSM.Shared.Models.CalendarEvent { Title = "Heritage Oaks Follow-Up Inspection", StartDateTime = today.AddDays(7).AddHours(10), EndDateTime = today.AddDays(7).AddHours(11).AddMinutes(30), Duration = 1.5m, Status = OneManVanFSM.Shared.Models.CalendarEventStatus.Tentative, EventType = "Job", Color = "#0d6efd", Employee = emp1, Job = job3, Notes = "Post-maintenance inspection" },
                new OneManVanFSM.Shared.Models.CalendarEvent { Title = "Sunrise Senior Living Walk-Through", StartDateTime = today.AddDays(4).AddHours(13), EndDateTime = today.AddDays(4).AddHours(15), Duration = 2m, Status = OneManVanFSM.Shared.Models.CalendarEventStatus.Confirmed, EventType = "Job", Color = "#0d6efd", Employee = emp1, Job = job6, Notes = "Pre-install site walk-through" }
            );

            _db.QuickNotes.AddRange(
                new OneManVanFSM.Shared.Models.QuickNote { Title = "Refrigerant leak at condenser", Text = "Found small leak at service valve on Chen AC. Recharged 1.5 lbs R-410A.", Category = "Repair", EntityType = "Job", EntityId = 1, Job = job1, CreatedByEmployee = emp1, Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Active, IsUrgent = false, CreatedAt = today.AddDays(-1).AddHours(11) },
                new OneManVanFSM.Shared.Models.QuickNote { Title = "Igniter replacement needed", Text = "Parker furnace — hot surface igniter cracked. Replaced with Honeywell Q3400A.", Category = "Repair", EntityType = "Job", EntityId = 4, Job = job4, CreatedByEmployee = emp1, Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Active, IsUrgent = true, CreatedAt = today.AddHours(8) },
                new OneManVanFSM.Shared.Models.QuickNote { Title = "Heritage Oaks access reminder", Text = "Gate code changed to 4521#. Previous code 1234# no longer works.", Category = "General", CreatedByEmployee = emp1, Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Active, IsUrgent = false, CreatedAt = today.AddDays(-2).AddHours(14) },
                new OneManVanFSM.Shared.Models.QuickNote { Title = "Safety concern - Reynolds attic", Text = "Attic access ladder is loose at Reynolds home. Almost slipped.", Category = "Safety", CreatedByEmployee = emp2, Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Active, IsUrgent = true, CreatedAt = today.AddDays(-3).AddHours(10) },
                new OneManVanFSM.Shared.Models.QuickNote { Title = "Follow-up: Chen filter order", Text = "Martha Chen requested quote for annual filter delivery.", Category = "Follow-Up", EntityType = "Customer", EntityId = 1, Customer = cust1, CreatedByEmployee = emp1, Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Draft, IsUrgent = false, CreatedAt = today.AddDays(-1).AddHours(12) }
            );

            _db.Documents.AddRange(
                new OneManVanFSM.Shared.Models.Document { Name = "Carrier AC Install Manual", Category = OneManVanFSM.Shared.Models.DocumentCategory.Manual, FileType = "PDF", FileSize = 2_450_000, Job = job1, Site = site1, UploadedByEmployee = emp1, UploadDate = today.AddDays(-1) },
                new OneManVanFSM.Shared.Models.Document { Name = "Chen AC Warranty Card", Category = OneManVanFSM.Shared.Models.DocumentCategory.WarrantyPrintout, FileType = "Image", FileSize = 850_000, Customer = cust1, Site = site1, UploadedByEmployee = emp1, UploadDate = today.AddDays(-1) },
                new OneManVanFSM.Shared.Models.Document { Name = "Honeywell T6 Pro Setup Guide", Category = OneManVanFSM.Shared.Models.DocumentCategory.SetupGuide, FileType = "PDF", FileSize = 1_200_000, Job = job2, UploadedByEmployee = emp2, UploadDate = today },
                new OneManVanFSM.Shared.Models.Document { Name = "EPA 608 Universal Certificate", Category = OneManVanFSM.Shared.Models.DocumentCategory.Certification, FileType = "PDF", FileSize = 350_000, Employee = emp1, UploadedByEmployee = emp1, UploadDate = today.AddMonths(-6) },
                new OneManVanFSM.Shared.Models.Document { Name = "Heritage Oaks Service Agreement", Category = OneManVanFSM.Shared.Models.DocumentCategory.Other, FileType = "PDF", FileSize = 980_000, Customer = cust3, Site = site3, UploadedByEmployee = emp1, UploadDate = today.AddDays(-10) }
            );

            await _db.SaveChangesAsync();

            // Material Lists (need IDs)
            var matList = new OneManVanFSM.Shared.Models.MaterialList { Name = "Clubhouse Seasonal Maintenance", Customer = cust3, Site = site3, Subtotal = 187.50m, MarkupPercent = 15m, TaxPercent = 8.25m, Total = 233.18m, Notes = "Standard spring maintenance materials" };
            _db.MaterialLists.Add(matList);
            await _db.SaveChangesAsync();
            job3.MaterialListId = matList.Id;
            _db.MaterialListItems.AddRange(
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Filters", ItemName = "20x25x4 MERV 13 Filter", Quantity = 4, Unit = "ea", BaseCost = 22.50m },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Filters", ItemName = "16x20x1 MERV 8 Filter", Quantity = 2, Unit = "ea", BaseCost = 8.00m },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Refrigerant", ItemName = "R-410A Refrigerant", Quantity = 5, Unit = "lbs", BaseCost = 12.50m },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Electrical", ItemName = "Capacitor 45/5 MFD", Quantity = 1, Unit = "ea", BaseCost = 18.00m, Notes = "Preventive replacement" },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Sealing", ItemName = "Mastic Sealant", Quantity = 1, Unit = "tube", BaseCost = 9.50m },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Sealing", ItemName = "Foil Tape (UL 181)", Quantity = 1, Unit = "roll", BaseCost = 7.00m }
            );

            // Estimate Lines, Invoice Lines, Expenses, Payments, JobAssets, AssetServiceLogs, Service Agreements, Suppliers, Templates, DropdownOptions
            _db.EstimateLines.AddRange(
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Equipment", Description = "Carrier 24ACC636A003 — 3-Ton 16 SEER AC Unit", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 3200m, LineTotal = 3200m, SortOrder = 1 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Labor", Description = "AC System Removal + Install (2 techs)", LineType = "Labor", Unit = "Hour", Quantity = 8, UnitPrice = 125m, LineTotal = 1000m, SortOrder = 2 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Labor", Description = "Ductwork Inspection + Leak Testing", LineType = "Labor", Unit = "Hour", Quantity = 3, UnitPrice = 125m, LineTotal = 375m, SortOrder = 1 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est3.Id, Section = "Equipment", Description = "Commercial RTU — 7.5 Ton", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 6500m, LineTotal = 6500m, SortOrder = 1 }
            );

            _db.InvoiceLines.AddRange(
                new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_1.Id, Description = "Refrigerant R-410A (1.5 lbs)", LineType = "Material", Unit = "Lbs", Quantity = 1.5m, UnitPrice = 28m, LineTotal = 42m, SortOrder = 1 },
                new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_1.Id, Description = "Leak detection + Repair", LineType = "Labor", Unit = "Hour", Quantity = 2, UnitPrice = 125m, LineTotal = 250m, SortOrder = 2 },
                new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_2.Id, Description = "Honeywell T6 Pro Thermostat", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 145m, LineTotal = 145m, SortOrder = 1 },
                new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_2.Id, Description = "Thermostat Install Labor", LineType = "Labor", Unit = "Hour", Quantity = 1, UnitPrice = 125m, LineTotal = 125m, SortOrder = 2 }
            );

            _db.Expenses.AddRange(
                new OneManVanFSM.Shared.Models.Expense { Category = "Fuel", Amount = 87.50m, IsBillable = false, Status = OneManVanFSM.Shared.Models.ExpenseStatus.Approved, Description = "Van #1 fuel fill-up", Employee = emp1, ExpenseDate = today.AddDays(-1) },
                new OneManVanFSM.Shared.Models.Expense { Category = "Parts", Amount = 24m, IsBillable = true, Status = OneManVanFSM.Shared.Models.ExpenseStatus.Approved, Description = "Honeywell Q3400A igniter", Employee = emp1, Job = job4, ExpenseDate = today }
            );

            _db.Payments.Add(new OneManVanFSM.Shared.Models.Payment { InvoiceId = inv_2.Id, Amount = 200m, Method = OneManVanFSM.Shared.Models.PaymentMethod.Card, Status = OneManVanFSM.Shared.Models.PaymentStatus.Completed, PaymentDate = today.AddDays(-3), Reference = "Partial payment", TransactionId = "TXN-44210" });

            _db.JobAssets.AddRange(
                new OneManVanFSM.Shared.Models.JobAsset { Job = job1, Asset = asset1, Role = "Serviced", Notes = "Recharged refrigerant, checked pressures" },
                new OneManVanFSM.Shared.Models.JobAsset { Job = job1, Asset = asset2, Role = "Inspected", Notes = "Verified furnace operation during AC service" },
                new OneManVanFSM.Shared.Models.JobAsset { Job = job2, Asset = asset3, Role = "Serviced", Notes = "Thermostat replacement affects AC system" }
            );

            _db.AssetServiceLogs.AddRange(
                new OneManVanFSM.Shared.Models.AssetServiceLog { Asset = asset1, ServiceType = "Refrigerant Charge", ServiceDate = today.AddDays(-1), PerformedBy = "Mike Johnson", Notes = "Recharged 1.5 lbs R-410A.", Cost = 185m, NextDueDate = today.AddDays(29) },
                new OneManVanFSM.Shared.Models.AssetServiceLog { Asset = asset1, ServiceType = "Filter Change", ServiceDate = today.AddMonths(-3), PerformedBy = "Mike Johnson", Notes = "Replaced 20x25x4 MERV 13 filter.", Cost = 22.50m, NextDueDate = today },
                new OneManVanFSM.Shared.Models.AssetServiceLog { Asset = asset2, ServiceType = "Inspection", ServiceDate = today.AddDays(-1), PerformedBy = "Mike Johnson", Notes = "Visual inspection during AC call." }
            );

            // Service Agreements
            var sa1 = new OneManVanFSM.Shared.Models.ServiceAgreement { AgreementNumber = "SA-2025-001", Title = "Annual Maintenance — Chen Residence", CoverageLevel = OneManVanFSM.Shared.Models.CoverageLevel.Premium, StartDate = today.AddMonths(-6), EndDate = today.AddMonths(6), VisitsIncluded = 4, VisitsUsed = 2, Fee = 349m, TradeType = "HVAC", BillingFrequency = "Annual", DiscountPercent = 10m, RenewalDate = today.AddMonths(6), AutoRenew = true, Status = OneManVanFSM.Shared.Models.AgreementStatus.Active, Customer = cust1, Site = site1 };
            var sa2 = new OneManVanFSM.Shared.Models.ServiceAgreement { AgreementNumber = "SA-2025-002", Title = "Commercial HVAC Maintenance — Heritage Oaks", CoverageLevel = OneManVanFSM.Shared.Models.CoverageLevel.Gold, StartDate = today.AddMonths(-2), EndDate = today.AddMonths(10), VisitsIncluded = 6, VisitsUsed = 1, Fee = 1200m, TradeType = "HVAC", BillingFrequency = "Quarterly", DiscountPercent = 15m, RenewalDate = today.AddMonths(10), AutoRenew = true, Status = OneManVanFSM.Shared.Models.AgreementStatus.Active, Customer = cust3, Site = site3 };
            _db.ServiceAgreements.AddRange(sa1, sa2);

            _db.ServiceAgreementAssets.AddRange(
                new OneManVanFSM.Shared.Models.ServiceAgreementAsset { ServiceAgreement = sa1, Asset = asset1, CoverageNotes = "Full coverage — AC tune-ups, refrigerant top-off" },
                new OneManVanFSM.Shared.Models.ServiceAgreementAsset { ServiceAgreement = sa1, Asset = asset2, CoverageNotes = "Full coverage — furnace tune-ups" }
            );

            // Suppliers
            _db.Suppliers.AddRange(
                new OneManVanFSM.Shared.Models.Supplier { Name = "Johnstone Supply", ContactName = "Kevin Marsh", Phone = "(555) 900-1001", Email = "kevin@johnstonesupply.com", AccountNumber = "JS-44210", PaymentTerms = "Net 30" },
                new OneManVanFSM.Shared.Models.Supplier { Name = "FilterDirect Supply", ContactName = "Amy Torres", Phone = "(555) 900-2002", Email = "orders@filterdirect.com", AccountNumber = "FD-8820", PaymentTerms = "Net 15" },
                new OneManVanFSM.Shared.Models.Supplier { Name = "Carrier Distributor", ContactName = "Dan Ortiz", Phone = "(555) 900-4004", Email = "dan.ortiz@carrierdist.com", AccountNumber = "CD-2025-110", PaymentTerms = "Net 30" }
            );

            // Templates
            _db.Templates.AddRange(
                new OneManVanFSM.Shared.Models.Template { Name = "Residential AC Tune-Up Checklist", Type = OneManVanFSM.Shared.Models.TemplateType.JobChecklist, IsCompanyDefault = true, UsageCount = 24, LastUsed = today.AddDays(-1), Data = "{\"sections\":[{\"name\":\"Outdoor Unit\",\"items\":[\"Clean condenser coil\",\"Check refrigerant levels\",\"Inspect contactor\",\"Test capacitor\",\"Lubricate fan motor\"]}]}" },
                new OneManVanFSM.Shared.Models.Template { Name = "Standard Estimate Format", Type = OneManVanFSM.Shared.Models.TemplateType.EstimateFormat, IsCompanyDefault = true, UsageCount = 12, Data = "{\"sections\":[\"Equipment\",\"Labor\",\"Materials\",\"Fees\",\"Warranty\"],\"defaults\":{\"markupPercent\":15,\"taxPercent\":8.25,\"validDays\":30}}" }
            );

            // Dropdown Options
            var sortOrder = 0;
            _db.DropdownOptions.AddRange(
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "TradeType", Value = "HVAC", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "TradeType", Value = "Plumbing", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "TradeType", Value = "Electrical", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "TradeType", Value = "General", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "JobType", Value = "Install", SortOrder = sortOrder = 1, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "JobType", Value = "Repair", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "JobType", Value = "Maintenance", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "JobType", Value = "Diagnostic", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Split System", SortOrder = sortOrder = 1, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Packaged Unit", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Mini-Split", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Heat Pump", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Commercial RTU", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "ExpenseCategory", Value = "Fuel", SortOrder = sortOrder = 1, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "ExpenseCategory", Value = "Parts", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "ExpenseCategory", Value = "Tools", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "FuelType", Value = "Natural Gas", SortOrder = sortOrder = 1, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "FuelType", Value = "Propane", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "FuelType", Value = "Electric", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "AC Unit", SortOrder = sortOrder = 1, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "Furnace", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "Heat Pump", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "RTU", SortOrder = ++sortOrder, IsSystem = true },
                new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "Ductless Mini-Split", SortOrder = ++sortOrder, IsSystem = true }
            );

            // Admin user
            if (!await _db.Users.AnyAsync())
            {
                _db.Users.Add(new OneManVanFSM.Shared.Models.AppUser
                {
                    Username = "admin",
                    Email = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "chris.eikel@bledsoe.net",
                    PasswordHash = AuthService.HashPassword(Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "!1235aSdf12sadf5!"),
                    Role = OneManVanFSM.Shared.Models.UserRole.Owner,
                    IsActive = true,
                });
            }

            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ---- Mobile Device Version Tracking ----

    public async Task<List<MobileDeviceInfo>> GetMobileDevicesAsync()
    {
        return await _db.MobileDevices
            .Include(d => d.Employee)
            .Include(d => d.User)
            .OrderByDescending(d => d.LastSyncTime)
            .Select(d => new MobileDeviceInfo
            {
                Id = d.Id,
                DeviceId = d.DeviceId,
                DeviceName = d.DeviceName,
                Platform = d.Platform,
                OsVersion = d.OsVersion,
                AppVersion = d.AppVersion,
                BuildNumber = d.BuildNumber,
                BuildTimestamp = d.BuildTimestamp,
                EmployeeId = d.EmployeeId,
                EmployeeName = d.Employee != null ? d.Employee.Name : null,
                UserId = d.UserId,
                Username = d.User != null ? d.User.Username : null,
                LastSyncTime = d.LastSyncTime,
                FirstSeenAt = d.FirstSeenAt,
                IsActive = d.IsActive,
                Notes = d.Notes
            })
            .ToListAsync();
    }

    public async Task UpdateDeviceNotesAsync(int deviceId, string? notes)
    {
        var device = await _db.MobileDevices.FindAsync(deviceId);
        if (device is not null)
        {
            device.Notes = notes;
            await _db.SaveChangesAsync();
        }
    }

    public async Task SetDeviceActiveAsync(int deviceId, bool isActive)
    {
        var device = await _db.MobileDevices.FindAsync(deviceId);
        if (device is not null)
        {
            device.IsActive = isActive;
            await _db.SaveChangesAsync();
        }
    }
}
