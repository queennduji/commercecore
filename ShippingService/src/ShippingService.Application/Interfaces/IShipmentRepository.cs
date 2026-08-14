using ShippingService.Domain.Entities;

namespace ShippingService.Application.Interfaces;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
