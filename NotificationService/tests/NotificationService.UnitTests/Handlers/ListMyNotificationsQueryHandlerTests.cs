using NotificationService.Application.Handlers;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Queries;
using NotificationService.Domain.Entities;
using NSubstitute;

namespace NotificationService.UnitTests.Handlers;

public class ListMyNotificationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsNotificationsForCaller()
    {
        var notificationRepository = Substitute.For<INotificationRepository>();
        var userId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = NotificationChannel.Email,
            Recipient = "shopper@example.com",
            Type = NotificationType.OrderCreated,
            Subject = "Order received",
            Body = "<p>...</p>",
            Status = NotificationStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };
        notificationRepository.ListByUserIdAsync(userId, 1, 20, Arg.Any<CancellationToken>()).Returns([notification]);

        var handler = new ListMyNotificationsQueryHandler(notificationRepository);
        var result = await handler.Handle(new ListMyNotificationsQuery(userId, 1, 20), CancellationToken.None);

        Assert.True(result.Succeeded);
        var single = Assert.Single(result.Value!);
        Assert.Equal(notification.Id, single.Id);
    }

    [Fact]
    public async Task Handle_NoNotifications_ReturnsEmptyList()
    {
        var notificationRepository = Substitute.For<INotificationRepository>();
        var userId = Guid.NewGuid();
        notificationRepository.ListByUserIdAsync(userId, 1, 20, Arg.Any<CancellationToken>()).Returns(Array.Empty<Notification>());

        var handler = new ListMyNotificationsQueryHandler(notificationRepository);
        var result = await handler.Handle(new ListMyNotificationsQuery(userId, 1, 20), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!);
    }
}
