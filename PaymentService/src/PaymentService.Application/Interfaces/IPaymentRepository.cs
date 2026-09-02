using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> ListByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>The payment a refund should act against – the most recent Succeeded charge for
    /// this order.</summary>
    Task<Payment?> GetLatestSucceededByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
