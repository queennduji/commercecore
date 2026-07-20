namespace CatalogService.Domain.Events;

public class ProductCreatedEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
}
