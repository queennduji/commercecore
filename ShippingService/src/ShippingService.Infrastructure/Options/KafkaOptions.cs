namespace ShippingService.Infrastructure.Options;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string SchemaRegistryUrl { get; set; } = string.Empty;

    // Topics this service owns and publishes to.
    public string ShipmentDispatchedTopic { get; set; } = "shipment.dispatched.v1";
    public string ShipmentDeliveredTopic { get; set; } = "shipment.delivered.v1";
    public string ShipmentExceptionTopic { get; set; } = "shipment.exception.v1";

    // Topic owned by OrderService that this service only consumes.
    public string OrderPaidTopic { get; set; } = "order.paid.v1";
    public string OrderPaidConsumerGroupId { get; set; } = "shipping-service.order-paid-consumer";
}
