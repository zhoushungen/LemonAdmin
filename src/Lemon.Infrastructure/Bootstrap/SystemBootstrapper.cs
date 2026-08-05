using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Domain.System;
using Lemon.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lemon.Infrastructure.Bootstrap;

public sealed class SystemBootstrapper
{
    private readonly IAdminRepository _admins;
    private readonly IPasswordHasher _hasher;
    private readonly BootstrapAdminOptions _options;
    private readonly ILogger<SystemBootstrapper> _logger;

    public SystemBootstrapper(
        IAdminRepository admins,
        IPasswordHasher hasher,
        IOptions<BootstrapAdminOptions> options,
        ILogger<SystemBootstrapper> logger)
    {
        _admins = admins;
        _hasher = hasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureBootstrapAdminAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled || await _admins.CountAsync(ct) > 0) return;

        var password = _hasher.Hash(_options.Password);
        await _admins.CreateAsync(new AdminUser
        {
            RoleId = null,
            Username = _options.Username,
            DisplayName = _options.DisplayName,
            PasswordHash = password.Hash,
            PasswordSalt = password.Salt,
            IsEnabled = true
        }, ct);

        _logger.LogWarning("已创建初始化超级管理员 {Username}，请立即修改密码并关闭 BootstrapAdmin:Enabled", _options.Username);
    }
}
