using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AuthenticationService.Infrastructure.Startup;

/// <summary>Runs once at startup: ensures the "Admin" role exists, then assigns it to any
/// *already-registered* user whose email is in Admin:Emails (config). This is the only mechanism
/// in this system for granting admin rights - there's no admin-management UI/API. Doesn't cover a
/// user who registers for the first time after this runs with a configured admin email; see
/// RegisterCommandHandler for that half of it (assigns immediately at registration time instead
/// of waiting for the next restart).
///
/// A plain IHostedService, not a Quartz job like RefreshTokenCleanupJob - this needs to run
/// exactly once at startup, not on a recurring schedule.</summary>
public class AdminRoleSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AdminOptions _options;

    public AdminRoleSeeder(IServiceProvider serviceProvider, IOptions<AdminOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(AdminOptions.RoleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(AdminOptions.RoleName));
        }

        foreach (var email in _options.Emails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                // Not registered yet - RegisterCommandHandler handles this case when they do.
                continue;
            }

            if (!await userManager.IsInRoleAsync(user, AdminOptions.RoleName))
            {
                await userManager.AddToRoleAsync(user, AdminOptions.RoleName);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
