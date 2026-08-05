using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Common;
using Lemon.Application.Modules.Permissions;
using Lemon.Domain.System;

namespace Lemon.Application.Modules.Roles;

public sealed class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;
    private readonly IAdminRepository _admins;
    private readonly IPermissionService _permissions;

    public RoleService(
        IRoleRepository repository,
        IAdminRepository admins,
        IPermissionService permissions)
    {
        _repository = repository;
        _admins = admins;
        _permissions = permissions;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var roles = await _repository.GetAllAsync(ct);
        var result = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            result.Add(new RoleDto(
                role.Id,
                role.Code,
                role.Name,
                role.Description,
                role.DataScope,
                role.IsSystem,
                role.IsEnabled,
                await _repository.GetPermissionIdsAsync(role.Id, ct)));
        }

        return result;
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default) =>
        (await _repository.GetPermissionsAsync(ct))
            .Select(x => new PermissionDto(x.Id, x.Code, x.Name, x.Module))
            .ToArray();

    public async Task<long> CreateAsync(CreateRoleRequest request, long operatorId, CancellationToken ct = default)
    {
        if (await _repository.CodeExistsAsync(request.Code.Trim(), cancellationToken: ct))
            throw new AppException(ErrorCodes.DuplicateData, "角色代码已存在");

        return await _repository.CreateAsync(new Role
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DataScope = request.DataScope,
            IsSystem = false,
            IsEnabled = true,
            CreatedBy = operatorId
        }, request.PermissionIds, ct);
    }

    public async Task UpdateAsync(long roleId, UpdateRoleRequest request, long operatorId, CancellationToken ct = default)
    {
        var role = await _repository.GetByIdAsync(roleId, ct)
            ?? throw new AppException(ErrorCodes.NotFound, "角色不存在", 404);

        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim();
        role.DataScope = request.DataScope;
        role.IsEnabled = request.IsEnabled;
        role.UpdatedBy = operatorId;

        await _repository.UpdateAsync(role, ct);
        await ClearRoleUsersAsync(roleId, ct);
    }

    public async Task UpdatePermissionsAsync(
        long roleId,
        IReadOnlyCollection<long> permissionIds,
        long operatorId,
        CancellationToken ct = default)
    {
        _ = await _repository.GetByIdAsync(roleId, ct)
            ?? throw new AppException(ErrorCodes.NotFound, "角色不存在", 404);

        await _repository.UpdatePermissionsAsync(roleId, permissionIds, operatorId, ct);
        await ClearRoleUsersAsync(roleId, ct);
    }

    private async Task ClearRoleUsersAsync(long roleId, CancellationToken ct)
    {
        foreach (var adminId in await _admins.GetAdminIdsByRoleAsync(roleId, ct))
            await _permissions.ClearCacheAsync(adminId, ct);
    }
}
