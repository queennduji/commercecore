using CartService.Domain.Entities;

namespace CartService.Application.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Upserts the cart and (re)sets its TTL — Redis is the primary store here, not a
    /// cache, so every write refreshes the expiry rather than relying on a fire-and-forget cache
    /// population.</summary>
    Task SaveAsync(Cart cart, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
