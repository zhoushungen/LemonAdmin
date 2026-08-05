using Lemon.Application.Abstractions.Cache;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Auth;
using Lemon.Application.Modules.Settings;
using Lemon.Domain.System;
using Lemon.Application.Common;

namespace Lemon.Application.Modules.Impersonation;

public sealed class ImpersonationService : IImpersonationService
{
    private readonly IAdminRepository _admins;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokens;
    private readonly ISettingService _settings;
    private readonly ICacheService _cache;

    public ImpersonationService(
        IAdminRepository admins,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokens,
        ISettingService settings,
        ICacheService cache)
    {
        _admins = admins;
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _settings = settings;
        _cache = cache;
    }

    public async Task<AuthResponse> StartAsync(
        long actorAdminId,
        StartImpersonationRequest request,
        CancellationToken ct = default)
    {
        var actor = await GetEnabledSuperAdminAsync(actorAdminId, ct);
        var features = await _settings.GetFeatureFlagsAsync(ct);
        if (!features.AccountSwitchEnabled)
            throw new AppException(ErrorCodes.Forbidden, "系统未启用账号切换", 403);

        if (request.TargetAdminId == actorAdminId)
            throw new AppException(ErrorCodes.ValidationFailed, "不能切换到当前账号");

        var target = await _admins.GetByIdAsync(request.TargetAdminId, ct)
            ?? throw new AppException(ErrorCodes.AdminNotFound, "目标管理员不存在", 404);
        if (!target.IsEnabled)
            throw new AppException(ErrorCodes.AdminDisabled, "目标管理员已禁用", 403);
        if (target.IsSuperAdmin)
            throw new AppException(ErrorCodes.Forbidden, "不能切换到另一个超级管理员", 403);

        var profile = await _admins.GetAccessProfileAsync(target.Id, ct);
        if (profile is null || !profile.RoleIsEnabled)
            throw new AppException(ErrorCodes.Forbidden, "目标管理员角色不存在或已禁用", 403);
        if (!profile.DepartmentId.HasValue || !profile.DepartmentIsEnabled)
            throw new AppException(ErrorCodes.Forbidden, "目标管理员主部门不存在或已禁用", 403);

        var permissions = await _admins.GetPermissionsAsync(target.Id, ct);
        var sessionId = Guid.NewGuid().ToString("N");
        var token = _tokens.CreateImpersonationToken(target, actor, sessionId);
        var ttl = token.AccessTokenExpiresAt - DateTime.UtcNow;
        await _cache.SetAsync(ImpersonationCacheKeys.Session(sessionId), $"{actor.Id}:{target.Id}", ttl, ct);

        return new AuthResponse(
            target.Id,
            target.DepartmentId,
            target.RoleId,
            target.Username,
            target.DisplayName,
            false,
            true,
            actor.Id,
            actor.Username,
            actor.DisplayName,
            token.AccessToken,
            token.AccessTokenExpiresAt,
            string.Empty,
            token.AccessTokenExpiresAt,
            permissions,
            features);
    }

    public async Task<AuthResponse> StopAsync(
        long actorAdminId,
        string sessionId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var actor = await GetEnabledSuperAdminAsync(actorAdminId, ct);
        await _cache.RemoveAsync(ImpersonationCacheKeys.Session(sessionId), ct);

        var permissions = new[] { "*" };
        var pair = _tokens.CreateAdminToken(actor);
        await _refreshTokens.CreateAsync(new RefreshToken
        {
            AdminUserId = actor.Id,
            TokenHash = _tokens.HashRefreshToken(pair.RefreshToken),
            ExpiresAt = pair.RefreshTokenExpiresAt,
            CreatedIp = ipAddress
        }, ct);

        return new AuthResponse(
            actor.Id,
            actor.DepartmentId,
            actor.RoleId,
            actor.Username,
            actor.DisplayName,
            true,
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

    public Task CancelSessionAsync(string? sessionId, CancellationToken ct = default) =>
        string.IsNullOrWhiteSpace(sessionId)
            ? Task.CompletedTask
            : _cache.RemoveAsync(ImpersonationCacheKeys.Session(sessionId), ct);

    private async Task<AdminUser> GetEnabledSuperAdminAsync(long adminId, CancellationToken ct)
    {
        var admin = await _admins.GetByIdAsync(adminId, ct)
            ?? throw new AppException(ErrorCodes.AdminNotFound, "超级管理员不存在", 401);
        if (!admin.IsEnabled || !admin.IsSuperAdmin)
            throw new AppException(ErrorCodes.Forbidden, "仅超级管理员可执行此操作", 403);
        return admin;
    }
}
