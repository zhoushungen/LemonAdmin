using Lemon.Application.Common; namespace Lemon.Application.Modules.AuditLogs;
public interface IAuditLogService{Task<PagedResult<AuditLogDto>> GetPagedAsync(int pageIndex,int pageSize,string? keyword,string? module,CancellationToken ct=default);}
