using Lemon.Domain.Common;

namespace Lemon.Domain.System;

public sealed class AuditLog : Entity
{
    /// <summary>当前生效身份。</summary>
    public long? AdminUserId { get; set; }

    /// <summary>当前生效身份所属部门快照。</summary>
    public long? DepartmentId { get; set; }

    /// <summary>真实操作者；未切换身份时与 AdminUserId 相同。</summary>
    public long? ActorAdminUserId { get; set; }

    public bool IsImpersonating { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? RequestSummary { get; set; }
    public int StatusCode { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string? TraceId { get; set; }
}
