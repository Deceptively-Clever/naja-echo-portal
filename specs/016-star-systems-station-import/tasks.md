---
description: "Task list for feature 016 — Star Systems & Space Station Import"
---

# Tasks: Star Systems & Space Station Import

**Input**: Design documents from `/specs/016-star-systems-station-import/`

**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/openapi.yaml ✅ | quickstart.md ✅

**TDD Required**: Constitution Principle II — every test task MUST be written and confirmed to **fail**
before the corresponding production code is written. Tests come before implementation within each phase.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to
- Exact file paths are included in every task description

---

## Phase 1: Setup — Domain Layer

**Purpose**: Create the domain entities and modify existing warehouse entities. These have no dependencies
and unblock all downstream phases. No tests needed here — pure data classes with no logic.

- [x] T001 Create `backend/src/NajaEcho.Domain/Locations/CatalogStatus.cs` as a `public static class` with two `string` constants: `Active = "Active"` and `SoftDeleted = "SoftDeleted"` (check first if a shared status constants class already exists in the Domain project; if so, add these values there instead)

- [x] T002 [P] Create `backend/src/NajaEcho.Domain/Locations/StarSystem.cs` as a `public sealed class` with these properties: `Guid Id`, `int UexId`, `string Name`, `string? Code`, `bool IsAvailable`, `bool IsVisible`, `string Status` (use `CatalogStatus` constants), `JsonDocument RawData` (`System.Text.Json`), `DateTimeOffset ImportedAt`, `DateTimeOffset UpdatedAt`, `DateTimeOffset? SoftDeletedAt` — all public settable; no constructor logic; mirrors the `Ship.cs` entity pattern

- [x] T003 [P] Create `backend/src/NajaEcho.Domain/Locations/SpaceStation.cs` as a `public sealed class` with these properties: `Guid Id`, `int UexId`, `Guid StarSystemId`, `string Name`, `string? Nickname`, `bool IsAvailable`, `bool IsDecommissioned`, `bool IsLandable`, `bool HasRefinery`, `bool HasTradeTerminal`, `string Status` (use `CatalogStatus`), `JsonDocument RawData`, `DateTimeOffset ImportedAt`, `DateTimeOffset UpdatedAt`, `DateTimeOffset? SoftDeletedAt`; also a navigation property `StarSystem? StarSystem` for EF — mirrors `Ship.cs`

- [x] T004 [P] Edit the existing `WarehouseInventoryEntry` domain entity (find its file via `grep -r "class WarehouseInventoryEntry" backend/src`) to add one new property: `public Guid? StationId { get; set; }` and a navigation property `public SpaceStation? Station { get; set; }` — these are the only additions; leave all existing properties untouched

- [x] T005 [P] Edit the existing `WarehouseMaterialEntry` domain entity (find via `grep -r "class WarehouseMaterialEntry" backend/src`) to add: `public Guid? StationId { get; set; }` and `public SpaceStation? Station { get; set; }` — same pattern as T004

**Checkpoint**: All domain classes compile. No tests yet.

---

## Phase 2: Foundational — EF Config, Migration, Ports, Repositories, DI

**Purpose**: Shared infrastructure that ALL three user stories depend on. Must complete before any story work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T006 Create `backend/src/NajaEcho.Application/Abstractions/IUexLocationClient.cs` as a `public interface` with two methods: `Task<IReadOnlyList<JsonDocument>> FetchAllStarSystemsAsync(CancellationToken ct = default)` and `Task<IReadOnlyList<JsonDocument>> FetchAllSpaceStationsAsync(CancellationToken ct = default)` — mirror the signature style of the existing `IUexVehicleClient` interface (find via `grep -r "IUexVehicleClient" backend/src/NajaEcho.Application`)

- [x] T007 [P] Create `backend/src/NajaEcho.Application/Abstractions/IStarSystemRepository.cs` with these methods: `Task<(int added, int updated, int reactivated, int softDeleted)> BulkUpsertAsync(IReadOnlyList<JsonDocument> records, CancellationToken ct = default)` and `Task<IReadOnlyDictionary<int, Guid>> GetActiveUexIdToIdMapAsync(CancellationToken ct = default)` (the map is `uex_id int → local Guid` and is used by the station upsert to resolve parent FK)

- [x] T008 [P] Create `backend/src/NajaEcho.Application/Abstractions/ISpaceStationRepository.cs` with these methods: `Task<(int added, int updated, int reactivated, int softDeleted, int skipped)> BulkUpsertAsync(IReadOnlyList<JsonDocument> records, IReadOnlyDictionary<int, Guid> starSystemMap, CancellationToken ct = default)` and `Task<IReadOnlyList<StationDto>> SearchActiveStationsAsync(string? search, int limit, CancellationToken ct = default)` and `Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)` — the `StationDto` will be created in T036 but declare it now as a forward reference (or put StationDto in this file temporarily)

- [x] T009 [P] Create `backend/src/NajaEcho.Application/Features/Locations/ImportLocations/EmptySourceException.cs` as `public sealed class EmptySourceException : Exception` with a constructor taking a `string entityName` and a message like `$"The UEX source returned an empty record set for {entityName}; import aborted."` — this is distinct from `ImportAlreadyInProgressException`

