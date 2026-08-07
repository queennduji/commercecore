using InventoryService.Domain.Entities;

namespace InventoryService.Application.Interfaces;

public interface IStockReservationRepository
{
    Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(StockReservation reservation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
