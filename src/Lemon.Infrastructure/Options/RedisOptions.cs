namespace Lemon.Infrastructure.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "lemon:";
}
