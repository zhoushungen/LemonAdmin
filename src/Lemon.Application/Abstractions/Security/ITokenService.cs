using Lemon.Domain.System;

namespace Lemon.Application.Abstractions.Security;

public sealed record TokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed record AccessTokenResult(string AccessToken, DateTime AccessTokenExpiresAt);

public interface ITokenService
{
    TokenPair CreateAdminToken(AdminUser admin);

    AccessTokenResult CreateImpersonationToken(
        AdminUser targetAdmin,
        AdminUser actorAdmin,
        string sessionId);

    string HashRefreshToken(string token);
}
