using Lemon.Application.Modules.Admins;

namespace Lemon.Application.Modules.Departments;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminOptionDto>> GetManagerOptionsAsync(CancellationToken cancellationToken = default);
    Task<long> CreateAsync(SaveDepartmentRequest request, long operatorId, CancellationToken cancellationToken = default);
    Task UpdateAsync(long id, SaveDepartmentRequest request, long operatorId, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, long operatorId, CancellationToken cancellationToken = default);
}
