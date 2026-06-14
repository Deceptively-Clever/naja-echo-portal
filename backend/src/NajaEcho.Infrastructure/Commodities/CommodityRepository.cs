using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Commodities.GetCommodities;
using NajaEcho.Domain.Commodities;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Commodities;

public sealed class CommodityRepository(AppDbContext db) : ICommodityRepository
{
    public async Task<(IReadOnlyList<CommodityListItem> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Commodities
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ThenBy(c => c.UexId);

        var total = await query.CountAsync(ct);

        // Project in the database so the heavy raw_data jsonb (and unused columns) are never loaded.
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CommodityListItem(c.Id, c.UexId, c.Name, c.Code, c.Kind, c.Status))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(int Inserted, int Updated, int Unchanged, int Restored, int SoftDeleted)> BulkUpsertAsync(
        IReadOnlyList<Commodity> incoming, CancellationToken ct = default)
    {
        // Tolerate duplicate uex_id in the feed (last record wins) rather than throwing on ToDictionary.
        var incomingByUexId = incoming
            .GroupBy(c => c.UexId)
            .ToDictionary(g => g.Key, g => g.Last());
        var incomingIds = incomingByUexId.Keys.ToHashSet();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existing = await db.Commodities
            .Where(c => incomingIds.Contains(c.UexId))
            .ToListAsync(ct);
        var existingByUexId = existing.ToDictionary(c => c.UexId);

        int inserted = 0, updated = 0, unchanged = 0, restored = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var inc in incomingByUexId.Values)
        {
            if (existingByUexId.TryGetValue(inc.UexId, out var stored))
            {
                var wasDeleted = stored.Status == CommodityStatus.SoftDeleted;
                var changed = HasChanges(stored, inc);

                if (wasDeleted)
                {
                    UpdateFields(stored, inc, now);
                    stored.Status = CommodityStatus.Active;
                    stored.SoftDeletedAt = null;
                    restored++;
                }
                else if (changed)
                {
                    UpdateFields(stored, inc, now);
                    updated++;
                }
                else
                {
                    // No promoted field changed — leave the row (and updated_at) untouched.
                    unchanged++;
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

        return (inserted, updated, unchanged, restored, toSoftDelete.Count);
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

    // Cheap change detection on the source-of-truth fields the feed can move. Avoids rewriting
    // (and bumping updated_at on) rows that are byte-for-byte identical to the last import.
    private static bool HasChanges(Commodity stored, Commodity inc) =>
        stored.Name != inc.Name ||
        stored.Uuid != inc.Uuid ||
        stored.Code != inc.Code ||
        stored.Slug != inc.Slug ||
        stored.Kind != inc.Kind ||
        stored.WeightScu != inc.WeightScu ||
        stored.IdParent != inc.IdParent ||
        stored.IdItem != inc.IdItem ||
        stored.SourceDateModified != inc.SourceDateModified;
}
