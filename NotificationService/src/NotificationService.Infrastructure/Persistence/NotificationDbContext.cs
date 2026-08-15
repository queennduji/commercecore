using NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserContact> UserContacts => Set<UserContact>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Channel).HasConversion<string>().HasMaxLength(16);
            entity.Property(n => n.Recipient).IsRequired().HasMaxLength(320);
            entity.Property(n => n.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(n => n.Subject).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Body).IsRequired();
            entity.Property(n => n.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(n => n.ProviderMessageId).HasMaxLength(100);
            entity.Property(n => n.FailureReason).HasMaxLength(500);
            entity.HasIndex(n => n.UserId);
        });

        builder.Entity<UserContact>(entity =>
        {
            entity.HasKey(c => c.UserId);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(320);
            entity.Property(c => c.PhoneNumber).HasMaxLength(20);
        });
    }
}
