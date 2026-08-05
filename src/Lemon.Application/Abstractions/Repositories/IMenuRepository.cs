using Lemon.Domain.System;
namespace Lemon.Application.Abstractions.Repositories;
public interface IMenuRepository
{
    Task<IReadOnlyList<Menu>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Menu>> GetAuthorizedAsync(long adminId, bool isSuperAdmin, CancellationToken cancellationToken = default);
    Task<Menu?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> CreateAsync(Menu menu, CancellationToken cancellationToken = default);
    Task UpdateAsync(Menu menu, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, long operatorId, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken = default);
}
