using Microsoft.EntityFrameworkCore;
using OneManVanFSM.Shared.Data;
using OneManVanFSM.Shared.Models;

namespace OneManVanFSM.Shared.Services;

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _db;
    private Dictionary<(UserRole, string), RolePermission>? _cache;

    public PermissionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanViewAsync(UserRole role, string feature)
    {
        // Owner always has full access — safety net
        if (role == UserRole.Owner) return true;

        var perm = await GetPermissionAsync(role, feature);
        return perm?.CanView ?? false;
    }

    public async Task<bool> CanEditAsync(UserRole role, string feature)
    {
        if (role == UserRole.Owner) return true;

        var perm = await GetPermissionAsync(role, feature);
        return perm?.CanEdit ?? false;
    }

    public async Task<bool> CanDeleteAsync(UserRole role, string feature)
    {
        if (role == UserRole.Owner) return true;

        var perm = await GetPermissionAsync(role, feature);
        return perm?.CanDelete ?? false;
    }

    public async Task<List<RolePermission>> GetAllPermissionsAsync()
    {
        return await _db.RolePermissions.AsNoTracking().ToListAsync();
    }

    public async Task SavePermissionsAsync(List<RolePermission> permissions)
    {
        foreach (var perm in permissions)
        {
            var existing = await _db.RolePermissions
                .FirstOrDefaultAsync(rp => rp.Role == perm.Role && rp.Feature == perm.Feature);

            if (existing is not null)
            {
                existing.CanView = perm.CanView;
                existing.CanEdit = perm.CanEdit;
                existing.CanDelete = perm.CanDelete;
            }
            else
            {
                _db.RolePermissions.Add(new RolePermission
                {
                    Role = perm.Role,
                    Feature = perm.Feature,
                    CanView = perm.CanView,
                    CanEdit = perm.CanEdit,
                    CanDelete = perm.CanDelete,
                });
            }
        }

        await _db.SaveChangesAsync();
        _cache = null; // Invalidate cache after save
    }

    public async Task SeedDefaultsIfEmptyAsync()
    {
        if (await _db.RolePermissions.AnyAsync())
            return;

        _db.RolePermissions.AddRange(Features.GetDefaults());
        await _db.SaveChangesAsync();
    }

    private async Task<RolePermission?> GetPermissionAsync(UserRole role, string feature)
    {
        await EnsureCacheAsync();
        _cache!.TryGetValue((role, feature), out var perm);
        return perm;
    }

    private async Task EnsureCacheAsync()
    {
        if (_cache is not null) return;

        var all = await _db.RolePermissions.AsNoTracking().ToListAsync();
        _cache = new Dictionary<(UserRole, string), RolePermission>();
        foreach (var rp in all)
            _cache[(rp.Role, rp.Feature)] = rp;
    }
}
