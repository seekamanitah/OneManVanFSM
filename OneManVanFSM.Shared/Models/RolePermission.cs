namespace OneManVanFSM.Shared.Models;

public class RolePermission
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
    public string Feature { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

/// <summary>
/// Constants for all permission-gated features and default matrix generator.
/// </summary>
public static class Features
{
    public const string Customers = "Customers";
    public const string Companies = "Companies";
    public const string Sites = "Sites";
    public const string Jobs = "Jobs";
    public const string Estimates = "Estimates";
    public const string Inventory = "Inventory";
    public const string Products = "Products";
    public const string MaterialLists = "MaterialLists";
    public const string Assets = "Assets";
    public const string ServiceHistory = "ServiceHistory";
    public const string ServiceAgreements = "ServiceAgreements";
    public const string Financials = "Financials";
    public const string Invoices = "Invoices";
    public const string Expenses = "Expenses";
    public const string Employees = "Employees";
    public const string Documents = "Documents";
    public const string QuickNotes = "QuickNotes";
    public const string Reports = "Reports";
    public const string Calendar = "Calendar";
    public const string Templates = "Templates";
    public const string Settings = "Settings";
    public const string Search = "Search";
    public const string Suppliers = "Suppliers";

    public static readonly string[] All =
    [
        Customers, Companies, Sites, Jobs, Estimates, Inventory, Products,
        MaterialLists, Assets, ServiceHistory, ServiceAgreements, Financials,
        Invoices, Expenses, Employees, Documents, QuickNotes, Reports,
        Calendar, Templates, Settings, Search, Suppliers
    ];

    /// <summary>
    /// Returns the full default permission matrix for all roles.
    /// Owner always has full access (enforced in code as well).
    /// </summary>
    public static List<RolePermission> GetDefaults()
    {
        var list = new List<RolePermission>();

        // Owner — full access to everything
        foreach (var f in All)
            list.Add(new RolePermission { Role = UserRole.Owner, Feature = f, CanView = true, CanEdit = true, CanDelete = true });

        // Admin — full access to everything except cannot delete Settings
        foreach (var f in All)
            list.Add(new RolePermission { Role = UserRole.Admin, Feature = f, CanView = true, CanEdit = true, CanDelete = f != Settings });

        // Manager — view + edit most features, limited delete, no Settings edit
        foreach (var f in All)
        {
            var canEdit = f is not Settings;
            var canDelete = f is Jobs or Estimates or Documents or QuickNotes or MaterialLists or Templates;
            list.Add(new RolePermission { Role = UserRole.Manager, Feature = f, CanView = true, CanEdit = canEdit, CanDelete = canDelete });
        }

        // Dispatcher — focused on scheduling and job management
        foreach (var f in All)
        {
            var canView = f is not (Settings or Employees or Financials or Invoices or Expenses);
            var canEdit = f is Jobs or Calendar or Estimates or QuickNotes or Documents;
            var canDelete = false;
            list.Add(new RolePermission { Role = UserRole.Dispatcher, Feature = f, CanView = canView, CanEdit = canEdit, CanDelete = canDelete });
        }

        // Tech — field technician, limited to operational features
        foreach (var f in All)
        {
            var canView = f is not (Settings or Employees or Financials or Invoices or Expenses or Reports);
            var canEdit = f is Jobs or QuickNotes or Documents or Inventory;
            var canDelete = false;
            list.Add(new RolePermission { Role = UserRole.Tech, Feature = f, CanView = canView, CanEdit = canEdit, CanDelete = canDelete });
        }

        // Apprentice — view-only on most operational features, can only edit notes
        foreach (var f in All)
        {
            var canView = f is not (Settings or Employees or Financials or Invoices or Expenses or Reports);
            var canEdit = f is QuickNotes or Documents;
            var canDelete = false;
            list.Add(new RolePermission { Role = UserRole.Apprentice, Feature = f, CanView = canView, CanEdit = canEdit, CanDelete = canDelete });
        }

        return list;
    }
}