- [x] T010 Create `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/StarSystemConfiguration.cs` implementing `IEntityTypeConfiguration<StarSystem>`. In `Configure`: set table `sc.star_systems`; PK = `Id`; `UexId` required, has unique index `ix_star_systems_uex_id`; `Name` required maxLength 256; `Code` optional maxLength 32; `IsAvailable` required; `IsVisible` required; `Status` required maxLength 32, has index `ix_star_systems_status`; `RawData` has column type `jsonb`; `ImportedAt` required; `UpdatedAt` required; `SoftDeletedAt` optional — copy the existing `ShipConfiguration.cs` structure (find via `grep -r "class ShipConfiguration" backend/src`)

- [x] T011 Create `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/SpaceStationConfiguration.cs` implementing `IEntityTypeConfiguration<SpaceStation>`. In `Configure`: table `sc.space_stations`; PK = `Id`; `UexId` required, unique index `ix_space_stations_uex_id`; `Name` required maxLength 256; `Nickname` optional maxLength 256; all bool flags required; `Status` required maxLength 32, index `ix_space_stations_status`; `RawData` column type `jsonb`; timestamps required/optional per data-model.md; FK: `HasOne(s => s.StarSystem).WithMany().HasForeignKey(s => s.StarSystemId).OnDelete(DeleteBehavior.Restrict)` with index on `StarSystemId`; composite index on `(IsAvailable, IsDecommissioned, Name)` for the list query

- [x] T012 Edit the existing EF configuration for `WarehouseInventoryEntry` (find via `grep -r "class WarehouseInventoryEntryConfiguration" backend/src`) to add: `builder.HasOne(e => e.Station).WithMany().HasForeignKey(e => e.StationId).OnDelete(DeleteBehavior.Restrict).IsRequired(false)` — leave all other configuration untouched

- [x] T013 Edit the existing EF configuration for `WarehouseMaterialEntry` (find via `grep -r "class WarehouseMaterialEntryConfiguration" backend/src`) to add the same `Station` FK configuration as T012 — leave all other configuration untouched

- [x] T014 Run `dotnet ef migrations add AddStarSystemsAndStationCatalog --project backend/src/NajaEcho.Infrastructure --startup-project backend/src/NajaEcho.Api` from the repo root to generate the EF migration. Verify the generated migration file in `backend/src/NajaEcho.Infrastructure/Persistence/Migrations/` creates `sc.star_systems`, `sc.space_stations`, and adds `station_id` columns to both warehouse tables. Confirm `Down()` reverses it cleanly.

- [x] T015 Create `backend/src/NajaEcho.Infrastructure/Locations/UexLocationClient.cs` implementing `IUexLocationClient`. Pattern: copy `UexVehicleClient.cs` exactly (find via `grep -r "class UexVehicleClient" backend/src`). `FetchAllStarSystemsAsync` calls `GET "star_systems"` (relative path); `FetchAllSpaceStationsAsync` calls `GET "space_stations"`. Both call `EnsureSuccessStatusCode()`, parse the `data` array, and return `IReadOnlyList<JsonDocument>`. Constructor takes `(HttpClient httpClient, ILogger<UexLocationClient> logger)`.

- [x] T016 Create `backend/src/NajaEcho.Infrastructure/Locations/StarSystemRepository.cs` implementing `IStarSystemRepository`. Copy `ShipRepository.BulkUpsertAsync` logic (find via `grep -r "class ShipRepository" backend/src`): open a transaction on `AppDbContext`; dedupe incoming records by `uex_id` (last wins); for each existing `StarSystem` with matching `uex_id`: update `Name`, `Code`, `IsAvailable`, `IsVisible`, `RawData`, `UpdatedAt = DateTimeOffset.UtcNow`; if `Status == CatalogStatus.SoftDeleted` → set `Status = CatalogStatus.Active`, `SoftDeletedAt = null` → reactivated count; else → updated count; for new `uex_id`s: insert new `StarSystem` rows with `Status = CatalogStatus.Active`, `ImportedAt = DateTimeOffset.UtcNow` → added count; for existing `Active` rows whose `uex_id` is absent from the incoming set: set `Status = CatalogStatus.SoftDeleted`, `SoftDeletedAt = DateTimeOffset.UtcNow` → softDeleted count; `SaveChangesAsync`; commit. `GetActiveUexIdToIdMapAsync` returns `Dictionary<int,Guid>` of all rows where `Status == CatalogStatus.Active`, keyed by `UexId`.

- [x] T017 Create `backend/src/NajaEcho.Infrastructure/Locations/SpaceStationRepository.cs` implementing `ISpaceStationRepository`. `BulkUpsertAsync` copies the `StarSystemRepository` pattern from T016 but also: for each incoming station record, extract `id_star_system` (int) from the `JsonDocument`, look it up in `starSystemMap`; if not found → increment `skipped` count and `continue`; otherwise resolve `StarSystemId` (Guid) and proceed with upsert. `SearchActiveStationsAsync(search, limit)`: query `sc.space_stations` where `Status == Active AND IsAvailable == true AND IsDecommissioned == false`; if `search` is non-empty apply `.Where(s => EF.Functions.ILike(s.Name, $"%{search}%"))` (case-insensitive); order by `Name`; take `Math.Clamp(limit, 1, 100)`; project to `StationDto(Id, Name)`. `ExistsAsync(id)`: `AnyAsync(s => s.Id == id && s.Status == CatalogStatus.Active)`.

- [x] T018 Edit `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` to register: `services.AddHttpClient<IUexLocationClient, UexLocationClient>(client => client.BaseAddress = new Uri(config["UexVehicleClient:BaseUrl"] ?? "https://api.uexcorp.uk/2.0/"))` (reuse the same config key as the existing vehicle client); `services.AddScoped<IStarSystemRepository, StarSystemRepository>()`; `services.AddScoped<ISpaceStationRepository, SpaceStationRepository>()`. Also register `ImportLocationsHandler`, `GetStationsHandler`, `TransferInventoryItemHandler`, and `TransferMaterialHandler` as scoped services (add these registrations now as stubs; the classes will be created in later phases).

