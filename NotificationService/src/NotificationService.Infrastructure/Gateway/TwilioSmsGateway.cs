using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace NotificationService.Infrastructure.Gateway;

/// <summary>Real Twilio integration. Mirrors ResendEmailGateway/StripePaymentGateway/
/// EasyPostShippingCarrierGateway's role — the Application layer never depends on the Twilio SDK
/// directly. Twilio's client is a process-wide static (TwilioClient.Init), so construction here is
/// idempotent even though this gateway is registered scoped like the others.</summary>
public class TwilioSmsGateway : ISmsGateway
{
    private readonly TwilioOptions _options;

    public TwilioSmsGateway(IOptions<TwilioOptions> options)
    {
        _options = options.Value;
        TwilioClient.Init(_options.AccountSid, _options.AuthToken);
    }

    public async Task<SmsSendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await MessageResource.CreateAsync(new CreateMessageOptions(new PhoneNumber(toPhoneNumber))
            {
                From = new PhoneNumber(_options.FromPhoneNumber),
                Body = body
            });

            return message.ErrorCode is null
                ? new SmsSendResult(true, message.Sid, null)
                : new SmsSendResult(false, message.Sid, message.ErrorMessage ?? $"Twilio error code {message.ErrorCode}.");
        }
        catch (TwilioException ex)
        {
            return new SmsSendResult(false, null, ex.Message);
        }
    }
}
