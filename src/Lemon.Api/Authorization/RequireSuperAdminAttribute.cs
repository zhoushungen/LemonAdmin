using Microsoft.AspNetCore.Authorization;

namespace Lemon.Api.Authorization;

public sealed class RequireSuperAdminAttribute : AuthorizeAttribute
{
    public const string PolicyName = "SuperAdmin";
    public RequireSuperAdminAttribute() => Policy = PolicyName;
}
