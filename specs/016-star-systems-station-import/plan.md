# Implementation Plan: Star Systems & Space Station Import

**Branch**: `016-star-systems-station-import` | **Date**: 2026-06-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/016-star-systems-station-import/spec.md`

## Summary

Import **star systems** and **space stations** from the UEX Corp API (`/2.0/star_systems`,
`/2.0/space_stations`) into two new catalog tables, then use the station catalog to drive a structured
**Location** combobox across the three warehouse features (item inventory, ship components, materials).
Warehouse entries keep their existing free-text `location` string (deprecated) and gain a new **nullable
`station_id` FK** that is the canonical location field going forward. A **Transfer** action on each
warehouse row opens a modal that updates only the `station_id`.

The dominant backend precedent is the existing **UEX import pipeline** (`ImportShipsHandler` +
`UexVehicleClient` + `ShipRepository.BulkUpsertAsync` + `IImportCoordinator` + `ShipAdminEndpoints`):
a feature-folder use-case layout, a typed `HttpClient` UEX client implementing an Application port, a
transactional bulk-upsert-with-soft-delete repository, an admin Minimal-API endpoint under
`/api/admin/...` gated by `AuthorizationPolicies.Admin`, and a returned summary record of
added/updated/reactivated/soft-deleted/total counts. The station catalog **upsert** copies the
`ShipRepository` pattern keyed on `uex_id`. The station **list endpoint** copies the existing warehouse
`catalog/search` pattern. The warehouse **add/transfer** changes extend the existing
`AddInventoryItemHandler` / `AddMaterialHandler` and add two small `Transfer…` use cases. The frontend
adds a **Locations** tab to `DataImportPage`, a `features/warehouse/` station combobox + Transfer modal,
and threads `stationId` through the existing add dialogs.

**Two deviations from the ships precedent**, both required by the spec:

1. **Empty source = error** (FR-012, clarification): unlike `ImportShipsHandler` which returns a warning
   on a zero-record feed, this import **aborts and commits no changes** when the source returns an empty
   record set or is unreachable.
2. **Two-stage import with referential skip** (FR-002, FR-013): star systems import first; space stations
   that reference a star-system `id` not present in the local catalog are **skipped and counted**, not
   inserted.

**Schema change IS required**: two new tables (`star_systems`, `space_stations`) plus a nullable
`station_id` FK column added to **two** existing warehouse tables (`warehouse_inventory` — which backs
both item inventory and ship components — and `warehouse_material_inventory`), via one EF Core migration.
The added columns are nullable and non-destructive (forward-only; no existing data altered), so the
constitution's destructive-migration approval clause does **not** apply.

**API contract changes ARE required**: a new admin import endpoint, a new station-list endpoint, two new
warehouse Transfer endpoints, and an added optional `stationId` field on the two add-entry request
bodies — all defined in `contracts/openapi.yaml` before implementation. This is **not** a UI-only
feature, so the constitution's "No API contract changes required" exemption does **not** apply.

New backend HTTP behaviour:

- `POST /api/admin/locations/import` — admin-only. Imports star systems then space stations in one run;
  returns a combined summary with **separate** count blocks per entity (FR-001, FR-002, FR-004, FR-005).
- `GET  /api/warehouse/stations` — any authenticated member. Searchable list of **active,
  non-decommissioned** stations (`id`, full `name`) for the Location combobox (FR-006, FR-007, FR-009).
- `PUT  /api/warehouse/items/{id}/station` — Quartermaster. Sets/updates the `station_id` of a
  warehouse_inventory row (serves both item-inventory and ship-component rows — same table) (FR-011).
- `PUT  /api/warehouse/materials/{id}/station` — Quartermaster. Sets/updates the `station_id` of a
  material row (FR-011).
- `POST /api/warehouse/items` and `POST /api/warehouse/materials` (existing) gain an optional
  `stationId` field (FR-008, FR-010).

## Technical Context

**Language/Version**: C# on .NET (`net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql` (snake_case),
ASP.NET Core Identity (cookie auth), a typed `HttpClient` UEX client registered via `AddHttpClient`
(base URL `https://api.uexcorp.uk/2.0/`, the existing `UexVehicleClient:BaseUrl` config key), Serilog,
`IImportCoordinator` (existing singleton import lock). Frontend — React 19 (Vite), React Router data
APIs, Tailwind CSS 4, shadcn/ui (Radix), Lucide, TanStack Query 5, React Hook Form + Zod, `apiFetch`.
**No new backend or frontend dependency is introduced** — the station combobox reuses the existing
`components/ui/combobox.tsx` / `command.tsx` / `popover.tsx` / `dialog.tsx` primitives (YAGNI).

