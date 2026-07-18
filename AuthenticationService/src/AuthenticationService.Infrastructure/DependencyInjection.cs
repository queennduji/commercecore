using AuthenticationService.Application.Behaviors;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Jobs;
using AuthenticationService.Infrastructure.Messaging;
using AuthenticationService.Infrastructure.Options;
using AuthenticationService.Infrastructure.Persistence;
using AuthenticationService.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace AuthenticationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AuthDatabase")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<TokenIssuer>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        services.AddValidatorsFromAssembly(typeof(Application.Commands.RegisterCommand).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Application.Commands.RegisterCommand).Assembly,
                typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey(nameof(RefreshTokenCleanupJob));
            quartz.AddJob<RefreshTokenCleanupJob>(opts => opts.WithIdentity(jobKey));
            quartz.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{nameof(RefreshTokenCleanupJob)}-trigger")
                .WithCronSchedule("0 0 * * * ?"));
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
