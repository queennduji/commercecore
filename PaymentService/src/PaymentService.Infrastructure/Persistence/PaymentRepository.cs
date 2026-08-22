using PaymentService.Application.Common;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _dbContext;

    public PaymentRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Payments.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> ListByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Payment?> GetLatestSucceededByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Payments
            .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Succeeded)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsSucceededPaymentUniqueViolation(ex))
        {
            // The concurrently-inserted Payment row is still tracked as Added here (SaveChanges
            // failed, it was never detached) - that's how OrderId is recovered without needing it
            // threaded through this call's signature.
            var orderId = _dbContext.ChangeTracker.Entries<Payment>()
                .First(e => e.State == EntityState.Added)
                .Entity.OrderId;

            throw new DuplicateSucceededPaymentException(orderId, ex);
        }
    }

    private static bool IsSucceededPaymentUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx
        && pgEx.ConstraintName == "IX_Payments_OrderId_Unique_Succeeded";
}
