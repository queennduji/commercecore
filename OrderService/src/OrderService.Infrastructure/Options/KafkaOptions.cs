namespace OrderService.Infrastructure.Options;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string SchemaRegistryUrl { get; set; } = string.Empty;

    public string OrderCreatedTopic { get; set; } = "order.created.v1";
    public string OrderPaidTopic { get; set; } = "order.paid.v1";
    public string OrderShippedTopic { get; set; } = "order.shipped.v1";
    public string OrderDeliveredTopic { get; set; } = "order.delivered.v1";
    public string OrderCancelledTopic { get; set; } = "order.cancelled.v1";
    public string OrderRefundedTopic { get; set; } = "order.refunded.v1";

    // Topics owned by ShippingService that this service only consumes – these are what now drive
    // Order.Status Paid->Shipped->Delivered, replacing the old manual ship/deliver ops endpoints.
    public string ShipmentDispatchedTopic { get; set; } = "shipment.dispatched.v1";
    public string ShipmentDispatchedConsumerGroupId { get; set; } = "order-service.shipment-dispatched-consumer";
    public string ShipmentDeliveredTopic { get; set; } = "shipment.delivered.v1";
    public string ShipmentDeliveredConsumerGroupId { get; set; } = "order-service.shipment-delivered-consumer";
}
