using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Characters;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Characters;

public sealed class PendingRegistrationRepository(AppDbContext db) : IPendingRegistrationRepository
{
    public Task<PendingCharacterRegistration?> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct)
        => db.PendingCharacterRegistrations
            .FirstOrDefaultAsync(p => p.OwnerUserId == ownerUserId, ct);

    public async Task UpsertAsync(PendingCharacterRegistration pending, CancellationToken ct)
    {
        var existing = await db.PendingCharacterRegistrations
            .FirstOrDefaultAsync(p => p.OwnerUserId == pending.OwnerUserId, ct);

        if (existing is null)
        {
            db.PendingCharacterRegistrations.Add(pending);
        }
        else
        {
            existing.Id = pending.Id;
            existing.Token = pending.Token;
            existing.ExpiresAt = pending.ExpiresAt;
            existing.CreatedAt = pending.CreatedAt;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveByOwnerAsync(Guid ownerUserId, CancellationToken ct)
    {
        await db.PendingCharacterRegistrations
            .Where(p => p.OwnerUserId == ownerUserId)
            .ExecuteDeleteAsync(ct);
    }
}
