using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Services;

public interface IMobileDropdownService
{
    Task<List<string>> GetCategoriesAsync();
    Task<List<DropdownOption>> GetOptionsAsync(string category);
    Task<DropdownOption> AddOptionAsync(string category, string value, string? label = null);
    Task<bool> UpdateOptionAsync(int id, string value, string? label, bool isActive);
    Task<bool> DeleteOptionAsync(int id);
    Task SeedDefaultsAsync();
}
