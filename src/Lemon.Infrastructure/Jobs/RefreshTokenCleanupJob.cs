using Lemon.Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lemon.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class RefreshTokenCleanupJob : IJob
{
    private readonly IRefreshTokenRepository _repository;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(IRefreshTokenRepository repository, ILogger<RefreshTokenCleanupJob> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var count = await _repository.DeleteExpiredAsync(context.CancellationToken);
        _logger.LogInformation("Refresh Token 清理完成，删除 {Count} 条", count);
    }
}
