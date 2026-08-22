namespace OrderService.Application.Interfaces;

/// <summary>
/// A distributed lock keyed by OrderId, held for the duration of one MarkOrderPaidCommandHandler
/// execution (the status check, the call to PaymentService, and the resulting order update/event
/// publish) - across all OrderService instances, not just within one process.
///
/// Mirrors PaymentService's IOrderChargeLock/PostgresAdvisoryOrderChargeLock exactly (same
/// reasoning: OrderService already has its own Postgres connection and doesn't otherwise depend
/// on Redis, so a session-level Postgres advisory lock needed no new infrastructure). This exists
/// because MarkOrderPaidCommandHandler's own "is this order still Pending" check was a
/// check-then-act race with nothing serializing it - two concurrent /pay requests could both read
/// Pending before either committed Paid, meaning both would call PaymentService concurrently.
/// PaymentService's own lock/idempotency-key/DB-constraint chain already makes that safe against
/// an actual double-charge, but this closes the race at its source instead of relying solely on
/// the downstream service to absorb it.
/// </summary>
public interface IOrderPaymentLock
{
    /// <summary>Blocks until the lock for <paramref name="orderId"/> is free, then holds it until
    /// the returned handle is disposed. Honors <paramref name="cancellationToken"/> while
    /// waiting - there's no separate lock-acquire timeout, so an upstream request timeout or
    /// client disconnect is what bounds an otherwise-indefinite wait.</summary>
    Task<IAsyncDisposable> AcquireAsync(Guid orderId, CancellationToken cancellationToken = default);
}
