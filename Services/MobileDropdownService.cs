using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public class MobileDropdownService(AppDbContext db) : IMobileDropdownService
{
    public async Task<List<string>> GetCategoriesAsync()
    {
        var dbCategories = await db.DropdownOptions
            .Select(d => d.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var allCategories = new HashSet<string>(dbCategories, StringComparer.OrdinalIgnoreCase);
        foreach (var cat in DefaultCategories.Keys)
            allCategories.Add(cat);

        return allCategories.OrderBy(c => c).ToList();
    }

    public async Task<List<DropdownOption>> GetOptionsAsync(string category)
    {
        var options = await db.DropdownOptions
            .Where(d => d.Category == category)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Value)
            .ToListAsync();

        if (options.Count == 0 && DefaultCategories.TryGetValue(category, out var defaults))
        {
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
                db.DropdownOptions.Add(opt);
                options.Add(opt);
            }
            await db.SaveChangesAsync();
        }

        return options;
    }

    public async Task<DropdownOption> AddOptionAsync(string category, string value, string? label = null)
    {
        var maxOrder = await db.DropdownOptions
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
        db.DropdownOptions.Add(option);
        await db.SaveChangesAsync();
        return option;
    }

    public async Task<bool> UpdateOptionAsync(int id, string value, string? label, bool isActive)
    {
        var opt = await db.DropdownOptions.FindAsync(id);
        if (opt is null) return false;
        opt.Value = value;
        opt.Label = label;
        opt.IsActive = isActive;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOptionAsync(int id)
    {
        var opt = await db.DropdownOptions.FindAsync(id);
        if (opt is null) return false;
        if (opt.IsSystem)
        {
            opt.IsActive = false;
            await db.SaveChangesAsync();
            return true;
        }
        db.DropdownOptions.Remove(opt);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task SeedDefaultsAsync()
    {
        foreach (var (category, values) in DefaultCategories)
        {
            if (await db.DropdownOptions.AnyAsync(d => d.Category == category))
                continue;

            int order = 0;
            foreach (var val in values)
            {
                db.DropdownOptions.Add(new DropdownOption
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
        await db.SaveChangesAsync();
    }

    private static readonly Dictionary<string, string[]> DefaultCategories = new()
    {
        ["SystemType"] = ["Split System", "Package Unit", "Mini-Split", "Ductless", "Geothermal", "Heat Pump", "Furnace", "Boiler", "RTU", "Trunk Duct"],
        ["EquipmentCategory"] = ["AC Unit", "Furnace", "Heat Pump", "Ductless Mini-Split", "RTU", "Boiler", "Air Handler", "Thermostat", "Water Heater"],
        ["ExpenseCategory"] = ["Parts", "Materials", "Tools", "Fuel", "Subcontractor", "Permits", "Misc"],
        ["ProductCategory"] = ["Ductwork", "Refrigerant", "Controls", "Filters", "Parts", "Equipment", "Tools", "Misc"],
        ["JobSource"] = ["Referral", "Website", "Google", "Social Media", "Repeat Customer", "Walk-In", "Other"],
        ["PaymentTerms"] = ["Due on Receipt", "Net 15", "Net 30", "Net 45", "Net 60"],
        ["FuelType"] = ["Natural Gas", "Propane", "Electric", "Oil", "Dual Fuel"],
        ["TradeType"] = ["HVAC", "Plumbing", "Electrical", "General"],
        ["MaterialListSection"] = ["General", "Ductwork", "Flex Duct", "Take Offs", "Boots", "Returns", "Trunk Line", "Hard Pipe", "Grills/Registers", "Sealing & Taping", "Support & Hangers", "Insulation", "Fittings", "Equipment", "Accessories", "Disposal", "Permits", "Misc", "Labor"],
        ["JobType"] = ["Service Call", "Inspection", "Repair", "Emergency", "New Install", "Changeout", "Ductwork", "Startup", "Estimate", "Maintenance", "Diagnostic", "Tune-Up"],
        ["TimeCategory"] = ["Travel", "On-Site", "Diagnostic", "Admin", "Break", "Training"],
        ["RefrigerantType"] = ["R-22", "R-410A", "R-32", "R-454B", "R-407C", "R-404A", "R-134A"],
        ["Brand"] = ["Carrier", "Lennox", "Trane", "Rheem", "Goodman", "Daikin", "Bryant", "York", "Amana", "American Standard"],
        ["CustomerType"] = ["Individual", "Company", "Landlord"],
        ["CompanyType"] = ["Customer", "Vendor", "Subcontractor", "Partner"],
        ["PropertyType"] = ["Residential", "Commercial", "Industrial"],
        ["LineType"] = ["Labor", "Material", "Equipment", "Fee", "Discount", "Permit", "Disposal"],
        ["EventType"] = ["Job", "Personal", "Meeting", "Reminder", "Block-Off", "Training"],
        ["DocumentCategory"] = ["Contract", "Warranty", "Receipt", "Manual", "Photo", "Report", "Permit", "License", "Insurance", "Other"],
    };
}
