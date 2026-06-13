using Microsoft.EntityFrameworkCore;
using NajaEcho.Application.Abstractions;
using NajaEcho.Application.Features.Hangar;
using NajaEcho.Application.Features.Hangar.AddShipToHangar;
using NajaEcho.Application.Features.Hangar.ImportHangar;
using NajaEcho.Application.Features.Hangar.GetMyHangar;
using NajaEcho.Application.Features.Hangar.GetOrgHangar;
using NajaEcho.Application.Features.Hangar.GetOwningMembers;
using NajaEcho.Application.Features.Hangar.SearchCatalogShips;
using NajaEcho.Domain.Hangar;
using NajaEcho.Domain.Ships;
using NajaEcho.Infrastructure.Persistence;

namespace NajaEcho.Infrastructure.Hangar;

public sealed class HangarRepository(AppDbContext db) : IHangarRepository
{
    // Projection rows (private — only used by raw SQL queries)
    private sealed record ShipCardRow(
        Guid ShipId, string Name, string? CompanyName, string? UrlPhoto, decimal? Scu, string? Crew);

    private sealed record OrgShipCardRow(
        Guid ShipId, string Name, string? CompanyName, string? UrlPhoto, decimal? Scu, string? Crew,
        int OwnerCount, string OwnersJson);

    private sealed record OwningMemberRow(Guid UserId, string DisplayName);

