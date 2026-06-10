using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Users;

namespace NajaEcho.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<UserProfile?> FindByDiscordUserIdAsync(string discordUserId, CancellationToken ct) =>
        db.UserProfiles.FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId, ct);

    public Task<UserProfile?> FindByIdAsync(Guid id, CancellationToken ct) =>
        db.UserProfiles.FindAsync([id], ct).AsTask();

    public async Task AddAsync(UserProfile user, CancellationToken ct) =>
        await db.UserProfiles.AddAsync(user, ct);
}
