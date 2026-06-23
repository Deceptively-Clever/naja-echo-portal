# Implementation Plan: Add Cities Import & Rename Stations to Locations

**Branch**: `018-add-cities-locations` | **Date**: 2026-06-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/018-add-cities-locations/spec.md`

## Summary

Extend the feature‑016 location catalog with a second source — **cities** from
`https://api.uexcorp.uk/2.0/cities` — and unify "Station" selection across the three warehouse
features (item inventory, ship components, materials) into a single **Location** concept that spans
both stations and cities.

Three distinct pieces of work:

1. **City catalog + import.** A new `City` domain entity and `sc.cities` table mirror the existing
   `SpaceStation` shape (UEX id, parent star‑system FK, name, status flags, `raw_data`, soft‑delete).
   A `CityRepository.BulkUpsertAsync` reuses the **exact** upsert/soft‑delete/skip pattern of
   `SpaceStationRepository` (skip when the parent star system is absent — FR‑006), and
   `IUexLocationClient` gains `FetchAllCitiesAsync` hitting the `cities` feed. Cities are folded into
   the **existing** `ImportLocationsHandler` (which already builds the `starSystemMap` cities need),
   adding a third `CityImportCounts` block to the import result/response — no new endpoint, no new
   pipeline abstraction (spec Assumptions; research Decision 1).

2. **Polymorphic location reference.** `WarehouseInventoryEntry` and `WarehouseMaterialEntry` replace
   the single `StationId` FK (+ `Station` nav) introduced in feature 016 with a **polymorphic**
   reference: nullable `LocationId` + a `LocationType` discriminator (`Station` | `City`). One EF Core
   migration creates `sc.cities`, adds `location_id` / `location_type` to both warehouse tables,
   **copies** every existing `station_id` into `location_id` with `location_type = 'Station'`, then
   **drops** the old `station_id` column and its FK (FR‑014, FR‑015). This is a destructive migration
   (column drop) and is flagged for explicit PR approval per the constitution Development Workflow.

3. **Unified Location endpoint + UI rename.** A new read‑only `GET /api/warehouse/locations` returns
   active stations **and** active cities as one flat alphabetical list of `{ id, name, type }`
   (stations: existing active filter; cities: `is_available = 1 AND is_visible = 1`, `is_available_live`
   stored but unused — FR‑007, FR‑008). The frontend renames `StationCombobox` → `LocationCombobox`
   (and the supporting hook/api/schema/keys), points it at the new endpoint, and stores both id and
   type. Every visible "Station" string across the three warehouse pages — column headers, form
   labels, filter labels, empty states, tooltips, the Transfer dialog — becomes "Location"
   (FR‑009 … FR‑013). No type badge or grouping; one interleaved alphabetical list (spec clarifications).

The dominant precedent is **feature 016**: `SpaceStation`/`StarSystem` entities, `SpaceStationRepository`,
`UexLocationClient`, `ImportLocationsHandler`, the `LocationAdminEndpoints` import action, the
`/api/warehouse/stations` search, and the warehouse Add/Update/Transfer commands and raw‑SQL read
joins are all copied or extended rather than reinvented.

**API contract changes ARE required** (not a UI‑only feature — the constitution's "No API contract
changes required" exemption does **not** apply): the import response gains a cities block, a new
`GET /api/warehouse/locations` endpoint is added, and the warehouse Add/Update/Transfer request
bodies change from `stationId` to `locationId` + `locationType`. All defined in
[`contracts/openapi.yaml`](./contracts/openapi.yaml) before implementation.

## Technical Context

**Language/Version**: C# on .NET (`net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql` (snake_case,
`sc` schema for the catalog), the existing typed `HttpClient` `UexLocationClient`, `IImportCoordinator`
(single‑flight import lock), Serilog. Frontend — React 19 (Vite), React Router data APIs, Tailwind
CSS 4, shadcn/ui (Radix `Command`/`Popover` combobox), Lucide, TanStack Query 5, React Hook Form + Zod,
`apiFetch`. **No new backend or frontend dependency is introduced** (YAGNI) — cities reuse the existing
HTTP client registration and the combobox reuses existing `components/ui/` primitives.

