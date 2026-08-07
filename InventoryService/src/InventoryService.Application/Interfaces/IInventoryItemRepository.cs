using InventoryService.Domain.Entities;

namespace InventoryService.Application.Interfaces;

public interface IInventoryItemRepository
{
    Task<InventoryItem?> GetByProductAndLocationAsync(Guid productId, Guid locationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItem>> ListByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryItem> Items, int TotalCount)> ListAsync(
        Guid? productId,
        Guid? locationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> AnyStockAtLocationAsync(Guid locationId, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
