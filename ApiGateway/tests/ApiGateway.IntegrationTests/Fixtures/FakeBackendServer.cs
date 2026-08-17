using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiGateway.IntegrationTests.Fixtures;

/// <summary>A minimal, real (socket-listening) HTTP server standing in for a backend microservice.
/// YARP makes genuine outbound HttpClient calls to whatever address a cluster's destination
/// points at, so an in-memory TestServer (no real socket) can't be used as a proxy target the way
/// it can for e.g. WebApplicationFactory's own client — this binds Kestrel to a real loopback
/// port instead, letting the gateway's own YARP pipeline exercise a real HTTP round trip in
/// tests, same rigor as the live Docker smoke test but without needing the real backend service
/// running.</summary>
public class FakeBackendServer : IAsyncDisposable
{
    private WebApplication? _app;

    public string BaseUrl { get; private set; } = string.Empty;
    public List<ReceivedRequest> ReceivedRequests { get; } = [];

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        _app = builder.Build();

        _app.Map("/{**catchAll}", (HttpContext ctx) =>
        {
            ReceivedRequests.Add(new ReceivedRequest(
                ctx.Request.Method,
                ctx.Request.Path.Value ?? string.Empty,
                ctx.Request.Headers.Authorization.ToString()));
            return Results.Ok(new { received = true, path = ctx.Request.Path.Value });
        });

        await _app.StartAsync();

        var addressFeature = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        BaseUrl = addressFeature!.Addresses.First();
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}

public record ReceivedRequest(string Method, string Path, string AuthorizationHeader);
