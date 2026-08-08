namespace CartService.Infrastructure.Options;

public class CatalogServiceOptions
{
    public const string SectionName = "CatalogService";

    public string BaseUrl { get; set; } = string.Empty;
}