**Checkpoint**: The solution builds. `dotnet build` is green. No tests yet.

---

## Phase 3: User Story 1 — Trigger Star Systems & Space Station Import (P1) 🎯 MVP

**Goal**: Admin triggers the import from the Data Import page; receives a per-entity summary.

**Independent Test**: `POST /api/admin/locations/import` succeeds when logged in as admin and returns
`{ starSystems: { added, updated, reactivated, softDeleted, total }, spaceStations: { ..., skipped } }`.
DB shows rows in `sc.star_systems` and `sc.space_stations`. A second run shifts counts to mostly `updated`.

### Tests — Write FIRST; confirm they FAIL before implementing T021–T026

- [x] T019 Create `backend/tests/NajaEcho.Application.Tests/Features/Locations/ImportLocationsHandlerTests.cs`. Use xUnit + FluentAssertions. Inject a `FakeUexLocationClient` (returns hardcoded `JsonDocument` lists) and `FakeStarSystemRepository` + `FakeSpaceStationRepository` (in-memory, capture calls). Write these test methods: (1) `HappyPath_ReturnsSeparateCountsForBothEntities` — feed has 3 star systems and 5 stations; verify result has `starSystems.Added == 3` and `spaceStations.Added == 5`; (2) `EmptyStarSystemsFeed_ThrowsEmptySourceException_NoWritesOccur` — fake returns empty list for star systems; verify `EmptySourceException` thrown and `BulkUpsertAsync` never called; (3) `EmptyStationsFeed_ThrowsEmptySourceException_NoWritesOccur` — same but empty stations list; (4) `UnreachableSource_HttpRequestException_Propagates` — fake throws `HttpRequestException`; verify it propagates; (5) `StationWithUnknownParentStarSystem_IsSkippedAndCounted` — station record has `id_star_system = 999` not in map; verify `spaceStations.Skipped == 1` and station not inserted; (6) `SoftDeletesAbsentRecords` — existing Active record has `uex_id` not in feed; verify it is soft-deleted via fake repo returning `softDeleted == 1`

- [x] T020 Create `backend/tests/NajaEcho.Api.Tests/Features/Locations/LocationAdminEndpointsTests.cs` using `WebApplicationFactory`. Write: (1) `Import_AsAdmin_Returns200WithSummary` — POST as admin, assert 200, response body matches `ImportLocationsResponse` schema (starSystems + spaceStations objects); (2) `Import_NotAuthenticated_Returns401`; (3) `Import_AuthenticatedNonAdmin_Returns403`; (4) `Import_WhenImportAlreadyInProgress_Returns409WithProblemJson` — stub `IImportCoordinator` to return false; (5) `Import_WhenSourceUnreachable_Returns502WithProblemJson` — stub `IUexLocationClient` to throw `HttpRequestException`; (6) `Import_WhenSourceEmpty_Returns502WithProblemJson` — stub returns empty list

### Implementation

- [x] T021 [P] Create `backend/src/NajaEcho.Application/Features/Locations/ImportLocations/ImportLocationsCommand.cs` as `public sealed record ImportLocationsCommand()` — no properties; it is a marker command (the import takes no parameters from the caller)

- [x] T022 [P] Create `backend/src/NajaEcho.Application/Features/Locations/ImportLocations/ImportLocationsResult.cs` with: `public sealed record EntityImportCounts(int Added, int Updated, int Reactivated, int SoftDeleted, int Total)` and `public sealed record StationImportCounts(int Added, int Updated, int Reactivated, int SoftDeleted, int Skipped, int Total) : EntityImportCounts(Added, Updated, Reactivated, SoftDeleted, Total)` and `public sealed record ImportLocationsResult(EntityImportCounts StarSystems, StationImportCounts SpaceStations)`

- [x] T023 Create `backend/src/NajaEcho.Application/Features/Locations/ImportLocations/ImportLocationsHandler.cs`. Constructor takes `(IUexLocationClient uexClient, IStarSystemRepository starSystemRepo, ISpaceStationRepository stationRepo, IImportCoordinator coordinator, ILogger<ImportLocationsHandler> logger)`. `HandleAsync(ImportLocationsCommand cmd, CancellationToken ct)` algorithm: (1) `if (!await coordinator.TryAcquireAsync()) throw new ImportAlreadyInProgressException()`; (2) `try { starSystemDocs = await uexClient.FetchAllStarSystemsAsync(ct); if (!starSystemDocs.Any()) throw new EmptySourceException("star systems"); (3) systemCounts = await starSystemRepo.BulkUpsertAsync(starSystemDocs, ct); (4) starSystemMap = await starSystemRepo.GetActiveUexIdToIdMapAsync(ct); (5) stationDocs = await uexClient.FetchAllSpaceStationsAsync(ct); if (!stationDocs.Any()) throw new EmptySourceException("space stations"); (6) stationCounts = await stationRepo.BulkUpsertAsync(stationDocs, starSystemMap, ct); (7) Log.Information("Locations import complete: systems={@SystemCounts} stations={@StationCounts}", systemCounts, stationCounts); return new ImportLocationsResult(systemCounts, stationCounts); } finally { coordinator.Release(); }` — the DB transaction is owned by the repositories (EF Core share of the scoped DbContext ensures atomicity)

