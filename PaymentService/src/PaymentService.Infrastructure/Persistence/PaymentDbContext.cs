using PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Currency).IsRequired().HasMaxLength(3);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(p => p.ProviderReference).HasMaxLength(200);
            entity.Property(p => p.FailureReason).HasMaxLength(500);
            entity.HasIndex(p => p.OrderId).HasDatabaseName("IX_Payments_OrderId");
            entity.HasIndex(p => p.UserId);

            // A second, separate index over the SAME property - the string-property-names overload
            // is required for that (repeating entity.HasIndex(p => p.OrderId) would just reconfigure
            // the index above instead of adding a new one; EF treats same-properties HasIndex calls
            // as the same index unless distinguished this way).
            //
            // Backstops ChargeCommandHandler's application-level idempotency guard
            // (GetLatestSucceededByOrderIdAsync check before charging) against two concurrent
            // requests for the same order both passing that check before either commits - the
            // check-then-act race the handler's own comment calls out as unprotected. A plain
            // unique index on OrderId would be wrong: multiple Failed attempts for the same order
            // are expected (e.g. a declined card retried with a different one). Postgres partial
            // indexes let the uniqueness apply only to Succeeded rows, which is the actual
            // invariant - at most one successful charge per order, ever.
            entity.HasIndex([nameof(Payment.OrderId)], "IX_Payments_OrderId_Unique_Succeeded")
                .IsUnique()
                .HasFilter("\"Status\" = 'Succeeded'");
        });
    }
}
