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
/// Subscribes to ShippingService's shipment.delivered.v1 topic and dispatches the existing
/// DeliverOrderCommand to flip Order.Status Shipped -> Delivered. Replaced the old manual
/// "POST /api/orders/{id}/deliver" ops endpoint — see ShipmentDispatchedConsumer for the fuller
/// explanation of why.
/// </summary>
public class ShipmentDeliveredConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShipmentDeliveredConsumer> _logger;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private IConsumer<string, ShipmentDeliveredAvro>? _consumer;

    public ShipmentDeliveredConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ShipmentDeliveredConsumer> logger)
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
            GroupId = _options.ShipmentDeliveredConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, ShipmentDeliveredAvro>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<ShipmentDeliveredAvro>(_schemaRegistryClient).AsSyncOverAsync())
            .SetErrorHandler((_, error) => _logger.LogError("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        _consumer.Subscribe(_options.ShipmentDeliveredTopic);

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
                    _logger.LogWarning("Received shipment.delivered.v1 message with an unparseable orderId: {OrderId}", result.Message.Value.OrderId);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var commandResult = sender.Send(new DeliverOrderCommand(orderId), stoppingToken).GetAwaiter().GetResult();
                if (!commandResult.Succeeded)
                {
                    _logger.LogWarning("DeliverOrderCommand for order {OrderId} did not succeed: {Errors}", orderId, string.Join("; ", commandResult.Errors));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Failed to consume from {Topic}", _options.ShipmentDeliveredTopic);
            }
            catch (Exception ex)
            {
                // A single malformed/invalid message must not take the whole consumer — and by
                // extension this host process, since HostOptions.BackgroundServiceExceptionBehavior
                // defaults to StopHost — down with it. Logged and skipped.
                _logger.LogError(ex, "Failed to process a message from {Topic}", _options.ShipmentDeliveredTopic);
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
