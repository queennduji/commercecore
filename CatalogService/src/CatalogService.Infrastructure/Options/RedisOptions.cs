namespace CatalogService.Infrastructure.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public int DefaultTtlSeconds { get; set; } = 300;
}
