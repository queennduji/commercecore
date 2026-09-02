using NotificationService.Application.Commands;
using NotificationService.Infrastructure.Messaging.Schemas;
using NotificationService.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Consumers;

/// <summary>Subscribes to AuthenticationService's auth.user-registered.v1 – the sole source for
/// this service's local userId -> email lookup table.</summary>
public class UserRegisteredConsumer : KafkaConsumerBackgroundService<UserRegisteredAvro>
{
    public UserRegisteredConsumer(IOptions<KafkaOptions> options, IServiceScopeFactory scopeFactory, ILogger<UserRegisteredConsumer> logger)
        : base(options.Value.BootstrapServers, options.Value.SchemaRegistryUrl, options.Value.UserRegisteredTopic, options.Value.UserRegisteredConsumerGroupId, scopeFactory, logger)
    {
    }

    protected override async Task HandleAsync(UserRegisteredAvro message, IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(message.UserId, out var userId))
        {
            return;
        }

        var sender = scopedServices.GetRequiredService<ISender>();
        await sender.Send(new RecordUserContactCommand(userId, message.Email, message.PhoneNumber), cancellationToken);
    }
}
