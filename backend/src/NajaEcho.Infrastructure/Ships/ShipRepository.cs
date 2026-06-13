using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Ships;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Ships;

public sealed class ShipRepository(AppDbContext db) : IShipRepository
{
    public async Task<(IReadOnlyList<Ship> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Ships.OrderBy(s => s.Name);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public Task<Ship?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Ships.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Ship?> GetByUexIdAsync(int uexId, CancellationToken ct = default) =>
        db.Ships.FirstOrDefaultAsync(s => s.UexId == uexId, ct);

    public async Task<(int Added, int Updated, int Reactivated, int SoftDeleted)> BulkUpsertAsync(
        IReadOnlyList<Ship> incomingShips, CancellationToken ct = default)
    {
        var incomingByUexId = incomingShips.ToDictionary(s => s.UexId);
        var incomingIds = incomingByUexId.Keys.ToHashSet();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existing = await db.Ships
            .Where(s => incomingIds.Contains(s.UexId))
            .ToListAsync(ct);
        var existingByUexId = existing.ToDictionary(s => s.UexId);

        int added = 0, updated = 0, reactivated = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var incoming in incomingShips)
        {
            if (existingByUexId.TryGetValue(incoming.UexId, out var stored))
            {
                // Update promoted columns and raw data
                stored.Uuid = incoming.Uuid;
                stored.Name = incoming.Name;
                stored.NameFull = incoming.NameFull;
                stored.CompanyName = incoming.CompanyName;
                stored.RawData = incoming.RawData;
                stored.UpdatedAt = now;

                if (stored.Status == ShipStatus.SoftDeleted)
                {
                    stored.Status = ShipStatus.Active;
                    stored.SoftDeletedAt = null;
                    reactivated++;
                }
                else
                {
                    updated++;
                }
            }
            else
            {
                incoming.Id = Guid.NewGuid();
                incoming.Status = ShipStatus.Active;
                incoming.ImportedAt = now;
                incoming.UpdatedAt = now;
                db.Ships.Add(incoming);
                added++;
            }
        }

        // Soft-delete Active ships absent from the feed
        var softDeleted = await db.Ships
            .Where(s => s.Status == ShipStatus.Active && !incomingIds.Contains(s.UexId))
            .ToListAsync(ct);

        foreach (var ship in softDeleted)
        {
            ship.Status = ShipStatus.SoftDeleted;
            ship.SoftDeletedAt = now;
            ship.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (added, updated, reactivated, softDeleted.Count);
    }

    public async Task<IReadOnlyList<int>> GetAllActiveUexIdsAsync(CancellationToken ct = default) =>
        await db.Ships
            .Where(s => s.Status == ShipStatus.Active)
            .Select(s => s.UexId)
            .ToListAsync(ct);
}
