using CatalogService.Application.Interfaces;
using CatalogService.Domain.Events;
using CatalogService.Infrastructure.Messaging.Schemas;
using CatalogService.Infrastructure.Options;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;

namespace CatalogService.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly KafkaOptions _options;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private readonly IProducer<string, ProductCreatedAvro> _productCreatedProducer;
    private readonly IProducer<string, ProductUpdatedAvro> _productUpdatedProducer;
    private readonly IProducer<string, ProductDeletedAvro> _productDeletedProducer;

    public KafkaEventPublisher(IOptions<KafkaOptions> options)
    {
        _options = options.Value;

        _schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig
        {
            Url = _options.SchemaRegistryUrl
        });

        var producerConfig = new ProducerConfig { BootstrapServers = _options.BootstrapServers };

        _productCreatedProducer = new ProducerBuilder<string, ProductCreatedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ProductCreatedAvro>(_schemaRegistryClient))
            .Build();

        _productUpdatedProducer = new ProducerBuilder<string, ProductUpdatedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ProductUpdatedAvro>(_schemaRegistryClient))
            .Build();

        _productDeletedProducer = new ProducerBuilder<string, ProductDeletedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ProductDeletedAvro>(_schemaRegistryClient))
            .Build();
    }

    public async Task PublishProductCreatedAsync(ProductCreatedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, ProductCreatedAvro>
        {
            Key = evt.ProductId.ToString(),
            Value = new ProductCreatedAvro
            {
                ProductId = evt.ProductId.ToString(),
                Name = evt.Name,
                Sku = evt.Sku,
                Price = (double)evt.Price,
                CategoryId = evt.CategoryId.ToString(),
                CreatedAt = evt.CreatedAt
            }
        };

        await _productCreatedProducer.ProduceAsync(_options.ProductCreatedTopic, message, cancellationToken);
    }

    public async Task PublishProductUpdatedAsync(ProductUpdatedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, ProductUpdatedAvro>
        {
            Key = evt.ProductId.ToString(),
            Value = new ProductUpdatedAvro
            {
                ProductId = evt.ProductId.ToString(),
                Name = evt.Name,
                Price = (double)evt.Price,
                Status = evt.Status,
                CategoryId = evt.CategoryId.ToString(),
                UpdatedAt = evt.UpdatedAt
            }
        };

        await _productUpdatedProducer.ProduceAsync(_options.ProductUpdatedTopic, message, cancellationToken);
    }

    public async Task PublishProductDeletedAsync(ProductDeletedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, ProductDeletedAvro>
        {
            Key = evt.ProductId.ToString(),
            Value = new ProductDeletedAvro
            {
                ProductId = evt.ProductId.ToString(),
                DeletedAt = evt.DeletedAt
            }
        };

        await _productDeletedProducer.ProduceAsync(_options.ProductDeletedTopic, message, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _productCreatedProducer.Flush(TimeSpan.FromSeconds(5));
        _productUpdatedProducer.Flush(TimeSpan.FromSeconds(5));
        _productDeletedProducer.Flush(TimeSpan.FromSeconds(5));
        _productCreatedProducer.Dispose();
        _productUpdatedProducer.Dispose();
        _productDeletedProducer.Dispose();
        _schemaRegistryClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
