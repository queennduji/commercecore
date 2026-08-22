using PaymentService.Application.Behaviors;
using PaymentService.Application.Interfaces;
using PaymentService.Infrastructure.Gateway;
using PaymentService.Infrastructure.Locking;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Options;
using PaymentService.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));

        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PaymentDatabase")));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        // Stateless (just holds a connection string and opens a fresh connection per Acquire
        // call), so this could be a singleton, but scoped matches every other per-request service
        // registered here.
        services.AddScoped<IOrderChargeLock, PostgresAdvisoryOrderChargeLock>();

        // StripePaymentGateway pulls this named client from IHttpClientFactory instead of letting
        // StripeClient build its own default HttpClient internally - that's what makes it possible
        // to wrap Stripe API calls in Polly's standard retry/circuit-breaker/timeout pipeline (see
        // StripePaymentGateway's constructor). Stripe's own SDK-level retry (BaseAddress/MaxTries)
        // is separate and left at its default - this is retry at the transport layer around it.
        services.AddHttpClient("Stripe").AddStandardResilienceHandler();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();

        services.AddValidatorsFromAssembly(typeof(Application.Commands.ChargeCommand).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Application.Commands.ChargeCommand).Assembly,
                typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
