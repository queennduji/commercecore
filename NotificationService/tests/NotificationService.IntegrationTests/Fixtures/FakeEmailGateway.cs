using NotificationService.Application.Interfaces;

namespace NotificationService.IntegrationTests.Fixtures;

/// <summary>Stands in for a real Resend account – same reasoning as PaymentService's
/// FakePaymentGateway / ShippingService's FakeShippingCarrierGateway. Defaults to succeeding
/// every send; tests can add a recipient to <see cref="DeclinedRecipients"/> to force a failure.</summary>
public class FakeEmailGateway : IEmailGateway
{
    public HashSet<string> DeclinedRecipients { get; } = [];
    public List<(string To, string Subject, string Body)> Sent { get; } = [];

    public Task<EmailSendResult> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        Sent.Add((toEmail, subject, htmlBody));

        if (DeclinedRecipients.Contains(toEmail))
        {
            return Task.FromResult(new EmailSendResult(false, null, "Simulated Resend rejection."));
        }

        return Task.FromResult(new EmailSendResult(true, $"msg_fake_{Guid.NewGuid():N}", null));
    }
}