**Storage**: PostgreSQL 16. One code-first EF migration, `AddStarSystemsAndStationCatalog`:
- `sc.star_systems` — mirrors `sc.ships`: `id` (Guid PK), `uex_id` (int, **unique**), `name`, `code`,
  `is_available`, `is_visible`, `status` (Active/SoftDeleted string), `raw_data` (jsonb), `imported_at`,
  `updated_at`, `soft_deleted_at`; index on `status`.
- `sc.space_stations` — `id` (Guid PK), `uex_id` (int, **unique**), `star_system_id` (Guid **FK** →
  `sc.star_systems.id`, `OnDelete.Restrict`), `name`, `nickname`, `is_available`, `is_decommissioned`,
  `is_landable`, capability flags promoted as needed (`has_refinery`, `has_trade_terminal`, …),
  `status`, `raw_data` (jsonb), `imported_at`, `updated_at`, `soft_deleted_at`; indexes on `status` and
  a filtered/index covering `(is_available, is_decommissioned)` for the list query.
- `warehouse_inventory` — add nullable `station_id` (Guid? **FK** → `sc.space_stations.id`,
  `OnDelete.Restrict`); existing `location` string **retained**.
- `warehouse_material_inventory` — add nullable `station_id` (same FK + retention).

`OnDelete.Restrict` + soft-delete (never hard-delete) means a referenced station is never physically
removed, so the warehouse FK never dangles (edge case: a soft-deleted station's name still resolves via
join).

**Testing**: Backend — xUnit + FluentAssertions. Application handler unit tests with a fake UEX client +
fake repositories: combined import happy path (separate counts), **empty-source aborts with zero
changes** (FR-012, SC-006), unreachable-source aborts, **station with unknown parent star system is
skipped + counted** (FR-013), soft-delete of records absent from source (FR-003), station-list filters
out unavailable/decommissioned (FR-007), add-entry persists `stationId`, transfer updates `station_id`
only and leaves `location` untouched. At least one Testcontainers (PostgreSQL) integration test through
the real tables exercising the `uex_id` unique indexes, the star-system→station FK, and the nullable
warehouse `station_id` FK. API endpoint tests for status codes + RFC-7807 problem mapping (admin gate,
502 on source failure, 409 on concurrent import, 404 on transfer of a missing row). Frontend — Vitest +
React Testing Library + MSW mirroring existing suites: Locations import tab (summary render, empty/error
states), station combobox filters as the member types, add-entry saves `stationId`, Transfer modal opens
/ confirms / cancels and **defaults to the last-selected station in the session** (FR-014).

**Target Platform**: Linux server (containerized API) + browser SPA.

**Performance Goals**: Per the spec assumption the catalog is tens–low-hundreds of records, fetched and
upserted in a single synchronous admin request within a normal HTTP timeout (SC-001). The station list
is a single indexed, filtered query with a `limit` (default 25, clamped ≤100), matching the existing
`catalog/search` ergonomics (SC-003). No pagination, no background job, no scheduled refresh (out of
scope per spec).

**Constraints**:
- Import is **admin-triggered only**, gated by `AuthorizationPolicies.Admin`; one global import lock via
  `IImportCoordinator` rejects concurrent runs with 409 (FR-004).
- The whole import (both entities) runs inside **one DB transaction**; any failure — including an empty
  or unreachable source — rolls back with **zero** inserts/updates/soft-deletes (FR-012, SC-006).
- Star systems are imported **before** space stations so the parent lookup map is populated; stations
  with an unresolved `id_star_system` are skipped and counted (FR-013).
- The station list returns **only** `is_available = 1 AND is_decommissioned = 0` stations (FR-007).
- `station_id` is the **canonical** location; the free-text `location` is deprecated but retained and
  **never written** by the Transfer action (FR-010, FR-011, assumption). Existing rows are **not**
  migrated (assumption, SC-007).
- The combobox shows the **full station name** (e.g. "ARC-L1 Wide Forest Station") (FR-009).
- Transfer "last station" default is **session/client-side** state (per the clarification), not
  persisted server-side.
- Out of scope (v1): importing planets/moons/cities/orbits, capability-based dropdown filtering,
  scheduled/auto refresh, migrating existing free-text locations, removing the `location` column.

### Verified existing facts (from codebase inspection)

