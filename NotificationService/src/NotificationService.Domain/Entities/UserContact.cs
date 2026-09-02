namespace NotificationService.Domain.Entities;

/// <summary>A local userId -> contact-info lookup, populated entirely by consuming
/// AuthenticationService's auth.user-registered.v1 – this service never calls
/// AuthenticationService synchronously.</summary>
public class UserContact
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional – E.164 format when present. A missing phone simply means no SMS
    /// channel for this user (their account predates this field, or they didn't provide one).</summary>
    public string? PhoneNumber { get; set; }

    public DateTime UpdatedAt { get; set; }
}
