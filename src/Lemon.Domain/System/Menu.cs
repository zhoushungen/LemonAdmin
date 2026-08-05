using Lemon.Domain.Common;
namespace Lemon.Domain.System;
public sealed class Menu : Entity
{
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MenuType { get; set; } = "page";
    public string? RouteName { get; set; }
    public string? RoutePath { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public string? PermissionCode { get; set; }
    public int Sort { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }
}
