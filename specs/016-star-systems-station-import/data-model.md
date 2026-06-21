# Phase 1 Data Model: Star Systems & Space Station Import

Two new catalog entities plus an additive nullable FK on two existing warehouse entities. All new tables
live in the `sc` schema and mirror the existing `sc.ships` catalog conventions (Guid PK, unique `uex_id`,
string `status`, `jsonb` raw_data, import/update/soft-delete timestamps). Column names are snake_case.

## Entity: StarSystem  (table `sc.star_systems`)

| Field | Type | Column | Notes |
|-------|------|--------|-------|
| Id | Guid | `id` | PK |
| UexId | int | `uex_id` | UEX `id`; **unique** index `ix_star_systems_uex_id` |
| Name | string | `name` | UEX `name`; max 256, required |
| Code | string? | `code` | UEX `code`; max 32 |
| IsAvailable | bool | `is_available` | UEX `is_available` (0/1) |
| IsVisible | bool | `is_visible` | UEX `is_visible` (0/1) |
| Status | enum→string | `status` | `Active` \| `SoftDeleted`; index `ix_star_systems_status` |
| RawData | JsonDocument | `raw_data` | `jsonb`, full source record, required |
| ImportedAt | DateTimeOffset | `imported_at` | required |
| UpdatedAt | DateTimeOffset | `updated_at` | required |
| SoftDeletedAt | DateTimeOffset? | `soft_deleted_at` | set when soft-deleted |

**Validation / rules**
- `uex_id` unique; upsert keyed on it (last record wins on duplicate, per `ShipRepository`).
- Soft-delete (not hard-delete) when absent from the current source (FR-003).
- Reactivate (clear `soft_deleted_at`, `status=Active`) when a previously soft-deleted `uex_id` reappears.

## Entity: SpaceStation  (table `sc.space_stations`)

| Field | Type | Column | Notes |
|-------|------|--------|-------|
| Id | Guid | `id` | PK |
| UexId | int | `uex_id` | UEX `id`; **unique** index `ix_space_stations_uex_id` |
| StarSystemId | Guid | `star_system_id` | **FK** → `sc.star_systems.id`, `OnDelete.Restrict`; index |
| Name | string | `name` | UEX `name`; max 256, required (combobox label, FR-009) |
| Nickname | string? | `nickname` | UEX `nickname`; max 256 |
| IsAvailable | bool | `is_available` | UEX `is_available` |
| IsDecommissioned | bool | `is_decommissioned` | UEX `is_decommissioned` |
| IsLandable | bool | `is_landable` | UEX `is_landable` |
| HasRefinery | bool | `has_refinery` | UEX `has_refinery` |
| HasTradeTerminal | bool | `has_trade_terminal` | UEX `has_trade_terminal` (spec's "trading post") |
| Status | enum→string | `status` | `Active` \| `SoftDeleted`; index `ix_space_stations_status` |
| RawData | JsonDocument | `raw_data` | `jsonb`, full source record incl. all other flags/ids, required |
| ImportedAt | DateTimeOffset | `imported_at` | required |
| UpdatedAt | DateTimeOffset | `updated_at` | required |
| SoftDeletedAt | DateTimeOffset? | `soft_deleted_at` | set when soft-deleted |

**Validation / rules**
- `uex_id` unique; upsert keyed on it.
- `star_system_id` resolved from UEX `id_star_system` via the post-upsert star-system `uex_id → Guid` map.
  If unresolved, the record is **skipped + counted** (FR-013) — never inserted with a null parent.
- List query for the combobox returns only `is_available = true AND is_decommissioned = false` (FR-007),
  ordered by `name`, filtered by an optional case-insensitive `name` contains, limited (default 25, ≤100).
- Soft-delete / reactivate semantics identical to StarSystem.
- Capability columns beyond the two promoted ones remain available in `raw_data` (out-of-scope filtering).

**Index note**: a covering/filtered index on `(is_available, is_decommissioned, name)` supports the list
query; exact form decided in implementation.

## Modified Entity: WarehouseInventoryEntry  (table `warehouse_inventory`)

Backs **both** item inventory and ship components.

| Field | Type | Column | Notes |
|-------|------|--------|-------|
| … existing fields … | | | unchanged |
| Location | string | `location` | **retained**, deprecated (FR-010); still writable on add for backward compat |
| StationId | Guid? | `station_id` | **NEW** nullable **FK** → `sc.space_stations.id`, `OnDelete.Restrict`; canonical location |

## Modified Entity: WarehouseMaterialEntry  (table `warehouse_material_inventory`)

| Field | Type | Column | Notes |
|-------|------|--------|-------|
| … existing fields … | | | unchanged |
| Location | string | `location` | **retained**, deprecated |
| StationId | Guid? | `station_id` | **NEW** nullable **FK** → `sc.space_stations.id`, `OnDelete.Restrict` |

**Rules (both warehouse entities)**
- `station_id` nullable; an entry may have a station, a free-text location, both, or (legacy) only the
  free-text location (SC-007, edge cases).
- Add-entry: if `stationId` is provided it MUST reference an existing station (else validation error).
- Transfer: updates **only** `station_id`; `location` is never modified (FR-011, assumption).
- A referenced station is never hard-deleted (soft-delete only) + `OnDelete.Restrict`, so the FK never
  dangles; a soft-deleted station's name still resolves via join (edge case).

## State transitions (catalog entities)

```
(absent)        --import sees new uex_id-->        Active
Active          --import: uex_id absent from src--> SoftDeleted (soft_deleted_at set)
SoftDeleted     --import: uex_id reappears-->       Active (soft_deleted_at cleared)   [reactivated count]
Active          --import: uex_id still present-->   Active (promoted cols + raw_data refreshed) [updated count]
```

## Migration

Single forward-only, **non-destructive** migration `AddStarSystemsAndStationCatalog`:
create `sc.star_systems` and `sc.space_stations` (with indexes + FK); `ALTER TABLE warehouse_inventory ADD
station_id` and `ALTER TABLE warehouse_material_inventory ADD station_id` (nullable, with FK). No column
drops, type changes, or data loss → no destructive-migration approval required.
