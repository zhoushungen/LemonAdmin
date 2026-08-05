using Lemon.Application.Common;

namespace Lemon.Application.Modules.Admins;

public interface IAdminService
{
    Task<PagedResult<AdminDto>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, long? departmentId, bool? enabled, CancellationToken cancellationToken = default);
    Task<AdminDetailDto> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<long> CreateAsync(CreateAdminRequest request, long operatorId, CancellationToken cancellationToken = default);
    Task UpdateAsync(long id, UpdateAdminRequest request, long operatorId, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(long id, bool enabled, long operatorId, CancellationToken cancellationToken = default);
}
