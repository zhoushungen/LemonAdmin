using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Common;
using Lemon.Domain.System;

namespace Lemon.Application.Modules.Menus;

public sealed class MenuService : IMenuService
{
    private readonly IMenuRepository _repository;
    private readonly IDataScopeService _dataScope;

    public MenuService(IMenuRepository repository, IDataScopeService dataScope)
    {
        _repository = repository;
        _dataScope = dataScope;
    }

    public async Task<IReadOnlyList<MenuDto>> GetAllAsync(CancellationToken ct = default) =>
        (await _repository.GetAllAsync(ct)).Select(ToDto).ToArray();

    public async Task<IReadOnlyList<MenuDto>> GetCurrentAsync(long adminId, CancellationToken ct = default)
    {
        var scope = await _dataScope.GetCurrentAsync(ct);
        if (scope.AdminId != adminId)
            throw new AppException(ErrorCodes.Unauthorized, "登录身份无效", 401);

        return (await _repository.GetAuthorizedAsync(adminId, scope.IsSuperAdmin, ct))
            .Select(ToDto)
            .ToArray();
    }

    public async Task<long> CreateAsync(SaveMenuRequest request, long operatorId, CancellationToken ct = default)
    {
        if (request.ParentId.HasValue)
            await EnsureParentExistsAsync(request.ParentId.Value, ct);

        return await _repository.CreateAsync(Map(request, new Menu { CreatedBy = operatorId }), ct);
    }

    public async Task UpdateAsync(long id, SaveMenuRequest request, long operatorId, CancellationToken ct = default)
    {
        var menu = await _repository.GetByIdAsync(id, ct)
            ?? throw new AppException(ErrorCodes.NotFound, "菜单不存在", 404);

        if (request.ParentId == id)
            throw new AppException(ErrorCodes.ValidationFailed, "上级菜单不能选择自身");
        if (request.ParentId.HasValue)
        {
            await EnsureParentExistsAsync(request.ParentId.Value, ct);
            await EnsureNoCycleAsync(id, request.ParentId.Value, ct);
        }

        menu.UpdatedBy = operatorId;
        await _repository.UpdateAsync(Map(request, menu), ct);
    }

    public async Task DeleteAsync(long id, long operatorId, CancellationToken ct = default)
    {
        _ = await _repository.GetByIdAsync(id, ct)
            ?? throw new AppException(ErrorCodes.NotFound, "菜单不存在", 404);
        if (await _repository.HasChildrenAsync(id, ct))
            throw new AppException(ErrorCodes.ValidationFailed, "请先删除子菜单");

        await _repository.DeleteAsync(id, operatorId, ct);
    }

    private async Task EnsureParentExistsAsync(long parentId, CancellationToken ct)
    {
        _ = await _repository.GetByIdAsync(parentId, ct)
            ?? throw new AppException(ErrorCodes.NotFound, "上级菜单不存在", 404);
    }

    private async Task EnsureNoCycleAsync(long id, long parentId, CancellationToken ct)
    {
        var parentMap = (await _repository.GetAllAsync(ct)).ToDictionary(x => x.Id, x => x.ParentId);
        long? current = parentId;
        var visited = new HashSet<long>();

        while (current.HasValue && visited.Add(current.Value))
        {
            if (current.Value == id)
                throw new AppException(ErrorCodes.ValidationFailed, "不能将菜单移动到自己的下级菜单");
            current = parentMap.GetValueOrDefault(current.Value);
        }
    }

    private static Menu Map(SaveMenuRequest request, Menu menu)
    {
        menu.ParentId = request.ParentId;
        menu.Name = request.Name.Trim();
        menu.MenuType = request.MenuType;
        menu.RouteName = request.RouteName?.Trim();
        menu.RoutePath = request.RoutePath?.Trim();
        menu.Component = request.Component?.Trim();
        menu.Icon = request.Icon?.Trim();
        menu.PermissionCode = request.PermissionCode?.Trim();
        menu.Sort = request.Sort;
        menu.IsVisible = request.IsVisible;
        menu.IsEnabled = request.IsEnabled;
        return menu;
    }

    private static MenuDto ToDto(Menu menu) => new(
        menu.Id,
        menu.ParentId,
        menu.Name,
        menu.MenuType,
        menu.RouteName,
        menu.RoutePath,
        menu.Component,
        menu.Icon,
        menu.PermissionCode,
        menu.Sort,
        menu.IsVisible,
        menu.IsEnabled);
}
