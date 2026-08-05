using Lemon.Domain.System;

namespace Lemon.Application.Abstractions.Repositories;

public sealed class DepartmentListItem
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public long? ManagerAdminId { get; set; }
    public string? ManagerName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
}

public interface IDepartmentRepository
{
    Task<IReadOnlyList<DepartmentListItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Department?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> CreateAsync(Department department, CancellationToken cancellationToken = default);
    Task UpdateAsync(Department department, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, long operatorId, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenOrAdminsAsync(long id, CancellationToken cancellationToken = default);
}
