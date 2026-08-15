using NotificationService.Application.Handlers;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Queries;
using NotificationService.Domain.Entities;
using NSubstitute;

namespace NotificationService.UnitTests.Handlers;

public class GetNotificationQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotificationOwnedByCaller_ReturnsIt()
    {
        var notificationRepository = Substitute.For<INotificationRepository>();
        var userId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = NotificationChannel.Email,
            Recipient = "shopper@example.com",
            Type = NotificationType.OrderPaid,
            Subject = "Payment received",
            Body = "<p>...</p>",
            Status = NotificationStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };
        notificationRepository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var handler = new GetNotificationQueryHandler(notificationRepository);
        var result = await handler.Handle(new GetNotificationQuery(notification.Id, userId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(notification.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_NotificationOwnedByDifferentUser_ReturnsNotFoundStyleFailure()
    {
        var notificationRepository = Substitute.For<INotificationRepository>();
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Channel = NotificationChannel.Email,
            Recipient = "shopper@example.com",
            Type = NotificationType.OrderPaid,
            Subject = "Payment received",
            Body = "<p>...</p>",
            Status = NotificationStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };
        notificationRepository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var handler = new GetNotificationQueryHandler(notificationRepository);
        var result = await handler.Handle(new GetNotificationQuery(notification.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_NotificationDoesNotExist_ReturnsFailure()
    {
        var notificationRepository = Substitute.For<INotificationRepository>();
        notificationRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Notification?)null);

        var handler = new GetNotificationQueryHandler(notificationRepository);
        var result = await handler.Handle(new GetNotificationQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
