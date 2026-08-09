namespace OrderService.Infrastructure.Options;

public class CartServiceOptions
{
    public const string SectionName = "CartService";

    public string BaseUrl { get; set; } = string.Empty;
}
