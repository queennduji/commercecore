using OrderService.Application.Interfaces;

namespace OrderService.IntegrationTests.Fixtures;

public class FakeCharge
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PaymentMethodId { get; init; }
}

/// <summary>Stands in for a real PaymentService — same reasoning as FakeCartServiceClient /
/// FakeInventoryServiceClient. Defaults to succeeding every charge/refund; tests can add an
/// OrderId to <see cref="DeclinedOrderIds"/> to force a decline and assert the checkout saga
/// handles it correctly, and inspect <see cref="Charges"/>/<see cref="RefundedOrderIds"/>
/// afterward to assert calls actually happened.</summary>
public class FakePaymentServiceClient : IPaymentServiceClient
{
    public List<FakeCharge> Charges { get; } = [];
    public List<Guid> RefundedOrderIds { get; } = [];
    public HashSet<Guid> DeclinedOrderIds { get; } = [];
    public HashSet<Guid> RefundFailureOrderIds { get; } = [];

    public Task<PaymentResult> ChargeAsync(Guid orderId, decimal amount, string currency, string paymentMethodId, CancellationToken cancellationToken = default)
    {
        Charges.Add(new FakeCharge { OrderId = orderId, Amount = amount, Currency = currency, PaymentMethodId = paymentMethodId });

        if (DeclinedOrderIds.Contains(orderId))
        {
            return Task.FromResult(new PaymentResult(false, "Your card was declined."));
        }

        return Task.FromResult(new PaymentResult(true, null));
    }

    public Task<PaymentResult> RefundAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (RefundFailureOrderIds.Contains(orderId))
        {
            return Task.FromResult(new PaymentResult(false, "No successful payment found for this order."));
        }

        RefundedOrderIds.Add(orderId);
        return Task.FromResult(new PaymentResult(true, null));
    }
}
