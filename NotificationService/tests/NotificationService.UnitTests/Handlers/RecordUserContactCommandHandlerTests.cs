using NotificationService.Application.Commands;
using NotificationService.Application.Handlers;
using NotificationService.Application.Interfaces;
using NSubstitute;

namespace NotificationService.UnitTests.Handlers;

public class RecordUserContactCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpsertsContactAndSaves()
    {
        var userContactRepository = Substitute.For<IUserContactRepository>();
        var userId = Guid.NewGuid();

        var handler = new RecordUserContactCommandHandler(userContactRepository);
        var result = await handler.Handle(new RecordUserContactCommand(userId, "shopper@example.com", "+15551234567"), CancellationToken.None);

        Assert.True(result.Succeeded);
        await userContactRepository.Received(1).UpsertAsync(userId, "shopper@example.com", "+15551234567", Arg.Any<CancellationToken>());
        await userContactRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoPhoneNumber_UpsertsWithNullPhone()
    {
        var userContactRepository = Substitute.For<IUserContactRepository>();
        var userId = Guid.NewGuid();

        var handler = new RecordUserContactCommandHandler(userContactRepository);
        var result = await handler.Handle(new RecordUserContactCommand(userId, "shopper@example.com"), CancellationToken.None);

        Assert.True(result.Succeeded);
        await userContactRepository.Received(1).UpsertAsync(userId, "shopper@example.com", null, Arg.Any<CancellationToken>());
    }
}
