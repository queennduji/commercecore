using ShippingService.Application.Interfaces;
using ShippingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShippingService.Infrastructure.Persistence;

public class ShipmentRepository : IShipmentRepository
{
    private readonly ShippingDbContext _dbContext;

    public ShipmentRepository(ShippingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Shipments.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Shipments.SingleOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Shipments.AddAsync(shipment, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