- [x] T024 [P] Create `backend/src/NajaEcho.Api/Features/Admin/Locations/Contracts/ImportLocationsResponse.cs` as a record mirroring the OpenAPI schema: `public sealed record EntityImportCountsResponse(int Added, int Updated, int Reactivated, int SoftDeleted, int Total)` and `public sealed record StationImportCountsResponse(int Added, int Updated, int Reactivated, int SoftDeleted, int Skipped, int Total)` and `public sealed record ImportLocationsResponse(EntityImportCountsResponse StarSystems, StationImportCountsResponse SpaceStations)` — maps from `ImportLocationsResult` application record

- [x] T025 Create `backend/src/NajaEcho.Api/Features/Admin/Locations/LocationAdminEndpoints.cs`. Method `MapLocationAdminEndpoints(IEndpointRouteBuilder app)`: `var group = app.MapGroup("/api/admin/locations").RequireAuthorization(AuthorizationPolicies.Admin)`. Register `POST /import`: call `handler.HandleAsync(new ImportLocationsCommand(), ct)`; on success return `Results.Ok(MapToResponse(result))`; on `ImportAlreadyInProgressException` return `Results.Conflict(new ProblemDetails { Status=409, Title="Import already in progress" })`; on `EmptySourceException ex` return `Results.Problem(statusCode:502, title:"Source returned empty data", detail:ex.Message)`; on `HttpRequestException ex` return `Results.Problem(statusCode:502, title:"UEX source unreachable", detail:ex.Message)` — copy the error-mapping style of `ShipAdminEndpoints` (find via `grep -r "class ShipAdminEndpoints" backend/src`)

- [x] T026 Edit the API composition file (find via `grep -r "MapLocationAdminEndpoints\|MapShipAdminEndpoints" backend/src/NajaEcho.Api`) to call `app.MapLocationAdminEndpoints()` alongside the existing ship admin endpoint registration

### Frontend — Locations Import Tab

- [x] T027 [P] Create `frontend/src/features/admin/api/locationsApi.ts`. Export `importLocations(): Promise<ImportLocationsResponse>` that calls `apiFetch<ImportLocationsResponse>("/api/admin/locations/import", { method: "POST" })` — copy the pattern from the existing `shipsApi.ts` (find via `find frontend/src -name "shipsApi.ts"`)

- [x] T028 [P] Create `frontend/src/features/admin/schemas/locationSchemas.ts`. Define Zod schemas matching the OpenAPI `ImportLocationsResponse`: `const entityImportCountsSchema = z.object({ added: z.number(), updated: z.number(), reactivated: z.number(), softDeleted: z.number(), total: z.number() })` and `const stationImportCountsSchema = entityImportCountsSchema.extend({ skipped: z.number() })` and `export const importLocationsResponseSchema = z.object({ starSystems: entityImportCountsSchema, spaceStations: stationImportCountsSchema })` — copy the schema style from `shipSchemas.ts`

- [x] T029 [P] Create `frontend/src/features/admin/hooks/useImportLocations.ts`. Export `useImportLocations()` returning a TanStack Query `useMutation` that calls `importLocations()`, parses the response with `importLocationsResponseSchema.parse(data)`, and invalidates any relevant query keys on success. Pattern: copy `useImportShips.ts` (find via `find frontend/src -name "useImportShips.ts"`)

- [x] T030 Create `frontend/src/features/admin/components/LocationsImportTab.tsx`. This is a React component with: an "Import Locations" button that calls `mutate()` from `useImportLocations()`; a loading state (disable button + show spinner while `isPending`); on success, render a summary panel showing two sections ("Star Systems" and "Space Stations") each listing Added / Updated / Reactivated / Soft Deleted / Total counts, plus a "Skipped" count for Space Stations; on error, render an error message from the mutation error. Pattern: copy `ShipsImportTab.tsx` structure exactly (find via `find frontend/src -name "ShipsImportTab.tsx"`)

- [x] T031 Edit `frontend/src/features/admin/pages/DataImportPage.tsx` to add a new tab: find the existing `<Tabs>` element and add a tab trigger `<TabsTrigger value="locations">Locations</TabsTrigger>` and its content `<TabsContent value="locations"><LocationsImportTab /></TabsContent>` — import `LocationsImportTab` from `../components/LocationsImportTab`

- [x] T032 Create `frontend/src/features/admin/__tests__/importLocations.test.tsx` using Vitest + React Testing Library + MSW. Write: (1) renders import button; (2) clicking button shows loading state and calls the API; (3) on success renders per-entity summary with star system and space station count sections including the skipped count; (4) on API error renders an error message; (5) on empty-source 502 renders the error detail from the Problem JSON

**Checkpoint**: US1 fully functional. Admin can trigger import and see summary. `dotnet test` and `npm test` are green.

---

## Phase 4: User Story 2 — Select Station Location When Adding a Warehouse Entry (P2)

**Goal**: The Location field in all three warehouse add dialogs shows a searchable station combobox.

**Independent Test**: `GET /api/warehouse/stations?search=ARC&limit=10` returns only available, non-decommissioned stations. Adding an inventory item with a valid `stationId` persists it. Adding without `stationId` still works.

### Tests — Write FIRST; confirm they FAIL before implementing T036–T043

