using Lemon.Application.Abstractions.Security;
using Lemon.Domain.System;
using Lemon.Application.Common;

namespace Lemon.Application.Abstractions.Repositories;

public interface IAuditLogRepository
{
    Task WriteAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLog>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, string? module, DataScopeContext dataScope, CancellationToken cancellationToken = default);
}
