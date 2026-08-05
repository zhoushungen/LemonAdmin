using Lemon.Application.Abstractions.Cache;
using Lemon.Application.Abstractions.Repositories;

namespace Lemon.Application.Modules.Permissions;

public sealed class PermissionService : IPermissionService
{
    private readonly IAdminRepository _admins;
    private readonly ICacheService _cache;

    public PermissionService(IAdminRepository admins, ICacheService cache)
    {
        _admins = admins;
        _cache = cache;
    }

    public async Task<bool> HasPermissionAsync(
        long adminId,
        string permission,
        CancellationToken ct = default)
    {
        var profile = await _admins.GetAccessProfileAsync(adminId, ct);
        if (profile is null || !profile.IsEnabled) return false;
        if (profile.RoleId.HasValue &&
            (!profile.RoleIsEnabled || !profile.DepartmentId.HasValue || !profile.DepartmentIsEnabled))
            return false;

        var key = CacheKey(adminId);
        var permissions = await _cache.GetAsync<HashSet<string>>(key, ct);
        if (permissions is null)
        {
            permissions = profile.RoleId is null
                ? new HashSet<string>(["*"], StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(await _admins.GetPermissionsAsync(adminId, ct), StringComparer.OrdinalIgnoreCase);

            await _cache.SetAsync(key, permissions, TimeSpan.FromMinutes(20), ct);
        }

        return permissions.Contains("*") || permissions.Contains(permission);
    }

    public Task ClearCacheAsync(long adminId, CancellationToken ct = default) =>
        _cache.RemoveAsync(CacheKey(adminId), ct);

    private static string CacheKey(long id) => $"permission:admin:{id}";
}
