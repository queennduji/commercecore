using NotificationService.Application.Commands;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Messaging.Schemas;
using NotificationService.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Consumers;

public class OrderDeliveredConsumer : KafkaConsumerBackgroundService<OrderDeliveredAvro>
{
    public OrderDeliveredConsumer(IOptions<KafkaOptions> options, IServiceScopeFactory scopeFactory, ILogger<OrderDeliveredConsumer> logger)
        : base(options.Value.BootstrapServers, options.Value.SchemaRegistryUrl, options.Value.OrderDeliveredTopic, options.Value.OrderDeliveredConsumerGroupId, scopeFactory, logger)
    {
    }

    protected override async Task HandleAsync(OrderDeliveredAvro message, IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(message.OrderId, out var orderId) || !Guid.TryParse(message.UserId, out var userId))
        {
            return;
        }

        var sender = scopedServices.GetRequiredService<ISender>();
        await sender.Send(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderDelivered), cancellationToken);
    }
}
