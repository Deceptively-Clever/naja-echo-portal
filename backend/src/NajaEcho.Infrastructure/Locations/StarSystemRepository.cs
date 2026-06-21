using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Locations;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Locations;

public sealed class StarSystemRepository(AppDbContext db) : IStarSystemRepository
{
    public async Task<(int added, int updated, int reactivated, int softDeleted)> BulkUpsertAsync(
        IReadOnlyList<JsonDocument> records,
        CancellationToken ct = default)
    {
        var incomingByUexId = records
            .GroupBy(r => r.RootElement.GetProperty("id").GetInt32())
            .ToDictionary(g => g.Key, g => g.Last());
        var incomingIds = incomingByUexId.Keys.ToHashSet();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existing = await db.StarSystems
            .Where(s => incomingIds.Contains(s.UexId))
            .ToListAsync(ct);
        var existingByUexId = existing.ToDictionary(s => s.UexId);

        int added = 0, updated = 0, reactivated = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var (uexId, incomingDoc) in incomingByUexId)
        {
            var root = incomingDoc.RootElement;
            if (existingByUexId.TryGetValue(uexId, out var stored))
            {
                stored.Name = root.GetProperty("name").GetString() ?? string.Empty;
                stored.Code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
                stored.IsAvailable = root.GetProperty("is_available").GetInt32() == 1;
                stored.IsVisible = root.GetProperty("is_visible").GetInt32() == 1;
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
                var newSystem = new StarSystem
                {
                    Id = Guid.NewGuid(),
                    UexId = uexId,
                    Name = root.GetProperty("name").GetString() ?? string.Empty,
                    Code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null,
                    IsAvailable = root.GetProperty("is_available").GetInt32() == 1,
                    IsVisible = root.GetProperty("is_visible").GetInt32() == 1,
                    Status = CatalogStatus.Active,
                    RawData = incomingDoc,
                    ImportedAt = now,
                    UpdatedAt = now,
                };
                db.StarSystems.Add(newSystem);
                added++;
            }
        }

        var softDeleted = await db.StarSystems
            .Where(s => s.Status == CatalogStatus.Active && !incomingIds.Contains(s.UexId))
            .ToListAsync(ct);

        foreach (var system in softDeleted)
        {
            system.Status = CatalogStatus.SoftDeleted;
            system.SoftDeletedAt = now;
            system.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (added, updated, reactivated, softDeleted.Count);
    }

    public async Task<IReadOnlyDictionary<int, Guid>> GetActiveUexIdToIdMapAsync(CancellationToken ct = default)
    {
        var results = await db.StarSystems
            .Where(s => s.Status == CatalogStatus.Active)
            .Select(s => new { s.UexId, s.Id })
            .ToListAsync(ct);
        return results.ToDictionary(x => x.UexId, x => x.Id);
    }
}
