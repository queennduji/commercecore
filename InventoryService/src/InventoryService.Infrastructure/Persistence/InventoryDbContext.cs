using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Location>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Name).IsRequired().HasMaxLength(200);
            entity.Property(l => l.Code).IsRequired().HasMaxLength(32);
            entity.HasIndex(l => l.Code).IsUnique();
        });

        builder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Ignore(i => i.Available);
            entity.HasIndex(i => new { i.ProductId, i.LocationId }).IsUnique();
            entity.HasIndex(i => i.LocationId);
        });

        builder.Entity<StockReservation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.ReferenceId).HasMaxLength(200);
            entity.HasIndex(r => r.ProductId);
            entity.HasIndex(r => r.LocationId);
            entity.HasIndex(r => r.Status);
        });
    }
}
