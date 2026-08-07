using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

public class LocationRepository : ILocationRepository
{
    private readonly InventoryDbContext _dbContext;

    public LocationRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Locations.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public Task<Location?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return _dbContext.Locations.SingleOrDefaultAsync(l => l.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Location>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Location>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Location location, CancellationToken cancellationToken = default)
    {
        await _dbContext.Locations.AddAsync(location, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
