using PaymentService.Application.Behaviors;
using PaymentService.Application.Interfaces;
using PaymentService.Infrastructure.Gateway;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Options;
using PaymentService.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
