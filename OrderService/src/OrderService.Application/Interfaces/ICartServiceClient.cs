namespace OrderService.Application.Interfaces;

public record CartLineSnapshot(Guid ProductId, string Sku, string Name, decimal UnitPrice, int Quantity);

public record CartSnapshot(Guid Id, IReadOnlyList<CartLineSnapshot> Items);

/// <summary>Synchronous HTTP call to CartService — checkout always operates on the caller's own
/// cart (Id == UserId, per CartService's deterministic-authenticated-cart convention), so there's
/// no separate cart id to pass around or spoof.</summary>
public interface ICartServiceClient
{
    Task<CartSnapshot?> GetCartAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default);
}
