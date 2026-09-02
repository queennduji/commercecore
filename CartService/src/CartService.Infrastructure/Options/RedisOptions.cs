namespace CartService.Infrastructure.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Redis is the primary store for carts here, not a cache – this is how long an
    /// abandoned cart survives before it's dropped entirely.</summary>
    public int TtlDays { get; set; } = 30;
}
