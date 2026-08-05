using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Common;
using Lemon.Application.Modules.Permissions;
using Lemon.Domain.System;

namespace Lemon.Application.Modules.Admins;

public sealed class AdminService : IAdminService
{
    private readonly IAdminRepository _repository;
    private readonly IRoleRepository _roles;
    private readonly IDepartmentRepository _departments;
    private readonly IDataScopeService _dataScope;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPermissionService _permissions;

    public AdminService(
        IAdminRepository repository,
        IRoleRepository roles,
        IDepartmentRepository departments,
        IDataScopeService dataScope,
        IPasswordHasher passwordHasher,
        IPermissionService permissions)
    {
        _repository = repository;
        _roles = roles;
        _departments = departments;
        _dataScope = dataScope;
        _passwordHasher = passwordHasher;
        _permissions = permissions;
    }

    public async Task<PagedResult<AdminDto>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? keyword,
        long? departmentId,
        bool? enabled,
        CancellationToken ct = default)
    {
        var scope = await _dataScope.GetCurrentAsync(ct);
        var result = await _repository.GetPagedAsync(pageIndex, pageSize, keyword, departmentId, enabled, scope, ct);
        return new PagedResult<AdminDto>(
            result.Items.Select(ToDto).ToArray(),
            result.PageIndex,
            result.PageSize,
            result.Total);
    }

    public async Task<AdminDetailDto> GetAsync(long id, CancellationToken ct = default)
    {
        if (!await _dataScope.CanAccessAdminAsync(id, ct))
            throw new AppException(ErrorCodes.Forbidden, "无权访问该管理员", 403);

        var admin = await _repository.GetByIdAsync(id, ct)
            ?? throw new AppException(ErrorCodes.AdminNotFound, "管理员不存在", 404);

        return new AdminDetailDto(
            admin.Id,
            admin.DepartmentId,
            admin.RoleId,
            admin.Username,
            admin.DisplayName,
            admin.Email,
            admin.Mobile,
            admin.IsSuperAdmin,
            admin.IsEnabled);
    }

    public async Task<long> CreateAsync(CreateAdminRequest request, long operatorId, CancellationToken ct = default)
    {
        if (!request.RoleId.HasValue)
            throw new AppException(ErrorCodes.ValidationFailed, "普通管理员必须选择角色");
        if (!request.DepartmentId.HasValue)
            throw new AppException(ErrorCodes.ValidationFailed, "普通管理员必须选择主部门");

        await EnsureUsableRoleAsync(request.RoleId.Value, ct);
        await EnsureUsableDepartmentAsync(request.DepartmentId.Value, ct);
        if (await _repository.UsernameExistsAsync(request.Username.Trim(), cancellationToken: ct))
            throw new AppException(ErrorCodes.DuplicateData, "用户名已存在");

        var password = _passwordHasher.Hash(request.Password);
        return await _repository.CreateAsync(new AdminUser
        {
            DepartmentId = request.DepartmentId,
            RoleId = request.RoleId,
            Username = request.Username.Trim(),
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = password.Hash,
            PasswordSalt = password.Salt,
            Email = request.Email?.Trim(),
            Mobile = request.Mobile?.Trim(),
            IsEnabled = true,
            CreatedBy = operatorId
        }, ct);
    }

    public async Task UpdateAsync(long id, UpdateAdminRequest request, long operatorId, CancellationToken ct = default)
    {
        var admin = await _repository.GetByIdAsync(id, ct)
            ?? throw new AppException(ErrorCodes.AdminNotFound, "管理员不存在", 404);

        if (admin.IsSuperAdmin)
        {
            if (request.RoleId.HasValue)
                throw new AppException(ErrorCodes.Forbidden, "超级管理员不能绑定普通角色", 403);
            if (!request.IsEnabled)
                throw new AppException(ErrorCodes.Forbidden, "超级管理员不能被禁用", 403);
        }
        else
        {
            if (!request.RoleId.HasValue)
                throw new AppException(ErrorCodes.ValidationFailed, "普通管理员必须选择角色");
            if (!request.DepartmentId.HasValue)
                throw new AppException(ErrorCodes.ValidationFailed, "普通管理员必须选择主部门");

            await EnsureUsableRoleAsync(request.RoleId.Value, ct);
            await EnsureUsableDepartmentAsync(request.DepartmentId.Value, ct);
            admin.RoleId = request.RoleId;
        }

        admin.DepartmentId = request.DepartmentId;
        admin.DisplayName = request.DisplayName.Trim();
        admin.Email = request.Email?.Trim();
        admin.Mobile = request.Mobile?.Trim();
        admin.IsEnabled = request.IsEnabled;
        admin.UpdatedBy = operatorId;

        await _repository.UpdateAsync(admin, ct);
        await _permissions.ClearCacheAsync(id, ct);
    }

    public async Task ChangeStatusAsync(long id, bool enabled, long operatorId, CancellationToken ct = default)
    {
        var admin = await _repository.GetByIdAsync(id, ct)
            ?? throw new AppException(ErrorCodes.AdminNotFound, "管理员不存在", 404);
        if (admin.IsSuperAdmin && !enabled)
            throw new AppException(ErrorCodes.Forbidden, "超级管理员不能被禁用", 403);

        await _repository.UpdateStatusAsync(id, enabled, operatorId, ct);
        await _permissions.ClearCacheAsync(id, ct);
    }

    private async Task EnsureUsableRoleAsync(long roleId, CancellationToken ct)
    {
        var role = await _roles.GetByIdAsync(roleId, ct)
            ?? throw new AppException(ErrorCodes.NotFound, "角色不存在", 404);
        if (!role.IsEnabled)
            throw new AppException(ErrorCodes.ValidationFailed, "不能分配已禁用的角色");
    }

    private async Task EnsureUsableDepartmentAsync(long departmentId, CancellationToken ct)
    {
        var department = await _departments.GetByIdAsync(departmentId, ct)
            ?? throw new AppException(ErrorCodes.NotFound, "部门不存在", 404);
        if (!department.IsEnabled)
            throw new AppException(ErrorCodes.ValidationFailed, "不能分配已禁用的部门");
    }

    private static AdminDto ToDto(AdminListItem item) => new(
        item.Id,
        item.DepartmentId,
        item.DepartmentName,
        item.RoleId,
        item.RoleName,
        item.Username,
        item.DisplayName,
        item.Email,
        item.Mobile,
        item.IsSuperAdmin,
        item.IsEnabled,
        item.LastLoginAt,
        item.LastLoginIp,
        item.CreatedAt);
}
