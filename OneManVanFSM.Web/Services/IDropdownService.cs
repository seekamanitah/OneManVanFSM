namespace OneManVanFSM.Web.Services;

using OneManVanFSM.Shared.Models;

public interface IDropdownService
{
    Task<List<string>> GetCategoriesAsync();
    Task<List<DropdownOption>> GetOptionsAsync(string category);
    Task<DropdownOption> AddOptionAsync(string category, string value, string? label = null);
    Task<DropdownOption> UpdateOptionAsync(int id, string value, string? label, bool isActive);
    Task<bool> DeleteOptionAsync(int id);
    Task SeedDefaultsAsync();
}