- **Import pipeline**: `ImportShipsHandler` acquires `IImportCoordinator.TryAcquire()` (→ 409
  `ImportAlreadyInProgressException` if busy), fetches via the UEX client, maps `JsonDocument` →
  domain, calls `repository.BulkUpsertAsync`, returns an `ImportShipsResult(added, updated, reactivated,
  softDeleted, total)`, and `Release()`s in `finally`. The new combined handler mirrors this but wraps
  **both** entity imports in one flow and **throws** (not warns) on an empty feed.
- **UEX client**: `UexVehicleClient(HttpClient, ILogger)` implements `IUexVehicleClient`, GETs a
  relative path (`"vehicles"`), `EnsureSuccessStatusCode()`, parses the `data` array into
  `IReadOnlyList<JsonDocument>`. Registered via `AddHttpClient<IUexVehicleClient, UexVehicleClient>` with
  `BaseAddress = UexVehicleClient:BaseUrl ?? https://api.uexcorp.uk/2.0/`. The new client fetches
  `"star_systems"` and `"space_stations"` the same way.
- **Upsert repo**: `ShipRepository.BulkUpsertAsync` opens a transaction, dedupes incoming by `uex_id`
  (last wins), updates promoted columns + `raw_data`, reactivates soft-deleted matches, adds new rows,
  soft-deletes `Active` rows absent from the feed, commits, returns the four counts. The two catalog
  repositories copy this verbatim (with the station upsert additionally resolving `star_system_id` and
  counting skips).
- **UEX field schema** (verified live, see [research.md](./research.md)): star systems expose
  `id, name, code, is_available, is_visible` (+ faction/jurisdiction/dates); space stations expose
  `id, id_star_system, name, nickname, is_available, is_decommissioned, is_landable, has_refinery,
  has_trade_terminal, …` (note: **no** `is_trading_post`; the trade-terminal flag is `has_trade_terminal`;
  flags are `0/1` integers).
- **Admin endpoints**: `ShipAdminEndpoints` maps `app.MapGroup("/api/admin/ships")
  .RequireAuthorization(AuthorizationPolicies.Admin)`, with `POST /import` returning `Results.Ok(summary)`
  / `Results.Accepted` / `Results.Conflict` (409) / `Results.Problem(502)` on `HttpRequestException` /
  `InvalidOperationException`. The new `LocationAdminEndpoints` mirrors this; the **empty-source case
  returns 502** (error) rather than `Accepted`.
- **Warehouse endpoints**: `WarehouseEndpoints` maps `/api/warehouse` with `RequireAuthorization()`
  (any authenticated) at the group level; individual mutating routes add
  `.RequireAuthorization(AuthorizationPolicies.Quartermaster)`. Handlers resolve the caller via
  `TryGetUserId(user, …)` and log with Serilog `Log.Information`. The station-list route stays at the
  group's authenticated level (FR-006); the two Transfer routes require Quartermaster.
- **Warehouse tables**: `warehouse_inventory` (`WarehouseInventoryEntry`) backs **both** item inventory
  and ship components (ship components are a filtered projection of `warehouse_inventory` joined with
  `sc.ship_component_attributes`). `warehouse_material_inventory` (`WarehouseMaterialEntry`) backs
  materials. So three UI features map to **two** physical tables → the `station_id` column is added to two
  entities, and **one** items-transfer endpoint serves both item and ship-component rows.
- **Add-entry handlers**: `AddInventoryItemHandler` / `AddMaterialHandler` take a command with
  `Location` and persist via `repository.AddOrIncrementAsync(...)`. They gain an optional `StationId` that
  is validated (if present, must reference an existing station) and persisted to the new column.
- **Catalog search precedent**: `GET /api/warehouse/catalog/search?search=&limit=` →
  `SearchSystemsCatalogQueryHandler` → repo `Search…Async(search, Math.Clamp(limit,1,100))` → DTO list.
  The station list copies this shape exactly (`GetStationsHandler` → `IStationCatalogRepository
  .SearchActiveStationsAsync`).
- **Frontend admin import**: `DataImportPage` is a `Tabs` shell; each tab (`ShipsImportTab`, …) pairs an
  import button hook (`useImportShips` mutation) with a results panel and `shipSchemas.ts` Zod parsing,
  `shipsApi.ts` `apiFetch` wrappers, and `shipKeys.ts` query-key factory. The new `LocationsImportTab`
  follows this exactly.
- **Frontend warehouse**: `features/warehouse/` owns `api/`, `hooks/` (TanStack Query + key factory),
  `schemas/` (Zod), `components/`, `pages/`, `__tests__/`. `AddInventoryDialog` already debounces a
  catalog search and reuses `useSystemsCatalogSearch`; the station combobox + `stationId` threading and
  the Transfer modal slot into this feature. The "last station this session" default is component/feature
  state (e.g. a small context or lifted state in the warehouse pages), not server state.
