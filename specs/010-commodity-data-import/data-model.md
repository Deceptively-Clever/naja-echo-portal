# Phase 1 Data Model: Commodity Data Import

## Entity: Commodity

Local catalog row for a single UEX commodity. Stored in **`sc.commodities`** (one new table, one EF
migration). Identity is the UEX `id` (`uex_id`). The full source object is retained verbatim in
`raw_data`. Mirrors the `Ship` shape, extended with commodity-specific promoted columns.

### Domain entity (`NajaEcho.Domain/Commodities/Commodity.cs`)

| Property | CLR type | Source field | Notes |
|---|---|---|---|
| `Id` | `Guid` | — | Local surrogate PK. |
| `UexId` | `int` | `id` | **Durable identity.** Unique index. Required. |
| `Uuid` | `string?` | `uuid` | Stored when present; **nullable** (record still imported when null). |
| `Name` | `string` | `name` | Required (record skipped if missing). |
| `Code` | `string?` | `code` | |
| `Slug` | `string?` | `slug` | |
| `Kind` | `string?` | `kind` | |
| `WeightScu` | `int?` | `weight_scu` | |
| `IdParent` | `int?` | `id_parent` | No FK / no existence check. |
| `IdItem` | `int?` | `id_item` | No FK / no existence check. |
| `Wiki` | `string?` | `wiki` | |
| **Location — raw** | | | |
| `IdsStarSystemsRaw` | `string?` | `ids_star_systems` | Raw comma-separated string. |
| `IdsPlanetsRaw` | `string?` | `ids_planets` | Raw comma-separated string. |
| `IdsMoonsRaw` | `string?` | `ids_moons` | Raw comma-separated string. |
| `IdsPoiRaw` | `string?` | `ids_poi` | Raw comma-separated string. |
| `IdsOrbitsRaw` | `string?` | `ids_orbits` | Raw comma-separated string. |
| **Location — parsed** | | | |
| `IdsStarSystems` | `int[]` | parsed `ids_star_systems` | `integer[]`; empty array when source null/empty. |
| `IdsPlanets` | `int[]` | parsed `ids_planets` | `integer[]`. |
| `IdsMoons` | `int[]` | parsed `ids_moons` | `integer[]`. |
| `IdsPoi` | `int[]` | parsed `ids_poi` | `integer[]`. |
| `IdsOrbits` | `int[]` | parsed `ids_orbits` | `integer[]`. |
| **Flags (all `bool`, non-null)** | | | normalized via `Number != 0` |
| `IsAvailable` | `bool` | `is_available` | |
| `IsAvailableLive` | `bool` | `is_available_live` | |
| `IsVisible` | `bool` | `is_visible` | |
| `IsExtractable` | `bool` | `is_extractable` | |
| `IsMineral` | `bool` | `is_mineral` | |
| `IsRaw` | `bool` | `is_raw` | |
| `IsPure` | `bool` | `is_pure` | |
| `IsRefined` | `bool` | `is_refined` | |
| `IsRefinable` | `bool` | `is_refinable` | |
| `IsHarvestable` | `bool` | `is_harvestable` | |
| `IsBuyable` | `bool` | `is_buyable` | |
| `IsSellable` | `bool` | `is_sellable` | |
| `IsTemporary` | `bool` | `is_temporary` | |
| `IsIllegal` | `bool` | `is_illegal` | |
| `IsVolatileQt` | `bool` | `is_volatile_qt` | |
| `IsVolatileTime` | `bool` | `is_volatile_time` | |
| `IsInert` | `bool` | `is_inert` | |
| `IsExplosive` | `bool` | `is_explosive` | |
| `IsBuggy` | `bool` | `is_buggy` | |
| `IsFuel` | `bool` | `is_fuel` | |
| **Timestamps** | | | |
| `SourceDateAdded` | `long?` | `date_added` | Raw Unix seconds. |
| `SourceDateModified` | `long?` | `date_modified` | Raw Unix seconds. |
| `SourceDateAddedUtc` | `DateTimeOffset?` | converted `date_added` | UTC; null if raw invalid. |
| `SourceDateModifiedUtc` | `DateTimeOffset?` | converted `date_modified` | UTC; null if raw invalid. |
| **Lifecycle / audit** | | | |
| `Status` | `CommodityStatus` | — | `Active` \| `SoftDeleted`; stored as string. |
| `RawData` | `JsonDocument` | full record | `jsonb`; verbatim source incl. excluded price fields. |
| `ImportedAt` | `DateTimeOffset` | — | Set on insert. |
| `UpdatedAt` | `DateTimeOffset` | — | Set on every upsert touch. |
| `SoftDeletedAt` | `DateTimeOffset?` | — | Set on soft-delete; cleared on restore. |

