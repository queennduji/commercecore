namespace AuthenticationService.Domain.Events;

public class UserRegisteredEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional – E.164 format when present. Added for NotificationService's SMS channel.</summary>
    public string? PhoneNumber { get; set; }

    public DateTime RegisteredAt { get; set; }
}
