namespace OneManVanFSM.Shared.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public CustomerType Type { get; set; } = CustomerType.Individual;
    public string? PrimaryPhone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? PreferredContactMethod { get; set; } // Phone, Email, Text, No Preference
    public string? ReferralSource { get; set; } // Word of Mouth, Google, Angi, etc.
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public DateTime SinceDate { get; set; } = DateTime.UtcNow;
    public decimal CreditLimit { get; set; }
    public bool TaxExempt { get; set; }
    public decimal BalanceOwed { get; set; }
    public string? Tags { get; set; } // JSON: "VIP", "Warranty Customer", "Propane", etc.
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public bool NeedsReview { get; set; }
    public string? CreatedFrom { get; set; } // "web", "mobile"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public ICollection<Site> Sites { get; set; } = [];
    public ICollection<Job> Jobs { get; set; } = [];
    public ICollection<Estimate> Estimates { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<ServiceAgreement> ServiceAgreements { get; set; } = [];
    public ICollection<QuickNote> QuickNotes { get; set; } = [];
    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<Asset> Assets { get; set; } = [];
}

public enum CustomerType
{
    Individual,
    Company,
    Landlord
}
