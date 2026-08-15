using NotificationService.Application.Commands;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Messaging.Schemas;
using NotificationService.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Consumers;

public class OrderPaidConsumer : KafkaConsumerBackgroundService<OrderPaidAvro>
{
    public OrderPaidConsumer(IOptions<KafkaOptions> options, IServiceScopeFactory scopeFactory, ILogger<OrderPaidConsumer> logger)
        : base(options.Value.BootstrapServers, options.Value.SchemaRegistryUrl, options.Value.OrderPaidTopic, options.Value.OrderPaidConsumerGroupId, scopeFactory, logger)
    {
    }

    protected override async Task HandleAsync(OrderPaidAvro message, IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(message.OrderId, out var orderId) || !Guid.TryParse(message.UserId, out var userId))
        {
            return;
        }

        var sender = scopedServices.GetRequiredService<ISender>();
        await sender.Send(new SendOrderLifecycleNotificationCommand(orderId, userId, NotificationType.OrderPaid), cancellationToken);
    }
}
