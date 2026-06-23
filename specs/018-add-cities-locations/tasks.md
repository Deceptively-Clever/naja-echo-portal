# Tasks: Add Cities Import & Rename Stations to Locations

**Input**: Design documents from `/specs/018-add-cities-locations/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/openapi.yaml ✅, quickstart.md ✅

**Tests**: Included — constitution Principle II (TDD) is NON-NEGOTIABLE. All tests MUST be written and confirmed failing before implementation code is written.

**Organization**: Tasks grouped by user story. US1 (city import) and the schema migration (Phase 2) are the critical path.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on in-progress tasks)
- **[Story]**: Which user story this task belongs to
- Tasks without `[Story]` are setup or foundational (no story label)

---

## Phase 1: Setup

**Purpose**: Verify the API contract is committed and complete before any implementation begins (constitution Principle I).

- [ ] T001 Verify `specs/018-add-cities-locations/contracts/openapi.yaml` covers all changed endpoints: `POST /api/admin/locations/import` response includes a `cities` block with `added`, `updated`, `softDeleted`, `skipped`, `total` fields; `GET /api/warehouse/locations` is defined with `search` and `limit` query params; `PUT …/items/{id}`, `PUT …/items/{id}/location`, and material twins accept `locationId (uuid)` + `locationType (string)` in request body; transfer routes are named `…/location` not `…/station`
- [ ] T002 [P] Verify `specs/018-add-cities-locations/contracts/openapi.yaml` defines: `LocationOption` schema with `id (uuid)`, `name (string)`, and `type (string, enum: Station|City)`; `LocationListResponse` wrapping an array of `LocationOption`; `CityImportCountsResponse` with all five count fields; and that `GET /api/warehouse/locations` is marked as requiring authentication (any authenticated member)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, interfaces, EF configuration, and the migration — must be complete before ANY user story can be implemented or tested against a real schema.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete. The migration is forward-only and destructive (column drop) — requires explicit PR approval note per constitution Development Workflow.

- [ ] T003 Create `City` domain entity in `backend/src/NajaEcho.Domain/Locations/City.cs` with fields: `Id (Guid)`, `UexId (int)`, `StarSystemId (Guid)`, `Name (string ≤256)`, `Code (string? ≤32)`, `IsAvailable (bool)`, `IsAvailableLive (bool)`, `IsVisible (bool)`, `Status (string ≤32)`, `RawData (JsonDocument)`, `ImportedAt`, `UpdatedAt`, `SoftDeletedAt?` — mirrors `SpaceStation`, no `IsDecommissioned`/`IsLandable`/`Nickname`
- [ ] T004 [P] Create `ICityRepository` port in `backend/src/NajaEcho.Application/Abstractions/ICityRepository.cs` with `BulkUpsertAsync(IEnumerable<JsonDocument>, IReadOnlyDictionary<int,Guid> starSystemMap)` returning `ImportCounts`, and `SearchActiveCitiesAsync(string? search, int limit)`
- [ ] T005 [P] Add `FetchAllCitiesAsync` to `IUexLocationClient` in `backend/src/NajaEcho.Application/Abstractions/IUexLocationClient.cs`
- [ ] T006 Update `WarehouseInventoryEntry` in `backend/src/NajaEcho.Domain/Warehouse/WarehouseInventoryEntry.cs`: remove `StationId (Guid?)` + `SpaceStation? Station` nav; add `LocationId (Guid?)` + `LocationType (string?)` (no nav, polymorphic — values `"Station"` or `"City"`)
- [ ] T007 [P] Update `WarehouseMaterialEntry` in `backend/src/NajaEcho.Domain/Warehouse/WarehouseMaterialEntry.cs`: same removal + addition as T006
- [ ] T008 Create `CityConfiguration` in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/CityConfiguration.cs` mapping `sc.cities` with all columns from the `City` entity including `is_available_live` (stored, not filtered — FR-008), FK to `sc.star_systems` (`Restrict`), and indexes: unique `ix_cities_uex_id`, `ix_cities_status`, `ix_cities_star_system_id`, covering `ix_cities_avail_visible_name (is_available, is_visible, name)`
- [ ] T009 [P] Update `WarehouseInventoryEntryConfiguration` in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/WarehouseInventoryEntryConfiguration.cs`: remove `station_id` FK mapping; add `location_id (uuid, nullable)`, `location_type (text, nullable)`, `CHECK (location_type IN ('Station','City'))`, index `ix_warehouse_inventory_location (location_id, location_type)`
- [ ] T010 [P] Update `WarehouseMaterialEntryConfiguration` in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/WarehouseMaterialEntryConfiguration.cs`: same changes as T009
- [ ] T011 Add `DbSet<City> Cities` to `AppDbContext` in `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs`
- [ ] T012 Generate EF Core migration `AddCitiesAndPolymorphicLocation` from `backend/src/NajaEcho.Api` — verify migration SQL follows data-model.md order: (1) create `sc.cities` with all columns including `is_available_live`, (2) ADD `location_id`+`location_type` to both warehouse tables, (3) UPDATE copy `station_id → location_id` with `location_type='Station'`, (4) DROP `station_id` FK and column, (5) ADD CHECK and index; also review the generated Down migration SQL and add a PR description note that city-typed rows are unrecoverable on rollback (documented as intentional in data-model.md)
- [ ] T013 Register `ICityRepository → CityRepository` and `GetLocationsHandler` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`

**Checkpoint**: Foundation ready — migration applied, domain entities updated, interfaces defined, DI wired. User story implementation can now begin.

---

## Phase 3: User Story 1 — Trigger City Import (Priority: P1) 🎯 MVP

**Goal**: Admin triggers city import on the data import page; cities are upserted into `sc.cities`; import summary shows fetched/inserted/updated/soft-deleted/skipped counts; empty source aborts with no commit.

**Independent Test**: Trigger the import on the admin page and confirm `sc.cities` is populated with records matching the UEX source; re-run confirms idempotent upsert; previously present cities now absent are soft-deleted.

### Tests for User Story 1 (write first — MUST FAIL before implementation)

- [ ] T014 [P] [US1] Write unit tests for `CityRepository.BulkUpsertAsync` covering insert, update, reactivate, soft-delete, and skip-on-missing-parent scenarios in `backend/tests/NajaEcho.Application.Tests/Features/Locations/CityRepositoryTests.cs` (use fake/in-memory)
- [ ] T015 [P] [US1] Write unit test for `ImportLocationsHandler` when cities source returns empty — expect `EmptySourceException("cities")` thrown, no commit, no star-system or station rows affected in `backend/tests/NajaEcho.Application.Tests/Features/Locations/ImportLocationsHandlerTests.cs`
- [ ] T016 [P] [US1] Write Testcontainers integration test for city upsert round-trip (insert → update → soft-delete → reactivate) against real PostgreSQL in `backend/tests/NajaEcho.Infrastructure.Tests/Locations/CityRepositoryIntegrationTests.cs`
- [ ] T017 [P] [US1] Write API endpoint test verifying `POST /api/admin/locations/import` response includes a `cities` block with `added`, `updated`, `softDeleted`, `skipped`, `total` fields in `backend/tests/NajaEcho.Api.Tests/Features/Admin/ImportLocationsEndpointTests.cs`

### Implementation for User Story 1

- [ ] T018 [US1] Add `CityImportCounts` to `ImportLocationsResult` in `backend/src/NajaEcho.Application/Features/Locations/ImportLocations/ImportLocationsResult.cs`
- [ ] T019 [P] [US1] Implement `CityRepository.BulkUpsertAsync` in `backend/src/NajaEcho.Infrastructure/Locations/CityRepository.cs` — copy `SpaceStationRepository` pattern (parent key `id_star_system`, flags `is_available`/`is_available_live`/`is_visible`, same skip/reactivate/soft-delete logic)
- [ ] T020 [P] [US1] Add `FetchAllCitiesAsync` to `UexLocationClient` in `backend/src/NajaEcho.Infrastructure/Locations/UexLocationClient.cs` — call `cities` feed, parse `{ data: [] }`, same `JsonDocument`-per-record pattern as stations
- [ ] T021 [US1] Update `ImportLocationsHandler` in `backend/src/NajaEcho.Application/Features/Locations/ImportLocations/ImportLocationsHandler.cs` to fetch cities via `FetchAllCitiesAsync` after stations, upsert via `ICityRepository.BulkUpsertAsync` reusing the already-built `starSystemMap`; throw `EmptySourceException("cities")` when city source empty (no commit); include `CityImportCounts` in returned result; emit Serilog structured log entries for city fetch count and upsert outcome (inserted/updated/softDeleted/skipped) mirroring the existing station log pattern
- [ ] T022 [P] [US1] Add `CityImportCountsResponse` to `ImportLocationsResponse` in `backend/src/NajaEcho.Api/Features/Admin/Locations/Contracts/ImportLocationsResponse.cs`
- [ ] T023 [US1] Update `LocationAdminEndpoints` in `backend/src/NajaEcho.Api/Features/Admin/Locations/LocationAdminEndpoints.cs` to map `CityImportCounts` from the result into `CityImportCountsResponse` in the response

**Checkpoint**: City import fully functional. Admin can trigger import, cities populate `sc.cities`, summary shows all counts, empty source aborts safely. Testable independently via admin page or `POST /api/admin/locations/import`.

---

## Phase 4: User Story 2 — Select a Location When Adding a Warehouse Entry (Priority: P2)

**Goal**: All three warehouse Add dialogs present a unified, searchable "Location" combobox listing both active stations and active cities in a flat alphabetical list. Saving an entry stores `locationId`+`locationType` via the updated API. The `GET /api/warehouse/locations` endpoint powers the combobox.

**Independent Test**: Open the **Add** dialog on each warehouse page (Items, Ship Components, Materials); type a partial name; confirm a flat alphabetical list with both stations and cities appears under a "Location" label; select a city, save, and confirm the row's Location column shows the city name after reload. Edit dialog pre-population is covered by US3 (Phase 5) — US2 is independently testable using Add-only flows.

### Tests for User Story 2 (write first — MUST FAIL before implementation)

- [ ] T024 [P] [US2] Write unit tests for `GetLocationsHandler` covering: merged station+city list sorted flat alphabetically (case-insensitive), station active filter (`Status=Active AND IsAvailable AND !IsDecommissioned`), city active filter (`IsAvailable AND IsVisible`), and `ILike` name filter applied to both in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/GetLocationsHandlerTests.cs`
- [ ] T025 [P] [US2] Write API test for `GET /api/warehouse/locations` in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/LocationsEndpointTests.cs`: returns interleaved stations+cities, requires auth (401 when anonymous), respects `search` query param
- [ ] T026 [P] [US2] Write API test for `POST /api/warehouse/items` (and materials twin) accepting `locationId`+`locationType: "City"` and persisting the polymorphic ref; and that invalid `locationType` returns 422 in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseCommandTests.cs`
- [ ] T027 [P] [US2] Write Testcontainers integration test for `GET /api/warehouse/locations` returning both types when both catalogs are seeded in `backend/tests/NajaEcho.Infrastructure.Tests/Warehouse/GetLocationsIntegrationTests.cs`
- [ ] T028 [P] [US2] Write Testcontainers integration test that `AddCitiesAndPolymorphicLocation` migration data-copy preserves all existing `station_id` references as `location_id`/`"Station"` and that the Location column renders the correct station name after migration in `backend/tests/NajaEcho.Infrastructure.Tests/Migrations/MigrationDataCopyTests.cs`
- [ ] T029 [P] [US2] Write frontend Vitest tests for `LocationCombobox` in `frontend/src/features/warehouse/__tests__/LocationCombobox.test.tsx`: lists interleaved stations and cities, filters by typed text, shows empty-state message when catalog empty (MSW mock returning empty list)
- [ ] T030 [P] [US2] Write frontend Vitest test in `frontend/src/features/warehouse/__tests__/AddInventoryDialog.test.tsx` that selecting a city from LocationCombobox stores `{ locationId, locationType: "City" }` in the submitted form payload; repeat for AddMaterialDialog

