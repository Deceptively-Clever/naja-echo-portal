using NajaEcho.Domain.Users;

namespace NajaEcho.Application.Abstractions;

public interface IExternalLoginService
{
    Task<LocalUser> FindOrCreateAsync(DiscordProfile profile, CancellationToken ct = default);
    Task<LocalUser?> GetByIdAsync(Guid userId, CancellationToken ct = default);
}
