using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Locations;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Locations;

public sealed class SpaceStationRepository(AppDbContext db) : ISpaceStationRepository
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

        var existing = await db.SpaceStations
            .Where(s => incomingIds.Contains(s.UexId))
            .ToListAsync(ct);
        var existingByUexId = existing.ToDictionary(s => s.UexId);

        int added = 0, updated = 0, reactivated = 0, skipped = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var (uexId, incomingDoc) in incomingByUexId)
        {
            var root = incomingDoc.RootElement;
            var parentUexId = root.GetProperty("id_star_system").GetInt32();

            if (!starSystemMap.TryGetValue(parentUexId, out var starSystemId))
            {
                skipped++;
                continue;
            }

            if (existingByUexId.TryGetValue(uexId, out var stored))
            {
                stored.Name = root.GetProperty("name").GetString() ?? string.Empty;
                stored.Nickname = root.TryGetProperty("nickname", out var nickEl) ? nickEl.GetString() : null;
                stored.IsAvailable = root.GetProperty("is_available").GetInt32() == 1;
                stored.IsDecommissioned = root.GetProperty("is_decommissioned").GetInt32() == 1;
                stored.IsLandable = root.GetProperty("is_landable").GetInt32() == 1;
                stored.HasRefinery = root.GetProperty("has_refinery").GetInt32() == 1;
                stored.HasTradeTerminal = root.GetProperty("has_trade_terminal").GetInt32() == 1;
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
                var newStation = new SpaceStation
                {
                    Id = Guid.NewGuid(),
                    UexId = uexId,
                    StarSystemId = starSystemId,
                    Name = root.GetProperty("name").GetString() ?? string.Empty,
                    Nickname = root.TryGetProperty("nickname", out var nickEl) ? nickEl.GetString() : null,
                    IsAvailable = root.GetProperty("is_available").GetInt32() == 1,
                    IsDecommissioned = root.GetProperty("is_decommissioned").GetInt32() == 1,
                    IsLandable = root.GetProperty("is_landable").GetInt32() == 1,
                    HasRefinery = root.GetProperty("has_refinery").GetInt32() == 1,
                    HasTradeTerminal = root.GetProperty("has_trade_terminal").GetInt32() == 1,
                    Status = CatalogStatus.Active,
                    RawData = incomingDoc,
                    ImportedAt = now,
                    UpdatedAt = now,
                };
                db.SpaceStations.Add(newStation);
                added++;
            }
        }

        var softDeleted = await db.SpaceStations
            .Where(s => s.Status == CatalogStatus.Active && !incomingIds.Contains(s.UexId))
            .ToListAsync(ct);

        foreach (var station in softDeleted)
        {
            station.Status = CatalogStatus.SoftDeleted;
            station.SoftDeletedAt = now;
            station.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (added, updated, reactivated, softDeleted.Count, skipped);
    }

    public async Task<IReadOnlyList<StationDto>> SearchActiveStationsAsync(
        string? search,
        int limit,
        CancellationToken ct = default)
    {
        var query = db.SpaceStations
            .Where(s => s.Status == CatalogStatus.Active
                && s.IsAvailable
                && !s.IsDecommissioned);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{search}%"));
        }

        var results = await query
            .OrderBy(s => s.Name)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(s => new StationDto(s.Id, s.Name))
            .ToListAsync(ct);

        return results;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        db.SpaceStations.AnyAsync(s => s.Id == id && s.Status == CatalogStatus.Active, ct);
}