### Backend Implementation for User Story 2

- [ ] T031 [P] [US2] Create `LocationDto` in `backend/src/NajaEcho.Application/Features/Warehouse/GetLocations/LocationDto.cs` with `Id (Guid)`, `Name (string)`, `Type (string)` (`"Station"` or `"City"`)
- [ ] T032 [P] [US2] Create `GetLocationsQuery` in `backend/src/NajaEcho.Application/Features/Warehouse/GetLocations/GetLocationsQuery.cs` with `Search (string?)` and `Limit (int)`
- [ ] T033 [US2] Implement `GetLocationsHandler` in `backend/src/NajaEcho.Application/Features/Warehouse/GetLocations/GetLocationsHandler.cs`: call `ISpaceStationRepository.SearchActiveStationsAsync` + `ICityRepository.SearchActiveCitiesAsync` with the same search/limit, project each to `LocationDto` with appropriate `Type`, merge, re-sort by name (case-insensitive), re-clamp to limit; emit a Serilog structured log entry for each locations search (search term, station count, city count, total returned)
- [ ] T034 [P] [US2] Implement `SearchActiveCitiesAsync` on `CityRepository` in `backend/src/NajaEcho.Infrastructure/Locations/CityRepository.cs`: active filter `Status=Active AND IsAvailable AND IsVisible`, `ILike` name filter, `OrderBy(Name)`, clamp 1–100
- [ ] T035 [P] [US2] Update `AddInventoryItemCommand` in `backend/src/NajaEcho.Application/Features/Warehouse/AddInventoryItem/AddInventoryItemCommand.cs`: replace `Guid? StationId` with `Guid? LocationId` + `string? LocationType`; add FluentValidation rules: (1) `LocationType` must be `'Station'` or `'City'` when set, and (2) `LocationId` and `LocationType` must be both null or both set — reject any payload where one is set without the other
- [ ] T036 [P] [US2] Update `UpdateInventoryItemCommand` in `backend/src/NajaEcho.Application/Features/Warehouse/UpdateInventoryItem/UpdateInventoryItemCommand.cs`: same `StationId → LocationId + LocationType` change and same two FluentValidation rules as T035
- [ ] T037 [P] [US2] Update `TransferInventoryItemCommand` in `backend/src/NajaEcho.Application/Features/Warehouse/TransferInventoryItem/TransferInventoryItemCommand.cs`: same change and same two FluentValidation rules as T035
- [ ] T038 [P] [US2] Update `AddMaterialCommand`, `UpdateMaterialCommand`, and `TransferMaterialCommand` in `backend/src/NajaEcho.Application/Features/Warehouse/Materials/`: same `StationId → LocationId + LocationType` change and same two FluentValidation rules as T035
- [ ] T039 [P] [US2] Create `LocationOption` and `LocationListResponse` contract types in `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/` (`LocationOption` has `Id`, `Name`, `Type`)
- [ ] T040 [US2] Update `WarehouseEndpoints` in `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`: add `GET /api/warehouse/locations` wired to `GetLocationsHandler` with `RequireAuthorization()` (any authenticated member — FR-007, not Admin-only); update Add/Update item+material endpoints to accept `locationId`+`locationType`; rename transfer routes from `…/station` to `…/location` with updated request body
- [ ] T041 [P] [US2] Update raw SQL in `WarehouseInventoryRepository` in `backend/src/NajaEcho.Infrastructure/Warehouse/WarehouseInventoryRepository.cs`: replace single `LEFT JOIN sc.space_stations ss ON ss.id = w.station_id` with dual type-guarded joins (`LEFT JOIN sc.space_stations ss ON ss.id = w.location_id AND w.location_type = 'Station'` + `LEFT JOIN sc.cities ci ON ci.id = w.location_id AND w.location_type = 'City'`) and `COALESCE(ss.name, ci.name, w.location) AS location`
- [ ] T042 [P] [US2] Update `MaterialInventoryRepository` in `backend/src/NajaEcho.Infrastructure/Warehouse/MaterialInventoryRepository.cs`: same dual LEFT JOIN change as T041
- [ ] T043 [P] [US2] Update `ShipComponentRepository` in `backend/src/NajaEcho.Infrastructure/Warehouse/ShipComponentRepository.cs`: same dual LEFT JOIN change as T041

