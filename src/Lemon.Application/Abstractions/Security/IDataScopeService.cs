using Lemon.Domain.System;

namespace Lemon.Application.Abstractions.Security;

public sealed record DataScopeContext(
    long AdminId,
    bool IsSuperAdmin,
    DataScopeType ScopeType,
    long? DepartmentId,
    IReadOnlyCollection<long> DepartmentIds)
{
    public static DataScopeContext SuperAdmin(long adminId) =>
        new(adminId, true, DataScopeType.All, null, Array.Empty<long>());
}

public interface IDataScopeService
{
    Task<DataScopeContext> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<bool> CanAccessAdminAsync(long adminId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessDepartmentAsync(long departmentId, CancellationToken cancellationToken = default);
}
