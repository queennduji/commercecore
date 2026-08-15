namespace NotificationService.Application.Interfaces;

public record SmsSendResult(bool Succeeded, string? ProviderMessageId, string? FailureReason);

/// <summary>Abstraction over the real SMS processor (Twilio) so the Application layer never
/// depends on the Twilio SDK directly, and so integration tests can swap in a deterministic fake
/// without needing a real Twilio account. Mirrors IEmailGateway.</summary>
public interface ISmsGateway
{
    Task<SmsSendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default);
}