### Frontend Implementation for User Story 2

- [ ] T044 [P] [US2] Create `frontend/src/features/warehouse/schemas/locationSchemas.ts` with Zod schemas: `LocationOption` (`{ id: z.string().uuid(), name: z.string(), type: z.enum(['Station','City']) }`), `LocationListResponse`; remove stationSchemas equivalents if safe
- [ ] T045 [P] [US2] Create `frontend/src/features/warehouse/api/locationsApi.ts` with `getLocations(search?: string, limit?: number)` calling `GET /api/warehouse/locations` via `apiFetch`; include transfer location call replacing the old station transfer
- [ ] T046 [P] [US2] Create `frontend/src/features/warehouse/hooks/locationKeys.ts` query key factory pointing at `/api/warehouse/locations`
- [ ] T047 [P] [US2] Create `frontend/src/features/warehouse/hooks/useLocationSearch.ts` TanStack Query hook wrapping `getLocations`, mirroring `useStationSearch` pattern
- [ ] T048 [US2] Create `frontend/src/features/warehouse/components/LocationCombobox.tsx` (rename/replace `StationCombobox`): uses `useLocationSearch`, `onValueChange` emits `{ id, name, type }`, `shouldFilter={false}` (server-side search), labelled "Location", empty-state message "No locations found — import stations and cities first" when list empty (US2 #5); stores both id and type for the save payload
- [ ] T049 [P] [US2] Update inventory, material, ship-component, and addItem Zod schemas in `frontend/src/features/warehouse/schemas/` to replace `stationId: z.string().uuid().optional()` with `locationId: z.string().uuid().optional()` + `locationType: z.enum(['Station','City']).optional()`
- [ ] T050 [US2] Update `AddInventoryDialog.tsx` in `frontend/src/features/warehouse/components/AddInventoryDialog.tsx` to use `LocationCombobox` instead of `StationCombobox`, wire `{ locationId, locationType }` into form state, label "Location"
- [ ] T051 [P] [US2] Update `AddMaterialDialog.tsx` in `frontend/src/features/warehouse/components/AddMaterialDialog.tsx`: same LocationCombobox swap and schema update as T050
- [ ] T052 [P] [US2] Create/update `frontend/src/features/warehouse/hooks/useTransferItemLocation.ts` (renamed from `useTransferItemStation`): call `PUT …/items/{id}/location` with `{ locationId, locationType }`
- [ ] T053 [P] [US2] Create/update `frontend/src/features/warehouse/hooks/useTransferMaterialLocation.ts` (renamed from `useTransferMaterialStation`): same pattern as T052 for materials
- [ ] T054 [P] [US2] Create `frontend/src/features/warehouse/hooks/useLastTransferLocation.ts` (renamed from `useLastTransferStation`): same local-storage persistence pattern for last-used location `{ id, name, type }`

**Checkpoint**: `GET /api/warehouse/locations` serves a flat alphabetical interleave of stations and cities. All three Add dialogs use `LocationCombobox` labelled "Location". Saving an entry persists `locationId`+`locationType`. Transfer routes updated to `…/location`.

---

## Phase 5: User Story 3 — Edit a Warehouse Entry's Location (Priority: P3)

**Goal**: The Edit modal for all three warehouse features uses `LocationCombobox` (from US2) labelled "Location", pre-populates the current location (whether station or city), and saves the updated polymorphic reference.

**Independent Test**: Trigger Edit on a row whose location is a station; confirm the combobox pre-populates with that station's name; select a city, confirm, and verify the row now shows the city name. Cancel an edit and confirm no change.

### Tests for User Story 3 (write first — MUST FAIL before implementation)

- [ ] T055 [P] [US3] Write frontend Vitest test in `frontend/src/features/warehouse/__tests__/EditInventoryDialog.test.tsx` that the Edit dialog pre-populates the LocationCombobox with the row's current location (station-backed and city-backed variants via MSW); and that confirming saves `{ locationId, locationType }` to the update endpoint
- [ ] T056 [P] [US3] Write frontend Vitest test for `TransferLocationDialog` in `frontend/src/features/warehouse/__tests__/TransferLocationDialog.test.tsx`: pre-populates with last-transfer location, submits to `PUT …/location` with correct body
- [ ] T057 [P] [US3] Write API test for `PUT /api/warehouse/items/{id}` accepting `locationType: "City"` and updating the row's polymorphic ref, and for `PUT /api/warehouse/items/{id}/location` same — in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseCommandTests.cs`

### Implementation for User Story 3

- [ ] T058 [US3] Update `EditInventoryDialog.tsx` in `frontend/src/features/warehouse/components/EditInventoryDialog.tsx`: swap `StationCombobox` → `LocationCombobox`, populate default value from row's `locationId`+`locationType` (resolved to `{ id, name, type }`), wire `{ locationId, locationType }` into form submission (FR-013)
- [ ] T059 [P] [US3] Update `EditMaterialDialog.tsx` in `frontend/src/features/warehouse/components/EditMaterialDialog.tsx`: same LocationCombobox swap and pre-population as T058
- [ ] T060 [P] [US3] Update `EditShipComponentDialog.tsx` in `frontend/src/features/warehouse/components/EditShipComponentDialog.tsx`: same as T058 (ship components are item-backed via `WarehouseInventoryEntry`)
- [ ] T061 [US3] Create/update `TransferLocationDialog.tsx` in `frontend/src/features/warehouse/components/TransferLocationDialog.tsx` (renamed from `TransferStationDialog`): uses `LocationCombobox`, calls `useTransferItemLocation`/`useTransferMaterialLocation`, pre-populates from `useLastTransferLocation`

**Checkpoint**: Edit and Transfer flows fully updated. All three warehouse features can change a row's location to any station or city in one modal interaction.

---

## Phase 6: User Story 4 — Rename "Station" Labels to "Location" (Priority: P4)

**Goal**: Every visible "Station" string across the three warehouse pages — column headers, form labels, filter labels, empty states, button tooltips — reads "Location". No behaviour change.

**Independent Test**: Visit all three warehouse pages (Items, Ship Components, Materials) and open each Add/Edit/Transfer dialog and filter panel; confirm zero occurrences of "Station" in any visible text.

### Tests for User Story 4 (write first — MUST FAIL before implementation)

- [ ] T062 [P] [US4] Write frontend Vitest assertions across all three warehouse page snapshots in `frontend/src/features/warehouse/__tests__/` confirming no text node contains "Station" (as a label/header/tooltip) on InventoryPage, MaterialsPage, and ShipComponentsPage
- [ ] T063 [P] [US4] Write frontend Vitest assertions for each filter component (`InventoryFilters`, `MaterialsFilters`, `ShipComponentsFilters`) confirming the location filter label reads "Location"

### Implementation for User Story 4

- [ ] T064 [P] [US4] Update column header in `InventoryTable.tsx` in `frontend/src/features/warehouse/components/InventoryTable.tsx`: rename "Station" → "Location" column header only (the display value in each row comes from the COALESCE read path in T041 and is unchanged)
- [ ] T065 [P] [US4] Update column headers in `MaterialsTable.tsx` and `ShipComponentsTable.tsx` in `frontend/src/features/warehouse/components/`: rename "Station" → "Location"
- [ ] T066 [P] [US4] Update filter labels in `InventoryFilters.tsx`, `MaterialsFilters.tsx`, and `ShipComponentsFilters.tsx` in `frontend/src/features/warehouse/components/`: rename "Station" filter label → "Location"
- [ ] T067 [P] [US4] Audit and update any remaining "Station" strings (empty-state messages, button tooltips, placeholder text, aria-labels) across all warehouse components in `frontend/src/features/warehouse/`

**Checkpoint**: No visible "Station" text remains on any of the three warehouse pages or their dialogs.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate end-to-end using quickstart.md, confirm CI passes, and delete dead code.

- [ ] T068 [P] Run full backend test suite (`dotnet test` from `backend/`) and confirm all tests green
- [ ] T069 [P] Run full frontend test suite (`npm run test` from `frontend/`) and confirm all tests green
- [ ] T070 Delete `StationCombobox.tsx`, `stationKeys.ts`, `stationsApi.ts`, `stationSchemas.ts`, `TransferStationDialog.tsx`, and the renamed `useStationSearch.ts`/transfer hooks from `frontend/src/features/warehouse/` if they have been fully replaced and are no longer imported
- [ ] T071 Run all seven quickstart.md validation scenarios against the running app (Scenario 1–7) and confirm each expected result is met

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1. Blocks **all** user stories.
- **US1 (Phase 3)**: Depends on Phase 2 (needs `City` entity, `ICityRepository`, migration). No dependency on US2–US4.
- **US2 (Phase 4)**: Depends on Phase 2 (needs migration applied, warehouse entity changes). Depends on US1 completing T019 (`CityRepository.SearchActiveCitiesAsync`). US2 backend and frontend tracks can proceed in parallel once Phase 2 is done.
- **US3 (Phase 5)**: Depends on US2 completing `LocationCombobox` (T048). Edit dialogs reuse the same component.
- **US4 (Phase 6)**: Depends on Phase 2 (for consistent vocabulary). Can proceed in parallel with US1/US2/US3 since it touches different files (table/filter components), but must complete before any warehouse page ships.
- **Polish (Phase 7)**: Depends on all user stories complete.

### User Story Internal Dependencies

- Within each story: **tests first** (must fail) → models/interfaces → services/handlers → endpoints → UI
- `GetLocationsHandler` (T033) depends on `LocationDto` (T031) + `GetLocationsQuery` (T032)
- `LocationCombobox` (T048) depends on `useLocationSearch` (T047) → `locationKeys` (T046) → `locationsApi` (T045)
- Edit dialogs (T058–T060) depend on `LocationCombobox` (T048)
- Backend command handlers for Add/Update/Transfer depend on the schema having `location_id`/`location_type` columns (T012 migration)

### Parallel Opportunities by Phase

**Phase 2**: T004, T005, T007, T009, T010 can all start in parallel after T003 (City entity). T008, T009, T010 can run in parallel with T004, T005.

**Phase 3**: T014–T017 (all tests) run in parallel. T019–T020 run in parallel (CityRepository + UexLocationClient). T018 + T019 must precede T021.

**Phase 4**: Backend track (T031–T043) and frontend track (T044–T054) can run in parallel after Phase 2 completes. Within backend: T031–T032, T035–T039, T041–T043 are all parallelizable. Within frontend: T044–T047, T049, T052–T054 are all parallelizable.

**Phase 6**: T064–T067 are all independent file changes — fully parallel.

---

## Parallel Example: User Story 1

```bash
# Run all US1 tests in parallel (write and fail-check first):
T014: CityRepository unit tests (fake)
T015: ImportLocationsHandler empty-source unit test
T016: Testcontainers city upsert integration test
T017: API import response includes cities block

# Then implement in parallel where possible:
T019: CityRepository.BulkUpsertAsync
T020: UexLocationClient.FetchAllCitiesAsync
# (T021 ImportLocationsHandler waits for T019+T020)
```

## Parallel Example: User Story 2

```bash
# Run all US2 tests in parallel first:
T024–T030 all run in parallel

# Backend and frontend implement concurrently:
Backend: T031–T043
Frontend: T044–T054
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Verify contracts
2. Complete Phase 2: Migration + entities (CRITICAL)
3. Complete Phase 3: City import (US1) — admin can now populate the city catalog
4. **STOP and VALIDATE**: Trigger import, inspect `sc.cities`, confirm counts match UEX source
5. Demo: Admin city import is fully functional before any UI changes ship

### Incremental Delivery

1. Setup + Foundational → migration applied, City entity live
2. US1 → city catalog populated; import summary visible to admins
3. US2 → unified Location combobox in Add dialogs; `GET /api/warehouse/locations` live
4. US3 → Edit dialogs pre-populate location; transfer updated
5. US4 → all "Station" labels renamed; consistent vocabulary
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With two developers available after Phase 2:
- Developer A: US1 backend (city import pipeline)
- Developer B: US2 backend (`GetLocations` + warehouse command changes)
- Frontend work (US2–US4 combined) can follow once backend API is stable

---

## Notes

- [P] = different files, no dependency on in-progress tasks in the same phase
- TDD is non-negotiable (constitution Principle II): write tests, confirm they fail, then implement
- The migration drops `station_id` — include explicit PR approval note for this destructive step
- `is_available_live` is stored on `sc.cities` but NOT used as an active filter (FR-008)
- The free-text `location` string column is retained untouched as the `COALESCE` fallback
- `LocationCombobox` uses `shouldFilter={false}` — all filtering is server-side via the search query param
- No type badge or grouping in the Location list — flat alphabetical interleave only (spec clarification)
- Valid `locationType` values in v1: `"Station"`, `"City"` — enforced by FluentValidation + DB CHECK constraint
