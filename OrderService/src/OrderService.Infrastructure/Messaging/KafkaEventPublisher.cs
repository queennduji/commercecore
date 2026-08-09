using OrderService.Application.Interfaces;
using OrderService.Domain.Events;
using OrderService.Infrastructure.Messaging.Schemas;
using OrderService.Infrastructure.Options;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;

namespace OrderService.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly KafkaOptions _options;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private readonly IProducer<string, OrderCreatedAvro> _orderCreatedProducer;
    private readonly IProducer<string, OrderPaidAvro> _orderPaidProducer;
    private readonly IProducer<string, OrderShippedAvro> _orderShippedProducer;
    private readonly IProducer<string, OrderDeliveredAvro> _orderDeliveredProducer;
    private readonly IProducer<string, OrderCancelledAvro> _orderCancelledProducer;
    private readonly IProducer<string, OrderRefundedAvro> _orderRefundedProducer;

    public KafkaEventPublisher(IOptions<KafkaOptions> options)
    {
        _options = options.Value;

        _schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig
        {
            Url = _options.SchemaRegistryUrl
        });

        var producerConfig = new ProducerConfig { BootstrapServers = _options.BootstrapServers };

        _orderCreatedProducer = new ProducerBuilder<string, OrderCreatedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderCreatedAvro>(_schemaRegistryClient))
            .Build();

        _orderPaidProducer = new ProducerBuilder<string, OrderPaidAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderPaidAvro>(_schemaRegistryClient))
            .Build();

        _orderShippedProducer = new ProducerBuilder<string, OrderShippedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderShippedAvro>(_schemaRegistryClient))
            .Build();

        _orderDeliveredProducer = new ProducerBuilder<string, OrderDeliveredAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderDeliveredAvro>(_schemaRegistryClient))
            .Build();

        _orderCancelledProducer = new ProducerBuilder<string, OrderCancelledAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderCancelledAvro>(_schemaRegistryClient))
            .Build();

        _orderRefundedProducer = new ProducerBuilder<string, OrderRefundedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderRefundedAvro>(_schemaRegistryClient))
            .Build();
    }

    public async Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, OrderCreatedAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new OrderCreatedAvro
            {
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                Subtotal = (double)evt.Subtotal,
                CreatedAt = evt.CreatedAt
            }
        };

        await _orderCreatedProducer.ProduceAsync(_options.OrderCreatedTopic, message, cancellationToken);
    }

    public async Task PublishOrderPaidAsync(OrderPaidEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, OrderPaidAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new OrderPaidAvro
            {
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                PaidAt = evt.PaidAt
            }
        };

        await _orderPaidProducer.ProduceAsync(_options.OrderPaidTopic, message, cancellationToken);
    }

    public async Task PublishOrderShippedAsync(OrderShippedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, OrderShippedAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new OrderShippedAvro
            {
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                ShippedAt = evt.ShippedAt
            }
        };

        await _orderShippedProducer.ProduceAsync(_options.OrderShippedTopic, message, cancellationToken);
    }

    public async Task PublishOrderDeliveredAsync(OrderDeliveredEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, OrderDeliveredAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new OrderDeliveredAvro
            {
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                DeliveredAt = evt.DeliveredAt
            }
        };

        await _orderDeliveredProducer.ProduceAsync(_options.OrderDeliveredTopic, message, cancellationToken);
    }

    public async Task PublishOrderCancelledAsync(OrderCancelledEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, OrderCancelledAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new OrderCancelledAvro
            {
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                CancelledAt = evt.CancelledAt
            }
        };

        await _orderCancelledProducer.ProduceAsync(_options.OrderCancelledTopic, message, cancellationToken);
    }

    public async Task PublishOrderRefundedAsync(OrderRefundedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, OrderRefundedAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new OrderRefundedAvro
            {
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                RefundedAt = evt.RefundedAt
            }
        };

        await _orderRefundedProducer.ProduceAsync(_options.OrderRefundedTopic, message, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _orderCreatedProducer.Flush(TimeSpan.FromSeconds(5));
        _orderPaidProducer.Flush(TimeSpan.FromSeconds(5));
        _orderShippedProducer.Flush(TimeSpan.FromSeconds(5));
        _orderDeliveredProducer.Flush(TimeSpan.FromSeconds(5));
        _orderCancelledProducer.Flush(TimeSpan.FromSeconds(5));
        _orderRefundedProducer.Flush(TimeSpan.FromSeconds(5));
        _orderCreatedProducer.Dispose();
        _orderPaidProducer.Dispose();
        _orderShippedProducer.Dispose();
        _orderDeliveredProducer.Dispose();
        _orderCancelledProducer.Dispose();
        _orderRefundedProducer.Dispose();
        _schemaRegistryClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
