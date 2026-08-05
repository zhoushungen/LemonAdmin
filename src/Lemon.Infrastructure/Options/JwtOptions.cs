namespace Lemon.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "Lemon";
    public string AdminAudience { get; set; } = "Lemon.Admin";
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 30;
    public int ImpersonationTokenMinutes { get; set; } = 15;
}
