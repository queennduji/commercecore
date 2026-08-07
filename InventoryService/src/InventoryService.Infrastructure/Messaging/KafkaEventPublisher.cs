using InventoryService.Application.Interfaces;
using InventoryService.Domain.Events;
using InventoryService.Infrastructure.Messaging.Schemas;
using InventoryService.Infrastructure.Options;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;

namespace InventoryService.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly KafkaOptions _options;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private readonly IProducer<string, StockAdjustedAvro> _stockAdjustedProducer;
    private readonly IProducer<string, StockReservedAvro> _stockReservedProducer;
    private readonly IProducer<string, ReservationReleasedAvro> _reservationReleasedProducer;
    private readonly IProducer<string, ReservationCommittedAvro> _reservationCommittedProducer;

    public KafkaEventPublisher(IOptions<KafkaOptions> options)
    {
        _options = options.Value;

        _schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig
        {
            Url = _options.SchemaRegistryUrl
        });

        var producerConfig = new ProducerConfig { BootstrapServers = _options.BootstrapServers };

        _stockAdjustedProducer = new ProducerBuilder<string, StockAdjustedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<StockAdjustedAvro>(_schemaRegistryClient))
            .Build();

        _stockReservedProducer = new ProducerBuilder<string, StockReservedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<StockReservedAvro>(_schemaRegistryClient))
            .Build();

        _reservationReleasedProducer = new ProducerBuilder<string, ReservationReleasedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ReservationReleasedAvro>(_schemaRegistryClient))
            .Build();

        _reservationCommittedProducer = new ProducerBuilder<string, ReservationCommittedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ReservationCommittedAvro>(_schemaRegistryClient))
            .Build();
    }

    public async Task PublishStockAdjustedAsync(StockAdjustedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, StockAdjustedAvro>
        {
            Key = evt.ProductId.ToString(),
            Value = new StockAdjustedAvro
            {
                ProductId = evt.ProductId.ToString(),
                LocationId = evt.LocationId.ToString(),
                Delta = evt.Delta,
                OnHandAfter = evt.OnHandAfter,
                Reason = evt.Reason,
                AdjustedAt = evt.AdjustedAt
            }
        };

        await _stockAdjustedProducer.ProduceAsync(_options.StockAdjustedTopic, message, cancellationToken);
    }

    public async Task PublishStockReservedAsync(StockReservedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, StockReservedAvro>
        {
            Key = evt.ProductId.ToString(),
            Value = new StockReservedAvro
            {
                ReservationId = evt.ReservationId.ToString(),
                ProductId = evt.ProductId.ToString(),
                LocationId = evt.LocationId.ToString(),
                Quantity = evt.Quantity,
                ReferenceId = evt.ReferenceId,
                ReservedAt = evt.ReservedAt
            }
        };

        await _stockReservedProducer.ProduceAsync(_options.StockReservedTopic, message, cancellationToken);
    }

    public async Task PublishReservationReleasedAsync(ReservationReleasedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, ReservationReleasedAvro>
        {
            Key = evt.ProductId.ToString(),
            Value = new ReservationReleasedAvro
            {
                ReservationId = evt.ReservationId.ToString(),
                ProductId = evt.ProductId.ToString(),
                LocationId = evt.LocationId.ToString(),
                Quantity = evt.Quantity,
                ReleasedAt = evt.ReleasedAt
            }
        };

        await _reservationReleasedProducer.ProduceAsync(_options.ReservationReleasedTopic, message, cancellationToken);
    }

    public async Task PublishReservationCommittedAsync(ReservationCommittedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, ReservationCommittedAvro>
        {
            Key = evt.ProductId.ToString(),
            Value = new ReservationCommittedAvro
            {
                ReservationId = evt.ReservationId.ToString(),
                ProductId = evt.ProductId.ToString(),
                LocationId = evt.LocationId.ToString(),
                Quantity = evt.Quantity,
                CommittedAt = evt.CommittedAt
            }
        };

        await _reservationCommittedProducer.ProduceAsync(_options.ReservationCommittedTopic, message, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _stockAdjustedProducer.Flush(TimeSpan.FromSeconds(5));
        _stockReservedProducer.Flush(TimeSpan.FromSeconds(5));
        _reservationReleasedProducer.Flush(TimeSpan.FromSeconds(5));
        _reservationCommittedProducer.Flush(TimeSpan.FromSeconds(5));
        _stockAdjustedProducer.Dispose();
        _stockReservedProducer.Dispose();
        _reservationReleasedProducer.Dispose();
        _reservationCommittedProducer.Dispose();
        _schemaRegistryClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
