namespace NotificationService.Domain.Entities;

/// <summary>A record of one notification attempt on one channel — recorded whether it succeeded
/// or failed, mirroring PaymentService's Payment/ShippingService's Shipment audit-trail pattern.
/// One lifecycle event that finds both an email and a phone on file produces two rows (one per
/// channel), not one row describing both outcomes.</summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; }

    /// <summary>The email address (Channel == Email) or E.164 phone number (Channel == Sms) this
    /// attempt was sent to.</summary>
    public string Recipient { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    /// <summary>Empty for SMS — Resend requires a subject, Twilio messages don't have one.</summary>
    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Failed;

    /// <summary>Resend's email id or Twilio's message SID — what you'd look this up by in either
    /// provider's dashboard.</summary>
    public string? ProviderMessageId { get; set; }

    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
