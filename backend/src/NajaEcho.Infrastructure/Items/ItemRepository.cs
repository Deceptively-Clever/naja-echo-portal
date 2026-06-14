using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Domain.Items;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Items;

public sealed class ItemRepository(AppDbContext db) : IItemRepository
{
    public async Task<(int Inserted, int Updated, int Unchanged, int SoftDeleted, int Restored)> BulkUpsertForCategoryAsync(
        int idCategory, IReadOnlyList<Item> incoming, CancellationToken ct = default)
    {
        var incomingByUexId = incoming.ToDictionary(i => i.UexId);
        var incomingUexIds = incomingByUexId.Keys.ToHashSet();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existing = await db.Items
            .Where(i => i.IdCategory == idCategory)
            .ToListAsync(ct);
        var existingByUexId = existing.ToDictionary(i => i.UexId);

        int inserted = 0, updated = 0, unchanged = 0, restored = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var inc in incoming)
        {
            if (existingByUexId.TryGetValue(inc.UexId, out var stored))
            {
                var wasDeleted = stored.Status == ItemStatus.SoftDeleted;

                UpdateFields(stored, inc, now);

                if (wasDeleted)
                {
                    stored.Status = ItemStatus.Active;
                    stored.SoftDeletedAt = null;
                    restored++;
                }
                else if (HasChanges(stored, inc))
                {
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
                inc.Status = ItemStatus.Active;
                inc.ImportedAt = now;
                inc.UpdatedAt = now;
                db.Items.Add(inc);
                inserted++;
            }
        }

        // Soft-delete Active items in THIS category that are absent from the incoming set
        var toSoftDelete = await db.Items
            .Where(i => i.IdCategory == idCategory &&
                        i.Status == ItemStatus.Active &&
                        !incomingUexIds.Contains(i.UexId))
            .ToListAsync(ct);

        foreach (var item in toSoftDelete)
        {
            item.Status = ItemStatus.SoftDeleted;
            item.SoftDeletedAt = now;
            item.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (inserted, updated, unchanged, toSoftDelete.Count, restored);
    }

    private static void UpdateFields(Item stored, Item inc, DateTimeOffset now)
    {
        stored.UexId = inc.UexId;
        stored.IdParent = inc.IdParent;
        stored.IdCategory = inc.IdCategory;
        stored.IdCompany = inc.IdCompany;
        stored.IdVehicle = inc.IdVehicle;
        stored.Name = inc.Name;
        stored.Section = inc.Section;
        stored.Category = inc.Category;
        stored.CompanyName = inc.CompanyName;
        stored.VehicleName = inc.VehicleName;
        stored.Slug = inc.Slug;
        stored.Size = inc.Size;
        stored.Color = inc.Color;
        stored.Color2 = inc.Color2;
        stored.UrlStore = inc.UrlStore;
        stored.Wiki = inc.Wiki;
        stored.Quality = inc.Quality;
        stored.IsExclusivePledge = inc.IsExclusivePledge;
        stored.IsExclusiveSubscriber = inc.IsExclusiveSubscriber;
        stored.IsExclusiveConcierge = inc.IsExclusiveConcierge;
        stored.IsCommodity = inc.IsCommodity;
        stored.IsHarvestable = inc.IsHarvestable;
        stored.Notification = inc.Notification;
        stored.GameVersion = inc.GameVersion;
        stored.SourceDateAdded = inc.SourceDateAdded;
        stored.SourceDateModified = inc.SourceDateModified;
        stored.RawData = inc.RawData;
        stored.UpdatedAt = now;
    }

    private static bool HasChanges(Item stored, Item inc) =>
        stored.Name != inc.Name ||
        stored.UexId != inc.UexId ||
        stored.IdCategory != inc.IdCategory ||
        stored.Section != inc.Section ||
        stored.GameVersion != inc.GameVersion ||
        stored.SourceDateModified != inc.SourceDateModified;
}
