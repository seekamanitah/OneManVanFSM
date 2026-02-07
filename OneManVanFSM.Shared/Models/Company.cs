namespace OneManVanFSM.Shared.Models;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties - contacts are customers linked to this company
    public ICollection<Customer> Contacts { get; set; } = [];
    public ICollection<Site> Sites { get; set; } = [];
    public ICollection<Job> Jobs { get; set; } = [];
    public ICollection<Estimate> Estimates { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<ServiceAgreement> ServiceAgreements { get; set; } = [];
}
