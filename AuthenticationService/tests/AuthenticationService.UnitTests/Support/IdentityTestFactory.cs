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
}
