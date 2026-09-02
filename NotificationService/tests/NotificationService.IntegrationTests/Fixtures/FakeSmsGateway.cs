using NotificationService.Application.Interfaces;

namespace NotificationService.IntegrationTests.Fixtures;

/// <summary>Stands in for a real Twilio account – same reasoning as <see cref="FakeEmailGateway"/>.
/// Defaults to succeeding every send; tests can add a recipient to <see cref="DeclinedRecipients"/>
/// to force a failure.</summary>
public class FakeSmsGateway : ISmsGateway
{
    public HashSet<string> DeclinedRecipients { get; } = [];
    public List<(string To, string Body)> Sent { get; } = [];

    public Task<SmsSendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((toPhoneNumber, body));

        if (DeclinedRecipients.Contains(toPhoneNumber))
        {
            return Task.FromResult(new SmsSendResult(false, null, "Simulated Twilio rejection."));
        }

        return Task.FromResult(new SmsSendResult(true, $"SM_fake_{Guid.NewGuid():N}", null));
    }
}
