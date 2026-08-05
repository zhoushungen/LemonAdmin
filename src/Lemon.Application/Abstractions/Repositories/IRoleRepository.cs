using Lemon.Domain.System;
namespace Lemon.Application.Abstractions.Repositories;
public sealed record PermissionItem(long Id, string Code, string Name, string Module);
public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionItem>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<long>> GetPermissionIdsAsync(long roleId, CancellationToken cancellationToken = default);
    Task<long> CreateAsync(Role role, IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task UpdatePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, long operatorId, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null, CancellationToken cancellationToken = default);
}
