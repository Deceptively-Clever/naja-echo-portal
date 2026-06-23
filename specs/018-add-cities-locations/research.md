# Research: Add Cities Import & Rename Stations to Locations

Feature **018-add-cities-locations** | Date 2026-06-22

All unknowns from the Technical Context are resolved below. Each decision records what was chosen, why,
and the alternatives rejected. The spec's Clarifications section already settled the product‑level
questions (polymorphic ref, city active filter, skip rule, migration strategy, flat combobox); this
document settles the **technical** how.

## Decision 1 — Fold cities into the existing import endpoint, not a new one

**Decision**: Add city fetch + upsert to the existing `ImportLocationsHandler` and
`POST /api/admin/locations/import`, producing a third `CityImportCounts` block in the response.

**Rationale**: Cities require the `id_star_system` → local `Guid` map to apply the skip rule (FR‑006).
That `starSystemMap` is already built mid‑`ImportLocationsHandler`
(`starSystemRepo.GetActiveUexIdToIdMapAsync`) for stations. Reusing it means one star‑systems fetch, one
import lock, one transaction boundary per source, and zero new abstractions — directly satisfying the
spec Assumption "the city import follows the same pipeline pattern… no new abstractions are introduced."
The response still carries a city‑specific summary (fetched/inserted/updated/soft‑deleted/skipped),
satisfying FR‑004 and User Story 1's "import summary… for cities."

**Alternatives considered**:
- *Separate `POST /api/admin/locations/cities/import`*: would re‑fetch star systems or duplicate the map
  build, add a second coordinator interaction, and split the admin UI into two buttons for no user
  benefit. Rejected (YAGNI, duplication).
- *Parallel `ImportCitiesHandler` reusing a shared map service*: introduces a new port and lifetime
  question for the map. Rejected — premature abstraction; the single handler stays small.

## Decision 2 — `City` entity shape and `sc.cities` columns

**Decision**: `City` mirrors `SpaceStation` but drops `IsDecommissioned`/`IsLandable`/`Nickname` and adds
`Code (string?)`, `IsAvailableLive (bool)`, `IsVisible (bool)`. Persisted columns: `id`, `uex_id`,
`star_system_id`, `name`, `code`, `is_available`, `is_available_live`, `is_visible`, `status`,
`raw_data (jsonb)`, `imported_at`, `updated_at`, `soft_deleted_at`. Indexes mirror the station table:
unique `uex_id`, `status`, `star_system_id`, and a covering `(is_available, is_visible, name)` for the
active search.

**Rationale**: The UEX `cities` payload (verified live) exposes `is_available`, `is_available_live`,
`is_visible` and a `code` field; the spec names exactly these three flags as the city status surface and
explicitly says cities have **no** decommission flag. Storing the full record in `raw_data (jsonb)`
preserves the dozens of `has_*` capability flags for future capability filtering without modelling them
now (YAGNI). The `(is_available, is_visible, name)` index matches the active‑search predicate.

**Alternatives considered**: modelling every `has_*` capability column now — rejected (out of scope per
spec Assumptions; `raw_data` retains them). Sharing one polymorphic catalog table for stations+cities —
rejected (the two have materially different flag sets; separate tables keep each upsert simple and the
feature‑016 station table untouched).

## Decision 3 — Polymorphic `LocationId` + `LocationType`, no FK

**Decision**: Both warehouse entities expose `Guid? LocationId` + `string? LocationType`
(`"Station"`/`"City"`), with **no** EF navigation and **no** database FK on `location_id`. Validity of the
discriminator is enforced by command validators (FluentValidation) and a DB `CHECK` constraint
(`location_type IN ('Station','City')`).

**Rationale**: A polymorphic id cannot carry a single relational FK to two different tables; the spec
explicitly chose the discriminator approach precisely to avoid accumulating one nullable FK column per
future location type (FR‑014, Assumptions). Referential integrity is intentionally traded for
extensibility; soft‑deleted catalog rows must not orphan a warehouse entry anyway (Edge Cases: the entry
keeps its reference and last‑known name). A `CHECK` on the discriminator + validators give defence in
depth without a cross‑table FK.

**Alternatives considered**:
- *Two nullable FKs (`station_id`, `city_id`)*: the column‑proliferation anti‑pattern the spec rejects.
- *EF TPH/TPT inheritance over a base `Location`*: heavy mapping for two tiny catalogs; the read path is
  raw SQL anyway. Rejected (complexity).

## Decision 4 — Combined locations read merges in memory, not in SQL

**Decision**: `GetLocationsHandler` calls `ISpaceStationRepository.SearchActiveStationsAsync` **and** a
new `ICityRepository.SearchActiveCitiesAsync` (each: per‑type active filter + `ILike` + `OrderBy(name)` +
clamp), then merges, re‑sorts by name (case‑insensitive), and re‑clamps to the limit, projecting
`LocationDto(Id, Name, Type)`.

