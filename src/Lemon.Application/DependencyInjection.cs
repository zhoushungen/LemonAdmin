using FluentValidation;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Admins;
using Lemon.Application.Modules.AuditLogs;
using Lemon.Application.Modules.Auth;
using Lemon.Application.Modules.Departments;
using Lemon.Application.Modules.DataScopes;
using Lemon.Application.Modules.Impersonation;
using Lemon.Application.Modules.Menus;
using Lemon.Application.Modules.Permissions;
using Lemon.Application.Modules.Roles;
using Lemon.Application.Modules.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Lemon.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AdminLoginRequestValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IDataScopeService, DataScopeService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IImpersonationService, ImpersonationService>();
        return services;
    }
}
