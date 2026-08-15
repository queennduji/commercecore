using NotificationService.Application.Commands;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Messaging.Schemas;
using NotificationService.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Consumers;

public class OrderShippedConsumer : KafkaConsumerBackgroundService<OrderShippedAvro>
{
    public OrderShippedConsumer(IOptions<KafkaOptions> options, IServiceScopeFactory scopeFactory, ILogger<OrderShippedConsumer> logger)
        : base(options.Value.BootstrapServers, options.Value.SchemaRegistryUrl, options.Value.OrderShippedTopic, options.Value.OrderShippedConsumerGroupId, scopeFactory, logger)
    {
    }

    protected override async Task HandleAsync(OrderShippedAvro message, IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(message.OrderId, out var orderId) || !Guid.TryParse(message.UserId, out var userId))
        {
            return;
        }

        var sender = scopedServices.GetRequiredService<ISender>();
        await sender.Send(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderShipped), cancellationToken);
    }
}
