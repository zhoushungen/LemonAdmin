namespace Lemon.Application.Abstractions.Security;

public interface ICurrentUser
{
    /// <summary>当前生效的管理员身份。</summary>
    long? UserId { get; }

    /// <summary>真实登录身份；未切换账号时等于 UserId。</summary>
    long? ActorUserId { get; }

    long? DepartmentId { get; }
    long? RoleId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
    bool IsImpersonating { get; }
    string? ImpersonationSessionId { get; }
    string? IpAddress { get; }
}
