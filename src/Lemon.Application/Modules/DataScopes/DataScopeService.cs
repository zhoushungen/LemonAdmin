using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Domain.System;
using Lemon.Application.Common;

namespace Lemon.Application.Modules.DataScopes;

public sealed class DataScopeService : IDataScopeService
{
    private readonly ICurrentUser _currentUser;
    private readonly IAdminRepository _admins;
    private readonly IDepartmentRepository _departments;
    private Task<DataScopeContext>? _currentTask;

    public DataScopeService(
        ICurrentUser currentUser,
        IAdminRepository admins,
        IDepartmentRepository departments)
    {
        _currentUser = currentUser;
        _admins = admins;
        _departments = departments;
    }

    public Task<DataScopeContext> GetCurrentAsync(CancellationToken ct = default) =>
        _currentTask ??= ResolveAsync(ct);

    public async Task<bool> CanAccessAdminAsync(long adminId, CancellationToken ct = default)
    {
        var scope = await GetCurrentAsync(ct);
        if (scope.IsSuperAdmin || scope.ScopeType == DataScopeType.All || scope.AdminId == adminId) return true;
        if (scope.ScopeType == DataScopeType.Self) return false;

        var target = await _admins.GetByIdAsync(adminId, ct);
        return target?.DepartmentId is long departmentId && scope.DepartmentIds.Contains(departmentId);
    }

    public async Task<bool> CanAccessDepartmentAsync(long departmentId, CancellationToken ct = default)
    {
        var scope = await GetCurrentAsync(ct);
        if (scope.IsSuperAdmin || scope.ScopeType == DataScopeType.All) return true;
        if (scope.ScopeType == DataScopeType.Self) return false;
        return scope.DepartmentIds.Contains(departmentId);
    }

    private async Task<DataScopeContext> ResolveAsync(CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            throw new AppException(ErrorCodes.Unauthorized, "未登录", 401);

        var profile = await _admins.GetAccessProfileAsync(_currentUser.UserId.Value, ct)
            ?? throw new AppException(ErrorCodes.AdminNotFound, "管理员不存在", 401);

        if (!profile.IsEnabled)
            throw new AppException(ErrorCodes.AdminDisabled, "管理员账号已禁用", 403);

        if (!profile.RoleId.HasValue)
            return DataScopeContext.SuperAdmin(profile.Id);

        if (!profile.RoleIsEnabled)
            throw new AppException(ErrorCodes.Forbidden, "管理员角色不存在或已禁用", 403);
        if (!profile.DepartmentId.HasValue || !profile.DepartmentIsEnabled)
            throw new AppException(ErrorCodes.Forbidden, "管理员主部门不存在或已禁用", 403);

        var departments = await _departments.GetAllAsync(ct);
        var allIds = departments.Select(x => x.Id).ToHashSet();
        var roots = new HashSet<long>();

        switch (profile.DataScope)
        {
            case DataScopeType.All:
                return new DataScopeContext(profile.Id, false, profile.DataScope, profile.DepartmentId, allIds);
            case DataScopeType.Department:
                if (profile.DepartmentId.HasValue) roots.Add(profile.DepartmentId.Value);
                break;
            case DataScopeType.DepartmentAndChildren:
                if (profile.DepartmentId.HasValue) roots.Add(profile.DepartmentId.Value);
                return new DataScopeContext(profile.Id, false, profile.DataScope, profile.DepartmentId, ExpandWithChildren(roots, departments));
            case DataScopeType.ManagedDepartments:
                foreach (var department in departments.Where(x => x.ManagerAdminId == profile.Id)) roots.Add(department.Id);
                return new DataScopeContext(profile.Id, false, profile.DataScope, profile.DepartmentId, ExpandWithChildren(roots, departments));
            case DataScopeType.Self:
                if (profile.DepartmentId.HasValue) roots.Add(profile.DepartmentId.Value);
                break;
            default:
                break;
        }

        return new DataScopeContext(profile.Id, false, profile.DataScope, profile.DepartmentId, roots);
    }

    private static IReadOnlyCollection<long> ExpandWithChildren(
        IReadOnlyCollection<long> roots,
        IReadOnlyCollection<DepartmentListItem> departments)
    {
        if (roots.Count == 0) return Array.Empty<long>();

        var result = roots.ToHashSet();
        var childrenByParent = departments
            .Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToArray());

        var queue = new Queue<long>(roots);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current, out var children)) continue;
            foreach (var child in children)
            {
                if (result.Add(child)) queue.Enqueue(child);
            }
        }

        return result;
    }
}
