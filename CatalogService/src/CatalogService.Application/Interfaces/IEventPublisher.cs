using CatalogService.Domain.Events;

namespace CatalogService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishProductCreatedAsync(ProductCreatedEvent evt, CancellationToken cancellationToken = default);

    Task PublishProductUpdatedAsync(ProductUpdatedEvent evt, CancellationToken cancellationToken = default);

    Task PublishProductDeletedAsync(ProductDeletedEvent evt, CancellationToken cancellationToken = default);
}
