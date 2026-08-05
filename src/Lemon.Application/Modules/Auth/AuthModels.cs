using Lemon.Application.Modules.Settings;

namespace Lemon.Application.Modules.Auth;

public sealed record AdminLoginRequest(string Username, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthResponse(
    long UserId,
    long? DepartmentId,
    long? RoleId,
    string Username,
    string DisplayName,
    bool IsSuperAdmin,
    bool IsImpersonating,
    long? OriginalUserId,
    string? OriginalUsername,
    string? OriginalDisplayName,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    IReadOnlyCollection<string> Permissions,
    SystemFeatureFlags Features);
