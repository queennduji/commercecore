using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

public class InventoryItemRepository : IInventoryItemRepository
{
    private readonly InventoryDbContext _dbContext;

    public InventoryItemRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<InventoryItem?> GetByProductAndLocationAsync(Guid productId, Guid locationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.InventoryItems
            .SingleOrDefaultAsync(i => i.ProductId == productId && i.LocationId == locationId, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItem>> ListByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InventoryItems
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.LocationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<InventoryItem> Items, int TotalCount)> ListAsync(
        Guid? productId,
        Guid? locationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InventoryItems.AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(i => i.ProductId == productId.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(i => i.LocationId == locationId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.ProductId).ThenBy(i => i.LocationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> AnyStockAtLocationAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.InventoryItems
            .AnyAsync(i => i.LocationId == locationId && (i.OnHand > 0 || i.Reserved > 0), cancellationToken);
    }

    public async Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        await _dbContext.InventoryItems.AddAsync(item, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
