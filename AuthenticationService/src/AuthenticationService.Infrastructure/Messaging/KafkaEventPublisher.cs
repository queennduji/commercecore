using AuthenticationService.Application.Interfaces;
using AuthenticationService.Domain.Events;
using AuthenticationService.Infrastructure.Messaging.Schemas;
using AuthenticationService.Infrastructure.Options;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;

namespace AuthenticationService.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly KafkaOptions _options;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private readonly IProducer<string, UserRegisteredAvro> _userRegisteredProducer;
    private readonly IProducer<string, UserLoggedInAvro> _userLoggedInProducer;

    public KafkaEventPublisher(IOptions<KafkaOptions> options)
    {
        _options = options.Value;

        _schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig
        {
            Url = _options.SchemaRegistryUrl
        });

        var producerConfig = new ProducerConfig { BootstrapServers = _options.BootstrapServers };

        _userRegisteredProducer = new ProducerBuilder<string, UserRegisteredAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<UserRegisteredAvro>(_schemaRegistryClient))
            .Build();

        _userLoggedInProducer = new ProducerBuilder<string, UserLoggedInAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<UserLoggedInAvro>(_schemaRegistryClient))
            .Build();
    }

    public async Task PublishUserRegisteredAsync(UserRegisteredEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, UserRegisteredAvro>
        {
            Key = evt.UserId.ToString(),
            Value = new UserRegisteredAvro
            {
                UserId = evt.UserId.ToString(),
                Email = evt.Email,
                RegisteredAt = evt.RegisteredAt
            }
        };

        await _userRegisteredProducer.ProduceAsync(_options.UserRegisteredTopic, message, cancellationToken);
    }

    public async Task PublishUserLoggedInAsync(UserLoggedInEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, UserLoggedInAvro>
        {
            Key = evt.UserId.ToString(),
            Value = new UserLoggedInAvro
            {
                UserId = evt.UserId.ToString(),
                LoggedInAt = evt.LoggedInAt
            }
        };

        await _userLoggedInProducer.ProduceAsync(_options.UserLoggedInTopic, message, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _userRegisteredProducer.Flush(TimeSpan.FromSeconds(5));
        _userLoggedInProducer.Flush(TimeSpan.FromSeconds(5));
        _userRegisteredProducer.Dispose();
        _userLoggedInProducer.Dispose();
        _schemaRegistryClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
