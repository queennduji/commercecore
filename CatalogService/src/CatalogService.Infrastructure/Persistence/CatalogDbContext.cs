using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(64);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasIndex(p => p.CategoryId);
        });

        builder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(c => c.ParentCategoryId);

            // Sibling-scoped uniqueness: a name can repeat elsewhere in the tree, just not twice
            // under the same parent. Postgres treats every NULL as distinct in a plain unique
            // index, so ParentCategoryId IS NULL (top-level categories) needs its own partial
            // index rather than relying on the composite one to catch it.
            entity.HasIndex(c => new { c.Name, c.ParentCategoryId })
                .IsUnique()
                .HasFilter("\"ParentCategoryId\" IS NOT NULL");
            entity.HasIndex(c => c.Name)
                .IsUnique()
                .HasFilter("\"ParentCategoryId\" IS NULL");
        });

        builder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ObjectKey).IsRequired().HasMaxLength(512);
            entity.Property(i => i.Url).IsRequired().HasMaxLength(1024);
            entity.HasIndex(i => i.ProductId);
        });
    }
}
