using OrderService.Application.Behaviors;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Clients;
using OrderService.Infrastructure.Consumers;
using OrderService.Infrastructure.Locking;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Options;
using OrderService.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<CartServiceOptions>(configuration.GetSection(CartServiceOptions.SectionName));
        services.Configure<InventoryServiceOptions>(configuration.GetSection(InventoryServiceOptions.SectionName));
        services.Configure<PaymentServiceOptions>(configuration.GetSection(PaymentServiceOptions.SectionName));

        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("OrderDatabase")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        // Mirrors PaymentService's IOrderChargeLock registration - see
        // PostgresAdvisoryOrderPaymentLock and MarkOrderPaidCommandHandler for why.
        services.AddScoped<IOrderPaymentLock, PostgresAdvisoryOrderPaymentLock>();
        services.AddHostedService<ShipmentDispatchedConsumer>();
        services.AddHostedService<ShipmentDeliveredConsumer>();

        services.AddHttpContextAccessor();
        services.AddTransient<ForwardAuthorizationHandler>();

        // The configureClient delegate on this overload already runs lazily per HttpClient
        // creation (via IHttpClientFactory), resolving IOptions<T> at that point — same
        // lazy-binding reasoning as every other config-dependent client in this project.
        // ForwardAuthorizationHandler copies the caller's own JWT onto these outgoing calls —
        // InventoryService's reserve/release/commit endpoints require [Authorize].
        // AddStandardResilienceHandler wraps every call below in Polly's standard pipeline: retry
        // (with jittered exponential backoff), a circuit breaker, and both per-attempt and
        // total-request timeouts - triggered on 5xx/408/network errors/timeouts, matching what
        // this repo previously had none of (see the note on ChargeAsync's idempotency key below
        // for why blindly retrying a mutating call needed a companion fix, not just this handler).
        services.AddHttpClient<ICartServiceClient, CartServiceClient>((sp, client) =>
        {
            var cartOptions = sp.GetRequiredService<IOptions<CartServiceOptions>>().Value;
            client.BaseAddress = new Uri(cartOptions.BaseUrl);
        }).AddHttpMessageHandler<ForwardAuthorizationHandler>()
          .AddStandardResilienceHandler();

        services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>((sp, client) =>
        {
            var inventoryOptions = sp.GetRequiredService<IOptions<InventoryServiceOptions>>().Value;
            client.BaseAddress = new Uri(inventoryOptions.BaseUrl);
        }).AddHttpMessageHandler<ForwardAuthorizationHandler>()
          .AddStandardResilienceHandler();

        // Retrying this one specifically is why PaymentService's IPaymentGateway.ChargeAsync
        // gained an idempotency-key parameter (see StripePaymentGateway) - a dropped response
        // after Stripe already processed the charge must not turn into a second charge on retry.
        services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>((sp, client) =>
        {
            var paymentOptions = sp.GetRequiredService<IOptions<PaymentServiceOptions>>().Value;
            client.BaseAddress = new Uri(paymentOptions.BaseUrl);
        }).AddHttpMessageHandler<ForwardAuthorizationHandler>()
          .AddStandardResilienceHandler();

        services.AddValidatorsFromAssembly(typeof(Application.Commands.CheckoutCommand).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Application.Commands.CheckoutCommand).Assembly,
                typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
