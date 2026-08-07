using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

public class StockReservationRepository : IStockReservationRepository
{
    private readonly InventoryDbContext _dbContext;

    public StockReservationRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.StockReservations.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        await _dbContext.StockReservations.AddAsync(reservation, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
