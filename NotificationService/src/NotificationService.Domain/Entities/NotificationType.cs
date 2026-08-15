namespace NotificationService.Domain.Entities;

/// <summary>One value per Kafka topic this service consumes to trigger a notification — see
/// KafkaOptions in the Infrastructure layer for the exact topic names.</summary>
public enum NotificationType
{
    OrderCreated,
    OrderPaid,
    OrderShipped,
    OrderDelivered,
    OrderCancelled,
    OrderRefunded,
    PaymentFailed
}
