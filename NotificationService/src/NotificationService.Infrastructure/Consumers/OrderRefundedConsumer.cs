using NotificationService.Application.Commands;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Messaging.Schemas;
using NotificationService.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Consumers;

public class OrderRefundedConsumer : KafkaConsumerBackgroundService<OrderRefundedAvro>
{
    public OrderRefundedConsumer(IOptions<KafkaOptions> options, IServiceScopeFactory scopeFactory, ILogger<OrderRefundedConsumer> logger)
        : base(options.Value.BootstrapServers, options.Value.SchemaRegistryUrl, options.Value.OrderRefundedTopic, options.Value.OrderRefundedConsumerGroupId, scopeFactory, logger)
    {
    }

    protected override async Task HandleAsync(OrderRefundedAvro message, IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(message.OrderId, out var orderId) || !Guid.TryParse(message.UserId, out var userId))
        {
            return;
        }

        var sender = scopedServices.GetRequiredService<ISender>();
        await sender.Send(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderRefunded), cancellationToken);
    }
}
