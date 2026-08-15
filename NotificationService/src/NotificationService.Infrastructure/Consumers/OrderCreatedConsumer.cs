using NotificationService.Application.Commands;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Messaging.Schemas;
using NotificationService.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Consumers;

public class OrderCreatedConsumer : KafkaConsumerBackgroundService<OrderCreatedAvro>
{
    public OrderCreatedConsumer(IOptions<KafkaOptions> options, IServiceScopeFactory scopeFactory, ILogger<OrderCreatedConsumer> logger)
        : base(options.Value.BootstrapServers, options.Value.SchemaRegistryUrl, options.Value.OrderCreatedTopic, options.Value.OrderCreatedConsumerGroupId, scopeFactory, logger)
    {
    }

    protected override async Task HandleAsync(OrderCreatedAvro message, IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(message.OrderId, out var orderId) || !Guid.TryParse(message.UserId, out var userId))
        {
            return;
        }

        var sender = scopedServices.GetRequiredService<ISender>();
        await sender.Send(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderCreated), cancellationToken);
    }
}
