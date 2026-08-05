using Lemon.Domain.Common;

namespace Lemon.Domain.System;

public sealed class RefreshToken : Entity
{
    public long AdminUserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedIp { get; set; }
}
