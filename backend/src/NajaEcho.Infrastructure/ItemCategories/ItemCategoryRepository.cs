using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.ItemCategories;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.ItemCategories;

public sealed class ItemCategoryRepository(AppDbContext db) : IItemCategoryRepository
{
    public async Task<(int Inserted, int Updated, int Unchanged)> BulkUpsertAsync(
        IReadOnlyList<ItemCategory> incoming, CancellationToken ct = default)
    {
        var incomingByUexId = incoming.ToDictionary(c => c.UexId);
        var incomingIds = incomingByUexId.Keys.ToHashSet();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existing = await db.ItemCategories
            .Where(c => incomingIds.Contains(c.UexId))
            .ToListAsync(ct);
        var existingByUexId = existing.ToDictionary(c => c.UexId);

        int inserted = 0, updated = 0, unchanged = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var inc in incoming)
        {
            if (existingByUexId.TryGetValue(inc.UexId, out var stored))
            {
                if (stored.Name != inc.Name ||
                    stored.Type != inc.Type ||
                    stored.Section != inc.Section ||
                    stored.IsGameRelated != inc.IsGameRelated ||
                    stored.IsMining != inc.IsMining ||
                    stored.SourceDateModified != inc.SourceDateModified)
                {
                    stored.Name = inc.Name;
                    stored.Type = inc.Type;
                    stored.Section = inc.Section;
                    stored.IsGameRelated = inc.IsGameRelated;
                    stored.IsMining = inc.IsMining;
                    stored.SourceDateAdded = inc.SourceDateAdded;
                    stored.SourceDateModified = inc.SourceDateModified;
                    stored.RawData = inc.RawData;
                    stored.UpdatedAt = now;
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }
            else
            {
                inc.Id = Guid.NewGuid();
                inc.ImportedAt = now;
                inc.UpdatedAt = now;
                db.ItemCategories.Add(inc);
                inserted++;
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (inserted, updated, unchanged);
    }

    public async Task<IReadOnlyList<ItemCategory>> GetAllAsync(CancellationToken ct = default) =>
        await db.ItemCategories.OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<ItemCategory>> GetEligibleAsync(CancellationToken ct = default) =>
        await db.ItemCategories
            .Where(c => c.Type == "item")
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<DateTimeOffset?> GetLastRefreshedAtAsync(CancellationToken ct = default)
    {
        if (!await db.ItemCategories.AnyAsync(ct))
            return null;
        return await db.ItemCategories.MaxAsync(c => (DateTimeOffset?)c.UpdatedAt, ct);
    }

    public async Task<int> GetActiveItemCountAsync(int categoryUexId, CancellationToken ct = default) =>
        await db.Items
            .Where(i => i.IdCategory == categoryUexId && i.Status == Domain.Items.ItemStatus.Active)
            .CountAsync(ct);

    public async Task<DateTimeOffset?> GetLastImportedAtAsync(int categoryUexId, CancellationToken ct = default)
    {
        if (!await db.Items.AnyAsync(i => i.IdCategory == categoryUexId, ct))
            return null;
        return await db.Items
            .Where(i => i.IdCategory == categoryUexId)
            .MaxAsync(i => (DateTimeOffset?)i.UpdatedAt, ct);
    }
}
