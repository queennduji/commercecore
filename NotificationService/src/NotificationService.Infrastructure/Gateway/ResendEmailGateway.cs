using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Resend;

namespace NotificationService.Infrastructure.Gateway;

/// <summary>Real Resend integration. Mirrors StripePaymentGateway/EasyPostShippingCarrierGateway's
/// role – the Application layer never depends on the Resend SDK directly.</summary>
public class ResendEmailGateway : IEmailGateway
{
    private readonly IResend _client;
    private readonly ResendOptions _options;

    public ResendEmailGateway(IOptions<ResendOptions> options)
    {
        _options = options.Value;
        _client = ResendClient.Create(_options.ApiKey);
    }

    public async Task<EmailSendResult> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new EmailMessage
            {
                From = _options.FromAddress,
                To = toEmail,
                Subject = subject,
                HtmlBody = htmlBody
            };

            var response = await _client.EmailSendAsync(message, cancellationToken);
            return response.Success
                ? new EmailSendResult(true, response.Content.ToString(), null)
                : new EmailSendResult(false, null, response.Exception?.Message ?? "Unknown Resend error.");
        }
        catch (ResendException ex)
        {
            return new EmailSendResult(false, null, ex.Message);
        }
    }
}
