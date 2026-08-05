using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lemon.Application.Abstractions.Security;
using Lemon.Domain.System;
using Lemon.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lemon.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public TokenPair CreateAdminToken(AdminUser admin)
    {
        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(_options.AccessTokenMinutes);
        var accessToken = CreateToken(admin, now, accessExpires, false, null, null);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenPair(
            accessToken,
            accessExpires,
            refreshToken,
            now.AddDays(_options.RefreshTokenDays));
    }

    public AccessTokenResult CreateImpersonationToken(
        AdminUser targetAdmin,
        AdminUser actorAdmin,
        string sessionId)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(Math.Clamp(_options.ImpersonationTokenMinutes, 5, 30));
        return new AccessTokenResult(
            CreateToken(targetAdmin, now, expires, true, actorAdmin, sessionId),
            expires);
    }

    public string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private string CreateToken(
        AdminUser admin,
        DateTime now,
        DateTime expires,
        bool isImpersonating,
        AdminUser? actorAdmin,
        string? sessionId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, admin.Username),
            new("display_name", admin.DisplayName),
            new("token_type", "admin"),
            new("is_super_admin", (!isImpersonating && admin.IsSuperAdmin).ToString().ToLowerInvariant(), ClaimValueTypes.Boolean),
            new("is_impersonating", isImpersonating.ToString().ToLowerInvariant(), ClaimValueTypes.Boolean),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (admin.DepartmentId.HasValue)
            claims.Add(new Claim("department_id", admin.DepartmentId.Value.ToString()));
        if (admin.RoleId.HasValue)
            claims.Add(new Claim("role_id", admin.RoleId.Value.ToString()));

        if (isImpersonating && actorAdmin is not null && !string.IsNullOrWhiteSpace(sessionId))
        {
            claims.Add(new Claim("actor_sub", actorAdmin.Id.ToString()));
            claims.Add(new Claim("actor_username", actorAdmin.Username));
            claims.Add(new Claim("actor_display_name", actorAdmin.DisplayName));
            claims.Add(new Claim("imp_session", sessionId));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_options.Issuer, _options.AdminAudience, claims, now, expires, credentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
