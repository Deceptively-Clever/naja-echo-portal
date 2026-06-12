using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Users;

namespace NajaEcho.Infrastructure.Identity;

public sealed class DiscordExternalLoginService(
    UserManager<ApplicationUser> userManager,
    ILogger<DiscordExternalLoginService> logger)
    : IExternalLoginService
{
    private const string DiscordProvider = "Discord";

    public async Task<LocalUser> FindOrCreateAsync(DiscordProfile profile, CancellationToken ct = default)
    {
        var user = await userManager.FindByLoginAsync(DiscordProvider, profile.Id);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = profile.Id,
                DisplayName = profile.DisplayName,
                DiscordUsername = profile.Username,
                SecurityStamp = Guid.NewGuid().ToString(),
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

            var loginInfo = new UserLoginInfo(DiscordProvider, profile.Id, DiscordProvider);
            var addLoginResult = await userManager.AddLoginAsync(user, loginInfo);
            if (!addLoginResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to link Discord login: {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");

            logger.LogInformation("Local user created {UserId}", user.Id);
            logger.LogInformation("External login linked {UserId} provider={Provider}", user.Id, DiscordProvider);
        }
        else
        {
            var changed = false;
            if (user.DisplayName != profile.DisplayName)
            {
                user.DisplayName = profile.DisplayName;
                changed = true;
            }
            if (user.DiscordUsername != profile.Username)
            {
                user.DiscordUsername = profile.Username;
                changed = true;
            }
            if (changed)
                await userManager.UpdateAsync(user);

            logger.LogInformation("Returning user found {UserId}", user.Id);
        }

        return new LocalUser(user.Id, user.DisplayName, user.DiscordUsername);
    }

    public async Task<LocalUser?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : new LocalUser(user.Id, user.DisplayName, user.DiscordUsername);
    }
}
