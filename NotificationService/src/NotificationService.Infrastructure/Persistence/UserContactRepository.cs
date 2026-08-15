using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Persistence;

public class UserContactRepository : IUserContactRepository
{
    private readonly NotificationDbContext _dbContext;

    public UserContactRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserContactInfo?> GetContactAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var contact = await _dbContext.UserContacts.SingleOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        return contact is null ? null : new UserContactInfo(contact.Email, contact.PhoneNumber);
    }

    public async Task UpsertAsync(Guid userId, string email, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserContacts.SingleOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            await _dbContext.UserContacts.AddAsync(new UserContact { UserId = userId, Email = email, PhoneNumber = phoneNumber, UpdatedAt = now }, cancellationToken);
        }
        else
        {
            existing.Email = email;
            existing.PhoneNumber = phoneNumber;
            existing.UpdatedAt = now;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
