namespace OneManVanFSM.Shared.Models;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? AccountNumber { get; set; }
    public string? PaymentTerms { get; set; } // Net 30, Due on Receipt, etc.
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional link to a Company record (auto-created as Vendor/Supplier type)
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
}
