using System.Threading.RateLimiting;
using ApiGateway.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var otelOptions = builder.Configuration.GetSection(OtelOptions.SectionName).Get<OtelOptions>()
    ?? throw new InvalidOperationException("Otel configuration section is missing.");

// Traces and logs deliberately go to two different backends — see OtelOptions — so this is two
// separate OTLP exporters, not one shared endpoint. Every downstream YARP proxy call also gets
// an outbound span from AddHttpClientInstrumentation() below, since YARP's forwarder runs on the
// standard .NET HttpClient machinery that instrumentation patches — a gateway->backend hop shows
// up in the trace the same as any other service's outbound HTTP call.
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(otelOptions.ServiceName));
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otelOptions.LogsEndpoint));
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(otelOptions.ServiceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otelOptions.TracesEndpoint)));

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Fixed-window, per-client-IP global limit — applies to every route (including /health and
// unauthenticated requests to protected routes) since abuse doesn't stop at auth. Read lazily
// from IConfiguration for the same test-host-override reason as the JWT options below.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? ((int)retryAfter.TotalSeconds).ToString()
            : "60";
        return ValueTask.CompletedTask;
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var rateLimitingOptions = httpContext.RequestServices.GetRequiredService<IConfiguration>()
            .GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitingOptions.PermitLimit,
            Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Resolved lazily from IConfiguration at options-creation time (not eagerly from
// builder.Configuration) so test hosts that layer config overrides via
// WebApplicationFactory.ConfigureAppConfiguration see the overridden values — same pattern
// used by every other service's JWT setup in this platform.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.Key))
        };
    });

// "RequireAuthenticatedUser" is applied per-route via the ReverseProxy:Routes config, only on
// the four fully-protected downstream services (Order/Payment/Shipping/Notification — those
// controllers are blanket [Authorize] with no anonymous actions). Auth/Catalog/Inventory/Cart
// routes carry no AuthorizationPolicy here and stay pass-through: those controllers have
// per-action [AllowAnonymous]/[Authorize] mixes (e.g. browsing products, guest carts, register/
// login), and blanket gateway enforcement there would incorrectly block requests that are
// legitimately anonymous today. Each downstream service still validates independently either
// way — this policy only decides whether the gateway itself rejects before proxying.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

var app = builder.Build();

// Ahead of auth deliberately — a client hammering a protected route with no/bad tokens should
// still get rate-limited, not exempted because every request fails auth first.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapReverseProxy();

app.Run();

public partial class Program;
