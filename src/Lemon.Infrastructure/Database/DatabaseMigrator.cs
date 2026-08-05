using System.Reflection;
using DbUp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lemon.Infrastructure.Database;

public sealed class DatabaseMigrator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(IConfiguration configuration, ILogger<DatabaseMigrator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void Migrate()
    {
        var connectionString = _configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("ConnectionStrings:MySql 未配置");

        var result = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToAutodetectedLog()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
            throw new InvalidOperationException("数据库迁移失败", result.Error);

        _logger.LogInformation("数据库迁移完成");
    }
}
