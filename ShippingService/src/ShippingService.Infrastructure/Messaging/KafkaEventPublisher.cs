using ShippingService.Application.Interfaces;
using ShippingService.Domain.Events;
using ShippingService.Infrastructure.Messaging.Schemas;
using ShippingService.Infrastructure.Options;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;

namespace ShippingService.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly KafkaOptions _options;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private readonly IProducer<string, ShipmentDispatchedAvro> _shipmentDispatchedProducer;
    private readonly IProducer<string, ShipmentDeliveredAvro> _shipmentDeliveredProducer;
    private readonly IProducer<string, ShipmentExceptionAvro> _shipmentExceptionProducer;

    public KafkaEventPublisher(IOptions<KafkaOptions> options)
    {
        _options = options.Value;

        _schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig
        {
            Url = _options.SchemaRegistryUrl
        });

        var producerConfig = new ProducerConfig { BootstrapServers = _options.BootstrapServers };

        _shipmentDispatchedProducer = new ProducerBuilder<string, ShipmentDispatchedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ShipmentDispatchedAvro>(_schemaRegistryClient))
            .Build();

        _shipmentDeliveredProducer = new ProducerBuilder<string, ShipmentDeliveredAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ShipmentDeliveredAvro>(_schemaRegistryClient))
            .Build();

        _shipmentExceptionProducer = new ProducerBuilder<string, ShipmentExceptionAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ShipmentExceptionAvro>(_schemaRegistryClient))
            .Build();
    }

    public async Task PublishShipmentDispatchedAsync(ShipmentDispatchedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, ShipmentDispatchedAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new ShipmentDispatchedAvro
            {
                ShipmentId = evt.ShipmentId.ToString(),
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                CarrierName = evt.CarrierName,
                TrackingNumber = evt.TrackingNumber,
                DispatchedAt = evt.DispatchedAt
            }
        };

        await _shipmentDispatchedProducer.ProduceAsync(_options.ShipmentDispatchedTopic, message, cancellationToken);
    }

    public async Task PublishShipmentDeliveredAsync(ShipmentDeliveredEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, ShipmentDeliveredAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new ShipmentDeliveredAvro
            {
                ShipmentId = evt.ShipmentId.ToString(),
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                DeliveredAt = evt.DeliveredAt
            }
        };

        await _shipmentDeliveredProducer.ProduceAsync(_options.ShipmentDeliveredTopic, message, cancellationToken);
    }

    public async Task PublishShipmentExceptionAsync(ShipmentExceptionEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, ShipmentExceptionAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new ShipmentExceptionAvro
            {
                ShipmentId = evt.ShipmentId.ToString(),
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                Reason = evt.Reason,
                OccurredAt = evt.OccurredAt
            }
        };

        await _shipmentExceptionProducer.ProduceAsync(_options.ShipmentExceptionTopic, message, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _shipmentDispatchedProducer.Flush(TimeSpan.FromSeconds(5));
        _shipmentDeliveredProducer.Flush(TimeSpan.FromSeconds(5));
        _shipmentExceptionProducer.Flush(TimeSpan.FromSeconds(5));
        _shipmentDispatchedProducer.Dispose();
        _shipmentDeliveredProducer.Dispose();
        _shipmentExceptionProducer.Dispose();
        _schemaRegistryClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
