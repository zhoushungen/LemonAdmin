using Lemon.Domain.System;

namespace Lemon.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetActiveAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> RotateAsync(long tokenId, string replacementHash, RefreshToken replacement, CancellationToken cancellationToken = default);
    Task RevokeAllAsync(long adminUserId, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
}
