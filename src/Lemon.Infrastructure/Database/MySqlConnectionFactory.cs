using System.Data.Common;
using Lemon.Application.Abstractions.Database;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Lemon.Infrastructure.Database;

public sealed class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("ConnectionStrings:MySql 未配置");
    }

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