    // Guard against crafted query strings: a negative/zero page yields a negative OFFSET
    // (Postgres rejects it) and a non-positive pageSize yields an empty FETCH.
    private const int MaxPageSize = 100;
    private static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));

    // ──────────────────────────────────────────────────────────────
    // My Hangar
    // ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<ShipCard>> GetMyHangarAsync(
        Guid userId, string? search, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%";

        var countQuery = db.Database.SqlQuery<int>($"""
            SELECT COUNT(*)::int AS "Value"
            FROM hangar_entries he
            JOIN sc.ships s ON s.id = he.ship_id
            WHERE he.user_id = {userId}
              AND s.status = {ShipStatus.Active.ToString()}
              AND ({searchPattern}::text IS NULL OR s.name ILIKE {searchPattern})
            """);
        var totalCount = await countQuery.FirstAsync(ct);

        var offset = (page - 1) * pageSize;
        var rows = await db.Database.SqlQuery<ShipCardRow>($"""
            SELECT
              s.id                                       AS ship_id,
              s.name                                     AS name,
              s.company_name                             AS company_name,
              NULLIF(TRIM(s.raw_data->>'url_photo'), '') AS url_photo,
              CASE WHEN TRIM(s.raw_data->>'scu') ~ '^-?[0-9]+(\.[0-9]+)?$'
                   THEN TRIM(s.raw_data->>'scu')::numeric END AS scu,
              NULLIF(TRIM(s.raw_data->>'crew'), '')      AS crew
            FROM hangar_entries he
            JOIN sc.ships s ON s.id = he.ship_id
            WHERE he.user_id = {userId}
              AND s.status = {ShipStatus.Active.ToString()}
              AND ({searchPattern}::text IS NULL OR s.name ILIKE {searchPattern})
            ORDER BY s.name
            OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
            """).ToListAsync(ct);

        var items = rows.Select(r => new ShipCard(r.ShipId, r.Name, r.CompanyName, r.UrlPhoto, r.Scu, r.Crew))
                        .ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(pageSize, 1));
        return new PagedResult<ShipCard>(items, page, pageSize, totalCount, totalPages);
    }

    // ──────────────────────────────────────────────────────────────
    // Org Hangar
    // ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<OrgShipCard>> GetOrgHangarAsync(
        Guid currentUserId, string? search, bool mine, Guid? memberId, int page, int pageSize, string sortBy, CancellationToken ct)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%";
        var filterUserId = memberId ?? (mine ? currentUserId : (Guid?)null);

        var countQuery = db.Database.SqlQuery<int>($"""
            SELECT COUNT(DISTINCT he.ship_id)::int AS "Value"
            FROM hangar_entries he
            JOIN sc.ships s ON s.id = he.ship_id
            WHERE s.status = {ShipStatus.Active.ToString()}
              AND ({searchPattern}::text IS NULL OR s.name ILIKE {searchPattern})
              AND ({filterUserId}::uuid IS NULL OR he.user_id = {filterUserId})
            """);
        var totalCount = await countQuery.FirstAsync(ct);

        var offset = (page - 1) * pageSize;
        var rows = await (sortBy == "name"
            ? db.Database.SqlQuery<OrgShipCardRow>($"""
                SELECT
                  s.id                                       AS ship_id,
                  s.name                                     AS name,
                  s.company_name                             AS company_name,
                  NULLIF(TRIM(s.raw_data->>'url_photo'), '') AS url_photo,
                  CASE WHEN TRIM(s.raw_data->>'scu') ~ '^-?[0-9]+(\.[0-9]+)?$'
                       THEN TRIM(s.raw_data->>'scu')::numeric END AS scu,
                  NULLIF(TRIM(s.raw_data->>'crew'), '')      AS crew,
                  COUNT(DISTINCT he.user_id)::int            AS owner_count,
                  json_agg(json_build_object('UserId', u.id, 'DisplayName', u.display_name)
                           ORDER BY u.display_name)::text   AS owners_json
                FROM hangar_entries he
                JOIN sc.ships s   ON s.id = he.ship_id
                JOIN "AspNetUsers" u ON u.id = he.user_id
                WHERE s.status = {ShipStatus.Active.ToString()}
                  AND ({searchPattern}::text IS NULL OR s.name ILIKE {searchPattern})
                  AND ({filterUserId}::uuid IS NULL OR he.user_id = {filterUserId})
                GROUP BY s.id, s.name, s.company_name, s.raw_data
                ORDER BY s.name
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                """)
            : db.Database.SqlQuery<OrgShipCardRow>($"""
                SELECT
                  s.id                                       AS ship_id,
                  s.name                                     AS name,
                  s.company_name                             AS company_name,
                  NULLIF(TRIM(s.raw_data->>'url_photo'), '') AS url_photo,
                  CASE WHEN TRIM(s.raw_data->>'scu') ~ '^-?[0-9]+(\.[0-9]+)?$'
                       THEN TRIM(s.raw_data->>'scu')::numeric END AS scu,
                  NULLIF(TRIM(s.raw_data->>'crew'), '')      AS crew,
                  COUNT(DISTINCT he.user_id)::int            AS owner_count,
                  json_agg(json_build_object('UserId', u.id, 'DisplayName', u.display_name)
                           ORDER BY u.display_name)::text   AS owners_json
                FROM hangar_entries he
                JOIN sc.ships s   ON s.id = he.ship_id
                JOIN "AspNetUsers" u ON u.id = he.user_id
                WHERE s.status = {ShipStatus.Active.ToString()}
                  AND ({searchPattern}::text IS NULL OR s.name ILIKE {searchPattern})
                  AND ({filterUserId}::uuid IS NULL OR he.user_id = {filterUserId})
                GROUP BY s.id, s.name, s.company_name, s.raw_data
                ORDER BY owner_count DESC, s.name
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                """)).ToListAsync(ct);

        var items = rows.Select(r =>
        {
            var owners = System.Text.Json.JsonSerializer.Deserialize<List<OwnerJson>>(r.OwnersJson)
                         ?? [];
            return new OrgShipCard(
                r.ShipId, r.Name, r.CompanyName, r.UrlPhoto, r.Scu, r.Crew,
                r.OwnerCount,
                owners.Select(o => new HangarOwner(o.UserId, o.DisplayName)).ToList());
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(pageSize, 1));
        return new PagedResult<OrgShipCard>(items, page, pageSize, totalCount, totalPages);
    }

    private sealed record OwnerJson(Guid UserId, string DisplayName);

    // ──────────────────────────────────────────────────────────────
    // Owning Members
    // ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<OwningMember>> GetOwningMembersAsync(CancellationToken ct)
    {
        var rows = await db.Database.SqlQuery<OwningMemberRow>($"""
            SELECT DISTINCT u.id AS user_id, u.display_name AS display_name
            FROM hangar_entries he
            JOIN "AspNetUsers" u ON u.id = he.user_id
            ORDER BY u.display_name
            """).ToListAsync(ct);

        return rows.Select(r => new OwningMember(r.UserId, r.DisplayName)).ToList();
    }

    // ──────────────────────────────────────────────────────────────
    // Catalog Search
    // ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<CatalogSearchRow>> SearchCatalogAsync(
        Guid userId, string? search, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%";

        var countQuery = db.Database.SqlQuery<int>($"""
            SELECT COUNT(*)::int AS "Value"
            FROM sc.ships s
            WHERE s.status = {ShipStatus.Active.ToString()}
              AND ({searchPattern}::text IS NULL OR s.name ILIKE {searchPattern})
            """);
        var totalCount = await countQuery.FirstAsync(ct);

        var offset = (page - 1) * pageSize;

        // Use a sealed record to include alreadyOwned
        var rows = await db.Database.SqlQuery<CatalogSearchRaw>($"""
            SELECT
              s.id                                       AS ship_id,
              s.name                                     AS name,
              s.company_name                             AS company_name,
              NULLIF(TRIM(s.raw_data->>'url_photo'), '') AS url_photo,
              CASE WHEN TRIM(s.raw_data->>'scu') ~ '^-?[0-9]+(\.[0-9]+)?$'
                   THEN TRIM(s.raw_data->>'scu')::numeric END AS scu,
              NULLIF(TRIM(s.raw_data->>'crew'), '')      AS crew,
              EXISTS(
                SELECT 1 FROM hangar_entries he
                WHERE he.ship_id = s.id AND he.user_id = {userId}
              ) AS already_owned
            FROM sc.ships s
            WHERE s.status = {ShipStatus.Active.ToString()}
              AND ({searchPattern}::text IS NULL OR s.name ILIKE {searchPattern})
            ORDER BY s.name
            OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
            """).ToListAsync(ct);

        var items = rows.Select(r => new CatalogSearchRow(
            r.ShipId, r.Name, r.CompanyName, r.UrlPhoto, r.Scu, r.Crew, r.AlreadyOwned)).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)Math.Max(pageSize, 1));
        return new PagedResult<CatalogSearchRow>(items, page, pageSize, totalCount, totalPages);
    }

    private sealed record CatalogSearchRaw(
        Guid ShipId, string Name, string? CompanyName, string? UrlPhoto, decimal? Scu, string? Crew, bool AlreadyOwned);

    // ──────────────────────────────────────────────────────────────
    // Add / Remove
    // ──────────────────────────────────────────────────────────────

    public Task<bool> ExistsAsync(Guid userId, Guid shipId, CancellationToken ct) =>
        db.HangarEntries.AnyAsync(h => h.UserId == userId && h.ShipId == shipId, ct);

    public async Task<ShipCard> AddAsync(Guid userId, Guid shipId, CancellationToken ct)
    {
        var entry = new HangarEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShipId = shipId,
            AddedAt = DateTimeOffset.UtcNow,
        };
        db.HangarEntries.Add(entry);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ux_hangar_entries_user_ship") == true)
        {
            throw new ShipAlreadyOwnedException(shipId);
        }

        var rows = await db.Database.SqlQuery<ShipCardRow>($"""
            SELECT
              s.id                                       AS ship_id,
              s.name                                     AS name,
              s.company_name                             AS company_name,
              NULLIF(TRIM(s.raw_data->>'url_photo'), '') AS url_photo,
              CASE WHEN TRIM(s.raw_data->>'scu') ~ '^-?[0-9]+(\.[0-9]+)?$'
                   THEN TRIM(s.raw_data->>'scu')::numeric END AS scu,
              NULLIF(TRIM(s.raw_data->>'crew'), '')      AS crew
            FROM sc.ships s
            WHERE s.id = {shipId}
            """).FirstAsync(ct);

        return new ShipCard(rows.ShipId, rows.Name, rows.CompanyName, rows.UrlPhoto, rows.Scu, rows.Crew);
    }

    public async Task RemoveAsync(Guid userId, Guid shipId, CancellationToken ct)
    {
        var entry = await db.HangarEntries
            .FirstOrDefaultAsync(h => h.UserId == userId && h.ShipId == shipId, ct);
        if (entry is not null)
        {
            db.HangarEntries.Remove(entry);
            await db.SaveChangesAsync(ct);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Import
    // ──────────────────────────────────────────────────────────────

    public async Task ReplaceFromImportAsync(Guid userId, IReadOnlyList<Guid> shipIds, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.Database.ExecuteSqlAsync(
            $"DELETE FROM hangar_entries WHERE user_id = {userId}", ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var shipId in shipIds)
        {
            db.HangarEntries.Add(new HangarEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ShipId = shipId,
                AddedAt = now,
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private sealed record ShipNameRow(Guid Id, string Name);

    public async Task<Dictionary<string, Guid>> GetShipIdsByNamesAsync(
        IReadOnlyList<string> names, CancellationToken ct)
    {
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0) return result;

        // Build a temporary values list and join against ships
        // EF parameterized interpolation keeps this safe.
        var rows = await db.Database.SqlQuery<ShipNameRow>($"""
            SELECT s.id AS id, s.name AS name
            FROM sc.ships s
            WHERE s.status = {ShipStatus.Active.ToString()}
              AND s.name ILIKE ANY(
                SELECT unnest({names.ToArray()}::text[])
              )
            """).ToListAsync(ct);

        foreach (var row in rows)
            result[row.Name] = row.Id;

        return result;
    }
}
