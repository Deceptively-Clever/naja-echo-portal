using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Characters.VerifyCharacter;
using NajaEcho.Domain.Characters;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Characters;

public sealed class CharacterRepository(AppDbContext db) : ICharacterRepository
{
    public Task<bool> HandleExistsAsync(string handle, CancellationToken ct)
        => db.Characters.AnyAsync(c => c.Handle.ToLower() == handle.ToLower(), ct);

    public async Task AddAsync(Character character, CancellationToken ct)
    {
        db.Characters.Add(character);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new HandleAlreadyClaimedException();
        }
    }

    public Task<IReadOnlyList<Character>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct)
        => db.Characters
            .Where(c => c.OwnerUserId == ownerUserId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Character>)t.Result, TaskScheduler.Default);

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("23505") == true;
}
