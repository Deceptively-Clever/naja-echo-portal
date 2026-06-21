using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponentFilters;
using NajaEcho.Application.Features.Warehouse.ShipComponents.GetShipComponents;
using NajaEcho.Application.Features.Warehouse.ShipComponents.SearchSystemsCatalog;
using NajaEcho.Domain.Items;
using NajaEcho.Domain.Warehouse;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Warehouse;

public sealed class ShipComponentRepository(AppDbContext db) : IShipComponentRepository
{
    private const string SystemsSectionLower = "systems";
    private const string VehicleWeaponsSectionLower = "vehicle weapons";

    // ── List ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ShipComponentRowDto>> GetShipComponentsAsync(
        GetShipComponentsQuery query, CancellationToken ct)
    {
        var namePattern = string.IsNullOrWhiteSpace(query.Name) ? null : $"%{query.Name.Trim()}%";
        var types = NormalizeTextFilter(query.Types);
        var classes = NormalizeTextFilter(query.Classes);
        var sizes = NormalizeNumericFilter(query.Sizes);
        var grades = NormalizeTextFilter(query.Grades);
        var ownerUserIds = NormalizeNumericFilter(query.OwnerUserIds);
        var locations = NormalizeTextFilter(query.Locations);

        var applyClassFilter = classes is not null || query.UnknownClass;
        var applySizeFilter = sizes is not null || query.UnknownSize;
        var applyGradeFilter = grades is not null || query.UnknownGrade;

        var rows = await db.Database.SqlQuery<ScRow>($"""
            SELECT
              w.id                                    AS id,
              w.item_id                               AS item_id,
              i.name                                  AS name,
              i.category                              AS type,
              sca.class                               AS class,
              sca.size                                AS size,
              sca.grade                               AS grade,
              w.quantity                              AS quantity,
              w.quality                               AS quality,
              w.owner_user_id                         AS owner_user_id,
              u.display_name                          AS owner_display_name,
              COALESCE(ss.name, w.location)           AS location,
              w.station_id                            AS station_id
            FROM warehouse_inventory w
            JOIN sc.items i                  ON i.id = w.item_id
            LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
            JOIN "AspNetUsers" u             ON u.id = w.owner_user_id
            LEFT JOIN sc.space_stations ss   ON ss.id = w.station_id
            WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
              AND ({namePattern}::text IS NULL OR i.name ILIKE {namePattern})
              AND ({types}::text[] IS NULL OR i.category ILIKE ANY(SELECT unnest({types}::text[])))
              AND (
                    NOT {applyClassFilter}
                    OR ({query.UnknownClass} AND sca.class IS NULL)
                    OR ({classes}::text[] IS NOT NULL AND sca.class ILIKE ANY(SELECT unnest({classes}::text[])))
                  )
              AND (
                    NOT {applySizeFilter}
                    OR ({query.UnknownSize} AND sca.size IS NULL)
                    OR ({sizes}::integer[] IS NOT NULL AND sca.size = ANY({sizes}::integer[]))
                  )
              AND (
                    NOT {applyGradeFilter}
                    OR ({query.UnknownGrade} AND sca.grade IS NULL)
                    OR ({grades}::text[] IS NOT NULL AND sca.grade ILIKE ANY(SELECT unnest({grades}::text[])))
                  )
              AND ({ownerUserIds}::uuid[] IS NULL OR w.owner_user_id = ANY({ownerUserIds}::uuid[]))
              AND ({locations}::text[] IS NULL OR COALESCE(ss.name, w.location) ILIKE ANY(SELECT unnest({locations}::text[])))
            ORDER BY i.name, i.category, sca.size NULLS LAST, sca.class NULLS LAST, sca.grade NULLS LAST
            """).ToListAsync(ct);

        return rows.Select(r => new ShipComponentRowDto(
            r.Id, r.ItemId, r.Name, r.Type, r.Class, r.Size, r.Grade,
            r.Quantity, r.Quality, r.OwnerUserId, r.OwnerDisplayName, r.Location, r.StationId)).ToList();
    }

    private sealed record ScRow(
        Guid Id, Guid ItemId, string Name, string? Type, string? Class, int? Size, string? Grade,
        int Quantity, int Quality, Guid OwnerUserId, string OwnerDisplayName, string Location, Guid? StationId);

    // ── Filters ──────────────────────────────────────────────────────────

    public async Task<ShipComponentFiltersDto> GetShipComponentFiltersAsync(CancellationToken ct)
    {
        var types = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT i.category AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
              AND i.category IS NOT NULL
            ORDER BY value
            """).ToListAsync(ct);

        var classes = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT sca.class AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
            WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
              AND sca.class IS NOT NULL
            ORDER BY value
            """).ToListAsync(ct);

        var sizes = await db.Database.SqlQuery<IntValue>($"""
            SELECT DISTINCT sca.size AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
            WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
              AND sca.size IS NOT NULL
            ORDER BY value
            """).ToListAsync(ct);

        var grades = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT sca.grade AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
            WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
              AND sca.grade IS NOT NULL
            ORDER BY value
            """).ToListAsync(ct);

        var owners = await db.Database.SqlQuery<OwnerRow>($"""
            SELECT DISTINCT u.id AS user_id, u.display_name AS display_name
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            JOIN "AspNetUsers" u ON u.id = w.owner_user_id
            WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
            ORDER BY u.display_name
            """).ToListAsync(ct);

        var locations = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT w.location AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
            ORDER BY value
            """).ToListAsync(ct);

        var unknownClass = await db.Database.SqlQuery<BoolValue>($"""
            SELECT EXISTS (
                SELECT 1
                FROM warehouse_inventory w
                JOIN sc.items i ON i.id = w.item_id
                LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
                WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
                  AND sca.class IS NULL
            ) AS value
            """).FirstAsync(ct);

        var unknownSize = await db.Database.SqlQuery<BoolValue>($"""
            SELECT EXISTS (
                SELECT 1
                FROM warehouse_inventory w
                JOIN sc.items i ON i.id = w.item_id
                LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
                WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
                  AND sca.size IS NULL
            ) AS value
            """).FirstAsync(ct);

        var unknownGrade = await db.Database.SqlQuery<BoolValue>($"""
            SELECT EXISTS (
                SELECT 1
                FROM warehouse_inventory w
                JOIN sc.items i ON i.id = w.item_id
                LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
                WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
                  AND sca.grade IS NULL
            ) AS value
            """).FirstAsync(ct);

        return new ShipComponentFiltersDto(
            types.Select(t => t.Value).ToList(),
            classes.Select(c => c.Value).ToList(),
            sizes.Select(s => s.Value).ToList(),
            grades.Select(g => g.Value).ToList(),
            owners.Select(o => new OwnerFilterOption(o.UserId, o.DisplayName)).ToList(),
            locations.Select(l => l.Value).ToList(),
            unknownClass.Value,
            unknownSize.Value,
            unknownGrade.Value);
    }

    private sealed record StringValue(string Value);
    private sealed record IntValue(int Value);
    private sealed record BoolValue(bool Value);
    private sealed record OwnerRow(Guid UserId, string DisplayName);

    // ── Catalog Search (Systems + Active only) ────────────────────────────

    public async Task<IReadOnlyList<SystemsCatalogItemDto>> SearchSystemsCatalogAsync(
        string? search, int limit, CancellationToken ct)
    {
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";

        var rows = await db.Database.SqlQuery<CatalogRow>($"""
            SELECT
              i.id       AS item_id,
              i.name     AS name,
              i.category AS type
            FROM sc.items i
            WHERE LOWER(i.section) IN ({SystemsSectionLower}, {VehicleWeaponsSectionLower})
              AND i.status = {ItemStatus.Active.ToString()}
              AND ({searchPattern}::text IS NULL OR i.name ILIKE {searchPattern})
            ORDER BY i.name
            FETCH FIRST {limit} ROWS ONLY
            """).ToListAsync(ct);

        return rows.Select(r => new SystemsCatalogItemDto(r.ItemId, r.Name, r.Type)).ToList();
    }

    private sealed record CatalogRow(Guid ItemId, string Name, string? Type);

    // ── Attribute cache ───────────────────────────────────────────────────

    public async Task<bool> HasCachedAttributesAsync(Guid itemId, CancellationToken ct)
        => await db.ItemAttributes.AnyAsync(a => a.ItemId == itemId, ct);

    public async Task SaveItemAttributesAsync(IReadOnlyList<ItemAttribute> attributes, CancellationToken ct)
    {
        if (attributes.Count == 0)
        {
            return;
        }

        var itemIds = NormalizeNumericFilter(attributes.Select(a => a.ItemId).ToArray())!;
        var categoryAttributeIds = NormalizeNumericFilter(attributes.Select(a => a.UexCategoryAttributeId).ToArray())!;

        var existingAttributes = await db.ItemAttributes
            .Where(a => itemIds.Contains(a.ItemId) && categoryAttributeIds.Contains(a.UexCategoryAttributeId))
            .ToListAsync(ct);

        var existingByKey = existingAttributes.ToDictionary(a => (a.ItemId, a.UexCategoryAttributeId));

        foreach (var attr in attributes)
        {
            var key = (attr.ItemId, attr.UexCategoryAttributeId);
            if (existingByKey.TryGetValue(key, out var existing))
            {
                existing.Value = attr.Value;
                existing.Unit = attr.Unit;
                existing.SourceDateModified = attr.SourceDateModified;
                existing.FetchedAt = attr.FetchedAt;
            }
            else
            {
                db.ItemAttributes.Add(attr);
                existingByKey[key] = attr;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static string[]? NormalizeTextFilter(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        var normalized = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.Length == 0 ? null : normalized;
    }

    private static T[]? NormalizeNumericFilter<T>(IReadOnlyList<T>? values) where T : struct
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        var normalized = values
            .Distinct()
            .ToArray();

        return normalized.Length == 0 ? null : normalized;
    }
}
