namespace Lemon.Application.Modules.Roles;
public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<long> CreateAsync(CreateRoleRequest request, long operatorId, CancellationToken cancellationToken = default);
    Task UpdateAsync(long roleId, UpdateRoleRequest request, long operatorId, CancellationToken cancellationToken = default);
    Task UpdatePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, long operatorId, CancellationToken cancellationToken = default);
}
