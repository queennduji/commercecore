namespace NotificationService.Application.Interfaces;

public record UserContactInfo(string? Email, string? PhoneNumber);

public interface IUserContactRepository
{
    Task<UserContactInfo?> GetContactAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Insert-or-update — auth.user-registered.v1 is the only source of truth for this
    /// table, and re-consuming the same message (Kafka's at-least-once delivery) must be
    /// idempotent. phoneNumber is optional, matching RegisterCommand's optional field.</summary>
    Task UpsertAsync(Guid userId, string email, string? phoneNumber, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
