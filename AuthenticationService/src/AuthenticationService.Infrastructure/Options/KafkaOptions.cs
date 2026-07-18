namespace AuthenticationService.Infrastructure.Options;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string SchemaRegistryUrl { get; set; } = string.Empty;
    public string UserRegisteredTopic { get; set; } = "auth.user-registered.v1";
    public string UserLoggedInTopic { get; set; } = "auth.user-logged-in.v1";
}
