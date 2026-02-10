using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Services;
using OneManVanFSM.Web.Components;
using OneManVanFSM.Web.Services;

// Force US English culture for consistent $ currency formatting across all threads
var usCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = usCulture;
CultureInfo.DefaultThreadCurrentUICulture = usCulture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// API controllers for mobile sync
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// JWT service
builder.Services.AddSingleton<IJwtService, JwtService>();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=OneManVanFSM.db"));

// Authentication — Cookie for Blazor UI, JWT Bearer for API
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(options =>
    {
        var jwtService = new JwtService();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtService.Issuer,
            ValidAudience = jwtService.Audience,
            IssuerSigningKey = jwtService.GetSigningKey(),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// Application services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IEstimateService, EstimateService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<IServiceAgreementService, ServiceAgreementService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IQuickNoteService, QuickNoteService>();
builder.Services.AddScoped<IMaterialListService, MaterialListService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IServiceHistoryService, ServiceHistoryService>();
builder.Services.AddScoped<IDataManagementService, DataManagementService>();
builder.Services.AddScoped<IDropdownService, DropdownService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ICompanyProfileService, CompanyProfileService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

var app = builder.Build();

// Force en-US culture on every request so ToString("C") always yields $
app.UseRequestLocalization("en-US");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/error?code={0}");
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers(); // REST API endpoints for mobile sync
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Auth endpoints — cookie operations must run in a real HTTP request, not a SignalR circuit
app.MapPost("/auth/login", async (HttpContext context, IAuthService authService) =>
{
    var form = await context.Request.ReadFormAsync();
    var usernameOrEmail = form["usernameOrEmail"].ToString();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("Username and password are required."));

    var result = await authService.LoginAsync(usernameOrEmail, password);
    if (!result.Succeeded)
        return Results.Redirect("/login?error=" + Uri.EscapeDataString(result.ErrorMessage ?? "Login failed."));

    // Force first-time password change
    if (result.User!.MustChangePassword)
        return Results.Redirect("/setup");

    var redirect = result.User!.Role switch
    {
        OneManVanFSM.Shared.Models.UserRole.Tech => "/calendar",
        _ => "/"
    };
    return Results.Redirect(redirect);
}).DisableAntiforgery();

app.MapGet("/auth/logout", async (IAuthService authService) =>
{
    await authService.LogoutAsync();
    return Results.Redirect("/login");
});

// Health check endpoint for Docker healthcheck and mobile connectivity test
app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow })
            : Results.Json(new { status = "degraded", database = "unreachable", timestamp = DateTime.UtcNow }, statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "unhealthy", database = "error", error = ex.Message, timestamp = DateTime.UtcNow }, statusCode: 503);
    }
});

// Ensure schema is up-to-date and tables exist (no data seeded)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Detect schema drift and recreate database if needed
    DatabaseInitializer.EnsureSchemaUpToDate(db);

    db.Database.EnsureCreated();

    // Seed default HVAC item associations (auto-pairings) if not already present
    if (!db.ItemAssociations.Any())
    {
        db.ItemAssociations.AddRange(
            // Flex Duct ? paired components (1:1 ratio each)
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 5\"", AssociatedItemName = "Floor Boot 4x10x5", AssociatedSection = "Boots", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 5\"", AssociatedItemName = "Take Off Round 5\"", AssociatedSection = "Take Offs", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 5\"", AssociatedItemName = "Collar 5\"", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 6\"", AssociatedItemName = "Floor Boot 4x10x6", AssociatedSection = "Boots", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 6\"", AssociatedItemName = "Take Off Round 6\"", AssociatedSection = "Take Offs", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 6\"", AssociatedItemName = "Collar 6\"", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 7\"", AssociatedItemName = "Floor Boot 4x10x7", AssociatedSection = "Boots", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 7\"", AssociatedItemName = "Take Off Round 7\"", AssociatedSection = "Take Offs", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 7\"", AssociatedItemName = "Collar 7\"", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 8\"", AssociatedItemName = "Floor Boot 4x10x8", AssociatedSection = "Boots", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 8\"", AssociatedItemName = "Take Off Round 8\"", AssociatedSection = "Take Offs", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 8\"", AssociatedItemName = "Collar 8\"", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 10\"", AssociatedItemName = "Take Off Round 10\"", AssociatedSection = "Take Offs", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 10\"", AssociatedItemName = "Collar 10\"", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 12\"", AssociatedItemName = "Take Off Round 12\"", AssociatedSection = "Take Offs", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Flex Duct 12\"", AssociatedItemName = "Collar 12\"", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" },
            // Return Grilles ? Filter pairing
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Return Grille 20x20", AssociatedItemName = "Filter 20x20x1", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Return Grille 20x25", AssociatedItemName = "Filter 20x25x1", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Return Grille 25x20", AssociatedItemName = "Filter 20x25x1", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" },
            new OneManVanFSM.Shared.Models.ItemAssociation { ItemName = "Return Grille 30x20", AssociatedItemName = "Filter 20x30x1", AssociatedSection = "Fittings", Ratio = 1, TradeType = "HVAC" }
        );
        db.SaveChanges();
    }

    // Seed default admin user if no users exist
    if (!db.Users.Any())
    {
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "chris.eikel@bledsoe.net";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "!1235aSdf12sadf5!";

        // Create a linked employee for the admin
        var adminEmployee = new OneManVanFSM.Shared.Models.Employee
        {
            Name = "Admin",
            Role = OneManVanFSM.Shared.Models.EmployeeRole.Owner,
            Status = OneManVanFSM.Shared.Models.EmployeeStatus.Active,
            Email = adminEmail,
        };
        db.Employees.Add(adminEmployee);

        db.Users.Add(new OneManVanFSM.Shared.Models.AppUser
        {
            Username = "admin",
            Email = adminEmail,
            PasswordHash = AuthService.HashPassword(adminPassword),
            Role = OneManVanFSM.Shared.Models.UserRole.Owner,
            IsActive = true,
            MustChangePassword = true,
            Employee = adminEmployee,
        });
        db.SaveChanges();
    }

    // Seed default role permissions if not already present
    var permSvc = scope.ServiceProvider.GetRequiredService<IPermissionService>();
    permSvc.SeedDefaultsIfEmptyAsync().GetAwaiter().GetResult();
}

app.Run();
