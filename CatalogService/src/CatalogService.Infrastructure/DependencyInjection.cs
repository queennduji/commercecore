using CatalogService.Application.Behaviors;
using CatalogService.Application.Interfaces;
using CatalogService.Infrastructure.Messaging;
using CatalogService.Infrastructure.Options;
using CatalogService.Infrastructure.Persistence;
using CatalogService.Infrastructure.Storage;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Minio;

namespace CatalogService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CatalogDatabase")));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        // Resolved lazily from IOptions<MinioOptions> when IMinioClient is first requested (not
        // eagerly here), so test hosts that layer config overrides via
        // WebApplicationFactory.ConfigureAppConfiguration see the overridden values — same
        // reasoning as the JWT lazy-binding fix in Program.cs.
        services.AddSingleton<IMinioClient>(sp =>
        {
            var minioOptions = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
            return new MinioClient()
                .WithEndpoint(minioOptions.Endpoint)
                .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
                .WithSSL(minioOptions.UseSSL)
                .Build();
        });
        services.AddScoped<IBlobStorageService, MinioBlobStorageService>();
        services.AddHostedService<MinioBucketInitializer>();

        services.AddValidatorsFromAssembly(typeof(Application.Commands.CreateProductCommand).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Application.Commands.CreateProductCommand).Assembly,
                typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
