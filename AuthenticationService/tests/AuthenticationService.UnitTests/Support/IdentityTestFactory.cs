using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthenticationService.UnitTests.Support;

public static class IdentityTestFactory
{
    public static UserManager<ApplicationUser> CreateUserManager(out AuthDbContext dbContext)
    {
        var services = new ServiceCollection();

        services.AddDbContext<AuthDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddLogging();
        services.AddDataProtection();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        dbContext = provider.GetRequiredService<AuthDbContext>();
        return provider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    /// <summary>Same setup as CreateUserManager, plus a RoleManager - for tests that need to
    /// create/check roles directly (mirroring what AdminRoleSeeder does at real startup) rather
    /// than just users. A separate method instead of adding an out param to CreateUserManager so
    /// its existing callers, which don't need roles, stay untouched.</summary>
    public static UserManager<ApplicationUser> CreateUserManagerWithRoles(out RoleManager<IdentityRole<Guid>> roleManager)
    {
        var services = new ServiceCollection();

        services.AddDbContext<AuthDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddLogging();
        services.AddDataProtection();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        return provider.GetRequiredService<UserManager<ApplicationUser>>();
    }
}
