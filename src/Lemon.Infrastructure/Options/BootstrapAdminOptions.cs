namespace Lemon.Infrastructure.Options;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";
    public bool Enabled { get; set; } = true;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "ChangeMe_123456";
    public string DisplayName { get; set; } = "超级管理员";
}
