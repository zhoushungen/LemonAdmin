using System.Text;
using Dapper;
using Lemon.Application.Abstractions.Cache;
using Lemon.Application.Abstractions.Database;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Abstractions.Storage;
using Lemon.Application.Modules.Impersonation;
using Lemon.Application.Modules.Settings;
using Lemon.Infrastructure.Bootstrap;
using Lemon.Infrastructure.Cache;
using Lemon.Infrastructure.Database;
using Lemon.Infrastructure.Jobs;
using Lemon.Infrastructure.Options;
using Lemon.Infrastructure.Repositories;
using Lemon.Infrastructure.Security;
using Lemon.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using StackExchange.Redis;

namespace Lemon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));

        services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<DatabaseMigrator>();
        services.AddScoped<SystemBootstrapper>();

        var storage = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        switch (storage.Provider.Trim().ToLowerInvariant())
        {
            case "local":
                services.AddSingleton<IObjectStorage, LocalObjectStorage>();
                break;
            case "qiniu":
                services.AddSingleton<IObjectStorage, QiniuObjectStorage>();
                break;
            default:
                throw new InvalidOperationException($"不支持的对象存储 Provider: {storage.Provider}");
        }

        var redis = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        services.AddMemoryCache();
        if (redis.Enabled)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis.ConnectionString));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (Encoding.UTF8.GetByteCount(jwt.Secret) < 32)
            throw new InvalidOperationException("Jwt:Secret 至少需要 32 字节");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.AdminAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var isImpersonating = bool.TryParse(context.Principal?.FindFirst("is_impersonating")?.Value, out var parsed) && parsed;
                        if (!isImpersonating) return;

                        var sessionId = context.Principal?.FindFirst("imp_session")?.Value;
                        if (string.IsNullOrWhiteSpace(sessionId))
                        {
                            context.Fail("无效的账号切换会话");
                            return;
                        }

                        var services = context.HttpContext.RequestServices;
                        var cache = services.GetRequiredService<ICacheService>();
                        if (!await cache.ExistsAsync(ImpersonationCacheKeys.Session(sessionId), context.HttpContext.RequestAborted))
                        {
                            context.Fail("账号切换会话已结束或过期");
                            return;
                        }

                        var settings = services.GetRequiredService<ISettingService>();
                        var features = await settings.GetFeatureFlagsAsync(context.HttpContext.RequestAborted);
                        if (!features.AccountSwitchEnabled)
                            context.Fail("系统已关闭账号切换");
                    }
                };
            });

        services.AddQuartz(quartz =>
        {
            var key = new JobKey("refresh-token-cleanup");
            quartz.AddJob<RefreshTokenCleanupJob>(options => options.WithIdentity(key));
            quartz.AddTrigger(options => options
                .ForJob(key)
                .WithIdentity("refresh-token-cleanup-daily")
                .WithCronSchedule("0 0 3 * * ?"));
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        return services;
    }
}
