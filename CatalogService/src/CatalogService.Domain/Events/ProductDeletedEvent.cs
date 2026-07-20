namespace CatalogService.Domain.Events;

public class ProductDeletedEvent
{
    public Guid ProductId { get; set; }
    public DateTime DeletedAt { get; set; }
}
