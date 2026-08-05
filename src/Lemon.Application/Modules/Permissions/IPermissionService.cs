namespace Lemon.Application.Modules.Permissions;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(long adminId, string permission, CancellationToken cancellationToken = default);
    Task ClearCacheAsync(long adminId, CancellationToken cancellationToken = default);
}
