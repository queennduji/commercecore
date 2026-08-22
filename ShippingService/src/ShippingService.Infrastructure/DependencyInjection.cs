using ShippingService.Application.Behaviors;
using ShippingService.Application.Interfaces;
using ShippingService.Infrastructure.Consumers;
using ShippingService.Infrastructure.Gateway;
using ShippingService.Infrastructure.Messaging;
using ShippingService.Infrastructure.Options;
using ShippingService.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace ShippingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<EasyPostOptions>(configuration.GetSection(EasyPostOptions.SectionName));

        services.AddDbContext<ShippingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ShippingDatabase")));

        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        // Same reasoning as PaymentService's "Stripe" named client - EasyPostShippingCarrierGateway
        // pulls this from IHttpClientFactory so its calls run through Polly's standard
        // retry/circuit-breaker/timeout pipeline. Lower stakes than the Stripe charge path (tracker
        // create/retrieve isn't a "charge money" operation), so no extra idempotency-key handling
        // needed here.
        services.AddHttpClient("EasyPost").AddStandardResilienceHandler();
        services.AddScoped<IShippingCarrierGateway, EasyPostShippingCarrierGateway>();
        services.AddHostedService<OrderPaidConsumer>();

        services.AddValidatorsFromAssembly(typeof(Application.Commands.DispatchShipmentCommand).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Application.Commands.DispatchShipmentCommand).Assembly,
                typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
