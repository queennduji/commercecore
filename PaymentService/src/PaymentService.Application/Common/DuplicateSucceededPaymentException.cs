namespace PaymentService.Application.Common;

/// <summary>Thrown when a concurrent request for the same order both passed
/// ChargeCommandHandler's pre-charge "does a Succeeded payment already exist" check and then lost
/// the race to actually insert theirs - the database's unique partial index (see
/// PaymentDbContext's "Succeeded"-only index on OrderId) rejected it. Infrastructure-agnostic on
/// purpose: PaymentRepository translates the underlying Npgsql/EF exception into this so the
/// Application layer's handler doesn't need to know it's Postgres under there.</summary>
public class DuplicateSucceededPaymentException : Exception
{
    public Guid OrderId { get; }

    public DuplicateSucceededPaymentException(Guid orderId, Exception innerException)
        : base($"A Succeeded payment for order {orderId} already exists (lost a concurrent insert race).", innerException)
    {
        OrderId = orderId;
    }
}
