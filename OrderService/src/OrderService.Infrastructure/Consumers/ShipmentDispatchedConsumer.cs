using OrderService.Application.Commands;
using OrderService.Infrastructure.Messaging.Schemas;
using OrderService.Infrastructure.Options;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrderService.Infrastructure.Consumers;

/// <summary>
/// Subscribes to ShippingService's shipment.dispatched.v1 topic (owned by ShippingService — this
/// service only consumes it, never publishes to it) and dispatches the existing ShipOrderCommand
/// to flip Order.Status Paid -> Shipped. This is what replaced the old manual
/// "POST /api/orders/{id}/ship" ops endpoint — shipping now genuinely drives order state instead
/// of a human calling an endpoint by hand.
/// </summary>
public class ShipmentDispatchedConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShipmentDispatchedConsumer> _logger;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private IConsumer<string, ShipmentDispatchedAvro>? _consumer;

    public ShipmentDispatchedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ShipmentDispatchedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig
        {
            Url = _options.SchemaRegistryUrl
        });
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ShipmentDispatchedConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, ShipmentDispatchedAvro>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<ShipmentDispatchedAvro>(_schemaRegistryClient).AsSyncOverAsync())
            .SetErrorHandler((_, error) => _logger.LogError("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        _consumer.Subscribe(_options.ShipmentDispatchedTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                if (!Guid.TryParse(result.Message.Value.OrderId, out var orderId))
                {
                    _logger.LogWarning("Received shipment.dispatched.v1 message with an unparseable orderId: {OrderId}", result.Message.Value.OrderId);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var commandResult = sender.Send(new ShipOrderCommand(orderId), stoppingToken).GetAwaiter().GetResult();
                if (!commandResult.Succeeded)
                {
                    // Order not found, or already past Paid (e.g. this message redelivered after a
                    // consumer restart) — logged, not thrown, since retrying won't help either case.
                    _logger.LogWarning("ShipOrderCommand for order {OrderId} did not succeed: {Errors}", orderId, string.Join("; ", commandResult.Errors));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Failed to consume from {Topic}", _options.ShipmentDispatchedTopic);
            }
            catch (Exception ex)
            {
                // A single malformed/invalid message must not take the whole consumer — and by
                // extension this host process, since HostOptions.BackgroundServiceExceptionBehavior
                // defaults to StopHost — down with it. Logged and skipped.
                _logger.LogError(ex, "Failed to process a message from {Topic}", _options.ShipmentDispatchedTopic);
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
