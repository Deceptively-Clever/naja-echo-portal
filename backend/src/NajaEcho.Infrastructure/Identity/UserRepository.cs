using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Identity;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> ExistsAsync(Guid userId, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Id == userId, ct);

    public async Task<IReadOnlyList<(Guid Id, string DisplayName)>> GetAllAsync(CancellationToken ct)
    {
        var users = await db.Users
            .OrderBy(u => u.DisplayName)
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(ct);
        return users.Select(u => (u.Id, u.DisplayName)).ToList();
    }
}
