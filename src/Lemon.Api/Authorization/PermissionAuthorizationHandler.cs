using System.IdentityModel.Tokens.Jwt;
using Lemon.Application.Modules.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace Lemon.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissions;
    public PermissionAuthorizationHandler(IPermissionService permissions) => _permissions = permissions;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!long.TryParse(context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var adminId)) return;
        if (await _permissions.HasPermissionAsync(adminId, requirement.Permission)) context.Succeed(requirement);
    }
}

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
