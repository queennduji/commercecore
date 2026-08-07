using InventoryService.Application.Commands;
using InventoryService.Infrastructure.Messaging.Schemas;
using InventoryService.Infrastructure.Options;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryService.Infrastructure.Consumers;

/// <summary>
/// Subscribes to CatalogService's catalog.product-created.v1 topic (owned by CatalogService — this
/// service only consumes it, never publishes to it) and provisions a zero-stock InventoryItem for
/// the new product at every active location, keeping the two services decoupled via events instead
/// of a direct HTTP call from Catalog into Inventory.
/// </summary>
public class ProductCreatedConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductCreatedConsumer> _logger;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private IConsumer<string, ProductCreatedAvro>? _consumer;

    public ProductCreatedConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ProductCreatedConsumer> logger)
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
            GroupId = _options.ProductCreatedConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, ProductCreatedAvro>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<ProductCreatedAvro>(_schemaRegistryClient).AsSyncOverAsync())
            .SetErrorHandler((_, error) => _logger.LogError("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        _consumer.Subscribe(_options.ProductCreatedTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                if (!Guid.TryParse(result.Message.Value.ProductId, out var productId))
                {
                    _logger.LogWarning("Received catalog.product-created.v1 message with an unparseable productId: {ProductId}", result.Message.Value.ProductId);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                sender.Send(new ProvisionInventoryForProductCommand(productId), stoppingToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Failed to consume from {Topic}", _options.ProductCreatedTopic);
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
