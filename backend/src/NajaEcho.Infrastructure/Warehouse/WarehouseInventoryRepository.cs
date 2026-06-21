using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.AddInventoryItem;
using NajaEcho.Application.Features.Warehouse.ChangeInventoryQuantity;
using NajaEcho.Application.Features.Warehouse.GetInventory;
using NajaEcho.Application.Features.Warehouse.GetInventoryFilters;
using NajaEcho.Application.Features.Warehouse.SearchCatalogItems;
using NajaEcho.Domain.Items;
using NajaEcho.Domain.Warehouse;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Warehouse;

public sealed class WarehouseInventoryRepository(AppDbContext db) : IWarehouseInventoryRepository
{
    // ──────────────────────────────────────────────────────────────────────
    // Read
    // ──────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<InventoryRowDto>> GetInventoryAsync(
        string? name, string? type, string? subtype, Guid? ownerUserId, string? location,
        CancellationToken ct)
    {
        var namePattern = string.IsNullOrWhiteSpace(name) ? null : $"%{name}%";
        var locationPattern = string.IsNullOrWhiteSpace(location) ? null : $"%{location}%";

        var rows = await db.Database.SqlQuery<InventoryRow>($"""
            SELECT
              w.id                  AS id,
              w.item_id             AS item_id,
              i.name                AS name,
              i.section             AS type,
              i.category            AS subtype,
              w.quantity            AS quantity,
              w.quality             AS quality,
              w.owner_user_id       AS owner_user_id,
              u.display_name        AS owner_display_name,
              w.location            AS location
            FROM warehouse_inventory w
            JOIN sc.items i       ON i.id = w.item_id
            JOIN "AspNetUsers" u  ON u.id = w.owner_user_id
            WHERE ({namePattern}::text IS NULL OR i.name ILIKE {namePattern})
              AND ({type}::text        IS NULL OR i.section   = {type})
              AND ({subtype}::text     IS NULL OR i.category  = {subtype})
              AND ({ownerUserId}::uuid IS NULL OR w.owner_user_id = {ownerUserId})
              AND ({locationPattern}::text IS NULL OR w.location ILIKE {locationPattern})
            ORDER BY i.name
            """).ToListAsync(ct);

        return rows.Select(r => new InventoryRowDto(
            r.Id, r.ItemId, r.Name, r.Type, r.Subtype, r.Quantity, r.Quality,
            r.OwnerUserId, r.OwnerDisplayName, r.Location)).ToList();
    }

    private sealed record InventoryRow(
        Guid Id, Guid ItemId, string Name, string? Type, string? Subtype,
        int Quantity, int Quality, Guid OwnerUserId, string OwnerDisplayName, string Location);

    public async Task<InventoryFiltersDto> GetInventoryFiltersAsync(CancellationToken ct)
    {
        var types = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT section AS value
            FROM sc.item_categories
            WHERE section IS NOT NULL
            ORDER BY value
            """).ToListAsync(ct);

        var subtypes = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT name AS value
            FROM sc.item_categories
            ORDER BY value
            """).ToListAsync(ct);

        var owners = await db.Database.SqlQuery<OwnerRow>($"""
            SELECT id AS user_id, display_name AS display_name
            FROM "AspNetUsers"
            ORDER BY display_name
            """).ToListAsync(ct);

        return new InventoryFiltersDto(
            types.Select(t => t.Value).ToList(),
            subtypes.Select(s => s.Value).ToList(),
            owners.Select(o => new OwnerOption(o.UserId, o.DisplayName)).ToList());
    }

    private sealed record StringValue(string Value);
    private sealed record OwnerRow(Guid UserId, string DisplayName);

    public async Task<IReadOnlyList<CatalogItemResultDto>> SearchCatalogItemsAsync(
        string? search, int limit, CancellationToken ct)
    {
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%";

        var rows = await db.Database.SqlQuery<CatalogRow>($"""
            SELECT
              i.id       AS item_id,
              i.name     AS name,
              i.section  AS type,
              i.category AS subtype
            FROM sc.items i
            WHERE i.status = {ItemStatus.Active.ToString()}
              AND ({searchPattern}::text IS NULL OR i.name ILIKE {searchPattern})
            ORDER BY i.name
            FETCH FIRST {limit} ROWS ONLY
            """).ToListAsync(ct);

        return rows.Select(r => new CatalogItemResultDto(r.ItemId, r.Name, r.Type, r.Subtype)).ToList();
    }

    private sealed record CatalogRow(Guid ItemId, string Name, string? Type, string? Subtype);

    // ──────────────────────────────────────────────────────────────────────
    // Write
    // ──────────────────────────────────────────────────────────────────────

    public async Task<(InventoryRowDto Row, bool IsNew)> AddOrIncrementAsync(
        Guid itemId, Guid ownerUserId, string location, int quantity, int quality, Guid? stationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var results = await db.Database.SqlQuery<UpsertResult>($"""
            INSERT INTO warehouse_inventory (
                id, item_id, owner_user_id, location, quantity, quality, station_id, created_at, updated_at
            )
            VALUES (
                {Guid.NewGuid()}, {itemId}, {ownerUserId}, {location}, {quantity}, {quality}, {stationId}, {now}, {now}
            )
            ON CONFLICT (item_id, owner_user_id, location)
            DO UPDATE SET
                quantity = warehouse_inventory.quantity + EXCLUDED.quantity,
                quality = EXCLUDED.quality,
                updated_at = EXCLUDED.updated_at
            RETURNING
                id AS id,
                (xmax = 0) AS is_new
            """).ToListAsync(ct);

        var result = results.Single();

        var dto = await LoadRowDtoAsync(result.Id, ct);
        return (dto, result.IsNew);
    }

    private sealed record UpsertResult(Guid Id, bool IsNew);

    public async Task<InventoryRowDto> UpdateQuantityAsync(Guid id, int quantity, CancellationToken ct)
    {
        var entry = await db.WarehouseInventory.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (entry is null)
            throw new InventoryRowNotFoundException(id);

        entry.Quantity = quantity;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return await LoadRowDtoAsync(id, ct);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct)
    {
        var entry = await db.WarehouseInventory.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (entry is null)
            throw new InventoryRowNotFoundException(id);

        db.WarehouseInventory.Remove(entry);
        await db.SaveChangesAsync(ct);
    }

    private async Task<InventoryRowDto> LoadRowDtoAsync(Guid id, CancellationToken ct)
    {
        var rows = await db.Database.SqlQuery<InventoryRow>($"""
            SELECT
              w.id                  AS id,
              w.item_id             AS item_id,
              i.name                AS name,
              i.section             AS type,
              i.category            AS subtype,
              w.quantity            AS quantity,
              w.quality             AS quality,
              w.owner_user_id       AS owner_user_id,
              u.display_name        AS owner_display_name,
              w.location            AS location
            FROM warehouse_inventory w
            JOIN sc.items i       ON i.id = w.item_id
            JOIN "AspNetUsers" u  ON u.id = w.owner_user_id
            WHERE w.id = {id}
            """).FirstAsync(ct);

        return new InventoryRowDto(
            rows.Id, rows.ItemId, rows.Name, rows.Type, rows.Subtype,
            rows.Quantity, rows.Quality, rows.OwnerUserId, rows.OwnerDisplayName, rows.Location);
    }
}
