using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileSearchService
{
    Task<MobileSearchResults> SearchAsync(string query, string? category = null);
}

public class MobileSearchResults
{
    public List<MobileSearchResult> Results { get; set; } = [];
    public int TotalCount { get; set; }
}

public class MobileSearchResult
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-search";
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public string NavigateTo { get; set; } = string.Empty;
}
