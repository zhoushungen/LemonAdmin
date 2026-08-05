namespace Lemon.Application.Modules.AuditLogs;

public sealed record AuditLogDto(
    long Id,
    long? AdminUserId,
    long? DepartmentId,
    long? ActorAdminUserId,
    bool IsImpersonating,
    string Module,
    string Action,
    string RequestPath,
    string HttpMethod,
    int StatusCode,
    string? IpAddress,
    long ElapsedMilliseconds,
    string? TraceId,
    DateTime CreatedAt);
