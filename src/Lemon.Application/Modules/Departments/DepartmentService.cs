using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Common;
using Lemon.Application.Modules.Admins;
using Lemon.Domain.System;

namespace Lemon.Application.Modules.Departments;

public sealed class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repository;
    private readonly IAdminRepository _admins;
    private readonly IDataScopeService _dataScope;

    public DepartmentService(
        IDepartmentRepository repository,
        IAdminRepository admins,
        IDataScopeService dataScope)
    {
        _repository = repository;
        _admins = admins;
        _dataScope = dataScope;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var scope = await _dataScope.GetCurrentAsync(ct);
        var departments = await _repository.GetAllAsync(ct);
        var visible = scope.IsSuperAdmin || scope.ScopeType == DataScopeType.All
            ? departments
            : departments.Where(x => scope.DepartmentIds.Contains(x.Id));

        return visible.Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyList<AdminOptionDto>> GetManagerOptionsAsync(CancellationToken ct = default)
    {
        var scope = await _dataScope.GetCurrentAsync(ct);
        return (await _admins.GetOptionsAsync(scope, ct))
            .Select(x => new AdminOptionDto(x.Id, x.DisplayName, x.Username, x.DepartmentId))
            .ToArray();
    }

    public async Task<long> CreateAsync(SaveDepartmentRequest request, long operatorId, CancellationToken ct = default)
    {
        var scope = await _dataScope.GetCurrentAsync(ct);
        if (!scope.IsSuperAdmin && scope.ScopeType != DataScopeType.All)
        {
            if (!request.ParentId.HasValue)
                throw new AppException(ErrorCodes.Forbidden, "当前数据范围不能新建根部门", 403);
            if (!await _dataScope.CanAccessDepartmentAsync(request.ParentId.Value, ct))
                throw new AppException(ErrorCodes.Forbidden, "无权在该上级部门下新建部门", 403);
        }

        if (request.ParentId.HasValue)
            await EnsureDepartmentExistsAsync(request.ParentId.Value, ct);
        await EnsureManagerAsync(request.ManagerAdminId, ct);

        if (await _repository.CodeExistsAsync(request.Code.Trim(), cancellationToken: ct))
            throw new AppException(ErrorCodes.DuplicateData, "部门代码已存在");

        return await _repository.CreateAsync(Map(request, new Department { CreatedBy = operatorId }), ct);
    }

    public async Task UpdateAsync(long id, SaveDepartmentRequest request, long operatorId, CancellationToken ct = default)
    {
        if (!await _dataScope.CanAccessDepartmentAsync(id, ct))
            throw new AppException(ErrorCodes.Forbidden, "无权修改该部门", 403);

        var department = await _repository.GetByIdAsync(id, ct)
            ?? throw new AppException(ErrorCodes.NotFound, "部门不存在", 404);
        if (request.ParentId == id)
            throw new AppException(ErrorCodes.ValidationFailed, "上级部门不能选择自身");

        var scope = await _dataScope.GetCurrentAsync(ct);
        if (request.ParentId.HasValue)
        {
            await EnsureDepartmentExistsAsync(request.ParentId.Value, ct);
            if (!scope.IsSuperAdmin && scope.ScopeType != DataScopeType.All &&
                !await _dataScope.CanAccessDepartmentAsync(request.ParentId.Value, ct))
                throw new AppException(ErrorCodes.Forbidden, "无权选择该上级部门", 403);
        }
        else if (!scope.IsSuperAdmin && scope.ScopeType != DataScopeType.All)
        {
            throw new AppException(ErrorCodes.Forbidden, "当前数据范围不能将部门调整为根部门", 403);
        }

        await EnsureNoCycleAsync(id, request.ParentId, ct);
        await EnsureManagerAsync(request.ManagerAdminId, ct);

        if (await _repository.CodeExistsAsync(request.Code.Trim(), id, ct))
            throw new AppException(ErrorCodes.DuplicateData, "部门代码已存在");

        Map(request, department);
        department.UpdatedBy = operatorId;
        await _repository.UpdateAsync(department, ct);
    }

    public async Task DeleteAsync(long id, long operatorId, CancellationToken ct = default)
    {
        if (!await _dataScope.CanAccessDepartmentAsync(id, ct))
            throw new AppException(ErrorCodes.Forbidden, "无权删除该部门", 403);
        if (await _repository.HasChildrenOrAdminsAsync(id, ct))
            throw new AppException(ErrorCodes.ValidationFailed, "部门存在子部门或管理员，不能删除");

        await _repository.DeleteAsync(id, operatorId, ct);
    }

    private async Task EnsureManagerAsync(long? managerAdminId, CancellationToken ct)
    {
        if (!managerAdminId.HasValue) return;
        if (!await _dataScope.CanAccessAdminAsync(managerAdminId.Value, ct))
            throw new AppException(ErrorCodes.Forbidden, "无权选择该部门主管", 403);

        var manager = await _admins.GetByIdAsync(managerAdminId.Value, ct)
            ?? throw new AppException(ErrorCodes.AdminNotFound, "部门主管不存在", 404);
        if (!manager.IsEnabled || manager.IsSuperAdmin)
            throw new AppException(ErrorCodes.ValidationFailed, "部门主管必须是启用的普通管理员");
    }

    private async Task EnsureDepartmentExistsAsync(long departmentId, CancellationToken ct)
    {
        _ = await _repository.GetByIdAsync(departmentId, ct)
            ?? throw new AppException(ErrorCodes.NotFound, "上级部门不存在", 404);
    }

    private async Task EnsureNoCycleAsync(long id, long? parentId, CancellationToken ct)
    {
        if (!parentId.HasValue) return;

        var parentMap = (await _repository.GetAllAsync(ct)).ToDictionary(x => x.Id, x => x.ParentId);
        var current = parentId;
        var visited = new HashSet<long>();
        while (current.HasValue && visited.Add(current.Value))
        {
            if (current.Value == id)
                throw new AppException(ErrorCodes.ValidationFailed, "不能将部门移动到自己的下级部门");
            current = parentMap.GetValueOrDefault(current.Value);
        }
    }

    private static Department Map(SaveDepartmentRequest request, Department department)
    {
        department.ParentId = request.ParentId;
        department.ManagerAdminId = request.ManagerAdminId;
        department.Name = request.Name.Trim();
        department.Code = request.Code.Trim();
        department.Phone = request.Phone?.Trim();
        department.Email = request.Email?.Trim();
        department.Sort = request.Sort;
        department.IsEnabled = request.IsEnabled;
        return department;
    }

    private static DepartmentDto ToDto(DepartmentListItem item) => new(
        item.Id,
        item.ParentId,
        item.ManagerAdminId,
        item.ManagerName,
        item.Name,
        item.Code,
        item.Phone,
        item.Email,
        item.Sort,
        item.IsEnabled);
}