**Rationale**: Keeps each repository owning its own catalog and active‑filter logic (stations:
`IsAvailable AND !IsDecommissioned`; cities: `is_available AND is_visible`), honouring Clean Architecture
boundaries and reusing the exact station search already shipped. Result cardinality is small (tens–low
hundreds total), so an in‑memory merge/sort is trivially fast and avoids a brittle hand‑written `UNION`
across two schemas. The flat interleaved alphabetical ordering with no grouping/badges is exactly the
clarified UX.

**Alternatives considered**: a single raw‑SQL `UNION ALL` over `sc.space_stations` and `sc.cities` —
rejected (duplicates the active‑filter predicates in SQL, harder to unit‑test, marginal perf gain at this
scale). A dedicated `ILocationRepository` aggregating both — rejected as an unnecessary port; the handler
composing two existing ports is simpler.

## Decision 5 — Warehouse read joins both catalogs via dual LEFT JOIN

**Decision**: The raw‑SQL reads in `WarehouseInventoryRepository`, `MaterialInventoryRepository`, and
`ShipComponentRepository` replace the single `LEFT JOIN sc.space_stations ss ON ss.id = w.station_id`
with two type‑guarded joins and a coalesced name:

```sql
LEFT JOIN sc.space_stations ss ON ss.id = w.location_id AND w.location_type = 'Station'
LEFT JOIN sc.cities        ci ON ci.id = w.location_id AND w.location_type = 'City'
...
COALESCE(ss.name, ci.name, w.location) AS location
```

**Rationale**: Preserves the existing "show the catalog name, else the free‑text fallback" behaviour
(Edge Cases) for both types in one expression, with the type guard preventing an id collision across
tables (both keyed by `Guid`, but the guard scopes each join). Minimal change to a proven query shape.

**Alternatives considered**: resolving the name in application code via two dictionary lookups — rejected
(reintroduces an N+1/extra‑round‑trip pattern the raw‑SQL read deliberately avoids).

## Decision 6 — Ordered, non‑lossy migration; `…/station` routes renamed to `…/location`

**Decision**: One migration `AddCitiesAndPolymorphicLocation` runs, per table, in order: (1) create
`sc.cities`; (2) `ADD COLUMN location_id uuid NULL`, `location_type text NULL`; (3)
`UPDATE … SET location_id = station_id, location_type = 'Station' WHERE station_id IS NOT NULL`;
(4) drop the `station_id` FK constraint and column; (5) add the `CHECK (location_type IN ('Station','City'))`
and an index on `(location_id, location_type)`. The transfer endpoints `PUT /api/warehouse/items/{id}/station`
and `…/materials/{id}/station` are renamed to `…/location`, and request bodies move from `{ stationId }`
to `{ locationId, locationType }`.

**Rationale**: Copy‑before‑drop guarantees no station reference is lost (FR‑015, SC‑008) while the
old column is removed in the same migration so the application reads from one place only (clarification
Q4). Renaming the routes keeps the API vocabulary consistent with the "Location" concept (FR‑010) and is a
clean breaking change shipped in the same PR as the frontend rename. The free‑text `location` column and
its uniqueness index are untouched.

**Alternatives considered**: keeping `station_id` and adding `location_id` alongside (dual‑read during a
transition) — rejected; the spec mandates dropping the old column and a single read source. Keeping the
`…/station` route names — rejected (leaves "station" in the contract, contradicting FR‑010/FR‑011).

## Decision 7 — Frontend: rename in place, thread `type` through, single combobox

**Decision**: Rename `StationCombobox`→`LocationCombobox`, `useStationSearch`→`useLocationSearch`,
`stationsApi`→`locationsApi`, `stationSchemas`→`locationSchemas`, `TransferStationDialog`→
`TransferLocationDialog`, and the transfer hooks; `LocationOption` becomes `{ id, name, type }` and the
combobox's `onValueChange` emits `(id, name, type)`. Every warehouse `Station` UI string becomes
`Location`. TanStack Query keys move under a `locationKeys` factory hitting `/api/warehouse/locations`.

**Rationale**: The combobox already does server‑side search with `shouldFilter={false}`, so pointing it at
the merged endpoint yields the interleaved alphabetical list with zero UI restructuring (FR‑009). Threading
`type` lets the save payload carry the polymorphic reference (SC‑004). A rename‑in‑place keeps the diff
reviewable and preserves the existing feature‑owned test structure.

**Alternatives considered**: a new parallel `LocationCombobox` alongside the old one — rejected (dead code,
the rename is total per FR‑011). Client‑side type grouping/badges — rejected (clarified out).

## Open questions

None. All spec Clarifications are resolved and the technical decisions above are internally consistent with
the feature‑016 patterns already in the codebase.
</content>