**Storage**: PostgreSQL 16, `sc` schema. **One migration** (`AddCitiesAndPolymorphicLocation`):
creates `sc.cities`; adds `location_id` + `location_type` to `warehouse_inventory` and
`warehouse_material`; data‑copies `station_id` → (`location_id`, `'Station'`); drops `station_id` +
its FK on both tables. Forward‑only; **destructive (column drop)** — requires the approval note
mandated by the constitution.

**Testing**: Backend — xUnit + FluentAssertions.
- Application handler unit tests with fakes: `CityRepository.BulkUpsertAsync` insert/update/reactivate/
  soft‑delete/skip‑on‑missing‑parent (FR‑002, FR‑003, FR‑006); `ImportLocationsHandler` empty‑cities
  source aborts with no commit (FR‑005, SC‑005); `GetLocationsHandler` merges + flat‑alpha‑sorts
  stations and cities and applies the per‑type active filter (FR‑007, FR‑008).
- ≥1 Testcontainers (PostgreSQL) integration test: city upsert + soft‑delete round‑trip; the
  migration data‑copy (`station_id` → `location_id`/`Station`) preserves existing references (SC‑008);
  the combined locations query returns both types.
- API endpoint tests: import response includes the cities counts block; `GET /api/warehouse/locations`
  returns interleaved results and requires auth; Add/Update/Transfer accept `locationId`+`locationType`
  and persist the polymorphic reference (SC‑004); invalid `locationType` rejected (422/400).

  Frontend — Vitest + RTL + MSW: `LocationCombobox` lists stations and cities interleaved and filters
  by typed text (FR‑009, SC‑003); selecting a city stores `{ id, type: 'City' }` (SC‑004); empty
  catalog → empty‑state message (US2 #5); Add/Edit dialogs and Transfer dialog render the "Location"
  label; no "Station" text remains on any of the three pages (SC‑006); Edit pre‑populates the current
  location for a station‑ or city‑backed row (FR‑013).

**Target Platform**: Linux server (containerized API) + browser SPA.

**Performance Goals**: City volume is tens–low hundreds (spec Assumptions) — import is one synchronous
fetch+upsert within a normal HTTP timeout (SC‑001); the locations list is two indexed `ILike` reads
merged in memory and returned unpaginated, filterable in a single interaction (SC‑003).

**Constraints**:
- City import is **admin‑only**, folded into the existing `/api/admin/locations/import`
  (`AuthorizationPolicies.Admin`), guarded by the same `IImportCoordinator` single‑flight lock.
- Cities with a null/missing/unknown `id_star_system` are **skipped** and counted, identical to the
  station rule (FR‑006).
- City active filter = `is_available = 1 AND is_visible = 1`; `is_available_live` is **persisted but
  not filtered on** in this version (FR‑008).
- The warehouse location reference is **polymorphic** (`location_id` + `location_type`), not a second
  FK column (FR‑014). Valid discriminator values: `Station`, `City`.
- The migration **must not lose** existing station references (FR‑015, SC‑008): copy before drop.
- The combined Location list is a **flat alphabetical** interleave — no grouping, no per‑row type
  badge, no type column in warehouse rows (spec clarifications, US4 #3).
- The deprecated free‑text `location` string column is **retained** as the display/uniqueness fallback
  (Edge Cases; existing `ux_warehouse_inventory_item_owner_location` index is unchanged).
- Out of scope (v1): scheduled/auto city refresh, capability filtering (e.g. trading‑post‑only),
  per‑row type indicators (spec Assumptions).

### Verified existing facts (from codebase inspection)

- **Domain catalog** (`NajaEcho.Domain/Locations/`): `StarSystem`, `SpaceStation`, `CatalogStatus`
  (`Active` / `SoftDeleted`). `SpaceStation` carries `UexId`, `StarSystemId`, `Name`, `Nickname`,
  bool status flags, `Status`, `RawData (JsonDocument/jsonb)`, `ImportedAt/UpdatedAt/SoftDeletedAt`,
  and a `StarSystem?` nav. `City` will mirror this exactly minus `IsDecommissioned`/`IsLandable` and
  plus `IsAvailableLive`/`IsVisible`/`Code`.
- **Station upsert** (`Infrastructure/Locations/SpaceStationRepository.BulkUpsertAsync`): transactional;
  groups incoming by `id`, looks up parent via `starSystemMap` and `skipped++` when absent, reactivates
  soft‑deleted rows, soft‑deletes actives missing from the source. `CityRepository` copies this verbatim
  (parent key `id_star_system`, flags `is_available`/`is_available_live`/`is_visible`).
- **UEX client** (`Infrastructure/Locations/UexLocationClient`): typed `HttpClient`, parses `{ data: [] }`,
  one `JsonDocument` per record; `FetchAllCitiesAsync("cities")` is added identically.
- **Import flow** (`Application/Features/Locations/ImportLocations/`): `ImportLocationsHandler` acquires
  `IImportCoordinator`, fetches star systems → upsert → `GetActiveUexIdToIdMapAsync`, fetches stations →
  upsert, returns `ImportLocationsResult(systemResult, stationResult)`. Cities slot in **after** stations
  using the **same** `starSystemMap`; result gains a `CityImportCounts` block. `EmptySourceException`
  aborts (no commit) and maps to `502` in `LocationAdminEndpoints`.
- **Locations admin endpoint** (`Api/Features/Admin/Locations/LocationAdminEndpoints`): `POST
  /api/admin/locations/import`, Admin‑only; maps `ImportAlreadyInProgress`→409, `EmptySource`/
  `HttpRequestException`→502. Response `ImportLocationsResponse` gains a `CityImportCountsResponse`.
- **Station search** (`Application/Features/Warehouse/GetStations` + `ISpaceStationRepository
  .SearchActiveStationsAsync`): active = `Status=Active AND IsAvailable AND !IsDecommissioned`,
  `ILike` name filter, `OrderBy(Name)`, clamp 1–100, returns `StationDto(Id, Name)`. The new
  `GetLocationsHandler` calls station search **and** city search, merges to `LocationDto(Id, Name, Type)`,
  re‑sorts by name, re‑clamps.
- **Warehouse entities** (`Domain/Warehouse/`): `WarehouseInventoryEntry` and `WarehouseMaterialEntry`
  both have `Guid? StationId` + `SpaceStation? Station`; EF configs map `station_id` with a
  `Restrict` FK to `space_stations`. These become `Guid? LocationId` + `string? LocationType` (no nav,
  no FK — polymorphic).
- **Warehouse read path** (`Infrastructure/Warehouse/WarehouseInventoryRepository`, and the Material/
  ShipComponent repos): raw SQL `LEFT JOIN sc.space_stations ss ON ss.id = w.station_id`,
  `COALESCE(ss.name, w.location) AS location`. Becomes a **dual** left join on `space_stations` (when
  `location_type='Station'`) and `cities` (when `'City'`) with `COALESCE(ss.name, ci.name, w.location)`.
- **Warehouse commands/endpoints** (`Application/Features/Warehouse/{AddInventoryItem,UpdateInventoryItem,
  TransferInventoryItem,Materials/*}` + `Api/Features/Warehouse/WarehouseEndpoints`): commands carry
  `Guid? StationId`; endpoints `POST /items`, `PUT /items/{id}`, `PUT /items/{id}/station` (+ material
  twins). These change to `LocationId` + `LocationType`; the `…/station` transfer routes become `…/location`.
- **Frontend warehouse** (`frontend/src/features/warehouse/`): `components/StationCombobox.tsx`,
  `hooks/{useStationSearch,stationKeys}.ts`, `api/stationsApi.ts`, `schemas/stationSchemas.ts`
  (`StationOption{id,name}`, `StationListResponse`), `components/TransferStationDialog.tsx`,
  `hooks/{useTransferItemStation,useTransferMaterialStation,useLastTransferStation}.ts`, and the
  Add/Edit dialogs + filters + tables for items/materials/ship‑components. All renamed to "Location"
  and re‑pointed; `LocationOption` becomes `{ id, name, type }`.
- **Zod‑from‑contract convention**: warehouse request/response types are hand‑written Zod schemas
  reviewed against `contracts/openapi.yaml` (no codegen) — the established pattern, retained here.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | PASS | All HTTP changes (import response cities block, new `GET /api/warehouse/locations`, `stationId`→`locationId`+`locationType` on Add/Update/Transfer) are defined in `contracts/openapi.yaml` before implementation. Not UI‑only, so the exemption is correctly **not** invoked. |
| II. Test-First / TDD | PASS | Failing tests first: city upsert (insert/update/reactivate/soft‑delete/skip), empty‑source abort, combined‑locations merge/sort/filter, the migration data‑copy preservation (Testcontainers), polymorphic persist round‑trip, plus frontend combobox interleave/filter/empty‑state and the full label rename. |
| III. Frontend/Backend Separation | PASS | SPA consumes only the new/changed `/api/warehouse/locations` and warehouse routes via `apiFetch`; UEX fetch is server‑side; no server‑rendered HTML, no DB access from the SPA. **Approved deviation — hand‑written Zod schemas** reviewed against the contract instead of codegen: the established project pattern (same justification as feature 017); any contract change ships with a matching schema update in the PR. |
| IV. Simplicity / YAGNI | PASS | `City`/`CityRepository`/`FetchAllCitiesAsync`/`GetLocations` copy the station equivalents; cities fold into the existing import (reusing `starSystemMap`) rather than a parallel endpoint+pipeline. Polymorphic ref chosen specifically to **avoid** column proliferation as location types grow (FR‑014). No new dependency. Out‑of‑scope items (auto‑refresh, capability filters, type badges) explicitly excluded. |
| V. Observability | PASS | City fetch/upsert and the combined‑locations read emit structured Serilog logs (counts, search, outcome) mirroring the station paths; no secrets involved (public UEX feed). |
| VI. Modular Monolith + Clean Architecture | PASS | `City` in Domain; `ICityRepository` port in Application implemented by `Infrastructure/Locations/CityRepository`; `GetLocations` use case in Application; endpoints in `NajaEcho.Api`. Dependencies point inward only. Frontend logic stays in feature‑owned hooks/schemas; route/page components stay thin. |

**Migration governance note**: `AddCitiesAndPolymorphicLocation` drops `station_id` from two tables —
a destructive change under the constitution's Development Workflow. The drop is **non‑lossy** (values
are copied to `location_id` in the same migration before the drop), and the PR description MUST record
explicit approval for the column drop. See [data-model.md](./data-model.md) for the ordered migration steps.

**Result**: PASS — no unjustified violations. Complexity Tracking table intentionally empty.

## Project Structure

### Documentation (this feature)

```text
specs/018-add-cities-locations/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output — City entity, polymorphic ref, migration steps
├── quickstart.md        # Phase 1 output — validation scenarios
├── contracts/
│   └── openapi.yaml     # Phase 1 output — import cities block, locations endpoint, location body
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Locations/
│       └── City.cs                                         # NEW — mirrors SpaceStation (no decomm; +IsAvailableLive/IsVisible/Code)
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   ├── ICityRepository.cs                              # NEW — BulkUpsertAsync + SearchActiveCitiesAsync
│   │   └── IUexLocationClient.cs                           # + FetchAllCitiesAsync (edited)
│   └── Features/
│       ├── Locations/ImportLocations/
│       │   ├── ImportLocationsHandler.cs                   # + fetch/upsert cities using starSystemMap (edited)
│       │   └── ImportLocationsResult.cs                    # + CityImportCounts block (edited)
│       └── Warehouse/
│           ├── GetLocations/                               # NEW — replaces GetStations
│           │   ├── GetLocationsQuery.cs                    # Search, Limit
│           │   ├── GetLocationsHandler.cs                  # merge station + city search → flat alpha
│           │   └── LocationDto.cs                          # Id, Name, Type ("Station"|"City")
│           ├── AddInventoryItem/AddInventoryItemCommand.cs # StationId → LocationId + LocationType (edited)
│           ├── UpdateInventoryItem/…                       # same (edited)
│           ├── TransferInventoryItem/…                     # StationId → LocationId + LocationType (edited)
│           └── Materials/{AddMaterial,UpdateMaterial,TransferMaterial}/…  # same (edited)
├── NajaEcho.Infrastructure/
│   ├── Locations/
│   │   ├── CityRepository.cs                               # NEW — copies SpaceStationRepository
│   │   └── UexLocationClient.cs                            # + FetchAllCitiesAsync (edited)
│   ├── Warehouse/
│   │   ├── WarehouseInventoryRepository.cs                 # dual LEFT JOIN stations+cities; LocationId/Type (edited)
│   │   ├── MaterialInventoryRepository.cs                  # same (edited)
│   │   └── ShipComponentRepository.cs                      # same (edited)
│   ├── Persistence/
│   │   ├── AppDbContext.cs                                 # + DbSet<City> (edited)
│   │   ├── Configurations/
│   │   │   ├── CityConfiguration.cs                        # NEW — sc.cities mapping + indexes
│   │   │   ├── WarehouseInventoryEntryConfiguration.cs     # station_id → location_id/location_type; drop FK (edited)
│   │   │   └── WarehouseMaterialEntryConfiguration.cs      # same (edited)
│   │   └── Migrations/
│   │       └── *_AddCitiesAndPolymorphicLocation.cs        # NEW — create cities; add+copy+drop (see data-model)
│   └── DependencyInjection.cs                              # + ICityRepository, GetLocationsHandler (edited)
└── NajaEcho.Api/
    ├── Features/
    │   ├── Admin/Locations/Contracts/ImportLocationsResponse.cs   # + CityImportCountsResponse (edited)
    │   └── Warehouse/
    │       ├── WarehouseEndpoints.cs                       # /stations→/locations; …/station→…/location; location body (edited)
    │       └── Contracts/                                  # LocationListResponse, LocationOption(Id,Name,Type); request bodies (edited)
    └── Features/Admin/Locations/LocationAdminEndpoints.cs  # map city counts into response (edited)

backend/tests/
├── NajaEcho.Application.Tests/Features/                    # CityRepository upsert (fake); ImportLocations empty‑cities; GetLocations merge
├── NajaEcho.Infrastructure.Tests/                          # Testcontainers: city upsert round‑trip; migration data‑copy; combined read
└── NajaEcho.Api.Tests/Features/Warehouse/                  # locations endpoint auth+interleave; polymorphic persist; invalid type

frontend/src/features/warehouse/
├── components/
│   ├── LocationCombobox.tsx                                # RENAMED from StationCombobox; stores {id,type}; "Location" copy
│   ├── TransferLocationDialog.tsx                          # RENAMED from TransferStationDialog
│   ├── AddInventoryDialog.tsx / EditInventoryDialog.tsx    # "Station"→"Location" labels; combobox swap (edited)
│   ├── AddMaterialDialog.tsx  / EditMaterialDialog.tsx     # same (edited)
│   ├── EditShipComponentDialog.tsx                         # same (edited)
│   ├── {Inventory,Materials,ShipComponents}Filters.tsx     # filter label "Location" (edited)
│   └── {Inventory,Materials,ShipComponents}Table.tsx       # column header "Location" (edited)
├── hooks/
│   ├── useLocationSearch.ts / locationKeys.ts              # RENAMED from useStationSearch/stationKeys → /locations
│   ├── useLastTransferLocation.ts                          # RENAMED (edited)
│   └── useTransfer{Item,Material}Location.ts               # RENAMED; PUT …/location (edited)
├── api/locationsApi.ts                                    # RENAMED from stationsApi; getLocations + transfer (edited)
├── schemas/locationSchemas.ts                             # RENAMED; LocationOption {id,name,type}; LocationListResponse
├── schemas/{inventory,material,shipComponent,addItem}Schemas.ts  # stationId → locationId+locationType (edited)
└── __tests__/                                              # combobox interleave/filter/empty; label rename; polymorphic save
```

**Structure Decision**: Fold cities into the existing `Locations/ImportLocations` slice and the
`Locations` Infrastructure folder (new `City` + `CityRepository` peers of `SpaceStation` +
`SpaceStationRepository`), reusing the single import endpoint and the `starSystemMap`. Replace the
warehouse `GetStations` slice with a `GetLocations` slice that merges the two catalog searches. The
warehouse entities adopt a polymorphic `LocationId` + `LocationType` pair (no second FK) handled in the
existing raw‑SQL repositories via a dual left join. The frontend renames the station combobox/hook/api/
schema surface to "Location", points it at `/api/warehouse/locations`, and threads the `type`
discriminator through save. Backend layering follows the established four‑project Clean Architecture split.

## Complexity Tracking

> No unjustified constitution violations — table intentionally empty. The one governance item (the
> destructive `station_id` column drop) is non‑lossy and recorded in the Constitution Check
> "Migration governance note" above; explicit approval is captured in the PR description, not here.
</content>
</invoke>
