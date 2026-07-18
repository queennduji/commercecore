using AuthenticationService.Application.Interfaces;
using AuthenticationService.Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;

namespace AuthenticationService.UnitTests.Jobs;

public class RefreshTokenCleanupJobTests
{
    [Fact]
    public async Task Execute_DeletesTokensOlderThanThirtyDays()
    {
        var repository = Substitute.For<IRefreshTokenRepository>();
        repository.DeleteInactiveOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(3);

        var logger = Substitute.For<ILogger<RefreshTokenCleanupJob>>();
        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(CancellationToken.None);

        var job = new RefreshTokenCleanupJob(repository, logger);

        await job.Execute(jobContext);

        var expectedCutoff = DateTime.UtcNow.AddDays(-30);
        await repository.Received(1).DeleteInactiveOlderThanAsync(
            Arg.Is<DateTime>(cutoff => Math.Abs((cutoff - expectedCutoff).TotalSeconds) < 5),
            Arg.Any<CancellationToken>());
    }
}
