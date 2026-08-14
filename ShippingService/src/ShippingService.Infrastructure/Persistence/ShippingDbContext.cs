using ShippingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShippingService.Infrastructure.Persistence;

public class ShippingDbContext : DbContext
{
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options)
    {
    }

    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Shipment>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ShippingAddress).IsRequired().HasMaxLength(500);
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(s => s.CarrierName).HasMaxLength(100);
            entity.Property(s => s.TrackingNumber).HasMaxLength(100);
            entity.Property(s => s.ProviderTrackerId).HasMaxLength(100);
            entity.Property(s => s.ExceptionReason).HasMaxLength(500);
            // One shipment per order (the granularity decision for this platform — see README).
            entity.HasIndex(s => s.OrderId).IsUnique();
            entity.HasIndex(s => s.UserId);
        });
    }
}
