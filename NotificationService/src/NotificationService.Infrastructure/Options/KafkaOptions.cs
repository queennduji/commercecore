namespace NotificationService.Infrastructure.Options;

/// <summary>NotificationService owns no topics of its own – it's a pure terminal consumer (there's
/// nothing downstream that needs to react to "a notification was sent"), so this only lists
/// topics owned by other services that this service consumes.</summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string SchemaRegistryUrl { get; set; } = string.Empty;

    // Owned by AuthenticationService.
    public string UserRegisteredTopic { get; set; } = "auth.user-registered.v1";
    public string UserRegisteredConsumerGroupId { get; set; } = "notification-service.user-registered-consumer";

    // Owned by OrderService.
    public string OrderCreatedTopic { get; set; } = "order.created.v1";
    public string OrderCreatedConsumerGroupId { get; set; } = "notification-service.order-created-consumer";
    public string OrderPaidTopic { get; set; } = "order.paid.v1";
    public string OrderPaidConsumerGroupId { get; set; } = "notification-service.order-paid-consumer";
    public string OrderShippedTopic { get; set; } = "order.shipped.v1";
    public string OrderShippedConsumerGroupId { get; set; } = "notification-service.order-shipped-consumer";
    public string OrderDeliveredTopic { get; set; } = "order.delivered.v1";
    public string OrderDeliveredConsumerGroupId { get; set; } = "notification-service.order-delivered-consumer";
    public string OrderCancelledTopic { get; set; } = "order.cancelled.v1";
    public string OrderCancelledConsumerGroupId { get; set; } = "notification-service.order-cancelled-consumer";
    public string OrderRefundedTopic { get; set; } = "order.refunded.v1";
    public string OrderRefundedConsumerGroupId { get; set; } = "notification-service.order-refunded-consumer";

    // Owned by PaymentService.
    public string PaymentFailedTopic { get; set; } = "payment.failed.v1";
    public string PaymentFailedConsumerGroupId { get; set; } = "notification-service.payment-failed-consumer";
}
