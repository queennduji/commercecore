using InventoryService.Domain.Entities;

namespace InventoryService.Application.Interfaces;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Location?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Location>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Location>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Location location, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