- [x] T033 Create `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/GetStationsHandlerTests.cs`. Write: (1) `ReturnsOnlyAvailableNonDecommissionedStations` — fake repo returns mix of available/decommissioned/soft-deleted; verify only available+non-decommissioned returned; (2) `FiltersStationsByName` — search="ARC"; verify only matching stations returned; (3) `ClampsLimitToMax100` — limit=200; verify Math.Clamp applied; (4) `ReturnsEmptyListWhenNoCatalogExists`

- [x] T034 Create `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/AddInventoryItemHandlerTests.cs` additions (or a new file `AddInventoryItemStationTests.cs`). Write: (1) `AddItem_WithValidStationId_PersistsStationId` — fake `ISpaceStationRepository.ExistsAsync` returns true; verify `WarehouseInventoryEntry.StationId` set; (2) `AddItem_WithInvalidStationId_ThrowsValidationException` — fake returns false; verify validation error; (3) `AddItem_WithNullStationId_PersistsNullStationId` — no stationId; verify null stored, no ExistsAsync call

- [x] T035 Create `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseStationEndpointsTests.cs`. Write: (1) `GetStations_AsAuthenticatedMember_Returns200WithStationList`; (2) `GetStations_Unauthenticated_Returns401`; (3) `GetStations_WithSearchParam_ReturnsFilteredStations`; (4) `AddInventoryItem_WithValidStationId_Returns201`; (5) `AddInventoryItem_WithInvalidStationId_Returns400WithProblemJson`

### Implementation

- [x] T036 Create `backend/src/NajaEcho.Application/Features/Warehouse/GetStations/GetStationsQuery.cs` as `public sealed record GetStationsQuery(string? Search, int Limit = 25)` and `public sealed record StationDto(Guid Id, string Name)` in the same file (or in a `StationDto.cs` next to it). If `ISpaceStationRepository` in T008 referenced `StationDto`, update the import namespace now.

- [x] T037 Create `backend/src/NajaEcho.Application/Features/Warehouse/GetStations/GetStationsHandler.cs`. Constructor: `(ISpaceStationRepository repo, ILogger<GetStationsHandler> logger)`. `HandleAsync(GetStationsQuery query, CancellationToken ct)`: call `await repo.SearchActiveStationsAsync(query.Search, Math.Clamp(query.Limit, 1, 100), ct)`; log at Debug level; return the list.

- [x] T038 Edit the existing `AddInventoryItemCommand` (find via `grep -r "class AddInventoryItemCommand\|record AddInventoryItemCommand" backend/src`) to add property `Guid? StationId` — this is an additive change; all existing properties remain

