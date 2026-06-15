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
    private const string SystemsSection = "Systems";

    // ── List ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ShipComponentRowDto>> GetShipComponentsAsync(
        GetShipComponentsQuery query, CancellationToken ct)
    {
        var namePattern = string.IsNullOrWhiteSpace(query.Name) ? null : $"%{query.Name.Trim()}%";

        var rows = await db.Database.SqlQuery<ScRow>($"""
            SELECT
              w.id                        AS id,
              w.item_id                   AS item_id,
              i.name                      AS name,
              i.category                  AS type,
              sca.class                   AS class,
              sca.size                    AS size,
              sca.grade                   AS grade,
              w.quantity                  AS quantity,
              w.owner_user_id             AS owner_user_id,
              u.display_name              AS owner_display_name,
              w.location                  AS location
            FROM warehouse_inventory w
            JOIN sc.items i              ON i.id = w.item_id
            LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
            JOIN "AspNetUsers" u         ON u.id = w.owner_user_id
            WHERE LOWER(i.section) = {SystemsSection.ToLower()}
              AND ({namePattern}::text IS NULL OR i.name ILIKE {namePattern})
            ORDER BY i.name, i.category, sca.size NULLS LAST, sca.class NULLS LAST, sca.grade NULLS LAST
            """).ToListAsync(ct);

        var result = rows.AsEnumerable();

        if (query.Types is { Count: > 0 })
            result = result.Where(r => query.Types.Contains(r.Type, StringComparer.OrdinalIgnoreCase));

        if (query.Classes is { Count: > 0 } || query.UnknownClass)
        {
            result = result.Where(r =>
                (query.UnknownClass && r.Class is null) ||
                (query.Classes is { Count: > 0 } && query.Classes.Contains(r.Class ?? "", StringComparer.OrdinalIgnoreCase)));
        }

        if (query.Sizes is { Count: > 0 } || query.UnknownSize)
        {
            result = result.Where(r =>
                (query.UnknownSize && r.Size is null) ||
                (query.Sizes is { Count: > 0 } && r.Size.HasValue && query.Sizes.Contains(r.Size.Value)));
        }

        if (query.Grades is { Count: > 0 } || query.UnknownGrade)
        {
            result = result.Where(r =>
                (query.UnknownGrade && r.Grade is null) ||
                (query.Grades is { Count: > 0 } && query.Grades.Contains(r.Grade ?? "", StringComparer.OrdinalIgnoreCase)));
        }

        if (query.OwnerUserIds is { Count: > 0 })
            result = result.Where(r => query.OwnerUserIds.Contains(r.OwnerUserId));

        if (query.Locations is { Count: > 0 })
            result = result.Where(r => query.Locations.Contains(r.Location, StringComparer.OrdinalIgnoreCase));

        return result.Select(r => new ShipComponentRowDto(
            r.Id, r.ItemId, r.Name, r.Type, r.Class, r.Size, r.Grade,
            r.Quantity, r.OwnerUserId, r.OwnerDisplayName, r.Location)).ToList();
    }

    private sealed record ScRow(
        Guid Id, Guid ItemId, string Name, string? Type, string? Class, int? Size, string? Grade,
        int Quantity, Guid OwnerUserId, string OwnerDisplayName, string Location);

    // ── Filters ──────────────────────────────────────────────────────────

    public async Task<ShipComponentFiltersDto> GetShipComponentFiltersAsync(CancellationToken ct)
    {
        var types = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT i.category AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            WHERE LOWER(i.section) = {SystemsSection.ToLower()}
              AND i.category IS NOT NULL
            ORDER BY value
            """).ToListAsync(ct);

        var classes = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT sca.class AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
            WHERE LOWER(i.section) = {SystemsSection.ToLower()}
              AND sca.class IS NOT NULL
            ORDER BY value
            """).ToListAsync(ct);

        var sizes = await db.Database.SqlQuery<IntValue>($"""
            SELECT DISTINCT sca.size AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
            WHERE LOWER(i.section) = {SystemsSection.ToLower()}
              AND sca.size IS NOT NULL
            ORDER BY value
            """).ToListAsync(ct);

        var grades = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT sca.grade AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
            WHERE LOWER(i.section) = {SystemsSection.ToLower()}
              AND sca.grade IS NOT NULL
            ORDER BY value
            """).ToListAsync(ct);

        var owners = await db.Database.SqlQuery<OwnerRow>($"""
            SELECT DISTINCT u.id AS user_id, u.display_name AS display_name
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            JOIN "AspNetUsers" u ON u.id = w.owner_user_id
            WHERE LOWER(i.section) = {SystemsSection.ToLower()}
            ORDER BY u.display_name
            """).ToListAsync(ct);

        var locations = await db.Database.SqlQuery<StringValue>($"""
            SELECT DISTINCT w.location AS value
            FROM warehouse_inventory w
            JOIN sc.items i ON i.id = w.item_id
            WHERE LOWER(i.section) = {SystemsSection.ToLower()}
            ORDER BY value
            """).ToListAsync(ct);

        var unknownClass = await db.Database.SqlQuery<BoolValue>($"""
            SELECT EXISTS (
                SELECT 1
                FROM warehouse_inventory w
                JOIN sc.items i ON i.id = w.item_id
                LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
                WHERE LOWER(i.section) = {SystemsSection.ToLower()}
                  AND sca.class IS NULL
            ) AS value
            """).FirstAsync(ct);

        var unknownSize = await db.Database.SqlQuery<BoolValue>($"""
            SELECT EXISTS (
                SELECT 1
                FROM warehouse_inventory w
                JOIN sc.items i ON i.id = w.item_id
                LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
                WHERE LOWER(i.section) = {SystemsSection.ToLower()}
                  AND sca.size IS NULL
            ) AS value
            """).FirstAsync(ct);

        var unknownGrade = await db.Database.SqlQuery<BoolValue>($"""
            SELECT EXISTS (
                SELECT 1
                FROM warehouse_inventory w
                JOIN sc.items i ON i.id = w.item_id
                LEFT JOIN sc.ship_component_attributes sca ON sca.item_id = i.id
                WHERE LOWER(i.section) = {SystemsSection.ToLower()}
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
            WHERE LOWER(i.section) = {SystemsSection.ToLower()}
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
        foreach (var attr in attributes)
        {
            var existing = await db.ItemAttributes
                .FirstOrDefaultAsync(a => a.ItemId == attr.ItemId && a.UexCategoryAttributeId == attr.UexCategoryAttributeId, ct);

            if (existing is not null)
            {
                existing.Value = attr.Value;
                existing.Unit = attr.Unit;
                existing.SourceDateModified = attr.SourceDateModified;
                existing.FetchedAt = attr.FetchedAt;
            }
            else
            {
                db.ItemAttributes.Add(attr);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task UpsertShipComponentAttributesAsync(Guid itemId, DateTimeOffset fetchedAt, CancellationToken ct)
    {
        var rawAttrs = await db.ItemAttributes
            .Where(a => a.ItemId == itemId)
            .ToListAsync(ct);

        var classAttr = rawAttrs.FirstOrDefault(a => string.Equals(a.AttributeName.Trim(), "Class", StringComparison.OrdinalIgnoreCase));
        var sizeAttr = rawAttrs.FirstOrDefault(a => string.Equals(a.AttributeName.Trim(), "Size", StringComparison.OrdinalIgnoreCase));
        var gradeAttr = rawAttrs.FirstOrDefault(a => string.Equals(a.AttributeName.Trim(), "Grade", StringComparison.OrdinalIgnoreCase));

        int? size = null;
        if (sizeAttr?.Value is not null && int.TryParse(sizeAttr.Value.Trim(), out var parsedSize))
            size = parsedSize;

        var existing = await db.ShipComponentAttributes.FirstOrDefaultAsync(s => s.ItemId == itemId, ct);
        if (existing is not null)
        {
            existing.Class = classAttr?.Value;
            existing.Size = size;
            existing.Grade = gradeAttr?.Value;
            existing.AttributesFetchedAt = fetchedAt;
        }
        else
        {
            db.ShipComponentAttributes.Add(new ShipComponentAttributes
            {
                ItemId = itemId,
                Class = classAttr?.Value,
                Size = size,
                Grade = gradeAttr?.Value,
                AttributesFetchedAt = fetchedAt,
            });
        }
        await db.SaveChangesAsync(ct);
    }
}
