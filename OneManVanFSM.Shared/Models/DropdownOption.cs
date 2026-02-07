namespace OneManVanFSM.Shared.Models;

public class DropdownOption
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty; // e.g., "JobPriority", "SystemType", "EquipmentCategory", "ExpenseCategory"
    public string Value { get; set; } = string.Empty;
    public string? Label { get; set; } // Display label if different from Value
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } // true = cannot be deleted, only deactivated
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
