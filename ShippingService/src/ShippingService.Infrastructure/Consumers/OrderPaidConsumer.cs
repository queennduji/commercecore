using ShippingService.Application.Commands;
using ShippingService.Infrastructure.Messaging.Schemas;
using ShippingService.Infrastructure.Options;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ShippingService.Infrastructure.Consumers;

/// <summary>
/// Subscribes to OrderService's order.paid.v1 topic (owned by OrderService – this service only
/// consumes it, never publishes to it) and auto-creates a Shipment for the paid order, keeping the
/// two services decoupled via events instead of a direct HTTP call from Order into Shipping. This
/// is also what makes OrderService's own former "ship"/"deliver" ops endpoints obsolete: fulfillment
/// activity in this service (Dispatch/RefreshTracking) is what now drives Order.Status forward, via
/// the shipment.dispatched.v1/shipment.delivered.v1 events OrderService consumes back.
/// </summary>
public class OrderPaidConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderPaidConsumer> _logger;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private IConsumer<string, OrderPaidAvro>? _consumer;

    public OrderPaidConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderPaidConsumer> logger)
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
        // Confluent.Kafka's IConsumer.Consume is a blocking call, so it needs its own thread rather
        // than running inline on the hosted-service startup path.
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.OrderPaidConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, OrderPaidAvro>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<OrderPaidAvro>(_schemaRegistryClient).AsSyncOverAsync())
            .SetErrorHandler((_, error) => _logger.LogError("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        _consumer.Subscribe(_options.OrderPaidTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                if (!Guid.TryParse(result.Message.Value.OrderId, out var orderId) ||
                    !Guid.TryParse(result.Message.Value.UserId, out var userId))
                {
                    _logger.LogWarning(
                        "Received order.paid.v1 message with an unparseable orderId/userId: {OrderId}/{UserId}",
                        result.Message.Value.OrderId, result.Message.Value.UserId);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                sender.Send(
                    new CreateShipmentForOrderCommand(orderId, userId, result.Message.Value.ShippingAddress),
                    stoppingToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Failed to consume from {Topic}", _options.OrderPaidTopic);
            }
            catch (Exception ex)
            {
                // A single malformed/invalid message (e.g. one published before shippingAddress was
                // added to this schema, so it deserializes with an empty string and fails
                // validation) must not take the whole consumer – and by extension this host process,
                // since HostOptions.BackgroundServiceExceptionBehavior defaults to StopHost – down
                // with it. Logged and skipped; the offset still advances since EnableAutoCommit is on.
                _logger.LogError(ex, "Failed to process a message from {Topic}", _options.OrderPaidTopic);
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
