using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.Materials.ChangeMaterialQuantity;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterialFilters;
using NajaEcho.Application.Features.Warehouse.Materials.GetMaterials;
using NajaEcho.Application.Features.Warehouse.Materials.SearchCommodities;
using NajaEcho.Domain.Commodities;
using NajaEcho.Domain.Warehouse;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Warehouse;

public sealed class MaterialInventoryRepository(AppDbContext db) : IMaterialInventoryRepository
{
    // ──────────────────────────────────────────────────────────────────────
    // Read
    // ──────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<MaterialRowDto>> GetMaterialsAsync(
        string? material, Guid? ownerUserId, string? location, int? qualityMin, int? qualityMax,
        CancellationToken ct)
    {
        var materialPattern = string.IsNullOrWhiteSpace(material) ? null : $"%{material}%";
        var locationPattern = string.IsNullOrWhiteSpace(location) ? null : $"%{location}%";

        var rows = await db.Database.SqlQuery<MaterialRow>($"""
            SELECT
              w.id                  AS id,
              w.commodity_id        AS commodity_id,
              c.name                AS material_name,
              c.code                AS material_code,
              w.quantity            AS quantity,
              w.quality             AS quality,
              w.owner_user_id       AS owner_user_id,
              u.display_name        AS owner_display_name,
              w.location            AS location
            FROM warehouse_material_inventory w
            JOIN sc.commodities c ON c.id = w.commodity_id
            JOIN "AspNetUsers" u  ON u.id = w.owner_user_id
            WHERE ({materialPattern}::text IS NULL OR c.name ILIKE {materialPattern} OR c.code ILIKE {materialPattern})
              AND ({ownerUserId}::uuid IS NULL OR w.owner_user_id = {ownerUserId})
              AND ({locationPattern}::text IS NULL OR w.location ILIKE {locationPattern})
              AND ({qualityMin}::int IS NULL OR w.quality >= {qualityMin})
              AND ({qualityMax}::int IS NULL OR w.quality <= {qualityMax})
            ORDER BY c.name ASC, w.quality DESC, u.display_name ASC, w.location ASC
            """).ToListAsync(ct);

        return rows.Select(r => new MaterialRowDto(
            r.Id, r.CommodityId, r.MaterialName, r.MaterialCode, r.Quantity, r.Quality,
            r.OwnerUserId, r.OwnerDisplayName, r.Location)).ToList();
    }

    private sealed record MaterialRow(
        Guid Id, Guid CommodityId, string MaterialName, string? MaterialCode,
        decimal Quantity, int Quality, Guid OwnerUserId, string OwnerDisplayName, string Location);

    public async Task<MaterialFiltersDto> GetMaterialFiltersAsync(CancellationToken ct)
    {
        var owners = await db.Database.SqlQuery<OwnerRow>($"""
            SELECT DISTINCT u.id AS user_id, u.display_name AS display_name
            FROM "AspNetUsers" u
            JOIN "AspNetUserRoles" ur ON ur.user_id = u.id
            JOIN "AspNetRoles" r ON r.id = ur.role_id
            WHERE r.name IN ('Quartermaster', 'Admin')
            ORDER BY u.display_name
            """).ToListAsync(ct);

        var locations = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT location AS value
            FROM warehouse_material_inventory
            ORDER BY value
            """).ToListAsync(ct);

        return new MaterialFiltersDto(
            owners.Select(o => new OwnerOption(o.UserId, o.DisplayName)).ToList(),
            locations.Select(l => l.Value).ToList());
    }

    private sealed record StringValue(string Value);
    private sealed record OwnerRow(Guid UserId, string DisplayName);

    public async Task<IReadOnlyList<CommodityResultDto>> SearchCommoditiesAsync(
        string? search, int limit, CancellationToken ct)
    {
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%";

        var rows = await db.Database.SqlQuery<CommodityRow>($"""
            SELECT
              c.id   AS commodity_id,
              c.name AS name,
              c.code AS code
            FROM sc.commodities c
            WHERE c.status = {CommodityStatus.Active.ToString()}
              AND ({searchPattern}::text IS NULL OR c.name ILIKE {searchPattern})
            ORDER BY c.name
            FETCH FIRST {limit} ROWS ONLY
            """).ToListAsync(ct);

        return rows.Select(r => new CommodityResultDto(r.CommodityId, r.Name, r.Code)).ToList();
    }

    private sealed record CommodityRow(Guid CommodityId, string Name, string? Code);

    // ──────────────────────────────────────────────────────────────────────
    // Write
    // ──────────────────────────────────────────────────────────────────────

    public async Task<(MaterialRowDto Row, bool IsNew)> AddOrIncrementAsync(
        Guid commodityId, Guid ownerUserId, string location, decimal quantity, int quality, Guid? stationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var results = await db.Database.SqlQuery<UpsertResult>($"""
            INSERT INTO warehouse_material_inventory (
                id, commodity_id, owner_user_id, location, quantity, quality, station_id, created_at, updated_at
            )
            VALUES (
                {Guid.NewGuid()}, {commodityId}, {ownerUserId}, {location}, {quantity}, {quality}, {stationId}, {now}, {now}
            )
            ON CONFLICT (commodity_id, owner_user_id, location, quality)
            DO UPDATE SET
                quantity = warehouse_material_inventory.quantity + EXCLUDED.quantity,
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

    public async Task<MaterialRowDto> UpdateQuantityAsync(Guid id, decimal quantity, CancellationToken ct)
    {
        var entry = await db.WarehouseMaterialInventory.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (entry is null)
            throw new MaterialRowNotFoundException(id);

        entry.Quantity = quantity;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return await LoadRowDtoAsync(id, ct);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct)
    {
        var entry = await db.WarehouseMaterialInventory.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (entry is null)
            throw new MaterialRowNotFoundException(id);

        db.WarehouseMaterialInventory.Remove(entry);
        await db.SaveChangesAsync(ct);
    }

    private async Task<MaterialRowDto> LoadRowDtoAsync(Guid id, CancellationToken ct)
    {
        var row = await db.Database.SqlQuery<MaterialRow>($"""
            SELECT
              w.id                  AS id,
              w.commodity_id        AS commodity_id,
              c.name                AS material_name,
              c.code                AS material_code,
              w.quantity            AS quantity,
              w.quality             AS quality,
              w.owner_user_id       AS owner_user_id,
              u.display_name        AS owner_display_name,
              w.location            AS location
            FROM warehouse_material_inventory w
            JOIN sc.commodities c ON c.id = w.commodity_id
            JOIN "AspNetUsers" u  ON u.id = w.owner_user_id
            WHERE w.id = {id}
            """).FirstAsync(ct);

        return new MaterialRowDto(
            row.Id, row.CommodityId, row.MaterialName, row.MaterialCode,
            row.Quantity, row.Quality, row.OwnerUserId, row.OwnerDisplayName, row.Location);
    }
}
