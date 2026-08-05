using Microsoft.AspNetCore.Authorization;

namespace Lemon.Api.Authorization;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string Prefix = "Permission:";
    public RequirePermissionAttribute(string permission) => Policy = Prefix + permission;
}
