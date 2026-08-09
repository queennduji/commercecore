namespace OrderService.Application.Interfaces;

public record LocationStockSnapshot(Guid LocationId, int Available);

/// <summary>Synchronous HTTP call to InventoryService — used both to pick a fulfilling location at
/// checkout (the first location with enough Available stock) and to reserve/release/commit against
/// it as the order moves through its lifecycle.</summary>
public interface IInventoryServiceClient
{
    Task<IReadOnlyList<LocationStockSnapshot>> GetStockAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<Guid?> ReserveAsync(Guid productId, Guid locationId, int quantity, string referenceId, CancellationToken cancellationToken = default);

    Task<bool> ReleaseAsync(Guid reservationId, CancellationToken cancellationToken = default);

    Task<bool> CommitAsync(Guid reservationId, CancellationToken cancellationToken = default);
}
