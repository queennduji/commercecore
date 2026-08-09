using OrderService.Domain.Events;

namespace OrderService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken = default);

    Task PublishOrderPaidAsync(OrderPaidEvent evt, CancellationToken cancellationToken = default);

    Task PublishOrderShippedAsync(OrderShippedEvent evt, CancellationToken cancellationToken = default);

    Task PublishOrderDeliveredAsync(OrderDeliveredEvent evt, CancellationToken cancellationToken = default);

    Task PublishOrderCancelledAsync(OrderCancelledEvent evt, CancellationToken cancellationToken = default);

    Task PublishOrderRefundedAsync(OrderRefundedEvent evt, CancellationToken cancellationToken = default);
}
