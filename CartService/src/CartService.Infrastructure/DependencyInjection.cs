using CartService.Application.Behaviors;
using CartService.Application.Interfaces;
using CartService.Infrastructure.Clients;
using CartService.Infrastructure.Options;
using CartService.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CartService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<CatalogServiceOptions>(configuration.GetSection(CatalogServiceOptions.SectionName));

        // Same lazy-binding reasoning used for every other Redis/Minio client in this project:
        // ConnectionMultiplexer.Connect is an eager network call, so it must be deferred to first
        // resolution (post-.Build()), not performed at AddInfrastructure-call-time.
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
        });
        services.AddScoped<ICartRepository, RedisCartRepository>();

        // The configureClient delegate here already runs lazily per HttpClient creation (via
        // IHttpClientFactory), resolving IOptions<CatalogServiceOptions> at that point rather than
        // at registration time – so no extra lazy-binding ceremony is needed for BaseAddress.
        services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>((sp, client) =>
        {
            var catalogOptions = sp.GetRequiredService<IOptions<CatalogServiceOptions>>().Value;
            client.BaseAddress = new Uri(catalogOptions.BaseUrl);
        });

        services.AddValidatorsFromAssembly(typeof(Application.Commands.CreateCartCommand).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Application.Commands.CreateCartCommand).Assembly,
                typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
