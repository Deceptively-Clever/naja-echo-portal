using NajaEcho.Application.Abstractions;

namespace NajaEcho.Application.Features.Auth.SignInWithDiscord;

public sealed class SignInWithDiscordHandler(IExternalLoginService loginService)
{
    public async Task<SignInWithDiscordResult> HandleAsync(
        SignInWithDiscordCommand command,
        CancellationToken ct = default)
    {
        var localUser = await loginService.FindOrCreateAsync(command.Profile, ct);
        return new SignInWithDiscordResult(localUser.Id, localUser.DisplayName, localUser.DiscordUsername);
    }
}
