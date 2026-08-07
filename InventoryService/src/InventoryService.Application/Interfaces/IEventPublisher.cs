using InventoryService.Domain.Events;

namespace InventoryService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishStockAdjustedAsync(StockAdjustedEvent evt, CancellationToken cancellationToken = default);

    Task PublishStockReservedAsync(StockReservedEvent evt, CancellationToken cancellationToken = default);

    Task PublishReservationReleasedAsync(ReservationReleasedEvent evt, CancellationToken cancellationToken = default);

    Task PublishReservationCommittedAsync(ReservationCommittedEvent evt, CancellationToken cancellationToken = default);
}
