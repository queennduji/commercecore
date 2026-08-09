namespace OrderService.Infrastructure.Options;

public class InventoryServiceOptions
{
    public const string SectionName = "InventoryService";

    public string BaseUrl { get; set; } = string.Empty;
}
