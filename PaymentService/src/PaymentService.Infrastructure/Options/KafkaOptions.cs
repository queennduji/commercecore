namespace PaymentService.Infrastructure.Options;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string SchemaRegistryUrl { get; set; } = string.Empty;

    public string PaymentSucceededTopic { get; set; } = "payment.succeeded.v1";
    public string PaymentFailedTopic { get; set; } = "payment.failed.v1";
    public string PaymentRefundedTopic { get; set; } = "payment.refunded.v1";
}
