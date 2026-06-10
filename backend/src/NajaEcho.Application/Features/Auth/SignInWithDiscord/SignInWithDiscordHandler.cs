using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Users;

namespace NajaEcho.Application.Features.Auth.SignInWithDiscord;

public sealed class SignInWithDiscordHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IClock clock)
{
    public async Task<SignInWithDiscordResult> HandleAsync(
        SignInWithDiscordCommand command,
        CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var existing = await users.FindByDiscordUserIdAsync(command.Profile.Id, ct);

        UserProfile user;
        if (existing is null)
        {
            user = UserProfile.CreateFromDiscord(command.Profile, now);
            await users.AddAsync(user, ct);
        }
        else
        {
            existing.RecordLogin(command.Profile, now);
            user = existing;
        }

        await uow.SaveChangesAsync(ct);
        return new SignInWithDiscordResult(user.Id, user.DisplayName, user.AvatarRef);
    }
}
