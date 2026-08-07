using InventoryService.Application.Behaviors;
using InventoryService.Application.Interfaces;
using InventoryService.Infrastructure.Consumers;
using InventoryService.Infrastructure.Messaging;
using InventoryService.Infrastructure.Options;
using InventoryService.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InventoryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("InventoryDatabase")));

        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddHostedService<ProductCreatedConsumer>();

        services.AddValidatorsFromAssembly(typeof(Application.Commands.CreateLocationCommand).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Application.Commands.CreateLocationCommand).Assembly,
                typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