- [x] T039 Edit the existing `AddInventoryItemHandler` (find via `grep -r "class AddInventoryItemHandler" backend/src`) to: (1) inject `ISpaceStationRepository stationRepo` in the constructor; (2) before persisting, if `command.StationId.HasValue` call `if (!await stationRepo.ExistsAsync(command.StationId.Value, ct)) throw new ValidationException("StationId", "Station not found")` (use the project's existing validation exception pattern); (3) set `entry.StationId = command.StationId` when creating/updating the warehouse entry — leave all other handler logic untouched

- [x] T040 Edit the existing `AddMaterialCommand` (find via `grep -r "class AddMaterialCommand\|record AddMaterialCommand" backend/src`) to add property `Guid? StationId` — same pattern as T038

- [x] T041 Edit the existing `AddMaterialHandler` (find via `grep -r "class AddMaterialHandler" backend/src`) to add the same stationId validation and persistence as T039

- [x] T042 Edit `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs` to add: `group.MapGet("/stations", async (GetStationsHandler handler, [FromQuery] string? search, [FromQuery] int limit = 25, CancellationToken ct = default) => { var stations = await handler.HandleAsync(new GetStationsQuery(search, limit), ct); return Results.Ok(new StationListResponse(stations.Select(s => new StationOption(s.Id, s.Name)).ToList())); })` — at the group level (any authenticated member); add `StationListResponse` and `StationOption` DTO records to `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/`

- [x] T043 Edit `WarehouseEndpoints.cs` to update the existing POST `/items` and POST `/materials` route handlers to bind and pass `stationId` from the request body into their respective commands — the add-item request body DTO already has `StationId?` per the OpenAPI contract (update `AddInventoryItemRequest` and `AddMaterialRequest` API-layer contracts in `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/` to add the `Guid? StationId` field)

### Frontend — Station Combobox

- [x] T044 [P] Create `frontend/src/features/warehouse/schemas/stationSchemas.ts`. Define: `export const stationOptionSchema = z.object({ id: z.string().uuid(), name: z.string() })` and `export const stationListResponseSchema = z.object({ stations: z.array(stationOptionSchema) })` and `export const transferStationRequestSchema = z.object({ stationId: z.string().uuid() })` and `export type StationOption = z.infer<typeof stationOptionSchema>`

- [x] T045 [P] Create `frontend/src/features/warehouse/api/stationsApi.ts`. Export: `searchStations(search?: string, limit = 25): Promise<StationOption[]>` calling `apiFetch<StationListResponse>(\`/api/warehouse/stations?${new URLSearchParams({ ...(search ? { search } : {}), limit: String(limit) })}\`)` then parsing + returning `data.stations`; `transferItemStation(id: string, stationId: string): Promise<void>` calling `apiFetch(\`/api/warehouse/items/${id}/station\`, { method: "PUT", body: JSON.stringify({ stationId }) })`; `transferMaterialStation(id: string, stationId: string): Promise<void>` same but `/materials/${id}/station`

- [x] T046 [P] Create `frontend/src/features/warehouse/hooks/stationKeys.ts` exporting a query key factory: `export const stationKeys = { all: ["stations"] as const, search: (search?: string, limit?: number) => [...stationKeys.all, "search", search, limit] as const }` — mirrors the key factory pattern used elsewhere in `features/warehouse/hooks/`

- [x] T047 [P] Create `frontend/src/features/warehouse/hooks/useStationSearch.ts`. Export `useStationSearch(search?: string, limit = 25)` returning a TanStack Query `useQuery({ queryKey: stationKeys.search(search, limit), queryFn: () => searchStations(search, limit), staleTime: 5 * 60 * 1000 })`. The component handles debouncing externally so this hook is straightforward.

- [x] T048 Create `frontend/src/features/warehouse/components/StationCombobox.tsx`. Props interface: `{ value?: string; onValueChange: (id: string, name: string) => void; placeholder?: string; disabled?: boolean }`. Implementation: use the existing `Popover`, `Command`, `CommandInput`, `CommandList`, `CommandEmpty`, `CommandItem` from `components/ui/` (same pattern as other comboboxes in the warehouse feature — find one via `grep -r "CommandInput" frontend/src/features/warehouse`). Maintain local `search` state string. Debounce search with `useDebounce` or a `useEffect`+`setTimeout` (250ms) before passing to `useStationSearch`. Display the selected station `name` as the trigger button label (or `placeholder` if none selected). Each `CommandItem` shows `station.name`; on select call `onValueChange(station.id, station.name)`.

- [x] T049 Edit `frontend/src/features/warehouse/components/AddInventoryDialog.tsx` (find via `find frontend/src -name "AddInventoryDialog.tsx"`): (1) add `stationId` field to the Zod form schema (optional uuid string); (2) add `<StationCombobox>` below the existing Location field; wire its `value` and `onValueChange` to the form field using `Controller` from React Hook Form; (3) include `stationId` in the submit payload to the add-item API call — leave all other dialog behavior untouched

- [x] T050 Edit `frontend/src/features/warehouse/components/AddMaterialDialog.tsx` (find via `find frontend/src -name "AddMaterialDialog.tsx"`) — same three changes as T049 but for the material dialog

- [x] T051 Create `frontend/src/features/warehouse/__tests__/stationCombobox.test.tsx` using Vitest + RTL + MSW. Write: (1) renders with placeholder when no value; (2) shows station list when opened; (3) typing in search box calls the API with the search term and renders filtered results; (4) clicking a station option calls `onValueChange` with the station id and name; (5) empty response renders "No stations found" message

**Checkpoint**: US2 fully functional. Station combobox appears in all three add dialogs. `dotnet test` and `npm test` green.

---

## Phase 5: User Story 3 — Transfer a Warehouse Entry to a New Station Location (P3)

**Goal**: Each warehouse row has a Transfer action opening a modal; confirms updates `station_id` only.

**Independent Test**: Click Transfer on any warehouse row, pick a station, confirm — row's station reference updates. Repeating Transfer on another row pre-selects the last station (session state). Cancel leaves row unchanged.

### Tests — Write FIRST; confirm they FAIL before implementing T055–T064

- [x] T052 Create `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/TransferInventoryItemHandlerTests.cs`. Write: (1) `Transfer_WithValidRowAndStation_SetsStationId` — fake repo has the row; verify `StationId` set to new value; (2) `Transfer_WithUnknownRow_ThrowsNotFoundException`; (3) `Transfer_WithInvalidStationId_ThrowsValidationException` — `stationRepo.ExistsAsync` returns false; (4) `Transfer_DoesNotModifyLocationField` — verify the `Location` string on the entry is unchanged after transfer

- [x] T053 Create `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/TransferMaterialHandlerTests.cs` with the same four test cases as T052 but for `WarehouseMaterialEntry`

- [x] T054 Create `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseTransferEndpointsTests.cs`. Write: (1) `TransferItem_AsQuartermaster_Returns204`; (2) `TransferItem_Unauthenticated_Returns401`; (3) `TransferItem_AuthenticatedNonQuartermaster_Returns403`; (4) `TransferItem_UnknownRowId_Returns404WithProblemJson`; (5) `TransferItem_InvalidStationId_Returns400WithProblemJson`; (6) `TransferMaterial_AsQuartermaster_Returns204`; (7) `TransferMaterial_UnknownRowId_Returns404WithProblemJson`

### Implementation

- [x] T055 Create `backend/src/NajaEcho.Application/Features/Warehouse/TransferInventoryItem/TransferInventoryItemCommand.cs` as `public sealed record TransferInventoryItemCommand(Guid RowId, Guid StationId)` and `TransferInventoryItemHandler.cs`: constructor `(IWarehouseInventoryRepository repo, ISpaceStationRepository stationRepo, ILogger<TransferInventoryItemHandler> logger)`; `HandleAsync`: (1) `if (!await stationRepo.ExistsAsync(cmd.StationId, ct)) throw new ValidationException("StationId", "Station not found")`; (2) `var entry = await repo.GetByIdAsync(cmd.RowId, ct) ?? throw new NotFoundException("Warehouse item", cmd.RowId)`; (3) `entry.StationId = cmd.StationId` (do NOT touch `entry.Location`); (4) `await repo.SaveChangesAsync(ct)`; (5) Log at Information level with rowId and stationId. Find the existing `IWarehouseInventoryRepository` interface via `grep -r "IWarehouseInventoryRepository" backend/src`.

- [x] T056 Create `backend/src/NajaEcho.Application/Features/Warehouse/Materials/TransferMaterial/TransferMaterialCommand.cs` as `public sealed record TransferMaterialCommand(Guid RowId, Guid StationId)` and `TransferMaterialHandler.cs` — same logic as T055 but targets `WarehouseMaterialEntry` via `IWarehouseMaterialRepository`

- [x] T057 Edit `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs` to add two new routes inside the existing `/api/warehouse` group: (1) `group.MapPut("/items/{id}/station", ...).RequireAuthorization(AuthorizationPolicies.Quartermaster)` — binds `id` (Guid) from path and `TransferStationRequest` from body; calls `transferItemHandler.HandleAsync(new TransferInventoryItemCommand(id, req.StationId), ct)`; returns `Results.NoContent()` on success; (2) same for `/materials/{id}/station` using `TransferMaterialHandler`. Map `NotFoundException` → 404 problem and `ValidationException` → 400 problem (use the same error-mapping helper as the rest of `WarehouseEndpoints`). Add `TransferStationRequest` record `(Guid StationId)` to `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/`

### Frontend — Transfer Modal

- [x] T058 [P] Create `frontend/src/features/warehouse/hooks/useLastTransferStation.ts`. This is client-side session state only (no server persistence, per R6). Use React `useRef` or a module-level variable to persist across component mounts within the same session. Export: `useLastTransferStation(): { lastStation: StationOption | undefined; setLastStation: (s: StationOption) => void }`. Simplest implementation: a module-level variable `let lastStation: StationOption | undefined = undefined` with a custom hook wrapping a forced re-render pattern, or use a small Zustand/context store if the project already has one — check if context exists first.

- [x] T059 [P] Create `frontend/src/features/warehouse/hooks/useTransferItemStation.ts`. Export `useTransferItemStation()` returning `useMutation({ mutationFn: ({ id, stationId }: { id: string; stationId: string }) => transferItemStation(id, stationId), onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["inventory"] }); queryClient.invalidateQueries({ queryKey: ["shipComponents"] }) } })` — use the existing query key constants for inventory and ship-components (find via `grep -r "inventoryKeys\|shipComponentKeys" frontend/src`)

- [x] T060 [P] Create `frontend/src/features/warehouse/hooks/useTransferMaterialStation.ts` — same pattern as T059 but calls `transferMaterialStation` and invalidates the materials query key

- [x] T061 Create `frontend/src/features/warehouse/components/TransferStationDialog.tsx`. Props: `{ open: boolean; onOpenChange: (open: boolean) => void; rowId: string; entityType: "item" | "material"; onSuccess?: () => void }`. Implementation: (1) use `Dialog`, `DialogContent`, `DialogHeader`, `DialogTitle`, `DialogFooter` from `components/ui/dialog`; (2) render `<StationCombobox>` with `value={selectedStation?.id}` and `onValueChange={(id, name) => setSelectedStation({ id, name })}`; (3) on mount/open set `selectedStation` to `lastStation` from `useLastTransferStation` (pre-select default, FR-014); (4) Confirm button: disabled if no station selected or mutation isPending; calls the appropriate mutation based on `entityType`; on mutation success: call `setLastStation(selectedStation)`, call `onSuccess?.()`, close dialog; (5) Cancel button: close dialog without changes; (6) show mutation error inline if transfer fails

- [x] T062 Edit the inventory table component (find via `grep -r "InventoryTable\|inventory.*Table" frontend/src/features/warehouse` and identify the correct file) to add a "Transfer" row action button in each row's action column; clicking it opens `<TransferStationDialog open={...} rowId={row.id} entityType="item" />` with local `transferDialogOpen` state; import `TransferStationDialog`

- [x] T063 Edit the ship components table component (find via `grep -r "ShipComponent.*Table\|shipComponent.*Table" frontend/src/features/warehouse`) to add the same Transfer row action as T062 with `entityType="item"` (same physical table)

- [x] T064 Edit the materials table component (find via `grep -r "Material.*Table\|material.*Table" frontend/src/features/warehouse`) to add the Transfer row action with `entityType="material"` and open `TransferStationDialog` with `entityType="material"`

- [x] T065 Create `frontend/src/features/warehouse/__tests__/transferStationDialog.test.tsx` using Vitest + RTL + MSW. Write: (1) dialog renders with station combobox when `open=true`; (2) Confirm button is disabled when no station selected; (3) selecting a station enables Confirm; (4) clicking Confirm calls the transfer API and closes dialog on success; (5) opening dialog a second time pre-selects the last station from the previous transfer (FR-014); (6) clicking Cancel closes dialog and does not call the API

**Checkpoint**: US3 fully functional. All three warehouse tables have Transfer action. `dotnet test` and `npm test` green.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Testcontainers integration tests through the real PostgreSQL tables, and a final end-to-end validation run.

- [x] T066 Create `backend/tests/NajaEcho.Infrastructure.Tests/Locations/LocationCatalogIntegrationTests.cs` using xUnit + Testcontainers (PostgreSQL container). Write tests against the real database with the real EF Core schema: (1) `StarSystem_UexId_UniqueConstraint_Rejected` — insert two StarSystem rows with the same `uex_id`; verify `DbUpdateException`; (2) `SpaceStation_FK_RejectsUnknownStarSystem` — insert SpaceStation with non-existent `star_system_id`; verify constraint exception; (3) `WarehouseInventory_StationId_NullableFK_Accepted` — insert WarehouseInventoryEntry with `station_id = null`; verify it persists; insert with a valid station id; verify FK enforced (no hard-delete → no cascade issues); (4) `WarehouseMaterial_StationId_NullableFK_Accepted` — same for materials table

- [x] T067 Verify Serilog structured logging is emitted in all new handlers: open `ImportLocationsHandler.cs`, `GetStationsHandler.cs`, `TransferInventoryItemHandler.cs`, `TransferMaterialHandler.cs` and confirm each has at least one `Log.Information` or `_logger.LogInformation` call with a meaningful structured message including the caller context (rowId, stationId, or import counts) — add any missing log statements

- [x] T068 Run the quickstart.md validation scenarios end-to-end: apply the migration via `./migrate.sh`, run backend + frontend, execute Scenario 1 (import), Scenario 2 (station combobox add), Scenario 3 (Transfer modal + last-selection default), and all error/abort paths listed in quickstart.md. Confirm `dotnet test` and `npm test` are fully green.

**Checkpoint**: All phases complete. All tests pass. All quickstart scenarios validated.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately; T002–T005 can run in parallel
- **Foundational (Phase 2)**: Depends on Phase 1 completion — **BLOCKS all user stories**
- **US1 (Phase 3)**: Depends on Phase 2 completion; tests (T019–T020) written before impl (T021–T032)
- **US2 (Phase 4)**: Depends on Phase 2 completion; can run in parallel with US1 if staffed
- **US3 (Phase 5)**: Depends on Phase 2 + ideally US2 (`StationCombobox` is reused in Transfer modal)
- **Polish (Phase 6)**: Depends on all story phases complete

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2 — no dependency on US2 or US3
- **US2 (P2)**: Can start after Phase 2 — no dependency on US1 (catalog must be populated at runtime but the code is independent)
- **US3 (P3)**: Depends on US2 (`StationCombobox` is reused in `TransferStationDialog`)

### Within Each User Story

1. Tests MUST be written and FAIL before production code
2. Application ports (Phase 2) before handler implementations
3. Handler before API endpoint
4. Backend endpoint before frontend API wrapper
5. Frontend API wrapper before hooks before components

### Parallel Opportunities

- Phase 1: T002, T003, T004, T005 — all parallel (different files)
- Phase 2: T006, T007, T008, T009 all parallel; T010, T011, T012, T013 parallel; T015, T016, T017 parallel after T014
- Phase 3: T019, T020 parallel; T021, T022, T024 parallel; T027, T028, T029 parallel after backend complete
- Phase 4: T033, T034, T035 parallel; T036, T044, T045, T046, T047 parallel after T035
- Phase 5: T052, T053, T054 parallel; T058, T059, T060 parallel; T062, T063, T064 parallel after T061

---

## Parallel Example: US1

```bash
# Step 1 — write tests in parallel (both fail):
T019: ImportLocationsHandlerTests.cs
T020: LocationAdminEndpointsTests.cs

# Step 2 — implement application layer in parallel:
T021: ImportLocationsCommand.cs
T022: ImportLocationsResult.cs
T024: ImportLocationsResponse.cs (API DTO)

# Step 3 — implement handler (depends on T021+T022):
T023: ImportLocationsHandler.cs

# Step 4 — implement endpoint + register (depends on T023+T024):
T025: LocationAdminEndpoints.cs
T026: Register in API composition

# Step 5 — implement frontend in parallel (after T025 is merged/available):
T027: locationsApi.ts
T028: locationSchemas.ts
T029: useImportLocations.ts
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Domain entities
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Write tests T019–T020 (confirm failure)
4. Complete Phase 3: US1 implementation (T021–T031)
5. **STOP and VALIDATE**: Admin can trigger import and see the summary
6. `dotnet test` green; `npm test` green for US1 tests

### Incremental Delivery

1. Setup + Foundational → compile and migrate
2. US1 → admin import works end-to-end (MVP)
3. US2 → warehouse add dialogs have station combobox
4. US3 → Transfer modal wired up on all three warehouse tables
5. Polish → integration tests + final validation

### Note on Existing Code

The following files are EDITED (not created):
- `WarehouseInventoryEntry`, `WarehouseMaterialEntry` — add `StationId` property (T004, T005)
- `WarehouseInventoryEntryConfiguration`, `WarehouseMaterialEntryConfiguration` — add FK config (T012, T013)
- `AddInventoryItemCommand`, `AddInventoryItemHandler` — add `StationId` (T038, T039)
- `AddMaterialCommand`, `AddMaterialHandler` — add `StationId` (T040, T041)
- `WarehouseEndpoints.cs` — add 4 new routes (T042, T043, T057)
- `DataImportPage.tsx` — add Locations tab (T031)
- `AddInventoryDialog.tsx`, `AddMaterialDialog.tsx` — add StationCombobox (T049, T050)
- `InventoryTable`, `ShipComponentsTable`, `MaterialsTable` — add Transfer action (T062, T063, T064)
- `DependencyInjection.cs` — register new services (T018)

---

## Notes

- `[P]` tasks operate on different files with no incomplete-task dependencies
- `[US#]` label maps to the user story in spec.md for traceability
- Constitution Principle II (TDD) is NON-NEGOTIABLE: tests in T019, T020, T033–T035, T052–T054 MUST fail before their corresponding implementations are written
- `station_id` is nullable; never coerce it to a non-null value on existing rows (SC-007)
- The Transfer action MUST NOT write to the `location` (free-text) column (FR-011, assumption)
- The "last station" default in the Transfer modal is session/component state only — no server persistence (R6)
- All EF queries touching `sc.space_stations` for the combobox MUST filter `is_available = true AND is_decommissioned = false` (FR-007)
