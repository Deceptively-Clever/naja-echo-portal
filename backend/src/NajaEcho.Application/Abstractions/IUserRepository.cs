using NajaEcho.Domain.Users;

namespace NajaEcho.Application.Abstractions;

public interface IUserRepository
{
    Task<UserProfile?> FindByDiscordUserIdAsync(string discordUserId, CancellationToken ct = default);
    Task<UserProfile?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(UserProfile user, CancellationToken ct = default);
}
