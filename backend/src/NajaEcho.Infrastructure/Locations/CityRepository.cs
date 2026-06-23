using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Locations;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Locations;

public sealed class CityRepository(AppDbContext db) : ICityRepository
{
    public async Task<(int added, int updated, int reactivated, int softDeleted, int skipped)> BulkUpsertAsync(
        IReadOnlyList<JsonDocument> records,
        IReadOnlyDictionary<int, Guid> starSystemMap,
        CancellationToken ct = default)
    {
        var incomingByUexId = records
            .GroupBy(r => r.RootElement.GetProperty("id").GetInt32())
            .ToDictionary(g => g.Key, g => g.Last());
        var incomingIds = incomingByUexId.Keys.ToHashSet();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existing = await db.Cities
            .Where(c => incomingIds.Contains(c.UexId))
            .ToListAsync(ct);
        var existingByUexId = existing.ToDictionary(c => c.UexId);

        int added = 0, updated = 0, reactivated = 0, skipped = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var (uexId, incomingDoc) in incomingByUexId)
        {
            var root = incomingDoc.RootElement;

            if (!root.TryGetProperty("id_star_system", out var starSystemEl) || starSystemEl.ValueKind == JsonValueKind.Null)
            {
                skipped++;
                continue;
            }

            var parentUexId = starSystemEl.GetInt32();
            if (!starSystemMap.TryGetValue(parentUexId, out var starSystemId))
            {
                skipped++;
                continue;
            }

            var isAvailable = root.TryGetProperty("is_available", out var avEl) && avEl.GetInt32() == 1;
            var isAvailableLive = root.TryGetProperty("is_available_live", out var avLiveEl) && avLiveEl.GetInt32() == 1;
            var isVisible = root.TryGetProperty("is_visible", out var visEl) && visEl.GetInt32() == 1;

            if (existingByUexId.TryGetValue(uexId, out var stored))
            {
                stored.Name = root.GetProperty("name").GetString() ?? string.Empty;
                stored.Code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
                stored.StarSystemId = starSystemId;
                stored.IsAvailable = isAvailable;
                stored.IsAvailableLive = isAvailableLive;
                stored.IsVisible = isVisible;
                stored.RawData = incomingDoc;
                stored.UpdatedAt = now;

                if (stored.Status == CatalogStatus.SoftDeleted)
                {
                    stored.Status = CatalogStatus.Active;
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
                var newCity = new City
                {
                    Id = Guid.NewGuid(),
                    UexId = uexId,
                    StarSystemId = starSystemId,
                    Name = root.GetProperty("name").GetString() ?? string.Empty,
                    Code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null,
                    IsAvailable = isAvailable,
                    IsAvailableLive = isAvailableLive,
                    IsVisible = isVisible,
                    Status = CatalogStatus.Active,
                    RawData = incomingDoc,
                    ImportedAt = now,
                    UpdatedAt = now,
                };
                db.Cities.Add(newCity);
                added++;
            }
        }

        var softDeleted = await db.Cities
            .Where(c => c.Status == CatalogStatus.Active && !incomingIds.Contains(c.UexId))
            .ToListAsync(ct);

        foreach (var city in softDeleted)
        {
            city.Status = CatalogStatus.SoftDeleted;
            city.SoftDeletedAt = now;
            city.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (added, updated, reactivated, softDeleted.Count, skipped);
    }

    public async Task<IReadOnlyList<CityDto>> SearchActiveCitiesAsync(
        string? search,
        int limit,
        CancellationToken ct = default)
    {
        var query = db.Cities
            .Where(c => c.Status == CatalogStatus.Active
                && c.IsAvailable
                && c.IsVisible);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"));
        }

        var results = await query
            .OrderBy(c => c.Name)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(c => new CityDto(c.Id, c.Name))
            .ToListAsync(ct);

        return results;
    }
}
