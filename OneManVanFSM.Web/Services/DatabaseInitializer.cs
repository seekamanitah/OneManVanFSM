using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OneManVanFSM.Shared.Data;

namespace OneManVanFSM.Web.Services;

/// <summary>
/// Compares the EF Core model against the actual SQLite schema on startup.
/// If ANY column is missing from ANY table the database is deleted and recreated.
/// This is safe because all data is seeded from Program.cs on first run.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Ensures the database schema matches the current EF Core model.
    /// Call this BEFORE EnsureCreated and seeding in Program.cs.
    /// </summary>
    public static void EnsureSchemaUpToDate(AppDbContext context)
    {
        // If the database doesn't exist yet, nothing to check — EnsureCreated will handle it.
        if (!context.Database.CanConnect())
            return;

        if (HasSchemaMismatch(context))
        {
            Console.WriteLine("[DatabaseInitializer] Schema mismatch detected — recreating database.");

            // Close the connection so SQLite releases its file lock before deletion.
            var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Closed)
                connection.Close();

            context.Database.EnsureDeleted();
            // EnsureCreated will be called by the caller in Program.cs.
        }
        else
        {
            // Fix any empty-string values in decimal columns (legacy data issue)
            var conn = context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            SanitizeEmptyDecimalColumns(context, conn);
        }
    }

    private static bool HasSchemaMismatch(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrEmpty(tableName))
                continue;

            var existingColumns = GetExistingColumns(connection, tableName);

            if (existingColumns.Count == 0)
            {
                Console.WriteLine($"[DatabaseInitializer] Missing table: {tableName}");
                return true;
            }

            var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject);
                if (!string.IsNullOrEmpty(columnName) && !existingColumns.Contains(columnName))
                {
                    Console.WriteLine($"[DatabaseInitializer] Missing column: {tableName}.{columnName}");
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Non-destructive schema migration: adds missing tables and columns to an
    /// existing database without deleting data. Use after restoring a backup from
    /// an older build so newer schema elements are present but all data is preserved.
    /// </summary>
    public static void MigrateSchemaPreservingData(AppDbContext context)
    {
        if (!context.Database.CanConnect())
            return;

        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        // Phase 1: Add missing columns to existing tables via ALTER TABLE ADD COLUMN.
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrEmpty(tableName))
                continue;

            var existingColumns = GetExistingColumns(connection, tableName);
            if (existingColumns.Count == 0)
                continue; // Table missing entirely — handled in Phase 2

            var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject);
                if (string.IsNullOrEmpty(columnName) || existingColumns.Contains(columnName))
                    continue;

                var storeType = property.GetColumnType(storeObject) ?? "TEXT";
                var defaultClause = property.IsNullable
                    ? ""
                    : $" NOT NULL DEFAULT {GetSqliteDefault(storeType, property.ClrType)}";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {storeType}{defaultClause}";
                cmd.ExecuteNonQuery();
                Console.WriteLine($"[DatabaseInitializer] Added column: {tableName}.{columnName}");
            }
        }

        // Phase 2: Create any entirely missing tables and indexes from the EF model.
        // GenerateCreateScript returns the full DDL; we inject IF NOT EXISTS so
        // existing objects are silently skipped.
        var script = context.Database.GenerateCreateScript();
        foreach (var rawStatement in script.Split(';'))
        {
            var stmt = rawStatement.Trim();
            if (string.IsNullOrWhiteSpace(stmt))
                continue;

            if (stmt.StartsWith("CREATE TABLE ", StringComparison.OrdinalIgnoreCase))
                stmt = stmt.Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ");
            else if (stmt.StartsWith("CREATE UNIQUE INDEX ", StringComparison.OrdinalIgnoreCase))
                stmt = stmt.Replace("CREATE UNIQUE INDEX ", "CREATE UNIQUE INDEX IF NOT EXISTS ");
            else if (stmt.StartsWith("CREATE INDEX ", StringComparison.OrdinalIgnoreCase))
                stmt = stmt.Replace("CREATE INDEX ", "CREATE INDEX IF NOT EXISTS ");
            else
                continue;

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = stmt;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseInitializer] Schema migration warning: {ex.Message}");
            }
        }

        // Phase 3: Fix any empty-string values in decimal columns (caused by prior TEXT default)
        SanitizeEmptyDecimalColumns(context, connection);

        Console.WriteLine("[DatabaseInitializer] Non-destructive schema migration completed — data preserved.");
    }

    private static HashSet<string> GetExistingColumns(System.Data.Common.DbConnection connection, string tableName)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            columns.Add(reader.GetString(1));
        return columns;
    }

    private static string GetSqliteDefault(string storeType, Type? clrType = null)
    {
        var upper = storeType.ToUpperInvariant();
        if (upper.Contains("INTEGER")) return "0";
        if (upper.Contains("REAL")) return "0.0";

        // EF Core maps decimal to TEXT in SQLite — default must be '0' not ''
        if (clrType is not null)
        {
            var underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;
            if (underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float))
                return "'0'";
        }

        return "''";
    }

    /// <summary>
    /// Scans all tables for decimal columns that contain empty strings and replaces
    /// them with '0'. This fixes data corrupted by a prior version of GetSqliteDefault.
    /// </summary>
    private static void SanitizeEmptyDecimalColumns(AppDbContext context, System.Data.Common.DbConnection connection)
    {
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrEmpty(tableName))
                continue;

            var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
            foreach (var property in entityType.GetProperties())
            {
                var underlying = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (underlying != typeof(decimal) && underlying != typeof(double) && underlying != typeof(float))
                    continue;

                var columnName = property.GetColumnName(storeObject);
                if (string.IsNullOrEmpty(columnName))
                    continue;

                try
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $"UPDATE \"{tableName}\" SET \"{columnName}\" = '0' WHERE \"{columnName}\" = '' OR \"{columnName}\" IS NULL AND {(property.IsNullable ? "0=1" : "1=1")}";
                    var affected = cmd.ExecuteNonQuery();
                    if (affected > 0)
                        Console.WriteLine($"[DatabaseInitializer] Sanitized {affected} empty value(s) in {tableName}.{columnName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DatabaseInitializer] Sanitize warning for {tableName}.{columnName}: {ex.Message}");
                }
            }
        }
    }
}
