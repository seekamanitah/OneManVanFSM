using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Shared.Services;

public interface IPermissionService
{
    Task<bool> CanViewAsync(UserRole role, string feature);
    Task<bool> CanEditAsync(UserRole role, string feature);
    Task<bool> CanDeleteAsync(UserRole role, string feature);
    Task<List<RolePermission>> GetAllPermissionsAsync();
    Task SavePermissionsAsync(List<RolePermission> permissions);
    Task SeedDefaultsIfEmptyAsync();
}