- **Zod-from-contract convention**: frontend request/response types are hand-written Zod schemas derived
  from and reviewed against `contracts/openapi.yaml` (established `features/warehouse/` + `features/admin/`
  pattern). The OpenAPI contract remains the single source of truth; no codegen tool is introduced (YAGNI).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | PASS | New import, station-list, and two transfer endpoints plus the added `stationId` request field are defined in `contracts/openapi.yaml` before implementation. Not UI-only, so the exemption clause is not invoked. |
| II. Test-First / TDD | PASS | Plan mandates failing tests first: handler unit tests (combined import with separate counts, empty-source/unreachable abort with zero changes, unknown-parent skip+count, soft-delete of absent records, list filtering, add persists `stationId`, transfer updates `station_id` only); ≥1 Testcontainers integration test through the real tables (unique `uex_id`, star-system→station FK, nullable warehouse FK); API endpoint tests for status/problem mapping; frontend component/hook tests mirroring existing suites. |
| III. Frontend/Backend Separation | PASS | Frontend consumes only the new `/api/...` routes via `apiFetch`; no server-rendered HTML, no DB access from the SPA. Request/response shapes governed by the OpenAPI contract; the UEX call is server-side only. |
| IV. Simplicity / YAGNI | PASS | Reuses the ship import pipeline, the catalog-search list pattern, the existing add-dialog flow, the existing combobox/dialog primitives, and `IImportCoordinator` wholesale. **No new dependency.** Two new tables justified by two distinct catalog entities; one combined import endpoint (not two) matches the single admin action; one items-transfer endpoint serves both inventory + ship-component rows (same table). Out-of-scope items (planets/moons, capability filtering, scheduling, data migration) are explicitly excluded. |
| V. Observability | PASS | Each endpoint/handler emits structured Serilog logs with the caller id, the per-entity counts, and the import outcome. No secrets are involved (UEX is unauthenticated); raw UEX payloads are not logged at info level — only counts and outcome. |
| VI. Modular Monolith + Clean Architecture | PASS | Domain entities in `NajaEcho.Domain/Locations`; use cases in `NajaEcho.Application/Features/Locations/...` and the warehouse Transfer/list use cases under `Features/Warehouse/...`; EF config, migration, repositories, and the UEX client in `NajaEcho.Infrastructure`; endpoints in `NajaEcho.Api`. The UEX client + repositories are Application ports implemented in Infrastructure — dependencies point inward only. Frontend logic lives in feature-owned hooks/schemas; routes stay thin. |

**Result**: PASS — no violations. Complexity Tracking table intentionally empty.

## Project Structure

### Documentation (this feature)