> **Excluded columns (FR-014)**: `price_buy`, `price_sell` are intentionally NOT promoted; they
> remain only inside `raw_data`.

### Status enum (`NajaEcho.Domain/Commodities/CommodityStatus.cs`)

```text
CommodityStatus { Active, SoftDeleted }
```

Mirrors `ShipStatus`; persisted via `HasConversion<string>()`.

## EF Configuration (`CommodityConfiguration.cs`)

- `ToTable("commodities", schema: "sc")`, PK `Id`.
- Snake-case column names (global convention) — explicit `HasColumnName` per the ship/item precedent.
- `Status` → `HasConversion<string>()`, required.
- `RawData` → `HasColumnType("jsonb")`, required.
- Parsed `int[]` properties map to PostgreSQL `integer[]` (Npgsql default), required (empty array
  default).
- Indexes:
  - `ix_commodities_uex_id` — **unique** on `UexId`.
  - `ix_commodities_status` — on `Status`.

## Validation rules (applied during mapping, before upsert)

| Rule | Source | Behaviour |
|---|---|---|
| `id` present & numeric | FR-005 | Missing/invalid → **skip + count** (`skipped`). |
| `name` present & non-empty | FR-005 | Missing/empty → **skip + count** (`skipped`). |
| `uuid` may be null | FR-010 | Null/absent → import normally. |
| integer flags → bool | FR-009 | `Number != 0`; missing → `false`. |
| location strings → `int[]` | FR-012 | Split on `,`, trim, discard non-numeric; null/empty → `[]`. |
| timestamps → UTC | FR-013 | Unix-seconds; invalid → keep raw, null converted. |
| price fields | FR-014 | Never promoted; remain in `raw_data` only. |
| relationships | FR-011 | `id_parent`/`id_item` stored as-is; no integrity check. |

## State transitions

```text
(absent) --import, valid record-->            Active
Active   --import, record absent from feed--> SoftDeleted   (SoftDeletedAt set)
SoftDeleted --import, record reappears-->     Active        (SoftDeletedAt cleared, fields updated)
```

Identical to the ship lifecycle, applied globally across the full feed.

## Upsert algorithm (`CommodityRepository.BulkUpsertAsync`)

Mirrors `ShipRepository.BulkUpsertAsync` (global scope):

1. Open transaction. Build `incomingByUexId`; `incomingUexIds` set.
2. Load existing rows where `uex_id ∈ incomingUexIds`.
3. For each incoming record:
   - match found → update promoted columns + `raw_data` + `UpdatedAt`; if `SoftDeleted` → set
     `Active`, clear `SoftDeletedAt`, count **restored**; else count **updated**.
   - no match → new `Guid` Id, `Active`, set `ImportedAt`/`UpdatedAt`, add, count **inserted**.
4. Load Active rows where `uex_id ∉ incomingUexIds`; set `SoftDeleted` + `SoftDeletedAt` +
   `UpdatedAt`; count **softDeleted**.
5. `SaveChanges` + commit. Return `(Inserted, Updated, Restored, SoftDeleted)`.

Empty feed (after the handler's zero-record guard, if reached) soft-deletes all Active rows.

## Import summary DTO (`ImportCommoditiesResult`, transient — not persisted)

| Field | Meaning |
|---|---|
| `Fetched` | Records returned by the source. |
| `Skipped` | Records skipped for missing `id`/`name`. |
| `Inserted` | New rows. |
| `Updated` | Existing Active rows updated. |
| `Restored` | Previously soft-deleted rows reactivated. |
| `SoftDeleted` | Active rows absent from the feed, now soft-deleted. |
| `StartedAt` / `CompletedAt` / `DurationMs` | Timing. |
| `Warning` | Optional message (e.g., zero-record feed). |
