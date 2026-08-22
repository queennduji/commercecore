namespace AuthenticationService.Infrastructure.Options;

/// <summary>Bootstraps who gets the "Admin" role - there's no admin-management UI/flow anywhere
/// in this system, so this config-driven list is the only mechanism. Emails are compared
/// case-insensitively (see AdminRoleSeeder/RegisterCommandHandler) since ASP.NET Core Identity's
/// own email storage is already normalized that way.</summary>
public class AdminOptions
{
    public const string SectionName = "Admin";
    public const string RoleName = "Admin";

    public string[] Emails { get; set; } = [];
}
