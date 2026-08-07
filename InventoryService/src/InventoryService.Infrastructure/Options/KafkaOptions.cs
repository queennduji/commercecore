namespace InventoryService.Infrastructure.Options;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string SchemaRegistryUrl { get; set; } = string.Empty;

    // Topics this service owns and publishes to.
    public string StockAdjustedTopic { get; set; } = "inventory.stock-adjusted.v1";
    public string StockReservedTopic { get; set; } = "inventory.stock-reserved.v1";
    public string ReservationReleasedTopic { get; set; } = "inventory.reservation-released.v1";
    public string ReservationCommittedTopic { get; set; } = "inventory.reservation-committed.v1";

    // Topic owned by CatalogService that this service only consumes.
    public string ProductCreatedTopic { get; set; } = "catalog.product-created.v1";
    public string ProductCreatedConsumerGroupId { get; set; } = "inventory-service.product-created-consumer";
}
