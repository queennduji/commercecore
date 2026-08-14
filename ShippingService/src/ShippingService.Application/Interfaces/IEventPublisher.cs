using ShippingService.Domain.Events;

namespace ShippingService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishShipmentDispatchedAsync(ShipmentDispatchedEvent evt, CancellationToken cancellationToken = default);

    Task PublishShipmentDeliveredAsync(ShipmentDeliveredEvent evt, CancellationToken cancellationToken = default);

    Task PublishShipmentExceptionAsync(ShipmentExceptionEvent evt, CancellationToken cancellationToken = default);
}
