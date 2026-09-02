using NotificationService.Application.Commands;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Messaging.Schemas;
using NotificationService.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Consumers;

/// <summary>The one exception to "only consume OrderService + AuthenticationService" – a declined
/// payment leaves the order in Pending with no OrderService event to hang a notification off, so
/// this is the only place NotificationService reaches into PaymentService's own topics.</summary>
public class PaymentFailedConsumer : KafkaConsumerBackgroundService<PaymentFailedAvro>
{
    public PaymentFailedConsumer(IOptions<KafkaOptions> options, IServiceScopeFactory scopeFactory, ILogger<PaymentFailedConsumer> logger)
        : base(options.Value.BootstrapServers, options.Value.SchemaRegistryUrl, options.Value.PaymentFailedTopic, options.Value.PaymentFailedConsumerGroupId, scopeFactory, logger)
    {
    }

    protected override async Task HandleAsync(PaymentFailedAvro message, IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(message.OrderId, out var orderId) || !Guid.TryParse(message.UserId, out var userId))
        {
            return;
        }

        var sender = scopedServices.GetRequiredService<ISender>();
        await sender.Send(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.PaymentFailed, message.FailureReason), cancellationToken);
    }
}