```text
specs/016-star-systems-station-import/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output — new endpoints + added stationId field
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Locations/
│       ├── StarSystem.cs                               # new entity (uex_id, name, code, flags, status, raw_data, timestamps)
│       ├── SpaceStation.cs                             # new entity (uex_id, star_system_id FK, name, nickname, flags, status, …)
│       └── CatalogStatus.cs                            # Active | SoftDeleted (or reuse an existing status enum)
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   ├── IUexLocationClient.cs                       # new port: FetchAllStarSystemsAsync / FetchAllSpaceStationsAsync → IReadOnlyList<JsonDocument>
│   │   ├── IStarSystemRepository.cs                    # new port: BulkUpsertAsync, GetActiveUexIdToIdMapAsync
│   │   └── ISpaceStationRepository.cs                  # new port: BulkUpsertAsync (returns counts + skipped), SearchActiveStationsAsync, ExistsAsync
│   └── Features/
│       ├── Locations/
│       │   └── ImportLocations/
│       │       ├── ImportLocationsCommand.cs
│       │       ├── ImportLocationsHandler.cs           # star systems → stations, one txn, empty/unreachable = throw
│       │       ├── ImportLocationsResult.cs            # per-entity count blocks (StarSystems, SpaceStations{+Skipped})
│       │       └── EmptySourceException.cs             # new: distinct from ImportAlreadyInProgressException
│       └── Warehouse/
│           ├── GetStations/                            # GetStationsQuery + Handler + StationDto (id, name)
│           ├── TransferInventoryItem/                  # command + handler (sets warehouse_inventory.station_id)
│           └── Materials/TransferMaterial/             # command + handler (sets warehouse_material_inventory.station_id)
│           # AddInventoryItem/ + Materials/AddMaterial/ commands gain optional StationId (edited)
├── NajaEcho.Infrastructure/
│   ├── Locations/
│   │   ├── UexLocationClient.cs                        # typed HttpClient: GET star_systems / space_stations
│   │   ├── StarSystemRepository.cs                     # BulkUpsert (ShipRepository pattern) + uex_id→id map
│   │   └── SpaceStationRepository.cs                   # BulkUpsert (+skip unknown parent) + active-station search
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   ├── StarSystemConfiguration.cs              # sc.star_systems, uex_id unique, status index
│   │   │   ├── SpaceStationConfiguration.cs            # sc.space_stations, uex_id unique, FK→star_systems, (avail,decom) index
│   │   │   ├── WarehouseInventoryEntryConfiguration.cs # + station_id nullable FK (edited)
│   │   │   └── WarehouseMaterialEntryConfiguration.cs  # + station_id nullable FK (edited)
│   │   └── Migrations/
│   │       └── <ts>_AddStarSystemsAndStationCatalog.cs # two new tables + two nullable FK columns
│   └── DependencyInjection.cs                          # +AddHttpClient<IUexLocationClient,…>, +repos, +ImportLocationsHandler, +transfer/list handlers (edited)
└── NajaEcho.Api/
    ├── Features/Admin/Locations/
    │   ├── LocationAdminEndpoints.cs                   # POST /api/admin/locations/import (Admin)
    │   └── Contracts/ImportLocationsResponse.cs        # per-entity summary response
    └── Features/Warehouse/
        ├── WarehouseEndpoints.cs                       # + GET /stations, PUT /items/{id}/station, PUT /materials/{id}/station; AddItem/AddMaterial accept stationId (edited)
        └── Contracts/                                  # + StationListResponse, TransferRequest, stationId on add DTOs (edited)
    # + registration of MapLocationAdminEndpoints in API composition (edited)

backend/tests/
├── NajaEcho.Application.Tests/Features/Locations/      # ImportLocations handler tests (counts, empty/unreachable abort, unknown-parent skip, soft-delete)
├── NajaEcho.Application.Tests/Features/Warehouse/      # GetStations filtering, Transfer (station-only) handler tests
├── NajaEcho.Infrastructure.Tests/Locations/           # Testcontainers: unique uex_id, star-system→station FK, warehouse station_id FK
└── NajaEcho.Api.Tests/Features/                        # admin import (200/409/502), station list, transfer (200/404) + RFC-7807

frontend/src/
├── features/admin/
│   ├── pages/DataImportPage.tsx                        # + "Locations" tab (edited)
│   ├── components/LocationsImportTab.tsx               # import button + per-entity summary panel + empty/error states
│   ├── hooks/useImportLocations.ts                     # mutation
│   ├── api/locationsApi.ts                             # apiFetch wrapper
│   ├── schemas/locationSchemas.ts                      # Zod for import summary
│   └── __tests__/importLocations.test.tsx
└── features/warehouse/
    ├── api/stationsApi.ts                              # searchStations, transferItemStation, transferMaterialStation
    ├── hooks/
    │   ├── useStationSearch.ts                         # debounced station combobox query
    │   ├── useTransferItemStation.ts                   # mutation → invalidate inventory/ship-components
    │   ├── useTransferMaterialStation.ts               # mutation → invalidate materials
    │   └── useLastTransferStation.ts                   # session default for the Transfer modal (FR-014)
    ├── schemas/stationSchemas.ts                       # Zod station option + transfer request/response
    ├── components/
    │   ├── StationCombobox.tsx                         # searchable station select (reuses components/ui/combobox)
    │   ├── TransferStationDialog.tsx                   # modal: station combobox, defaults to last selection, confirm/cancel
    │   ├── AddInventoryDialog.tsx                      # + StationCombobox, threads stationId (edited)
    │   └── AddMaterialDialog.tsx                       # + StationCombobox, threads stationId (edited)
    │   # InventoryTable / ShipComponentsTable / MaterialsTable: add a Transfer row action (edited)
    └── __tests__/                                      # station combobox filter, transfer open/confirm/cancel + last-selection default
```

**Structure Decision**: Add a new `NajaEcho.Domain/Locations` + `Application/Features/Locations` +
`Infrastructure/Locations` slice for the catalog (mirroring the existing `Ships` slice), plus small
additions to the existing `Warehouse` feature for the station list and Transfer use cases. The admin
import gets a new `Features/Admin/Locations` endpoint group and a new **Locations** tab in the existing
`DataImportPage`. The station combobox and Transfer modal live in `features/warehouse/`. Backend layering
follows the established four-project Clean Architecture split; the UEX call reuses the typed-`HttpClient`
+ Application-port pattern from `UexVehicleClient`.

## Complexity Tracking

> No constitution violations — table intentionally empty.
