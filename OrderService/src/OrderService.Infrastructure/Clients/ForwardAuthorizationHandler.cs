using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace OrderService.Infrastructure.Clients;

/// <summary>
/// Copies the current inbound request's Authorization header onto every outgoing call made
/// through this handler. InventoryService's write endpoints (reserve/release/commit) require a
/// valid JWT, and since the shared symmetric key/issuer/audience is trusted across every service
/// in this project, forwarding the caller's own token is the natural way for OrderService to act
/// "on behalf of" the shopper it's checking out – no separate service-account token needed.
/// </summary>
public class ForwardAuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var incomingAuthorization = _httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization].ToString();
        if (!string.IsNullOrEmpty(incomingAuthorization))
        {
            request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, incomingAuthorization);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
