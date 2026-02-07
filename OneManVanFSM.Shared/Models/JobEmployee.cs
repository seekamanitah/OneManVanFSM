namespace OneManVanFSM.Shared.Models;

public class JobEmployee
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job? Job { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string? Role { get; set; } // Lead, Helper, Apprentice, etc.
    public JobEmployeePayType PayType { get; set; } = JobEmployeePayType.Hourly;
    public decimal? FlatRateAmount { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public enum JobEmployeePayType
{
    Hourly,
    FlatRate
}
