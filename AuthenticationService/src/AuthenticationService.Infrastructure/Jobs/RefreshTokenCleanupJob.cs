using AuthenticationService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace AuthenticationService.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class RefreshTokenCleanupJob : IJob
{
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromDays(30);

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(IRefreshTokenRepository refreshTokenRepository, ILogger<RefreshTokenCleanupJob> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cutoff = DateTime.UtcNow - RetentionWindow;
        var deleted = await _refreshTokenRepository.DeleteInactiveOlderThanAsync(cutoff, context.CancellationToken);

        _logger.LogInformation("Refresh token cleanup removed {Count} expired/revoked tokens older than {Cutoff}.", deleted, cutoff);
    }
}
