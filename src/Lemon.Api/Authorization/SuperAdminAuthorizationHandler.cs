using System.IdentityModel.Tokens.Jwt;
using Lemon.Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Lemon.Api.Authorization;

public sealed class SuperAdminAuthorizationHandler : AuthorizationHandler<SuperAdminRequirement>
{
    private readonly IAdminRepository _admins;
    public SuperAdminAuthorizationHandler(IAdminRepository admins) => _admins = admins;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SuperAdminRequirement requirement)
    {
        var isImpersonating = bool.TryParse(context.User.FindFirst("is_impersonating")?.Value, out var parsed) && parsed;
        if (isImpersonating) return;

        if (!long.TryParse(context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var adminId)) return;
        if (await _admins.IsSuperAdminAsync(adminId)) context.Succeed(requirement);
    }
}

public sealed class SuperAdminRequirement : IAuthorizationRequirement;
