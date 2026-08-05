using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Lemon.Api.Authorization;

public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequirePermissionAttribute.Prefix, StringComparison.Ordinal))
            return await base.GetPolicyAsync(policyName);
        var permission = policyName[RequirePermissionAttribute.Prefix.Length..];
        return new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission)).Build();
    }
}
