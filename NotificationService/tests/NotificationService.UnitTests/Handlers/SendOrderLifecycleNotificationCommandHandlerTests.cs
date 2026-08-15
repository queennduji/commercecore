using NotificationService.Application.Commands;
using NotificationService.Application.Handlers;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NSubstitute;

namespace NotificationService.UnitTests.Handlers;

public class SendOrderLifecycleNotificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_EmailOnlyContact_SendsEmailAndRecordsOneSentRow()
    {
        var userContactRepository = Substitute.For<IUserContactRepository>();
        var notificationRepository = Substitute.For<INotificationRepository>();
        var emailGateway = Substitute.For<IEmailGateway>();
        var smsGateway = Substitute.For<ISmsGateway>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        userContactRepository.GetContactAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserContactInfo("shopper@example.com", null));
        emailGateway.SendAsync("shopper@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmailSendResult(true, "msg_123", null));

        var handler = new SendOrderLifecycleNotificationCommandHandler(userContactRepository, notificationRepository, emailGateway, smsGateway);
        var result = await handler.Handle(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderPaid), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Sent", result.Value!.Status);
        Assert.Equal("Email", result.Value.Channel);
        Assert.Equal("shopper@example.com", result.Value.Recipient);
        await notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n != null && n.Channel == NotificationChannel.Email && n.UserId == userId && n.Status == NotificationStatus.Sent && n.ProviderMessageId == "msg_123"),
            Arg.Any<CancellationToken>());
        await smsGateway.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await notificationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PhoneOnlyContact_SendsSmsAndRecordsOneSentRow()
    {
        var userContactRepository = Substitute.For<IUserContactRepository>();
        var notificationRepository = Substitute.For<INotificationRepository>();
        var emailGateway = Substitute.For<IEmailGateway>();
        var smsGateway = Substitute.For<ISmsGateway>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        userContactRepository.GetContactAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserContactInfo(null, "+15551234567"));
        smsGateway.SendAsync("+15551234567", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SmsSendResult(true, "SM123", null));

        var handler = new SendOrderLifecycleNotificationCommandHandler(userContactRepository, notificationRepository, emailGateway, smsGateway);
        var result = await handler.Handle(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderShipped), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Sms", result.Value!.Channel);
        Assert.Equal("+15551234567", result.Value.Recipient);
        Assert.Equal(string.Empty, result.Value.Subject);
        await notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n != null && n.Channel == NotificationChannel.Sms && n.Status == NotificationStatus.Sent && n.ProviderMessageId == "SM123"),
            Arg.Any<CancellationToken>());
        await emailGateway.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BothEmailAndPhoneOnFile_SendsBothAndRecordsTwoRows()
    {
        var userContactRepository = Substitute.For<IUserContactRepository>();
        var notificationRepository = Substitute.For<INotificationRepository>();
        var emailGateway = Substitute.For<IEmailGateway>();
        var smsGateway = Substitute.For<ISmsGateway>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        userContactRepository.GetContactAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserContactInfo("shopper@example.com", "+15551234567"));
        emailGateway.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmailSendResult(true, "msg_123", null));
        smsGateway.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SmsSendResult(true, "SM123", null));

        var handler = new SendOrderLifecycleNotificationCommandHandler(userContactRepository, notificationRepository, emailGateway, smsGateway);
        var result = await handler.Handle(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderDelivered), CancellationToken.None);

        Assert.True(result.Succeeded);
        await notificationRepository.Received(2).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await notificationRepository.Received(1).AddAsync(Arg.Is<Notification>(n => n != null && n.Channel == NotificationChannel.Email), Arg.Any<CancellationToken>());
        await notificationRepository.Received(1).AddAsync(Arg.Is<Notification>(n => n != null && n.Channel == NotificationChannel.Sms), Arg.Any<CancellationToken>());
        await notificationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoContactAtAll_RecordsFailedRowWithoutCallingEitherGateway()
    {
        var userContactRepository = Substitute.For<IUserContactRepository>();
        var notificationRepository = Substitute.For<INotificationRepository>();
        var emailGateway = Substitute.For<IEmailGateway>();
        var smsGateway = Substitute.For<ISmsGateway>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        userContactRepository.GetContactAsync(userId, Arg.Any<CancellationToken>()).Returns((UserContactInfo?)null);

        var handler = new SendOrderLifecycleNotificationCommandHandler(userContactRepository, notificationRepository, emailGateway, smsGateway);
        var result = await handler.Handle(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderCreated), CancellationToken.None);

        Assert.False(result.Succeeded);
        await emailGateway.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await smsGateway.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n != null && n.Status == NotificationStatus.Failed && n.Recipient == string.Empty),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailFailsButSmsSucceeds_OverallSucceedsWithBothRowsRecorded()
    {
        var userContactRepository = Substitute.For<IUserContactRepository>();
        var notificationRepository = Substitute.For<INotificationRepository>();
        var emailGateway = Substitute.For<IEmailGateway>();
        var smsGateway = Substitute.For<ISmsGateway>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        userContactRepository.GetContactAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserContactInfo("shopper@example.com", "+15551234567"));
        emailGateway.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmailSendResult(false, null, "Resend rejected the request."));
        smsGateway.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SmsSendResult(true, "SM123", null));

        var handler = new SendOrderLifecycleNotificationCommandHandler(userContactRepository, notificationRepository, emailGateway, smsGateway);
        var result = await handler.Handle(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderRefunded), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Sms", result.Value!.Channel);
        await notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n != null && n.Channel == NotificationChannel.Email && n.Status == NotificationStatus.Failed && n.FailureReason == "Resend rejected the request."),
            Arg.Any<CancellationToken>());
        await notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n != null && n.Channel == NotificationChannel.Sms && n.Status == NotificationStatus.Sent),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BothChannelsFail_OverallFails()
    {
        var userContactRepository = Substitute.For<IUserContactRepository>();
        var notificationRepository = Substitute.For<INotificationRepository>();
        var emailGateway = Substitute.For<IEmailGateway>();
        var smsGateway = Substitute.For<ISmsGateway>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        userContactRepository.GetContactAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserContactInfo("shopper@example.com", "+15551234567"));
        emailGateway.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmailSendResult(false, null, "Resend rejected the request."));
        smsGateway.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SmsSendResult(false, null, "Twilio rejected the request."));

        var handler = new SendOrderLifecycleNotificationCommandHandler(userContactRepository, notificationRepository, emailGateway, smsGateway);
        var result = await handler.Handle(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderCancelled), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Resend rejected the request.", result.Errors);
        Assert.Contains("Twilio rejected the request.", result.Errors);
    }

    [Fact]
    public async Task Handle_PaymentFailedType_IncludesDetailInBothChannelsBodies()
    {
        var userContactRepository = Substitute.For<IUserContactRepository>();
        var notificationRepository = Substitute.For<INotificationRepository>();
        var emailGateway = Substitute.For<IEmailGateway>();
        var smsGateway = Substitute.For<ISmsGateway>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        userContactRepository.GetContactAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserContactInfo("shopper@example.com", "+15551234567"));
        emailGateway.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Is<string>(b => b != null && b.Contains("Your card was declined.")), Arg.Any<CancellationToken>())
            .Returns(new EmailSendResult(true, "msg_456", null));
        smsGateway.SendAsync(Arg.Any<string>(), Arg.Is<string>(b => b != null && b.Contains("Your card was declined.")), Arg.Any<CancellationToken>())
            .Returns(new SmsSendResult(true, "SM456", null));

        var handler = new SendOrderLifecycleNotificationCommandHandler(userContactRepository, notificationRepository, emailGateway, smsGateway);
        var result = await handler.Handle(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.PaymentFailed, "Your card was declined."), CancellationToken.None);

        Assert.True(result.Succeeded);
        await emailGateway.Received(1).SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Is<string>(b => b != null && b.Contains("Your card was declined.")), Arg.Any<CancellationToken>());
        await smsGateway.Received(1).SendAsync(Arg.Any<string>(), Arg.Is<string>(b => b != null && b.Contains("Your card was declined.")), Arg.Any<CancellationToken>());
    }
}
