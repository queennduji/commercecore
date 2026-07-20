namespace CatalogService.Infrastructure.Options;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string SchemaRegistryUrl { get; set; } = string.Empty;
    public string ProductCreatedTopic { get; set; } = "catalog.product-created.v1";
    public string ProductUpdatedTopic { get; set; } = "catalog.product-updated.v1";
    public string ProductDeletedTopic { get; set; } = "catalog.product-deleted.v1";
}
