using PaymentService.Domain.Events;

namespace PaymentService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishPaymentSucceededAsync(PaymentSucceededEvent evt, CancellationToken cancellationToken = default);

    Task PublishPaymentFailedAsync(PaymentFailedEvent evt, CancellationToken cancellationToken = default);

    Task PublishPaymentRefundedAsync(PaymentRefundedEvent evt, CancellationToken cancellationToken = default);
}
