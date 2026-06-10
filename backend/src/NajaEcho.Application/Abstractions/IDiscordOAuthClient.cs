using NajaEcho.Domain.Users;

namespace NajaEcho.Application.Abstractions;

public interface IDiscordOAuthClient
{
    Task<DiscordProfile> GetUserProfileAsync(string accessToken, CancellationToken ct = default);
}
