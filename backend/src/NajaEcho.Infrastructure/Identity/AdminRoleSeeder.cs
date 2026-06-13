using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace NajaEcho.Infrastructure.Identity;

public sealed class AdminRoleSeeder(RoleManager<IdentityRole<Guid>> roleManager, ILogger<AdminRoleSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        const string adminRole = "Admin";
        if (await roleManager.RoleExistsAsync(adminRole))
            return;

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>(adminRole));
        if (result.Succeeded)
            logger.LogInformation("Admin role seeded");
        else
            logger.LogError("Failed to seed Admin role: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}
