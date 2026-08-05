using System.Threading.RateLimiting;
using System.Reflection;
using FluentValidation.AspNetCore;
using Lemon.Api.Authorization;
using Lemon.Api.Filters;
using Lemon.Api.Middlewares;
using Lemon.Api.Services;
using Lemon.Application;
using Lemon.Application.Abstractions.Security;
using Lemon.Infrastructure;
using Lemon.Infrastructure.Bootstrap;
using Lemon.Infrastructure.Database;
using Lemon.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Lemon.Api"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SuperAdminAuthorizationHandler>();
builder.Services.AddScoped<AdminAuditActionFilter>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RequireSuperAdminAttribute.PolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new SuperAdminRequirement());
    });
});

builder.Services.AddControllers(options => options.Filters.AddService<AdminAuditActionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Lemon 通用后端 API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"), true);
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", context =>
    {
        var subject = context.User.FindFirst("sub")?.Value;
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = !string.IsNullOrWhiteSpace(subject) ? $"admin:{subject}" : $"ip:{remoteIp}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    });
});

var app = builder.Build();

ValidateProductionConfiguration(app);

if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().Migrate();
    await scope.ServiceProvider.GetRequiredService<SystemBootstrapper>().EnsureBootstrapAdminAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    var uploadRoot = Path.GetFullPath(app.Configuration["Storage:LocalRoot"] ?? "uploads");
    Directory.CreateDirectory(uploadRoot);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadRoot),
        RequestPath = "/uploads"
    });
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("api");
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" })).AllowAnonymous();
app.Run();

static void ValidateProductionConfiguration(WebApplication app)
{
    if (app.Environment.IsDevelopment()) return;

    var jwt = app.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    if (jwt.Secret.Contains("PLEASE_CHANGE", StringComparison.OrdinalIgnoreCase) ||
        jwt.Secret.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("生产环境必须通过环境变量设置随机 JWT 密钥");

    if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
        throw new InvalidOperationException("生产环境禁止 API 启动时自动执行数据库迁移");

    var bootstrap = app.Configuration.GetSection(BootstrapAdminOptions.SectionName).Get<BootstrapAdminOptions>() ?? new BootstrapAdminOptions();
    if (bootstrap.Enabled)
        throw new InvalidOperationException("生产环境必须关闭 BootstrapAdmin:Enabled");
}
