using NotificationService.Application.Behaviors;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Consumers;
using NotificationService.Infrastructure.Gateway;
using NotificationService.Infrastructure.Options;
using NotificationService.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("NotificationDatabase")));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserContactRepository, UserContactRepository>();
        services.AddScoped<IEmailGateway, ResendEmailGateway>();
        services.AddScoped<ISmsGateway, TwilioSmsGateway>();

        services.AddHostedService<UserRegisteredConsumer>();
        services.AddHostedService<OrderCreatedConsumer>();
        services.AddHostedService<OrderPaidConsumer>();
        services.AddHostedService<OrderShippedConsumer>();
        services.AddHostedService<OrderDeliveredConsumer>();
        services.AddHostedService<OrderCancelledConsumer>();
        services.AddHostedService<OrderRefundedConsumer>();
        services.AddHostedService<PaymentFailedConsumer>();

        services.AddValidatorsFromAssembly(typeof(Application.Commands.SendOrderLifecycleNotificationCommand).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Application.Commands.SendOrderLifecycleNotificationCommand).Assembly,
                typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
