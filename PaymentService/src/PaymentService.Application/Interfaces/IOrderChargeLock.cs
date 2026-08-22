namespace PaymentService.Application.Interfaces;

/// <summary>
/// A distributed lock keyed by OrderId, held for the duration of one charge attempt (the
/// existing-payment check, the Stripe call, and the resulting insert) so that at most one request
/// for a given order is ever actually processing at a time - across all PaymentService instances,
/// not just within one process. Complements, rather than replaces, the database's unique partial
/// index on Payments (OrderId where Status = Succeeded): the lock is what stops a second
/// concurrent request from wastefully reaching Stripe at all; the index is the backstop that still
/// guarantees correctness even if a lock is ever bypassed or misbehaves.
/// </summary>
public interface IOrderChargeLock
{
    /// <summary>Blocks until the lock for <paramref name="orderId"/> is free, then holds it until
    /// the returned handle is disposed. Honors <paramref name="cancellationToken"/> while
    /// waiting - there's no separate lock-acquire timeout, so an upstream request timeout or
    /// client disconnect is what bounds an otherwise-indefinite wait.</summary>
    Task<IAsyncDisposable> AcquireAsync(Guid orderId, CancellationToken cancellationToken = default);
}
