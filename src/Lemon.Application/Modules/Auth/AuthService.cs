using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Settings;
using Lemon.Domain.System;
using Lemon.Application.Common;

namespace Lemon.Application.Modules.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IAdminRepository _admins;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ISettingService _settings;

    public AuthService(
        IAdminRepository admins,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ISettingService settings)
    {
        _admins = admins;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _settings = settings;
    }

    public async Task<AuthResponse> LoginAsync(AdminLoginRequest request, string? ip, CancellationToken ct = default)
    {
        var admin = await _admins.GetByUsernameAsync(request.Username.Trim(), ct)
            ?? throw new AppException(ErrorCodes.InvalidCredentials, "用户名或密码错误", 401);
        await EnsureLoginAllowedAsync(admin, ct);

        if (!_passwordHasher.Verify(request.Password, admin.PasswordHash, admin.PasswordSalt))
        {
            var failed = admin.FailedLoginCount + 1;
            await _admins.UpdateLoginFailureAsync(admin.Id, failed, failed >= 5 ? DateTime.UtcNow.AddMinutes(15) : null, ct);
            throw new AppException(ErrorCodes.InvalidCredentials, "用户名或密码错误", 401);
        }

        await _admins.UpdateLoginSuccessAsync(admin.Id, ip, ct);
        return await IssueAsync(admin, ip, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ip, CancellationToken ct = default)
    {
        var stored = await _refreshTokens.GetActiveAsync(_tokenService.HashRefreshToken(request.RefreshToken), ct)
            ?? throw new AppException(ErrorCodes.TokenExpired, "刷新令牌无效或已过期", 401);
        var admin = await _admins.GetByIdAsync(stored.AdminUserId, ct)
            ?? throw new AppException(ErrorCodes.AdminNotFound, "管理员不存在", 401);
        await EnsureLoginAllowedAsync(admin, ct);

        var permissions = await GetPermissionsAsync(admin, ct);
        var pair = _tokenService.CreateAdminToken(admin);
        var replacement = new RefreshToken
        {
            AdminUserId = admin.Id,
            TokenHash = _tokenService.HashRefreshToken(pair.RefreshToken),
            ExpiresAt = pair.RefreshTokenExpiresAt,
            CreatedIp = ip
        };
        if (!await _refreshTokens.RotateAsync(stored.Id, replacement.TokenHash, replacement, ct))
            throw new AppException(ErrorCodes.TokenExpired, "刷新令牌已被使用或已过期", 401);

        return await ToResponseAsync(admin, permissions, pair, ct);
    }

    public Task RevokeAllAsync(long id, CancellationToken ct = default) =>
        _refreshTokens.RevokeAllAsync(id, ct);

    private async Task<AuthResponse> IssueAsync(AdminUser admin, string? ip, CancellationToken ct)
    {
        var permissions = await GetPermissionsAsync(admin, ct);
        var pair = _tokenService.CreateAdminToken(admin);
        await _refreshTokens.CreateAsync(new RefreshToken
        {
            AdminUserId = admin.Id,
            TokenHash = _tokenService.HashRefreshToken(pair.RefreshToken),
            ExpiresAt = pair.RefreshTokenExpiresAt,
            CreatedIp = ip
        }, ct);
        return await ToResponseAsync(admin, permissions, pair, ct);
    }

    private async Task<AuthResponse> ToResponseAsync(AdminUser admin, IReadOnlyCollection<string> permissions, TokenPair pair, CancellationToken ct)
    {
        return new AuthResponse(
            admin.Id,
            admin.DepartmentId,
            admin.RoleId,
            admin.Username,
            admin.DisplayName,
            admin.IsSuperAdmin,
            false,
            null,
            null,
            null,
            pair.AccessToken,
            pair.AccessTokenExpiresAt,
            pair.RefreshToken,
            pair.RefreshTokenExpiresAt,
            permissions,
            await _settings.GetFeatureFlagsAsync(ct));
    }

    private async Task<IReadOnlyCollection<string>> GetPermissionsAsync(AdminUser admin, CancellationToken ct) =>
        admin.IsSuperAdmin ? ["*"] : await _admins.GetPermissionsAsync(admin.Id, ct);

    private async Task EnsureLoginAllowedAsync(AdminUser admin, CancellationToken ct)
    {
        if (!admin.IsEnabled)
            throw new AppException(ErrorCodes.AdminDisabled, "管理员账号已禁用", 403);
        if (admin.LockedUntil is not null && admin.LockedUntil > DateTime.UtcNow)
            throw new AppException(ErrorCodes.InvalidCredentials, "登录失败次数过多，请稍后重试", 429);
        if (admin.IsSuperAdmin) return;

        var profile = await _admins.GetAccessProfileAsync(admin.Id, ct);
        if (profile is null || !profile.RoleIsEnabled)
            throw new AppException(ErrorCodes.Forbidden, "管理员角色不存在或已禁用", 403);
        if (!profile.DepartmentId.HasValue || !profile.DepartmentIsEnabled)
            throw new AppException(ErrorCodes.Forbidden, "管理员主部门不存在或已禁用", 403);
    }
}
