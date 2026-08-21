namespace OrderService.Api.Options;

/// <summary>Traces and logs go to two different backends (Jaeger and the logs-only OTel
/// Collector fanning out to Elasticsearch respectively) — see root docker-compose.yml — so each
/// gets its own OTLP endpoint rather than sharing one.</summary>
public class OtelOptions
{
    public const string SectionName = "Otel";

    public string ServiceName { get; set; } = string.Empty;
    public string TracesEndpoint { get; set; } = string.Empty;
    public string LogsEndpoint { get; set; } = string.Empty;
}
