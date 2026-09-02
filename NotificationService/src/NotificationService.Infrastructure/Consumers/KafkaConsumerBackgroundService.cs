using Avro.Specific;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Consumers;

/// <summary>
/// Generic Kafka topic consumer, factored out because this service subscribes to eight different
/// topics across three other services (every other service in this platform tops out at one or
/// two consumers) – writing that ConsumeLoop by hand eight times would mean eight chances to
/// forget the exception-resilience fix ShippingService's OrderPaidConsumer needed after a live
/// smoke test proved a single malformed message can crash the whole host (see git history).
/// Concrete subclasses only supply the topic/group id and what to do with a deserialized message.
/// </summary>
public abstract class KafkaConsumerBackgroundService<TAvro> : BackgroundService
    where TAvro : class, ISpecificRecord, new()
{
    private readonly string _bootstrapServers;
    private readonly string _schemaRegistryUrl;
    private readonly string _topic;
    private readonly string _groupId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private IConsumer<string, TAvro>? _consumer;

    protected KafkaConsumerBackgroundService(
        string bootstrapServers,
        string schemaRegistryUrl,
        string topic,
        string groupId,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _bootstrapServers = bootstrapServers;
        _schemaRegistryUrl = schemaRegistryUrl;
        _topic = topic;
        _groupId = groupId;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig
        {
            Url = _schemaRegistryUrl
        });
    }

    /// <summary>Handle one deserialized message. Throwing here is safe – the loop logs and moves
    /// on rather than crashing the host; validate/parse defensively but don't worry about a stray
    /// exception taking the process down.</summary>
    protected abstract Task HandleAsync(TAvro message, IServiceProvider scopedServices, CancellationToken cancellationToken);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Confluent.Kafka's IConsumer.Consume is a blocking call, so it needs its own thread rather
        // than running inline on the hosted-service startup path.
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, TAvro>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<TAvro>(_schemaRegistryClient).AsSyncOverAsync())
            .SetErrorHandler((_, error) => _logger.LogError("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        _consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                HandleAsync(result.Message.Value, scope.ServiceProvider, stoppingToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Failed to consume from {Topic}", _topic);
            }
            catch (Exception ex)
            {
                // A single malformed/invalid message must not take the whole consumer – and by
                // extension this host process, since HostOptions.BackgroundServiceExceptionBehavior
                // defaults to StopHost – down with it. Logged and skipped.
                _logger.LogError(ex, "Failed to process a message from {Topic}", _topic);
            }
        }

        _consumer.Close();
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        _schemaRegistryClient.Dispose();
        base.Dispose();
    }
}
