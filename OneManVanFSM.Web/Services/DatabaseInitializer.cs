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

            // Get existing columns from SQLite
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info(\"{tableName}\")";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    existingColumns.Add(reader.GetString(1));
                }
            }

            if (existingColumns.Count == 0)
            {
                // Table is entirely missing
                Console.WriteLine($"[DatabaseInitializer] Missing table: {tableName}");
                return true;
            }

            // Check every mapped column in the EF model
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
}
