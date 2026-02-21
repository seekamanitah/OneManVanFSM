using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using OneManVanFSM.Services;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;
using OneManVanFSM.Shared.Services;

namespace OneManVanFSM
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Force US English culture for consistent $ currency formatting
            var usCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = usCulture;
            CultureInfo.DefaultThreadCurrentUICulture = usCulture;
            Thread.CurrentThread.CurrentCulture = usCulture;
            Thread.CurrentThread.CurrentUICulture = usCulture;

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // EF Core SQLite — local database in app data directory
            // Support remote connection when configured via preferences
            var dbMode = Preferences.Default.Get("db_mode", "Local");
            var remoteUrl = Preferences.Default.Get("db_server_url", "");

            string dbConnectionString;
            if (dbMode == "Remote" && !string.IsNullOrWhiteSpace(remoteUrl))
            {
                // Remote mode: store the server URL for API-based sync
                // Still use a local SQLite cache for offline capability
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "OneManVanFSM.db");
                dbConnectionString = $"Data Source={dbPath}";
                System.Diagnostics.Debug.WriteLine($"[DB] Remote mode configured — server: {remoteUrl}, local cache: {dbPath}");
            }
            else
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "OneManVanFSM.db");
                dbConnectionString = $"Data Source={dbPath}";
                System.Diagnostics.Debug.WriteLine($"[DB] Local mode — path: {dbPath}");
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(dbConnectionString));

            // Shared infrastructure
            builder.Services.AddSingleton<ApiClient>();
            builder.Services.AddSingleton<IPlatformHelper, PlatformHelper>();
            builder.Services.AddScoped<IPermissionService, PermissionService>();
            builder.Services.AddScoped<IMobileSettingsService, MobileSettingsService>();

            var isRemote = dbMode == "Remote" && !string.IsNullOrWhiteSpace(remoteUrl);

            if (isRemote)
            {
                // Remote mode — core services talk to the REST API; others read from synced local cache
                builder.Services.AddSingleton<IOfflineQueueService, OfflineQueueService>();
                builder.Services.AddSingleton<BackgroundSyncService>();
                builder.Services.AddScoped<ISyncService, SyncService>();
                builder.Services.AddScoped<IMobileAuthService, RemoteMobileAuthService>();
                builder.Services.AddScoped<IMobileDashboardService, RemoteMobileDashboardService>();
                builder.Services.AddScoped<IMobileJobService, RemoteMobileJobService>();
                builder.Services.AddScoped<IMobileCustomerService, RemoteMobileCustomerService>();
                builder.Services.AddScoped<IMobileTimeService, RemoteMobileTimeService>();

                // Read-only / local-cache services (populated by SyncService)
                builder.Services.AddScoped<IMobileCalendarService, MobileCalendarService>();
                builder.Services.AddScoped<IMobileNoteService, RemoteMobileNoteService>();
                builder.Services.AddScoped<IMobileDocumentService, MobileDocumentService>();
                builder.Services.AddScoped<IMobileAssetService, MobileAssetService>();
                builder.Services.AddScoped<IMobileSearchService, MobileSearchService>();
                builder.Services.AddScoped<IMobileInventoryService, RemoteMobileInventoryService>();
                builder.Services.AddScoped<IMobileEstimateService, RemoteMobileEstimateService>();
                builder.Services.AddScoped<IMobileReportService, RemoteMobileReportService>();
                builder.Services.AddScoped<IMobileServiceAgreementService, MobileServiceAgreementService>();
                builder.Services.AddScoped<IMobileSiteService, MobileSiteService>();
                builder.Services.AddScoped<IMobileCompanyService, MobileCompanyService>();
                builder.Services.AddScoped<IMobileProductService, MobileProductService>();
                builder.Services.AddScoped<IMobileExpenseService, RemoteMobileExpenseService>();
                builder.Services.AddScoped<IMobileInvoiceService, MobileInvoiceService>();
                builder.Services.AddScoped<IMobilePdfService, MobilePdfService>();
                builder.Services.AddScoped<IMobileQrCodeService, MobileQrCodeService>();
                builder.Services.AddScoped<IMobilePhotoService, MobilePhotoService>();
                builder.Services.AddScoped<IMobileMaterialListService, MobileMaterialListService>();
                builder.Services.AddScoped<IMobileServiceHistoryService, MobileServiceHistoryService>();
                builder.Services.AddScoped<IMobileFinancialService, MobileFinancialService>();
                builder.Services.AddScoped<IMobileDropdownService, MobileDropdownService>();
                builder.Services.AddScoped<IMobileSupplierService, MobileSupplierService>();
                builder.Services.AddScoped<IMobileDataManagementService, MobileDataManagementService>();

                System.Diagnostics.Debug.WriteLine("[DI] Remote mode \u2014 registered API-backed services.");
            }
            else
            {
                // Local mode — all services use direct SQLite access
                builder.Services.AddScoped<IMobileAuthService, MobileAuthService>();
                builder.Services.AddScoped<IMobileDashboardService, MobileDashboardService>();
                builder.Services.AddScoped<IMobileJobService, MobileJobService>();
                builder.Services.AddScoped<IMobileCalendarService, MobileCalendarService>();
                builder.Services.AddScoped<IMobileNoteService, MobileNoteService>();
                builder.Services.AddScoped<IMobileTimeService, MobileTimeService>();
                builder.Services.AddScoped<IMobileDocumentService, MobileDocumentService>();
                builder.Services.AddScoped<IMobileAssetService, MobileAssetService>();
                builder.Services.AddScoped<IMobileSearchService, MobileSearchService>();
                builder.Services.AddScoped<IMobileInventoryService, MobileInventoryService>();
                builder.Services.AddScoped<IMobileEstimateService, MobileEstimateService>();
                builder.Services.AddScoped<IMobileCustomerService, MobileCustomerService>();
                builder.Services.AddScoped<IMobileReportService, MobileReportService>();
                builder.Services.AddScoped<IMobileServiceAgreementService, MobileServiceAgreementService>();
                builder.Services.AddScoped<IMobileSiteService, MobileSiteService>();
                builder.Services.AddScoped<IMobileCompanyService, MobileCompanyService>();
                builder.Services.AddScoped<IMobileProductService, MobileProductService>();
                builder.Services.AddScoped<IMobileExpenseService, MobileExpenseService>();
                builder.Services.AddScoped<IMobileInvoiceService, MobileInvoiceService>();
                builder.Services.AddScoped<IMobilePdfService, MobilePdfService>();
                builder.Services.AddScoped<IMobileQrCodeService, MobileQrCodeService>();
                builder.Services.AddScoped<IMobilePhotoService, MobilePhotoService>();
                builder.Services.AddScoped<IMobileMaterialListService, MobileMaterialListService>();
                builder.Services.AddScoped<IMobileServiceHistoryService, MobileServiceHistoryService>();
                builder.Services.AddScoped<IMobileFinancialService, MobileFinancialService>();
                builder.Services.AddScoped<IMobileDropdownService, MobileDropdownService>();
                builder.Services.AddScoped<IMobileSupplierService, MobileSupplierService>();
                builder.Services.AddScoped<IMobileDataManagementService, MobileDataManagementService>();

                System.Diagnostics.Debug.WriteLine("[DI] Local mode \u2014 registered direct-DB services.");
            }

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Wire up global unhandled exception handlers for crash resilience
            SetupGlobalExceptionHandling(app.Services);

            // Ensure schema is up-to-date and tables exist
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                EnsureSchemaUpToDate(db);
                db.Database.EnsureCreated();
                // Only create admin user if database is empty - no automatic demo data seeding
                EnsureAdminUserExists(db);

            // Seed default role permissions if not present
                var permSvc = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                permSvc.SeedDefaultsIfEmptyAsync().GetAwaiter().GetResult();
            }

            // Start background auto-sync in remote mode
            if (isRemote)
            {
                var bgSync = app.Services.GetRequiredService<BackgroundSyncService>();
                bgSync.Start();
            }

            return app;
        }

        /// <summary>
        /// Registers global handlers so unhandled exceptions on background threads,
        /// async task continuations, and the AppDomain are logged instead of silently crashing.
        /// </summary>
        private static void SetupGlobalExceptionHandling(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Global");

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                    logger.LogCritical(ex, "Unhandled AppDomain exception (terminating: {Terminating}).", args.IsTerminating);
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                logger.LogError(args.Exception, "Unobserved task exception.");
                args.SetObserved(); // Prevent process crash on unobserved task exceptions
            };
        }

        /// <summary>
        /// Non-destructive schema migration: adds missing columns and tables to an
        /// existing database via ALTER TABLE ADD COLUMN, preserving all user data.
        /// Only creates missing elements — never deletes or recreates.
        /// </summary>
        private static void EnsureSchemaUpToDate(AppDbContext context)
        {
            if (!context.Database.CanConnect())
                return;

            var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            bool anyChanges = false;

            // Phase 1: Add missing columns to existing tables
            foreach (var entityType in context.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (string.IsNullOrEmpty(tableName))
                    continue;

                var existingColumns = GetExistingColumns(connection, tableName);
                if (existingColumns.Count == 0)
                    continue; // Entire table missing — handled in Phase 2

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

                    try
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {storeType}{defaultClause}";
                        cmd.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine($"[Schema] Added column: {tableName}.{columnName}");
                        anyChanges = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Schema] Warning adding {tableName}.{columnName}: {ex.Message}");
                    }
                }
            }

            // Phase 2: Create any entirely missing tables and indexes
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
                    System.Diagnostics.Debug.WriteLine($"[Schema] Migration warning: {ex.Message}");
                }
            }

            if (anyChanges)
                System.Diagnostics.Debug.WriteLine("[Schema] Non-destructive migration completed — all data preserved.");
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

            if (clrType is not null)
            {
                var underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;
                if (underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float))
                    return "'0'";
            }

            return "''";
        }

        /// <summary>
        /// Ensures the admin AppUser exists even when seed data was partially
        /// created by a previous build. Fixes login failures caused by the
        /// employee-count early-return skipping user creation.
        /// </summary>
        private static void EnsureAdminUserExists(AppDbContext db)
        {
            if (db.Users.Any())
            {
                System.Diagnostics.Debug.WriteLine("[SEED] Admin user already exists.");
                return;
            }

            // Generate a temporary random password for first-run.
            // MustChangePassword forces the user through the Setup page on first login.
            var tempPassword = GenerateTemporaryPassword();
            System.Diagnostics.Debug.WriteLine($"[SEED] Creating admin user — temporary password generated (change on first login).");

            db.Users.Add(new OneManVanFSM.Shared.Models.AppUser
            {
                Username = "admin",
                Email = "admin@localhost",
                PasswordHash = MobileAuthService.HashPassword(tempPassword),
                Role = OneManVanFSM.Shared.Models.UserRole.Owner,
                IsActive = true,
                MustChangePassword = true,
                EmployeeId = null,
            });
            db.SaveChanges();

            // Store the temp password in SecureStorage so the Setup page can display it once
            try
            {
                SecureStorage.Default.SetAsync("admin_temp_password", tempPassword).GetAwaiter().GetResult();
            }
            catch
            {
                // SecureStorage may not be available in all environments
                System.Diagnostics.Debug.WriteLine($"[SEED] Temp admin password: {tempPassword}");
            }
        }

        /// <summary>
        /// Generates a cryptographically random temporary password (16 chars, mixed case + digits + symbols).
        /// </summary>
        private static string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
            var password = new char[16];
            for (int i = 0; i < 16; i++)
                password[i] = chars[bytes[i] % chars.Length];
            return new string(password);
        }

        public static void SeedMobileData(AppDbContext db)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[SEED] SeedMobileData starting...");

                // Force fresh connection to avoid stale state
                db.Database.EnsureCreated();

                var employeeCount = db.Employees.Count();
                System.Diagnostics.Debug.WriteLine($"[SEED] Current employee count: {employeeCount}");

                if (employeeCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine("[SEED] Employees already exist. Skipping demo data seed.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[SEED] Database is empty. Creating seed data...");

                var today = DateTime.Now.Date;

                // Create the tech employee (this is who the mobile user "is")
                var tech = new OneManVanFSM.Shared.Models.Employee
                {
                    Name = "Mike Johnson",
                    Role = OneManVanFSM.Shared.Models.EmployeeRole.Tech,
                    Status = OneManVanFSM.Shared.Models.EmployeeStatus.Active,
                    Phone = "(555) 100-1000",
                    Email = "mike.johnson@onemanvan.com",
                    HourlyRate = 35m,
                    OvertimeRate = 52.50m,
                    HireDate = today.AddYears(-3),
                    Territory = "Springfield Metro",
                    Certifications = "[\"EPA 608 Universal\",\"NATE HVAC\"]",
                    LicenseNumber = "EPA-608-U-44210",
                    LicenseExpiry = today.AddYears(2),
                    VehicleAssigned = "Van #1 \u2014 2022 Ford Transit",
                    EmergencyContactName = "Sarah Johnson",
                    EmergencyContactPhone = "(555) 100-0001",
                };
                db.Employees.Add(tech);

                // Link existing admin user to the tech employee (if admin exists and has no employee)
                var adminUser = db.Users.FirstOrDefault(u => u.Username == "admin" && u.EmployeeId == null);
                if (adminUser != null)
                {
                    adminUser.Employee = tech;
                    System.Diagnostics.Debug.WriteLine("[SEED] Linked admin user to tech employee.");
                }

                // Customers
                var cust1 = new OneManVanFSM.Shared.Models.Customer { Name = "Martha Chen", Type = OneManVanFSM.Shared.Models.CustomerType.Individual, PrimaryPhone = "(555) 111-2222", SecondaryPhone = "(555) 111-2223", PrimaryEmail = "martha.chen@email.com", PreferredContactMethod = "Email", ReferralSource = "Word of Mouth", Address = "123 Oak St", City = "Springfield", State = "IL", Zip = "62704", SinceDate = today.AddYears(-2), Tags = "[\"VIP\",\"Warranty Customer\"]", BalanceOwed = 324m };
                var cust2 = new OneManVanFSM.Shared.Models.Customer { Name = "Bob Reynolds", Type = OneManVanFSM.Shared.Models.CustomerType.Individual, PrimaryPhone = "(555) 222-3333", PrimaryEmail = "breynolds@email.com", PreferredContactMethod = "Phone", ReferralSource = "Google", Address = "456 Maple Ave", City = "Springfield", State = "IL", Zip = "62701", SinceDate = today.AddYears(-1), BalanceOwed = 450m };
                var cust3 = new OneManVanFSM.Shared.Models.Customer { Name = "Heritage Oaks HOA", Type = OneManVanFSM.Shared.Models.CustomerType.Company, PrimaryPhone = "(555) 333-4444", PrimaryEmail = "board@heritageoaks.org", PreferredContactMethod = "Email", ReferralSource = "Angi", Address = "100 Heritage Blvd", City = "Springfield", State = "IL", Zip = "62711", SinceDate = today.AddYears(-3), TaxExempt = true, Tags = "[\"Commercial\",\"Service Agreement\"]" };
                var cust4 = new OneManVanFSM.Shared.Models.Customer { Name = "Linda Parker", Type = OneManVanFSM.Shared.Models.CustomerType.Landlord, PrimaryPhone = "(555) 555-6666", SecondaryPhone = "(555) 555-6667", PrimaryEmail = "lparker@rentals.com", PreferredContactMethod = "Text", ReferralSource = "Repeat Customer", Address = "200 Pine St", City = "Springfield", State = "IL", Zip = "62703", SinceDate = today.AddMonths(-3), Tags = "[\"Landlord\",\"Multi-Site\"]" };
                db.Customers.AddRange(cust1, cust2, cust3, cust4);

                // Companies
                var comp1 = new OneManVanFSM.Shared.Models.Company { Name = "Heritage Oaks HOA", Type = OneManVanFSM.Shared.Models.CompanyType.Customer, Phone = "(555) 333-4444", Email = "board@heritageoaks.org", Address = "100 Heritage Blvd", City = "Springfield", State = "IL", Zip = "62711", Industry = "Property Management", IsActive = true };
                var comp2 = new OneManVanFSM.Shared.Models.Company { Name = "Parker Rental Properties", Type = OneManVanFSM.Shared.Models.CompanyType.Customer, Phone = "(555) 555-6666", Email = "lparker@rentals.com", Address = "200 Pine St", City = "Springfield", State = "IL", Zip = "62703", Industry = "Real Estate", IsActive = true };
                db.Companies.AddRange(comp1, comp2);

                // Link customers and sites to companies
                cust3.Company = comp1;
                cust4.Company = comp2;

                // Sites
                var site1 = new OneManVanFSM.Shared.Models.Site { Name = "Chen Residence", Address = "123 Oak St", City = "Springfield", State = "IL", Zip = "62704", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Residential, SqFt = 2200, Zones = 2, Stories = 2, EquipmentLocation = "Basement", Customer = cust1, GasLineLocation = "Left side of house near meter", ElectricalPanelLocation = "Basement \u2014 east wall", WaterShutoffLocation = "Basement \u2014 near hot water heater", HeatingFuelSource = "Natural Gas", YearBuilt = 1998, HasAtticAccess = true, HasCrawlSpace = false, HasBasement = true };
                var site2 = new OneManVanFSM.Shared.Models.Site { Name = "Reynolds Home", Address = "456 Maple Ave", City = "Springfield", State = "IL", Zip = "62701", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Residential, SqFt = 1800, Zones = 1, Stories = 1, EquipmentLocation = "Attic", Customer = cust2, GasLineLocation = "Rear of house", ElectricalPanelLocation = "Garage", WaterShutoffLocation = "Under kitchen sink", HeatingFuelSource = "Natural Gas", YearBuilt = 2005, HasAtticAccess = true, HasCrawlSpace = true, HasBasement = false, Notes = "Attic ladder loose \u2014 safety concern" };
                var site3 = new OneManVanFSM.Shared.Models.Site { Name = "Heritage Oaks Clubhouse", Address = "100 Heritage Blvd", City = "Springfield", State = "IL", Zip = "62711", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Commercial, SqFt = 5000, Zones = 4, Stories = 1, Customer = cust3, Company = comp1, AccessCodes = "Gate: 4521#", EquipmentLocation = "Rooftop", ElectricalPanelLocation = "Utility closet \u2014 main hall", HeatingFuelSource = "Electric", YearBuilt = 2010 };
                var site4 = new OneManVanFSM.Shared.Models.Site { Name = "Parker Rental Unit A", Address = "201 Pine St", City = "Springfield", State = "IL", Zip = "62703", PropertyType = OneManVanFSM.Shared.Models.PropertyType.Residential, SqFt = 1100, Zones = 1, Stories = 1, Customer = cust4, Company = comp2, AccessCodes = "Lock box: 9876", HeatingFuelSource = "Propane", YearBuilt = 1992, HasCrawlSpace = true, HasBasement = false };
                db.Sites.AddRange(site1, site2, site3, site4);

                // Assets
                var asset1 = new OneManVanFSM.Shared.Models.Asset { Name = "Carrier 3-Ton AC", AssetType = "AC Unit", Brand = "Carrier", Model = "24ACC636A003", SerialNumber = "SN-AC-4421", Tonnage = 3m, SEER = 16m, BTURating = 36000, FuelType = "Electric", UnitConfiguration = "Split", FilterSize = "20x25x4", Voltage = "240V", Phase = "Single Phase", LocationOnSite = "Side Yard \u2014 South", RefrigerantType = "R-410A", RefrigerantQuantity = 6.2m, ManufactureDate = today.AddYears(-5), InstallDate = today.AddYears(-4), WarrantyStartDate = today.AddYears(-4), WarrantyTermYears = 5, WarrantyExpiry = today.AddYears(1), Status = OneManVanFSM.Shared.Models.AssetStatus.Active, Value = 4200m, Customer = cust1, Site = site1 };
                var asset2 = new OneManVanFSM.Shared.Models.Asset { Name = "Trane XV80 Furnace", AssetType = "Furnace", Brand = "Trane", Model = "TUD2B060A9V3VB", SerialNumber = "SN-FURN-7782", BTURating = 80000, AFUE = 80m, FuelType = "Natural Gas", UnitConfiguration = "Split", FilterSize = "16x25x1", Voltage = "120V", Phase = "Single Phase", LocationOnSite = "Basement", ManufactureDate = today.AddYears(-6), InstallDate = today.AddYears(-5), WarrantyStartDate = today.AddYears(-5), WarrantyTermYears = 5, WarrantyExpiry = today.AddMonths(-2), Status = OneManVanFSM.Shared.Models.AssetStatus.Active, Value = 2800m, Customer = cust1, Site = site1, Notes = "Warranty recently expired" };
                var asset3 = new OneManVanFSM.Shared.Models.Asset { Name = "Lennox XC21 AC", AssetType = "AC Unit", Brand = "Lennox", Model = "XC21-036-230", SerialNumber = "SN-AC-9931", Tonnage = 3m, SEER = 21m, BTURating = 36000, FuelType = "Electric", UnitConfiguration = "Split", FilterSize = "20x20x4", Voltage = "240V", Phase = "Single Phase", LocationOnSite = "Backyard \u2014 Concrete Pad", RefrigerantType = "R-410A", RefrigerantQuantity = 7.1m, ManufactureDate = today.AddYears(-2).AddMonths(-3), InstallDate = today.AddYears(-2), WarrantyStartDate = today.AddYears(-2), WarrantyTermYears = 5, WarrantyExpiry = today.AddYears(3), Status = OneManVanFSM.Shared.Models.AssetStatus.Active, Value = 5500m, Customer = cust2, Site = site2 };
                db.Assets.AddRange(asset1, asset2, asset3);

                // Jobs (assigned to this tech)
                var job1 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0041", Title = "AC Repair - Low Refrigerant", Description = "Customer reports warm air. Check refrigerant levels.", Status = OneManVanFSM.Shared.Models.JobStatus.Completed, Priority = OneManVanFSM.Shared.Models.JobPriority.High, TradeType = "HVAC", JobType = "Repair", SystemType = "Split System", ScheduledDate = today.AddDays(-1), ScheduledTime = new TimeSpan(9, 0, 0), EstimatedDuration = 2.5m, EstimatedTotal = 350m, ActualDuration = 2.5m, ActualTotal = 324m, CompletedDate = today.AddDays(-1), Customer = cust1, Site = site1, AssignedEmployee = tech };
                var job2 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0042", Title = "Thermostat Replacement", Description = "Replace old mercury thermostat with Honeywell T6 Pro.", Status = OneManVanFSM.Shared.Models.JobStatus.Scheduled, Priority = OneManVanFSM.Shared.Models.JobPriority.Standard, TradeType = "HVAC", JobType = "Install", ScheduledDate = today, ScheduledTime = new TimeSpan(10, 30, 0), EstimatedDuration = 1m, EstimatedTotal = 245m, Customer = cust2, Site = site2, AssignedEmployee = tech };
                var job3 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0043", Title = "Seasonal Maintenance - Clubhouse", Description = "Spring maintenance: inspect, replace filters, check refrigerant.", Status = OneManVanFSM.Shared.Models.JobStatus.Scheduled, Priority = OneManVanFSM.Shared.Models.JobPriority.Standard, TradeType = "HVAC", JobType = "Maintenance", SystemType = "Commercial RTU", ScheduledDate = today.AddDays(1), ScheduledTime = new TimeSpan(8, 0, 0), EstimatedDuration = 3m, EstimatedTotal = 450m, Customer = cust3, Site = site3, AssignedEmployee = tech };
                var job4 = new OneManVanFSM.Shared.Models.Job { JobNumber = "J-2025-0044", Title = "Emergency - No Heat", Description = "Furnace not igniting. Tenant reports no heat.", Status = OneManVanFSM.Shared.Models.JobStatus.EnRoute, Priority = OneManVanFSM.Shared.Models.JobPriority.Emergency, TradeType = "HVAC", JobType = "Repair", ScheduledDate = today, ScheduledTime = new TimeSpan(7, 0, 0), EstimatedDuration = 2m, EstimatedTotal = 500m, Customer = cust4, Site = site4, AssignedEmployee = tech };
                db.Jobs.AddRange(job1, job2, job3, job4);

                // Time Entries (completed job yesterday + today partial)
                var te1 = new OneManVanFSM.Shared.Models.TimeEntry
                {
                    Employee = tech,
                    Job = job1,
                    StartTime = today.AddDays(-1).AddHours(9),
                    EndTime = today.AddDays(-1).AddHours(11).AddMinutes(30),
                    Hours = 2.5m,
                    IsBillable = true,
                    TimeCategory = "On-Site",
                    Notes = "Diagnosed low refrigerant, recharged system",
                };
                var te2 = new OneManVanFSM.Shared.Models.TimeEntry
                {
                    Employee = tech,
                    Job = job1,
                    StartTime = today.AddDays(-1).AddHours(8).AddMinutes(30),
                    EndTime = today.AddDays(-1).AddHours(9),
                    Hours = 0.5m,
                    IsBillable = false,
                    TimeCategory = "Travel",
                    Notes = "Drive to Chen residence",
                };
                var te3 = new OneManVanFSM.Shared.Models.TimeEntry
                {
                    Employee = tech,
                    Job = job4,
                    StartTime = today.AddHours(7),
                    EndTime = today.AddHours(8).AddMinutes(45),
                    Hours = 1.75m,
                    IsBillable = true,
                    TimeCategory = "On-Site",
                    Notes = "Emergency furnace no-heat call",
                };
                var te4 = new OneManVanFSM.Shared.Models.TimeEntry
                {
                    Employee = tech,
                    StartTime = today.AddDays(-2).AddHours(8),
                    EndTime = today.AddDays(-2).AddHours(16).AddMinutes(30),
                    Hours = 8.5m,
                    IsBillable = true,
                    TimeCategory = "On-Site",
                    Notes = "Full day — maintenance rounds",
                };
                var te5 = new OneManVanFSM.Shared.Models.TimeEntry
                {
                    Employee = tech,
                    StartTime = today.AddDays(-3).AddHours(9),
                    EndTime = today.AddDays(-3).AddHours(17),
                    Hours = 8m,
                    IsBillable = true,
                    TimeCategory = "On-Site",
                    Notes = "Service calls (3 jobs)",
                };
                db.TimeEntries.AddRange(te1, te2, te3, te4, te5);

                // Quick Notes
                var qn1 = new OneManVanFSM.Shared.Models.QuickNote
                {
                    Title = "Refrigerant leak at condenser",
                    Text = "Found small leak at service valve on Chen AC. Recharged 1.5 lbs R-410A. Recommend follow-up in 30 days to verify pressure holds.",
                    Category = "Repair",
                    EntityType = "Job",
                    EntityId = 1,
                    Job = job1,
                    CreatedByEmployee = tech,
                    Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Active,
                    IsUrgent = false,
                    CreatedAt = today.AddDays(-1).AddHours(11),
                };
                var qn2 = new OneManVanFSM.Shared.Models.QuickNote
                {
                    Title = "Igniter replacement needed",
                    Text = "Parker furnace — hot surface igniter cracked. Replaced with Honeywell Q3400A. Original part was OEM Goodman. Noted corrosion on flame sensor too.",
                    Category = "Repair",
                    EntityType = "Job",
                    EntityId = 4,
                    Job = job4,
                    CreatedByEmployee = tech,
                    Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Active,
                    IsUrgent = true,
                    CreatedAt = today.AddHours(8),
                };
                var qn3 = new OneManVanFSM.Shared.Models.QuickNote
                {
                    Title = "Heritage Oaks access reminder",
                    Text = "Gate code changed to 4521#. Previous code 1234# no longer works. Updated on site record.",
                    Category = "General",
                    CreatedByEmployee = tech,
                    Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Active,
                    IsUrgent = false,
                    CreatedAt = today.AddDays(-2).AddHours(14),
                };
                var qn4 = new OneManVanFSM.Shared.Models.QuickNote
                {
                    Title = "Safety concern - Reynolds attic",
                    Text = "Attic access ladder is loose at Reynolds home. Almost slipped. Notify customer to repair before next visit. Document for safety records.",
                    Category = "Safety",
                    CreatedByEmployee = tech,
                    Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Active,
                    IsUrgent = true,
                    CreatedAt = today.AddDays(-3).AddHours(10),
                };
                var qn5 = new OneManVanFSM.Shared.Models.QuickNote
                {
                    Title = "Follow-up: Chen filter order",
                    Text = "Martha Chen requested quote for annual filter delivery. Carrier 20x25x4 MERV 13. Check pricing with supplier.",
                    Category = "Follow-Up",
                    EntityType = "Customer",
                    EntityId = 1,
                    Customer = cust1,
                    CreatedByEmployee = tech,
                    Status = OneManVanFSM.Shared.Models.QuickNoteStatus.Draft,
                    IsUrgent = false,
                    CreatedAt = today.AddDays(-1).AddHours(12),
                };
                db.QuickNotes.AddRange(qn1, qn2, qn3, qn4, qn5);

                // Documents
                var doc1 = new OneManVanFSM.Shared.Models.Document
                {
                    Name = "Carrier AC Install Manual",
                    Category = OneManVanFSM.Shared.Models.DocumentCategory.Manual,
                    FileType = "PDF",
                    FileSize = 2_450_000,
                    Job = job1,
                    Site = site1,
                    UploadedByEmployee = tech,
                    UploadDate = today.AddDays(-1),
                    Notes = "Reference manual for 24ACC636A003 unit",
                };
                var doc2 = new OneManVanFSM.Shared.Models.Document
                {
                    Name = "Chen AC Warranty Card",
                    Category = OneManVanFSM.Shared.Models.DocumentCategory.WarrantyPrintout,
                    FileType = "Image",
                    FileSize = 850_000,
                    Asset = asset1,
                    Customer = cust1,
                    Site = site1,
                    UploadedByEmployee = tech,
                    UploadDate = today.AddDays(-1),
                };
                var doc3 = new OneManVanFSM.Shared.Models.Document
                {
                    Name = "Honeywell T6 Pro Setup Guide",
                    Category = OneManVanFSM.Shared.Models.DocumentCategory.SetupGuide,
                    FileType = "PDF",
                    FileSize = 1_200_000,
                    Job = job2,
                    UploadedByEmployee = tech,
                    UploadDate = today,
                };
                var doc4 = new OneManVanFSM.Shared.Models.Document
                {
                    Name = "EPA 608 Universal Certificate",
                    Category = OneManVanFSM.Shared.Models.DocumentCategory.Certification,
                    FileType = "PDF",
                    FileSize = 350_000,
                    Employee = tech,
                    UploadedByEmployee = tech,
                    UploadDate = today.AddMonths(-6),
                };
                var doc5 = new OneManVanFSM.Shared.Models.Document
                {
                    Name = "Heritage Oaks Service Agreement",
                    Category = OneManVanFSM.Shared.Models.DocumentCategory.Other,
                    FileType = "PDF",
                    FileSize = 980_000,
                    Customer = cust3,
                    Site = site3,
                    UploadedByEmployee = tech,
                    UploadDate = today.AddDays(-10),
                    Notes = "Annual maintenance agreement for clubhouse HVAC",
                };
                db.Documents.AddRange(doc1, doc2, doc3, doc4, doc5);

                // Material List for maintenance job (job3)
                var matList = new OneManVanFSM.Shared.Models.MaterialList
                {
                    Name = "Clubhouse Seasonal Maintenance",
                    Customer = cust3,
                    Site = site3,
                    Subtotal = 187.50m,
                    MarkupPercent = 15m,
                    TaxPercent = 8.25m,
                    Total = 233.18m,
                    Notes = "Standard spring maintenance materials",
                };
                db.MaterialLists.Add(matList);
                db.SaveChanges(); // flush to get matList.Id

                job3.MaterialListId = matList.Id;

                var matItems = new[]
                {
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Filters", ItemName = "20x25x4 MERV 13 Filter", Quantity = 4, Unit = "ea", BaseCost = 22.50m },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Filters", ItemName = "16x20x1 MERV 8 Filter", Quantity = 2, Unit = "ea", BaseCost = 8.00m },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Refrigerant", ItemName = "R-410A Refrigerant", Quantity = 5, Unit = "lbs", BaseCost = 12.50m },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Electrical", ItemName = "Capacitor 45/5 MFD", Quantity = 1, Unit = "ea", BaseCost = 18.00m, Notes = "Preventive replacement" },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Sealing", ItemName = "Mastic Sealant", Quantity = 1, Unit = "tube", BaseCost = 9.50m },
                new OneManVanFSM.Shared.Models.MaterialListItem { MaterialListId = matList.Id, Section = "Sealing", ItemName = "Foil Tape (UL 181)", Quantity = 1, Unit = "roll", BaseCost = 7.00m },
            };
                db.MaterialListItems.AddRange(matItems);

                // Calendar Events
                var cal1 = new OneManVanFSM.Shared.Models.CalendarEvent
                {
                    Title = "Team Safety Meeting",
                    StartDateTime = today.AddDays(2).AddHours(8),
                    EndDateTime = today.AddDays(2).AddHours(9),
                    Duration = 1m,
                    Status = OneManVanFSM.Shared.Models.CalendarEventStatus.Confirmed,
                    EventType = "Meeting",
                    Color = "#6f42c1",
                    Employee = tech,
                    Notes = "Monthly safety briefing — PPE review",
                };
                var cal2 = new OneManVanFSM.Shared.Models.CalendarEvent
                {
                    Title = "NATE Certification Renewal",
                    StartDateTime = today.AddDays(14).AddHours(9),
                    EndDateTime = today.AddDays(14).AddHours(12),
                    Duration = 3m,
                    Status = OneManVanFSM.Shared.Models.CalendarEventStatus.Tentative,
                    EventType = "Training",
                    Color = "#fd7e14",
                    Employee = tech,
                    Notes = "Online renewal exam",
                };
                var cal3 = new OneManVanFSM.Shared.Models.CalendarEvent
                {
                    Title = "Van Service Appointment",
                    StartDateTime = today.AddDays(5).AddHours(7),
                    EndDateTime = today.AddDays(5).AddHours(9),
                    Duration = 2m,
                    Status = OneManVanFSM.Shared.Models.CalendarEventStatus.Confirmed,
                    EventType = "Personal",
                    Color = "#20c997",
                    Employee = tech,
                    Notes = "Oil change + tire rotation at Fleet Service Center",
                };
                var cal4 = new OneManVanFSM.Shared.Models.CalendarEvent
                {
                    Title = "Heritage Oaks Follow-Up Inspection",
                    StartDateTime = today.AddDays(7).AddHours(10),
                    EndDateTime = today.AddDays(7).AddHours(11).AddMinutes(30),
                    Duration = 1.5m,
                    Status = OneManVanFSM.Shared.Models.CalendarEventStatus.Tentative,
                    EventType = "Job",
                    Color = "#0d6efd",
                    Employee = tech,
                    Job = job3,
                    Notes = "Post-maintenance inspection",
                };
                db.CalendarEvents.AddRange(cal1, cal2, cal3, cal4);

                // JobAsset links (connect assets to jobs they were serviced in)
                var ja1 = new OneManVanFSM.Shared.Models.JobAsset
                {
                    Job = job1,
                    Asset = asset1,
                    Role = "Serviced",
                    Notes = "Recharged refrigerant, checked pressures",
                };
                var ja2 = new OneManVanFSM.Shared.Models.JobAsset
                {
                    Job = job1,
                    Asset = asset2,
                    Role = "Inspected",
                    Notes = "Verified furnace operation during AC service",
                };
                var ja3 = new OneManVanFSM.Shared.Models.JobAsset
                {
                    Job = job2,
                    Asset = asset3,
                    Role = "Serviced",
                    Notes = "Thermostat replacement affects AC system",
                };
                db.JobAssets.AddRange(ja1, ja2, ja3);

                // AssetServiceLog entries (service history)
                var sl1 = new OneManVanFSM.Shared.Models.AssetServiceLog
                {
                    Asset = asset1,
                    ServiceType = "Refrigerant Charge",
                    ServiceDate = today.AddDays(-1),
                    PerformedBy = "Mike Johnson",
                    Notes = "Recharged 1.5 lbs R-410A. Pressures: Suction 118 psi, Discharge 340 psi. Hold test recommended in 30 days.",
                    Cost = 185m,
                    NextDueDate = today.AddDays(29),
                };
                var sl2 = new OneManVanFSM.Shared.Models.AssetServiceLog
                {
                    Asset = asset1,
                    ServiceType = "Filter Change",
                    ServiceDate = today.AddMonths(-3),
                    PerformedBy = "Mike Johnson",
                    Notes = "Replaced 20x25x4 MERV 13 filter. Old filter was heavily loaded.",
                    Cost = 22.50m,
                    NextDueDate = today,
                };
                var sl3 = new OneManVanFSM.Shared.Models.AssetServiceLog
                {
                    Asset = asset1,
                    ServiceType = "Tune-Up",
                    ServiceDate = today.AddMonths(-6),
                    PerformedBy = "Mike Johnson",
                    Notes = "Annual spring tune-up. Cleaned condenser coil, checked capacitor, lubricated fan motor. All within spec.",
                    Cost = 125m,
                    NextDueDate = today.AddMonths(6),
                };
                var sl4 = new OneManVanFSM.Shared.Models.AssetServiceLog
                {
                    Asset = asset2,
                    ServiceType = "Inspection",
                    ServiceDate = today.AddDays(-1),
                    PerformedBy = "Mike Johnson",
                    Notes = "Visual inspection during AC call. Furnace operational. Noted warranty recently expired — recommend extended coverage.",
                };
                var sl5 = new OneManVanFSM.Shared.Models.AssetServiceLog
                {
                    Asset = asset2,
                    ServiceType = "Filter Change",
                    ServiceDate = today.AddMonths(-2),
                    PerformedBy = "Mike Johnson",
                    Notes = "Replaced standard 1\" filter with MERV 11.",
                    Cost = 12m,
                    NextDueDate = today.AddMonths(1),
                };
                var sl6 = new OneManVanFSM.Shared.Models.AssetServiceLog
                {
                    Asset = asset3,
                    ServiceType = "Tune-Up",
                    ServiceDate = today.AddMonths(-4),
                    PerformedBy = "Mike Johnson",
                    Notes = "Seasonal maintenance. System running excellent — 21 SEER verified. Cleaned outdoor unit.",
                    Cost = 135m,
                    NextDueDate = today.AddMonths(8),
                };
                db.AssetServiceLogs.AddRange(sl1, sl2, sl3, sl4, sl5, sl6);

                // Update asset LastServiceDate / NextServiceDue from seed logs
                asset1.LastServiceDate = today.AddDays(-1);
                asset1.NextServiceDue = today; // filter change due
                asset2.LastServiceDate = today.AddDays(-1);
                asset2.NextServiceDue = today.AddMonths(1);
                asset3.LastServiceDate = today.AddMonths(-4);
                asset3.NextServiceDue = today.AddMonths(8);

                // Inventory Items (truck stock + warehouse)
                var inv1 = new OneManVanFSM.Shared.Models.InventoryItem
                {
                    Name = "20x25x4 MERV 13 Filter",
                    SKU = "FLT-20254-M13",
                    ShelfBin = "Truck Drawer 1",
                    Location = OneManVanFSM.Shared.Models.InventoryLocation.Truck,
                    Quantity = 3,
                    MinThreshold = 2,
                    MaxCapacity = 8,
                    Cost = 22.50m,
                    Price = 35.00m,
                    MarkupPercent = 55m,
                    PreferredSupplier = "FilterDirect Supply",
                    LastRestockedDate = today.AddDays(-5),
                    Notes = "Carrier compatible — most common residential size",
                };
                var inv2 = new OneManVanFSM.Shared.Models.InventoryItem
                {
                    Name = "R-410A Refrigerant",
                    SKU = "REF-410A-25",
                    ShelfBin = "Truck Compartment B",
                    Location = OneManVanFSM.Shared.Models.InventoryLocation.Truck,
                    Quantity = 12,
                    MinThreshold = 5,
                    MaxCapacity = 25,
                    Cost = 12.50m,
                    Price = 28.00m,
                    MarkupPercent = 124m,
                    PreferredSupplier = "Johnstone Supply",
                    LotNumber = "R410-2025-04",
                    ExpiryDate = today.AddYears(3),
                    LastRestockedDate = today.AddDays(-10),
                    Notes = "Per-pound pricing. Keep cylinder upright.",
                };
                var inv3 = new OneManVanFSM.Shared.Models.InventoryItem
                {
                    Name = "Capacitor 45/5 MFD 440V",
                    SKU = "CAP-455-440",
                    ShelfBin = "Truck Drawer 3",
                    Location = OneManVanFSM.Shared.Models.InventoryLocation.Truck,
                    Quantity = 2,
                    MinThreshold = 2,
                    MaxCapacity = 6,
                    Cost = 18.00m,
                    Price = 45.00m,
                    MarkupPercent = 150m,
                    PreferredSupplier = "Johnstone Supply",
                    LastRestockedDate = today.AddDays(-14),
                    Notes = "Most common dual-run capacitor for residential AC",
                };
                var inv4 = new OneManVanFSM.Shared.Models.InventoryItem
                {
                    Name = "Honeywell Hot Surface Igniter",
                    SKU = "IGN-Q3400A",
                    ShelfBin = "Truck Drawer 4",
                    Location = OneManVanFSM.Shared.Models.InventoryLocation.Truck,
                    Quantity = 1,
                    MinThreshold = 2,
                    MaxCapacity = 4,
                    Cost = 24.00m,
                    Price = 55.00m,
                    MarkupPercent = 129m,
                    PreferredSupplier = "Johnstone Supply",
                    LastRestockedDate = today.AddDays(-30),
                    Notes = "Universal replacement — fits most Goodman/Rheem",
                };
                var inv5 = new OneManVanFSM.Shared.Models.InventoryItem
                {
                    Name = "Foil Tape UL 181",
                    SKU = "TAPE-FOIL-181",
                    ShelfBin = "Truck Bin A",
                    Location = OneManVanFSM.Shared.Models.InventoryLocation.Truck,
                    Quantity = 4,
                    MinThreshold = 2,
                    MaxCapacity = 10,
                    Cost = 7.00m,
                    Price = 12.00m,
                    MarkupPercent = 71m,
                    PreferredSupplier = "Home Depot Pro",
                    LastRestockedDate = today.AddDays(-7),
                };
                var inv6 = new OneManVanFSM.Shared.Models.InventoryItem
                {
                    Name = "16x20x1 MERV 8 Filter",
                    SKU = "FLT-16201-M8",
                    ShelfBin = "Shelf B2",
                    Location = OneManVanFSM.Shared.Models.InventoryLocation.Warehouse,
                    Quantity = 12,
                    MinThreshold = 6,
                    MaxCapacity = 24,
                    Cost = 8.00m,
                    Price = 15.00m,
                    MarkupPercent = 87m,
                    PreferredSupplier = "FilterDirect Supply",
                    LastRestockedDate = today.AddDays(-3),
                };
                var inv7 = new OneManVanFSM.Shared.Models.InventoryItem
                {
                    Name = "Mastic Sealant",
                    SKU = "SEAL-MAST-10",
                    ShelfBin = "Shelf C1",
                    Location = OneManVanFSM.Shared.Models.InventoryLocation.Warehouse,
                    Quantity = 6,
                    MinThreshold = 3,
                    MaxCapacity = 12,
                    Cost = 9.50m,
                    Price = 16.00m,
                    MarkupPercent = 68m,
                    PreferredSupplier = "Home Depot Pro",
                    LastRestockedDate = today.AddDays(-12),
                };
                var inv8 = new OneManVanFSM.Shared.Models.InventoryItem
                {
                    Name = "Thermostat Wire 18/5 (250ft)",
                    SKU = "WIRE-185-250",
                    ShelfBin = "Truck Compartment C",
                    Location = OneManVanFSM.Shared.Models.InventoryLocation.Truck,
                    Quantity = 1,
                    MinThreshold = 1,
                    MaxCapacity = 3,
                    Cost = 65.00m,
                    Price = 0.75m,
                    MarkupPercent = 0m,
                    PreferredSupplier = "Johnstone Supply",
                    LastRestockedDate = today.AddDays(-20),
                    Notes = "Per-foot billing to customer. Truck carries 1 spool.",
                };
                db.InventoryItems.AddRange(inv1, inv2, inv3, inv4, inv5, inv6, inv7, inv8);

                // Estimates
                var est1 = new OneManVanFSM.Shared.Models.Estimate
                {
                    EstimateNumber = "EST-2025-001",
                    Title = "AC System Replacement — Chen Residence",
                    Status = OneManVanFSM.Shared.Models.EstimateStatus.Sent,
                    Priority = OneManVanFSM.Shared.Models.JobPriority.Standard,
                    TradeType = "HVAC",
                    SystemType = "Split System AC",
                    SqFt = 2200,
                    Zones = 1,
                    Stories = 2,
                    ExpiryDate = today.AddDays(30),
                    PricingMethod = OneManVanFSM.Shared.Models.PricingMethod.FlatRate,
                    Subtotal = 6850.00m,
                    MarkupPercent = 0m,
                    TaxPercent = 8.25m,
                    Total = 7415.13m,
                    DepositRequired = 2000m,
                    DepositReceived = false,
                    Customer = cust1,
                    Site = site1,
                    Notes = "Customer considering upgrade from 14 SEER to 18 SEER unit",
                    CreatedAt = today.AddDays(-3),
                };
                var est2 = new OneManVanFSM.Shared.Models.Estimate
                {
                    EstimateNumber = "EST-2025-002",
                    Title = "Ductwork Repair — Heritage Oaks Clubhouse",
                    Status = OneManVanFSM.Shared.Models.EstimateStatus.Approved,
                    Priority = OneManVanFSM.Shared.Models.JobPriority.High,
                    TradeType = "HVAC",
                    SystemType = "Commercial Ducted",
                    SqFt = 4500,
                    Zones = 3,
                    Stories = 1,
                    ExpiryDate = today.AddDays(14),
                    PricingMethod = OneManVanFSM.Shared.Models.PricingMethod.TimeBased,
                    Subtotal = 3200.00m,
                    MarkupPercent = 10m,
                    TaxPercent = 8.25m,
                    Total = 3808.80m,
                    DepositRequired = 1000m,
                    DepositReceived = true,
                    Customer = cust3,
                    Site = site3,
                    Notes = "Approved by board. Schedule for next available window.",
                    CreatedAt = today.AddDays(-7),
                };
                var est3 = new OneManVanFSM.Shared.Models.Estimate
                {
                    EstimateNumber = "EST-2025-003",
                    Title = "Furnace Tune-Up + Safety Inspection — Parker",
                    Status = OneManVanFSM.Shared.Models.EstimateStatus.Draft,
                    Priority = OneManVanFSM.Shared.Models.JobPriority.Low,
                    TradeType = "HVAC",
                    SystemType = "Gas Furnace",
                    SqFt = 1800,
                    Zones = 1,
                    Stories = 1,
                    PricingMethod = OneManVanFSM.Shared.Models.PricingMethod.FlatRate,
                    Subtotal = 189.00m,
                    MarkupPercent = 0m,
                    TaxPercent = 8.25m,
                    Total = 204.59m,
                    Customer = cust4,
                    Site = site4,
                    Notes = "Annual maintenance package offer",
                    CreatedAt = today.AddDays(-1),
                };
                db.Estimates.AddRange(est1, est2, est3);
                db.SaveChanges(); // flush to get estimate IDs

                // Estimate Lines
                var estLines = new[]
                {
                // EST-2025-001 lines
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Equipment", Description = "Carrier 24ACC636A003 — 3-Ton 16 SEER AC Unit", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 3200.00m, LineTotal = 3200.00m, SortOrder = 1 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Equipment", Description = "Carrier Matching Evaporator Coil", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 850.00m, LineTotal = 850.00m, SortOrder = 2 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Labor", Description = "AC System Removal + Install (2 techs)", LineType = "Labor", Unit = "Hour", Quantity = 8, UnitPrice = 125.00m, LineTotal = 1000.00m, SortOrder = 3 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Labor", Description = "Electrical Disconnect + Reconnect", LineType = "Labor", Unit = "Hour", Quantity = 2, UnitPrice = 125.00m, LineTotal = 250.00m, SortOrder = 4 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Materials", Description = "Refrigerant R-410A Charge", LineType = "Material", Unit = "Lbs", Quantity = 8, UnitPrice = 28.00m, LineTotal = 224.00m, SortOrder = 5 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Materials", Description = "Line Set + Fittings", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 185.00m, LineTotal = 185.00m, SortOrder = 6 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Materials", Description = "Concrete Pad", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 95.00m, LineTotal = 95.00m, SortOrder = 7 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Fees", Description = "Permit + Inspection Fee", LineType = "Fee", Unit = "Each", Quantity = 1, UnitPrice = 350.00m, LineTotal = 350.00m, SortOrder = 8 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Fees", Description = "Haul-Away Old Unit", LineType = "Fee", Unit = "Each", Quantity = 1, UnitPrice = 150.00m, LineTotal = 150.00m, SortOrder = 9 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est1.Id, Section = "Warranty", Description = "Extended 10-Year Parts + Labor Warranty", LineType = "Fee", Unit = "Each", Quantity = 1, UnitPrice = 496.00m, LineTotal = 496.00m, SortOrder = 10 },

                // EST-2025-002 lines
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Labor", Description = "Ductwork Inspection + Leak Testing", LineType = "Labor", Unit = "Hour", Quantity = 3, UnitPrice = 125.00m, LineTotal = 375.00m, SortOrder = 1 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Labor", Description = "Duct Sealing + Repair", LineType = "Labor", Unit = "Hour", Quantity = 6, UnitPrice = 125.00m, LineTotal = 750.00m, SortOrder = 2 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Labor", Description = "Insulation Replacement (Damaged Sections)", LineType = "Labor", Unit = "Hour", Quantity = 4, UnitPrice = 125.00m, LineTotal = 500.00m, SortOrder = 3 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Materials", Description = "Mastic Sealant", LineType = "Material", Unit = "Tube", Quantity = 8, UnitPrice = 16.00m, LineTotal = 128.00m, SortOrder = 4 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Materials", Description = "Foil Tape UL 181", LineType = "Material", Unit = "Roll", Quantity = 6, UnitPrice = 12.00m, LineTotal = 72.00m, SortOrder = 5 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Materials", Description = "R-8 Duct Insulation (50ft roll)", LineType = "Material", Unit = "Roll", Quantity = 3, UnitPrice = 145.00m, LineTotal = 435.00m, SortOrder = 6 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Materials", Description = "Sheet Metal Patches", LineType = "Material", Unit = "Each", Quantity = 4, UnitPrice = 35.00m, LineTotal = 140.00m, SortOrder = 7 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Testing", Description = "Post-Repair Pressure Test + Airflow Balance", LineType = "Labor", Unit = "Hour", Quantity = 4, UnitPrice = 125.00m, LineTotal = 500.00m, SortOrder = 8 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est2.Id, Section = "Testing", Description = "Duct Blaster Rental", LineType = "Equipment", Unit = "Day", Quantity = 1, UnitPrice = 300.00m, LineTotal = 300.00m, SortOrder = 9 },

                // EST-2025-003 lines
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est3.Id, Section = "Labor", Description = "Furnace Tune-Up (Standard)", LineType = "Labor", Unit = "Each", Quantity = 1, UnitPrice = 129.00m, LineTotal = 129.00m, SortOrder = 1 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est3.Id, Section = "Labor", Description = "Carbon Monoxide Safety Inspection", LineType = "Labor", Unit = "Each", Quantity = 1, UnitPrice = 45.00m, LineTotal = 45.00m, SortOrder = 2 },
                new OneManVanFSM.Shared.Models.EstimateLine { EstimateId = est3.Id, Section = "Materials", Description = "Standard 1\" MERV 8 Filter", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 15.00m, LineTotal = 15.00m, SortOrder = 3 },
            };
                db.EstimateLines.AddRange(estLines);

                // Service Agreements
                var sa1 = new OneManVanFSM.Shared.Models.ServiceAgreement
                {
                    AgreementNumber = "SA-2025-001",
                    Title = "Annual Maintenance — Chen Residence",
                    CoverageLevel = OneManVanFSM.Shared.Models.CoverageLevel.Premium,
                    StartDate = today.AddMonths(-6),
                    EndDate = today.AddMonths(6),
                    VisitsIncluded = 4,
                    VisitsUsed = 2,
                    Fee = 349.00m,
                    TradeType = "HVAC",
                    BillingFrequency = "Annual",
                    DiscountPercent = 10m,
                    RenewalDate = today.AddMonths(6),
                    AutoRenew = true,
                    Status = OneManVanFSM.Shared.Models.AgreementStatus.Active,
                    Customer = cust1,
                    Site = site1,
                    Notes = "Includes 2 AC tune-ups + 2 furnace tune-ups per year",
                };
                var sa2 = new OneManVanFSM.Shared.Models.ServiceAgreement
                {
                    AgreementNumber = "SA-2025-002",
                    Title = "Commercial HVAC Maintenance — Heritage Oaks",
                    CoverageLevel = OneManVanFSM.Shared.Models.CoverageLevel.Gold,
                    StartDate = today.AddMonths(-2),
                    EndDate = today.AddMonths(10),
                    VisitsIncluded = 6,
                    VisitsUsed = 1,
                    Fee = 1200.00m,
                    TradeType = "HVAC",
                    BillingFrequency = "Quarterly",
                    DiscountPercent = 15m,
                    RenewalDate = today.AddMonths(10),
                    AutoRenew = true,
                    Status = OneManVanFSM.Shared.Models.AgreementStatus.Active,
                    Customer = cust3,
                    Site = site3,
                    Notes = "Covers all RTU units in clubhouse. Priority scheduling included.",
                };
                var sa3 = new OneManVanFSM.Shared.Models.ServiceAgreement
                {
                    AgreementNumber = "SA-2024-008",
                    Title = "Basic Plan — Parker Rental",
                    CoverageLevel = OneManVanFSM.Shared.Models.CoverageLevel.Basic,
                    StartDate = today.AddMonths(-11),
                    EndDate = today.AddMonths(-1),
                    VisitsIncluded = 2,
                    VisitsUsed = 2,
                    Fee = 149.00m,
                    TradeType = "HVAC",
                    BillingFrequency = "Annual",
                    RenewalDate = today.AddMonths(-1),
                    AutoRenew = false,
                    Status = OneManVanFSM.Shared.Models.AgreementStatus.Expired,
                    Customer = cust4,
                    Site = site4,
                    Notes = "Expired — customer has not renewed yet",
                };
                db.ServiceAgreements.AddRange(sa1, sa2, sa3);

                // ServiceAgreementAsset links (which assets are covered by agreements)
                var saa1 = new OneManVanFSM.Shared.Models.ServiceAgreementAsset
                {
                    ServiceAgreement = sa1,
                    Asset = asset1,
                    CoverageNotes = "Full coverage — AC tune-ups, refrigerant top-off, coil cleaning",
                };
                var saa2 = new OneManVanFSM.Shared.Models.ServiceAgreementAsset
                {
                    ServiceAgreement = sa1,
                    Asset = asset2,
                    CoverageNotes = "Full coverage — furnace tune-ups, igniter check, heat exchanger inspection",
                };
                var saa3 = new OneManVanFSM.Shared.Models.ServiceAgreementAsset
                {
                    ServiceAgreement = sa2,
                    Asset = asset3,
                    CoverageNotes = "Included under commercial plan — seasonal maintenance visits",
                };
                db.ServiceAgreementAssets.AddRange(saa1, saa2, saa3);

                // Products (reference catalog for inventory/estimates)
                var prod1 = new OneManVanFSM.Shared.Models.Product { Name = "20x25x4 MERV 13 Filter", Category = "Filters", Cost = 22.50m, Price = 35m, MarkupPercent = 55m, Unit = "Each", SupplierName = "FilterDirect Supply" };
                var prod2 = new OneManVanFSM.Shared.Models.Product { Name = "R-410A Refrigerant (per lb)", Category = "Refrigerant", Cost = 12.50m, Price = 28m, MarkupPercent = 124m, Unit = "Lb", SupplierName = "Johnstone Supply" };
                var prod3 = new OneManVanFSM.Shared.Models.Product { Name = "Honeywell T6 Pro Thermostat", Category = "Controls", Cost = 85m, Price = 145m, MarkupPercent = 70m, Unit = "Each", SupplierName = "Johnstone Supply" };
                var prod4 = new OneManVanFSM.Shared.Models.Product { Name = "Capacitor 45/5 MFD 440V", Category = "Parts", Cost = 18m, Price = 45m, MarkupPercent = 150m, Unit = "Each", SupplierName = "Johnstone Supply" };
                db.Products.AddRange(prod1, prod2, prod3, prod4);

                // Invoices
                var inv_1 = new OneManVanFSM.Shared.Models.Invoice { InvoiceNumber = "INV-2025-0024", Status = OneManVanFSM.Shared.Models.InvoiceStatus.Overdue, InvoiceDate = today.AddDays(-15), DueDate = today.AddDays(-10), PaymentTerms = "Net 5", Subtotal = 300m, TaxAmount = 24m, Total = 324m, BalanceDue = 324m, Customer = cust1, Job = job1, Notes = "AC Repair — overdue" };
                var inv_2 = new OneManVanFSM.Shared.Models.Invoice { InvoiceNumber = "INV-2025-0025", Status = OneManVanFSM.Shared.Models.InvoiceStatus.Sent, InvoiceDate = today.AddDays(-7), DueDate = today.AddDays(-5), PaymentTerms = "Due on Receipt", Subtotal = 400m, TaxAmount = 50m, Total = 450m, BalanceDue = 250m, Customer = cust2 };
                db.Invoices.AddRange(inv_1, inv_2);
                db.SaveChanges(); // flush for FK IDs

                // Invoice Lines
                db.InvoiceLines.AddRange(
                    new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_1.Id, Description = "Refrigerant R-410A (1.5 lbs)", LineType = "Material", Unit = "Lbs", Quantity = 1.5m, UnitPrice = 28m, LineTotal = 42m, SortOrder = 1 },
                    new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_1.Id, Description = "Leak Detection + Repair", LineType = "Labor", Unit = "Hour", Quantity = 2, UnitPrice = 125m, LineTotal = 250m, SortOrder = 2 },
                    new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_1.Id, Description = "Diagnostic Fee", LineType = "Fee", Unit = "Each", Quantity = 1, UnitPrice = 8m, LineTotal = 8m, SortOrder = 3 },
                    new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_2.Id, Description = "Honeywell T6 Pro Thermostat", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 145m, LineTotal = 145m, SortOrder = 1 },
                    new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_2.Id, Description = "Thermostat Install Labor", LineType = "Labor", Unit = "Hour", Quantity = 1, UnitPrice = 125m, LineTotal = 125m, SortOrder = 2 },
                    new OneManVanFSM.Shared.Models.InvoiceLine { InvoiceId = inv_2.Id, Description = "Wire + Misc Parts", LineType = "Material", Unit = "Each", Quantity = 1, UnitPrice = 80m, LineTotal = 80m, SortOrder = 3 }
                );

                // Payments
                db.Payments.AddRange(
                    new OneManVanFSM.Shared.Models.Payment { InvoiceId = inv_2.Id, Amount = 200m, Method = OneManVanFSM.Shared.Models.PaymentMethod.Card, Status = OneManVanFSM.Shared.Models.PaymentStatus.Completed, PaymentDate = today.AddDays(-3), Reference = "Partial payment", TransactionId = "TXN-44210" }
                );

                // Expenses
                db.Expenses.AddRange(
                    new OneManVanFSM.Shared.Models.Expense { Category = "Fuel", Amount = 87.50m, IsBillable = false, Status = OneManVanFSM.Shared.Models.ExpenseStatus.Approved, Description = "Van #1 fuel fill-up", Employee = tech, ExpenseDate = today.AddDays(-1) },
                    new OneManVanFSM.Shared.Models.Expense { Category = "Parts", Amount = 24m, IsBillable = true, Status = OneManVanFSM.Shared.Models.ExpenseStatus.Approved, Description = "Honeywell Q3400A igniter — emergency replacement", Employee = tech, Job = job4, ExpenseDate = today }
                );

                // Suppliers
                db.Suppliers.AddRange(
                    new OneManVanFSM.Shared.Models.Supplier { Name = "Johnstone Supply", ContactName = "Kevin Marsh", Phone = "(555) 900-1001", Email = "kevin@johnstonesupply.com", AccountNumber = "JS-44210", PaymentTerms = "Net 30", Notes = "Primary HVAC parts supplier" },
                    new OneManVanFSM.Shared.Models.Supplier { Name = "FilterDirect Supply", ContactName = "Amy Torres", Phone = "(555) 900-2002", Email = "orders@filterdirect.com", AccountNumber = "FD-8820", PaymentTerms = "Net 15", Notes = "Bulk filter orders" },
                    new OneManVanFSM.Shared.Models.Supplier { Name = "Home Depot Pro", Phone = "(555) 900-3003", PaymentTerms = "Due on Receipt", Notes = "Backup supplier" }
                );

                // Templates
                db.Templates.AddRange(
                    new OneManVanFSM.Shared.Models.Template { Name = "Residential AC Tune-Up Checklist", Type = OneManVanFSM.Shared.Models.TemplateType.JobChecklist, IsCompanyDefault = true, UsageCount = 24, LastUsed = today.AddDays(-1), Data = "{\"sections\":[{\"name\":\"Outdoor Unit\",\"items\":[\"Clean condenser coil\",\"Check refrigerant levels\",\"Inspect contactor\",\"Test capacitor\"]},{\"name\":\"Indoor Unit\",\"items\":[\"Replace air filter\",\"Check evaporator coil\",\"Inspect blower wheel\",\"Test thermostat\"]},{\"name\":\"Electrical\",\"items\":[\"Measure amp draw\",\"Check wiring\",\"Verify voltage\"]}]}" },
                    new OneManVanFSM.Shared.Models.Template { Name = "Furnace Tune-Up Checklist", Type = OneManVanFSM.Shared.Models.TemplateType.JobChecklist, IsCompanyDefault = true, UsageCount = 18, Data = "{\"sections\":[{\"name\":\"Burners\",\"items\":[\"Clean burners\",\"Check flame sensor\",\"Inspect igniter\"]},{\"name\":\"Heat Exchanger\",\"items\":[\"Visual inspection\",\"CO test\"]},{\"name\":\"Airflow\",\"items\":[\"Replace filter\",\"Check blower\",\"Measure temp rise\"]}]}" }
                );

                // Dropdown Options
                db.DropdownOptions.AddRange(
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "TradeType", Value = "HVAC", SortOrder = 1, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "TradeType", Value = "Plumbing", SortOrder = 2, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "TradeType", Value = "Electrical", SortOrder = 3, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "TradeType", Value = "General", SortOrder = 4, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "JobType", Value = "Install", SortOrder = 1, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "JobType", Value = "Repair", SortOrder = 2, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "JobType", Value = "Maintenance", SortOrder = 3, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "JobType", Value = "Diagnostic", SortOrder = 4, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "JobType", Value = "Inspection", SortOrder = 5, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Split System", SortOrder = 1, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Packaged Unit", SortOrder = 2, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Mini-Split", SortOrder = 3, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Heat Pump", SortOrder = 4, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "SystemType", Value = "Commercial RTU", SortOrder = 5, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "AC Unit", SortOrder = 1, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "Furnace", SortOrder = 2, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "Heat Pump", SortOrder = 3, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "RTU", SortOrder = 4, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "Ductless Mini-Split", SortOrder = 5, IsSystem = true },
                    new OneManVanFSM.Shared.Models.DropdownOption { Category = "AssetType", Value = "Water Heater", SortOrder = 6, IsSystem = true }
                );

                db.SaveChanges();
                System.Diagnostics.Debug.WriteLine("[SEED] Seed data completed successfully! AppUsers count: " + db.Users.Count());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SEED] CRITICAL ERROR: {ex.GetType().Name} — {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SEED] Stack: {ex.StackTrace}");
            }
        }
    }
}
