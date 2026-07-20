namespace CatalogService.Domain.Events;

public class ProductUpdatedEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
