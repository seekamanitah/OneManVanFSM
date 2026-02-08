using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Web.Services;

public class DropdownService : IDropdownService
{
    private readonly AppDbContext _db;
    public DropdownService(AppDbContext db) => _db = db;

    public async Task<List<string>> GetCategoriesAsync()
    {
        var dbCategories = await _db.DropdownOptions
            .Select(d => d.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        // Merge with known default categories
        var allCategories = new HashSet<string>(dbCategories, StringComparer.OrdinalIgnoreCase);
        foreach (var cat in DefaultCategories.Keys)
            allCategories.Add(cat);

        return allCategories.OrderBy(c => c).ToList();
    }

    public async Task<List<DropdownOption>> GetOptionsAsync(string category)
    {
        var options = await _db.DropdownOptions
            .Where(d => d.Category == category)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Value)
            .ToListAsync();

        if (options.Count == 0 && DefaultCategories.TryGetValue(category, out var defaults))
        {
            // Auto-seed defaults on first access
            int order = 0;
            foreach (var val in defaults)
            {
                var opt = new DropdownOption
                {
                    Category = category,
                    Value = val,
                    SortOrder = order++,
                    IsSystem = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _db.DropdownOptions.Add(opt);
                options.Add(opt);
            }
            await _db.SaveChangesAsync();
        }

        return options;
    }

    public async Task<DropdownOption> AddOptionAsync(string category, string value, string? label = null)
    {
        var maxOrder = await _db.DropdownOptions
            .Where(d => d.Category == category)
            .MaxAsync(d => (int?)d.SortOrder) ?? 0;

        var option = new DropdownOption
        {
            Category = category,
            Value = value,
            Label = label,
            SortOrder = maxOrder + 1,
            IsActive = true,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.DropdownOptions.Add(option);
        await _db.SaveChangesAsync();
        return option;
    }

    public async Task<DropdownOption> UpdateOptionAsync(int id, string value, string? label, bool isActive)
    {
        var opt = await _db.DropdownOptions.FindAsync(id)
            ?? throw new InvalidOperationException("Option not found.");
        opt.Value = value;
        opt.Label = label;
        opt.IsActive = isActive;
        await _db.SaveChangesAsync();
        return opt;
    }

    public async Task<bool> DeleteOptionAsync(int id)
    {
        var opt = await _db.DropdownOptions.FindAsync(id);
        if (opt is null) return false;
        if (opt.IsSystem)
        {
            opt.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }
        _db.DropdownOptions.Remove(opt);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task SeedDefaultsAsync()
    {
        foreach (var (category, values) in DefaultCategories)
        {
            if (await _db.DropdownOptions.AnyAsync(d => d.Category == category))
                continue;

            int order = 0;
            foreach (var val in values)
            {
                _db.DropdownOptions.Add(new DropdownOption
                {
                    Category = category,
                    Value = val,
                    SortOrder = order++,
                    IsSystem = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    private static readonly Dictionary<string, string[]> DefaultCategories = new()
    {
        ["SystemType"] = ["Split System", "Package Unit", "Mini-Split", "Ductless", "Geothermal", "Heat Pump", "Furnace", "Boiler", "RTU", "Trunk Duct"],
        ["EquipmentCategory"] = ["AC Unit", "Furnace", "Heat Pump", "Ductless Mini-Split", "RTU", "Boiler", "Air Handler", "Thermostat"],
        ["ExpenseCategory"] = ["Parts", "Materials", "Tools", "Fuel", "Subcontractor", "Permits", "Misc"],
        ["ProductCategory"] = ["Ductwork", "Refrigerant", "Controls", "Filters", "Parts", "Equipment", "Tools", "Misc"],
        ["JobSource"] = ["Referral", "Website", "Google", "Social Media", "Repeat Customer", "Walk-In", "Other"],
        ["PaymentTerms"] = ["Due on Receipt", "Net 15", "Net 30", "Net 45", "Net 60"],
        ["FuelType"] = ["Natural Gas", "Propane", "Electric", "Oil", "Dual Fuel"],
        ["UnitConfiguration"] = ["Split System", "Packaged Unit", "Furnace", "Air Handler", "Condenser", "Mini-Split", "Ductless", "Heat Pump", "Coil", "Boiler"],
        ["TradeType"] = ["HVAC", "Plumbing", "Electrical", "General"],
        ["MaterialListSection"] = ["General", "Ductwork", "Flex Duct", "Take Offs", "Boots", "Returns", "Trunk Line", "Hard Pipe", "Grills/Registers", "Sealing & Taping", "Support & Hangers", "Insulation", "Fittings", "Equipment", "Accessories", "Disposal", "Permits", "Misc", "Labor"],
        ["JobType"] = ["Service Call", "Inspection", "Repair", "Emergency", "New Install", "Changeout", "Ductwork", "Startup", "Estimate", "Maintenance", "Diagnostic", "Tune-Up", "Replacement", "Retrofit"],
        ["TimeCategory"] = ["Travel", "On-Site", "Diagnostic", "Admin", "Break", "Training"],
        ["HeatingFuelSource"] = ["Natural Gas", "Propane", "Electric", "Oil", "Geothermal", "Solar"],
        ["FilterSize"] = ["14x20x1", "14x25x1", "16x20x1", "16x25x1", "20x20x1", "20x25x1", "16x25x4", "20x20x4", "20x25x4", "Custom"],
        ["JobAssetRole"] = ["Serviced", "Installed", "Replaced", "Inspected", "Diagnosed", "Decommissioned"],
        ["Voltage"] = ["120V", "208V", "240V", "277V", "480V"],
        ["AssetLocation"] = ["Basement", "Attic", "Closet", "Garage", "Roof", "Side Yard", "Crawl Space", "Utility Room", "Backyard", "Mechanical Room"],
        ["LineType"] = ["Labor", "Material", "Equipment", "Fee", "Discount", "Permit", "Disposal"],
        ["EventType"] = ["Job", "Personal", "Meeting", "Reminder", "Block-Off", "Training"],
        ["BillingFrequency"] = ["Monthly", "Quarterly", "Semi-Annual", "Annual"],
        ["ReferralSource"] = ["Word of Mouth", "Google", "Angi", "HomeAdvisor", "Facebook", "Yard Sign", "Repeat Customer", "Other"],
        ["PreferredContact"] = ["Phone", "Email", "Text", "No Preference"],
        ["Phase"] = ["Single Phase", "Three Phase"],
        ["PipeMaterial"] = ["Copper", "PEX", "PVC", "CPVC", "Cast Iron", "Galvanized", "ABS"],
        ["PanelType"] = ["Main Breaker", "Main Lug", "Sub-Panel"],
        ["AssetServiceType"] = ["Filter Change", "Refrigerant Charge", "Coil Cleaning", "Calibration", "Inspection", "Drain Flush", "Belt Replacement"],
        ["RefrigerantType"] = ["R-22", "R-410A", "R-32", "R-454B", "R-407C", "R-404A", "R-134A"],
        ["FilterType"] = ["Fiberglass", "Pleated", "HEPA", "Electrostatic", "Washable", "Carbon", "Media"],
        ["ThermostatType"] = ["Non-Programmable", "Programmable", "Smart", "WiFi-Enabled", "Line Voltage", "Communicating"],
        ["Brand"] = ["Carrier", "Lennox", "Trane", "Rheem", "Goodman", "Daikin", "Bryant", "York", "Amana", "American Standard", "Bosch", "Mitsubishi", "Fujitsu", "Ruud", "Heil"],
        ["CustomerType"] = ["Individual", "Company", "Landlord"],
        ["CompanyType"] = ["Customer", "Vendor", "Subcontractor", "Partner"],
        ["PropertyType"] = ["Residential", "Commercial", "Industrial"],
    };
}
