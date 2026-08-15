namespace NotificationService.Application.Interfaces;

public record EmailSendResult(bool Succeeded, string? ProviderMessageId, string? FailureReason);

/// <summary>Abstraction over the real email processor (Resend) so the Application layer never
/// depends on the Resend SDK directly, and so integration tests can swap in a deterministic fake
/// without needing a real Resend account. Mirrors PaymentService's IPaymentGateway /
/// ShippingService's IShippingCarrierGateway split.</summary>
public interface IEmailGateway
{
    Task<EmailSendResult> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
