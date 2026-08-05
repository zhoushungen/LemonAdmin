namespace Lemon.Application.Modules.Menus;
public sealed record MenuDto(long Id,long? ParentId,string Name,string MenuType,string? RouteName,string? RoutePath,string? Component,string? Icon,string? PermissionCode,int Sort,bool IsVisible,bool IsEnabled);
public sealed record SaveMenuRequest(long? ParentId,string Name,string MenuType,string? RouteName,string? RoutePath,string? Component,string? Icon,string? PermissionCode,int Sort,bool IsVisible,bool IsEnabled);
