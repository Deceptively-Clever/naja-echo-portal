# Data Model: Add Cities Import & Rename Stations to Locations

Feature **018-add-cities-locations** | Date 2026-06-22

One new catalog entity (`City`) and a polymorphic location reference on the two warehouse entry entities.
A single forward‑only migration, `AddCitiesAndPolymorphicLocation`, applies all schema changes.

## New entity: `City`

`NajaEcho.Domain/Locations/City.cs` — peer of `SpaceStation`, persisted to **`sc.cities`**.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `Guid` (PK) | App‑generated. |
| `UexId` | `int` | UEX `id`. Unique. |
| `StarSystemId` | `Guid` (FK → `sc.star_systems`) | Resolved from UEX `id_star_system` via `starSystemMap`; FK `Restrict`. |
| `Name` | `string` (≤256, required) | UEX `name`. |
| `Code` | `string?` (≤32) | UEX `code` (e.g. `AR18`). |
| `IsAvailable` | `bool` | UEX `is_available == 1`. Active filter. |
| `IsAvailableLive` | `bool` | UEX `is_available_live == 1`. **Stored, not filtered** (FR‑008). |
| `IsVisible` | `bool` | UEX `is_visible == 1`. Active filter. |
| `Status` | `string` (≤32) | `CatalogStatus.Active` / `SoftDeleted` (reused). |
| `RawData` | `JsonDocument` (`jsonb`, required) | Full UEX record; retains `has_*` capability flags for future use. |
| `ImportedAt` / `UpdatedAt` | `DateTimeOffset` | |
| `SoftDeletedAt` | `DateTimeOffset?` | Set when soft‑deleted. |

**Indexes** (`CityConfiguration`, mirroring `SpaceStationConfiguration`):
`ix_cities_uex_id` (unique), `ix_cities_status`, `ix_cities_star_system_id`,
`ix_cities_avail_visible_name` on `(is_available, is_visible, name)`.

**Active filter** (used by `SearchActiveCitiesAsync` and the locations endpoint):
`Status = Active AND IsAvailable AND IsVisible`. (Stations remain `Status = Active AND IsAvailable AND NOT IsDecommissioned`.)

**Validation / import rules**:
- Skip + count when UEX `id_star_system` is null/missing **or** absent from `starSystemMap` (FR‑006).
- Soft‑delete active cities missing from the current source (FR‑003).
- Reactivate a soft‑deleted city that reappears (sets `Status=Active`, clears `SoftDeletedAt`).
- An empty cities source aborts the whole import with no commit (FR‑005) — `EmptySourceException("cities")`.

## Changed entities: `WarehouseInventoryEntry`, `WarehouseMaterialEntry`

Both drop the feature‑016 station FK and gain the polymorphic pair.

| Field | Before (feat 016) | After (this feature) |
|-------|-------------------|----------------------|
| location FK | `Guid? StationId` (`station_id`, FK→`space_stations`, `Restrict`) | **removed** |
| nav | `SpaceStation? Station` | **removed** |
| polymorphic id | — | `Guid? LocationId` (`location_id`, **no FK**) |
| discriminator | — | `string? LocationType` (`location_type`, `"Station"`/`"City"`) |
| free‑text | `string Location` (`location`, ≤200) | **unchanged** (display/uniqueness fallback) |

**Constraints / indexes** (both tables):
- `CHECK (location_type IS NULL OR location_type IN ('Station','City'))` — discriminator guard.
- Invariant (validators): `LocationId` and `LocationType` are both null or both set.
- New `ix_<table>_location` on `(location_id, location_type)`.
- Existing `ux_warehouse_inventory_item_owner_location` (and material twin) on `(…, location)` — **unchanged**.

> `ShipComponent` warehouse rows reuse `WarehouseInventoryEntry` (item‑backed); no separate entity change.

## Discriminator values

`LocationType ∈ { "Station", "City" }` in this version. New location types in future add only a new
discriminator value (and a join arm in the read query) — **no new column** (FR‑014).

## Migration: `AddCitiesAndPolymorphicLocation` (forward‑only, destructive — needs PR approval)

Ordered steps (applied to **both** `warehouse_inventory` and `warehouse_material`):

1. **Create** `sc.cities` with the columns and indexes above.
2. **Add** `location_id uuid NULL` and `location_type text NULL`.
3. **Copy** existing data — non‑lossy (FR‑015, SC‑008):
   ```sql
   UPDATE warehouse_inventory
      SET location_id = station_id, location_type = 'Station'
    WHERE station_id IS NOT NULL;
   ```
4. **Drop** the `fk_warehouse_inventory_station_id` constraint and the `station_id` column.
5. **Add** `CHECK (location_type IS NULL OR location_type IN ('Station','City'))` and the
   `(location_id, location_type)` index.

Down migration recreates `station_id` (+ FK) and copies back rows where `location_type = 'Station'`
(city‑typed rows cannot be represented in the old schema and are documented as data‑lossy on rollback —
hence forward‑only is the supported direction).

## State transitions (catalog status — reused from feature 016)

```
(absent) --import--> Active
Active   --missing from source--> SoftDeleted   (SoftDeletedAt set)
SoftDeleted --reappears in source--> Active       (SoftDeletedAt cleared)
```

Identical for `City` and `SpaceStation`. A soft‑deleted catalog row referenced by a warehouse entry does
**not** cascade — the entry keeps `location_id`/`location_type` and displays the last‑known name via the
read‑path `COALESCE` (Edge Cases).
</content>
