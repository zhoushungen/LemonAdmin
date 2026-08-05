using Dapper;
using Lemon.Application.Abstractions.Database;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Domain.System;

namespace Lemon.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _factory;
    public RefreshTokenRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task CreateAsync(RefreshToken token, CancellationToken ct = default)
    {
        const string sql = "INSERT INTO sys_refresh_token(admin_user_id,token_hash,expires_at,created_ip,created_at) VALUES(@AdminUserId,@TokenHash,@ExpiresAt,@CreatedIp,UTC_TIMESTAMP())";
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(sql, token, cancellationToken: ct));
    }

    public async Task<RefreshToken?> GetActiveAsync(string tokenHash, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM sys_refresh_token WHERE token_hash=@Hash AND revoked_at IS NULL AND expires_at>UTC_TIMESTAMP() LIMIT 1";
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<RefreshToken>(
            new CommandDefinition(sql, new { Hash = tokenHash }, cancellationToken: ct));
    }

    public async Task<bool> RotateAsync(
        long tokenId,
        string replacementHash,
        RefreshToken replacement,
        CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            var updated = await db.ExecuteAsync(new CommandDefinition(
                "UPDATE sys_refresh_token SET revoked_at=UTC_TIMESTAMP(),replaced_by_token_hash=@Hash WHERE id=@Id AND revoked_at IS NULL AND expires_at>UTC_TIMESTAMP()",
                new { Id = tokenId, Hash = replacementHash },
                transaction,
                cancellationToken: ct));

            if (updated != 1)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            await db.ExecuteAsync(new CommandDefinition(
                "INSERT INTO sys_refresh_token(admin_user_id,token_hash,expires_at,created_ip,created_at) VALUES(@AdminUserId,@TokenHash,@ExpiresAt,@CreatedIp,UTC_TIMESTAMP())",
                replacement,
                transaction,
                cancellationToken: ct));
            await transaction.CommitAsync(ct);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task RevokeAllAsync(long adminUserId, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_refresh_token SET revoked_at=UTC_TIMESTAMP() WHERE admin_user_id=@Id AND revoked_at IS NULL",
            new { Id = adminUserId }, cancellationToken: ct));
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteAsync(new CommandDefinition(
            "DELETE FROM sys_refresh_token WHERE expires_at<DATE_SUB(UTC_TIMESTAMP(), INTERVAL 7 DAY)",
            cancellationToken: ct));
    }
}
