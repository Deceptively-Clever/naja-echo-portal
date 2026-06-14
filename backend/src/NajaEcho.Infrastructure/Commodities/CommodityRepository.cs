using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Commodities;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Commodities;

public sealed class CommodityRepository(AppDbContext db) : ICommodityRepository
{
    public async Task<(IReadOnlyList<Commodity> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Commodities.OrderBy(c => c.Name);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(int Inserted, int Updated, int Restored, int SoftDeleted)> BulkUpsertAsync(
        IReadOnlyList<Commodity> incoming, CancellationToken ct = default)
    {
        var incomingByUexId = incoming.ToDictionary(c => c.UexId);
        var incomingIds = incomingByUexId.Keys.ToHashSet();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existing = await db.Commodities
            .Where(c => incomingIds.Contains(c.UexId))
            .ToListAsync(ct);
        var existingByUexId = existing.ToDictionary(c => c.UexId);

        int inserted = 0, updated = 0, restored = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var inc in incoming)
        {
            if (existingByUexId.TryGetValue(inc.UexId, out var stored))
            {
                var wasDeleted = stored.Status == CommodityStatus.SoftDeleted;
                UpdateFields(stored, inc, now);

                if (wasDeleted)
                {
                    stored.Status = CommodityStatus.Active;
                    stored.SoftDeletedAt = null;
                    restored++;
                }
                else
                {
                    updated++;
                }
            }
            else
            {
                inc.Id = Guid.NewGuid();
                inc.Status = CommodityStatus.Active;
                inc.ImportedAt = now;
                inc.UpdatedAt = now;
                db.Commodities.Add(inc);
                inserted++;
            }
        }

        // Soft-delete Active commodities globally absent from the incoming feed
        var toSoftDelete = await db.Commodities
            .Where(c => c.Status == CommodityStatus.Active && !incomingIds.Contains(c.UexId))
            .ToListAsync(ct);

        foreach (var commodity in toSoftDelete)
        {
            commodity.Status = CommodityStatus.SoftDeleted;
            commodity.SoftDeletedAt = now;
            commodity.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (inserted, updated, restored, toSoftDelete.Count);
    }

    private static void UpdateFields(Commodity stored, Commodity inc, DateTimeOffset now)
    {
        stored.Uuid = inc.Uuid;
        stored.Name = inc.Name;
        stored.Code = inc.Code;
        stored.Slug = inc.Slug;
        stored.Kind = inc.Kind;
        stored.WeightScu = inc.WeightScu;
        stored.IdParent = inc.IdParent;
        stored.IdItem = inc.IdItem;
        stored.Wiki = inc.Wiki;
        stored.IdsStarSystemsRaw = inc.IdsStarSystemsRaw;
        stored.IdsPlanetsRaw = inc.IdsPlanetsRaw;
        stored.IdsMoonsRaw = inc.IdsMoonsRaw;
        stored.IdsPoiRaw = inc.IdsPoiRaw;
        stored.IdsOrbitsRaw = inc.IdsOrbitsRaw;
        stored.IdsStarSystems = inc.IdsStarSystems;
        stored.IdsPlanets = inc.IdsPlanets;
        stored.IdsMoons = inc.IdsMoons;
        stored.IdsPoi = inc.IdsPoi;
        stored.IdsOrbits = inc.IdsOrbits;
        stored.IsAvailable = inc.IsAvailable;
        stored.IsAvailableLive = inc.IsAvailableLive;
        stored.IsVisible = inc.IsVisible;
        stored.IsExtractable = inc.IsExtractable;
        stored.IsMineral = inc.IsMineral;
        stored.IsRaw = inc.IsRaw;
        stored.IsPure = inc.IsPure;
        stored.IsRefined = inc.IsRefined;
        stored.IsRefinable = inc.IsRefinable;
        stored.IsHarvestable = inc.IsHarvestable;
        stored.IsBuyable = inc.IsBuyable;
        stored.IsSellable = inc.IsSellable;
        stored.IsTemporary = inc.IsTemporary;
        stored.IsIllegal = inc.IsIllegal;
        stored.IsVolatileQt = inc.IsVolatileQt;
        stored.IsVolatileTime = inc.IsVolatileTime;
        stored.IsInert = inc.IsInert;
        stored.IsExplosive = inc.IsExplosive;
        stored.IsBuggy = inc.IsBuggy;
        stored.IsFuel = inc.IsFuel;
        stored.SourceDateAdded = inc.SourceDateAdded;
        stored.SourceDateModified = inc.SourceDateModified;
        stored.SourceDateAddedUtc = inc.SourceDateAddedUtc;
        stored.SourceDateModifiedUtc = inc.SourceDateModifiedUtc;
        stored.RawData = inc.RawData;
        stored.UpdatedAt = now;
    }
}
