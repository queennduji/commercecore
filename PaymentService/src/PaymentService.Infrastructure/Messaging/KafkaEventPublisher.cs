using PaymentService.Application.Interfaces;
using PaymentService.Domain.Events;
using PaymentService.Infrastructure.Messaging.Schemas;
using PaymentService.Infrastructure.Options;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;

namespace PaymentService.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly KafkaOptions _options;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private readonly IProducer<string, PaymentSucceededAvro> _paymentSucceededProducer;
    private readonly IProducer<string, PaymentFailedAvro> _paymentFailedProducer;
    private readonly IProducer<string, PaymentRefundedAvro> _paymentRefundedProducer;

    public KafkaEventPublisher(IOptions<KafkaOptions> options)
    {
        _options = options.Value;

        _schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig
        {
            Url = _options.SchemaRegistryUrl
        });

        var producerConfig = new ProducerConfig { BootstrapServers = _options.BootstrapServers };

        _paymentSucceededProducer = new ProducerBuilder<string, PaymentSucceededAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<PaymentSucceededAvro>(_schemaRegistryClient))
            .Build();

        _paymentFailedProducer = new ProducerBuilder<string, PaymentFailedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<PaymentFailedAvro>(_schemaRegistryClient))
            .Build();

        _paymentRefundedProducer = new ProducerBuilder<string, PaymentRefundedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<PaymentRefundedAvro>(_schemaRegistryClient))
            .Build();
    }

    public async Task PublishPaymentSucceededAsync(PaymentSucceededEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, PaymentSucceededAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new PaymentSucceededAvro
            {
                PaymentId = evt.PaymentId.ToString(),
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                Amount = (double)evt.Amount,
                Currency = evt.Currency,
                SucceededAt = evt.SucceededAt
            }
        };

        await _paymentSucceededProducer.ProduceAsync(_options.PaymentSucceededTopic, message, cancellationToken);
    }

    public async Task PublishPaymentFailedAsync(PaymentFailedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, PaymentFailedAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new PaymentFailedAvro
            {
                PaymentId = evt.PaymentId.ToString(),
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                FailureReason = evt.FailureReason,
                FailedAt = evt.FailedAt
            }
        };

        await _paymentFailedProducer.ProduceAsync(_options.PaymentFailedTopic, message, cancellationToken);
    }

    public async Task PublishPaymentRefundedAsync(PaymentRefundedEvent evt, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, PaymentRefundedAvro>
        {
            Key = evt.OrderId.ToString(),
            Value = new PaymentRefundedAvro
            {
                PaymentId = evt.PaymentId.ToString(),
                OrderId = evt.OrderId.ToString(),
                UserId = evt.UserId.ToString(),
                Amount = (double)evt.Amount,
                RefundedAt = evt.RefundedAt
            }
        };

        await _paymentRefundedProducer.ProduceAsync(_options.PaymentRefundedTopic, message, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _paymentSucceededProducer.Flush(TimeSpan.FromSeconds(5));
        _paymentFailedProducer.Flush(TimeSpan.FromSeconds(5));
        _paymentRefundedProducer.Flush(TimeSpan.FromSeconds(5));
        _paymentSucceededProducer.Dispose();
        _paymentFailedProducer.Dispose();
        _paymentRefundedProducer.Dispose();
        _schemaRegistryClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
