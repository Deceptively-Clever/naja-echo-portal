# Tasks: Warehouse Materials Subpage

**Input**: Design documents from `/specs/014-warehouse-materials/`

**Branch**: `014-warehouse-materials` | **Date**: 2026-06-15

**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/openapi.yaml ✅ quickstart.md ✅

**Tests**: Included per Constitution II (TDD non-negotiable). Write tests FIRST, confirm they FAIL, then implement.

**Organization**: Tasks grouped by user story to enable independent implementation and delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other [P] tasks in the same phase (different files, no shared dependencies)
- **[Story]**: Which user story this task belongs to (US1/US2/US3/US4/US5)
- Exact file paths included in every task description

---

## Phase 1: Setup

**Purpose**: Wire up API type-generation for the new contract and generate the one missing shadcn primitive (Constitution I).

- [X] T001 Add `gen:api:materials` npm script to `frontend/package.json` pointing at `../specs/014-warehouse-materials/contracts/openapi.yaml` with output `src/lib/api/materials.d.ts` (mirror the existing `gen:api:*` pattern)
- [X] T002 Run `npm run gen:api:materials` from `frontend/` to emit `frontend/src/lib/api/materials.d.ts` (commit the generated file)
- [X] T003 [P] Generate the shadcn `slider` primitive (Radix `@radix-ui/react-slider`) into `frontend/src/components/ui/slider.tsx` via the shadcn CLI; keep it application-agnostic (no Materials-specific logic)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The new domain entity, EF configuration, migration, and core abstractions that ALL user stories depend on. Nothing in Phase 3+ can begin until this phase is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Create `WarehouseMaterialEntry.cs` domain entity in `backend/src/NajaEcho.Domain/Warehouse/` with `Id`, `CommodityId`, `OwnerUserId`, `Location`, `Quantity` (decimal), `Quality` (int, default 500), `CreatedAt`, `UpdatedAt` per data-model.md
- [X] T005 [P] Create `WarehouseMaterialEntryConfiguration.cs` in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/` — maps to `public.warehouse_material_inventory`, snake_case columns, `quantity` as `decimal(18,2)`, unique index `ux_warehouse_material_inventory_commodity_owner_location_quality` on `(commodity_id, owner_user_id, location, quality)`, check constraints `ck_warehouse_material_inventory_quantity` (`quantity > 0`) and `ck_warehouse_material_inventory_quality` (`quality between 1 and 1000`), indexes on `commodity_id` and `owner_user_id`, FK `fk_warehouse_material_inventory_commodity_id` → `sc.commodities(id)` `OnDelete Restrict`, `Location` `HasMaxLength(200)`
- [X] T006 Add `DbSet<WarehouseMaterialEntry>` to `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs` and register `WarehouseMaterialEntryConfiguration` in `OnModelCreating` (depends on T004, T005)
- [X] T007 Create EF Core migration `AddWarehouseMaterialInventory` via `dotnet ef migrations add AddWarehouseMaterialInventory --project src/NajaEcho.Infrastructure --startup-project src/NajaEcho.Api` from `backend/` and apply with `dotnet ef database update` (depends on T006)
- [X] T008 [P] Create `IMaterialInventoryRepository.cs` abstraction in `backend/src/NajaEcho.Application/Abstractions/` declaring `GetMaterialsAsync`, `GetMaterialFiltersAsync`, `SearchCommoditiesAsync`, `AddOrIncrementAsync`, `UpdateQuantityAsync`, `RemoveAsync`
- [X] T009 [P] Create `MaterialDtos.cs` in `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/` with all request/response records per `contracts/openapi.yaml`: `MaterialRowResponse`, `MaterialListResponse`, `MaterialFiltersResponse`, `OwnerOption`, `CommodityCatalogResponse`, `CommodityCatalogItem`, `AddMaterialRequest`, `ChangeMaterialQuantityRequest`

**Checkpoint**: Foundation complete — all five user story phases can begin.

---

## Phase 3: User Story 1 — View Materials Inventory (Priority: P1) 🎯 MVP

**Goal**: Any authenticated user can navigate to Warehouse → Materials and see all material inventory rows (Material, Owner, Location, Quantity, Quality) sorted by the default multi-key order, with empty-inventory state and anonymous-access denial.

**Independent Test**: Navigate to Warehouse → Materials as an authenticated user and confirm the list renders the five columns in the default sort order; hit the route while signed out and confirm the same denial behavior as the Items page; with no rows, confirm the empty state.

### Tests for User Story 1 — Write FIRST, confirm FAIL before implementing

- [X] T010 [P] [US1] Create `GetMaterialsQueryHandlerTests.cs` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/Materials/GetMaterials/` — tests: handler delegates to `IMaterialInventoryRepository.GetMaterialsAsync` with no filters and returns mapped `MaterialRowDto` rows including decimal `Quantity` and `Quality`
- [X] T011 [P] [US1] Create `MaterialInventoryRepositoryTests.cs` in `backend/tests/NajaEcho.Infrastructure.Tests/Warehouse/` (Testcontainers/PostgreSQL) — tests: `quantity > 0` check constraint rejects `0`/negative inserts; `quality between 1 and 1000` check constraint rejects out-of-range inserts; unique index on `(commodity_id, owner_user_id, location, quality)` blocks a raw duplicate insert; `GetMaterialsAsync` with no filters returns all rows sorted Material name ↑ → Quality ↓ → Owner name ↑ → Location ↑
- [X] T012 [P] [US1] Create `MaterialsEndpointTests.cs` in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/` — tests: `GET /api/warehouse/materials` returns 200 for an authenticated user; returns 401/redirect for anonymous, matching the Items page behavior; response rows include all five fields with `Quantity` serialized as a 2-decimal-place number
- [X] T013 [P] [US1] Create `MaterialsTable.test.tsx` in `frontend/src/features/warehouse/__tests__/` — tests: renders Material, Owner, Location, Quantity, Quality columns; formats Quantity with exactly 2 decimal places; renders the "no material inventory" empty state when rows is empty

### Implementation for User Story 1

- [X] T014 [P] [US1] Create `GetMaterialsQuery.cs` (query + `MaterialRowDto`) and `GetMaterialsQueryHandler.cs` in `backend/src/NajaEcho.Application/Features/Warehouse/Materials/GetMaterials/` — handler calls `IMaterialInventoryRepository.GetMaterialsAsync`, maps `Quantity`/`Quality`/`OwnerDisplayName`/`MaterialName`/`MaterialCode`
- [X] T015 [US1] Implement `GetMaterialsAsync` on `MaterialInventoryRepository.cs` in `backend/src/NajaEcho.Infrastructure/Warehouse/` — SQL-projected join `warehouse_material_inventory` → `sc.commodities` → `AspNetUsers`; `ORDER BY commodity.name ASC, quality DESC, owner.display_name ASC, location ASC` (no filter params yet — added in US5)
- [X] T016 [US1] Register `IMaterialInventoryRepository`/`MaterialInventoryRepository` and `GetMaterialsQueryHandler` as `AddScoped` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`
- [X] T017 [US1] Add `GET /api/warehouse/materials` endpoint to `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs` — `.RequireAuthorization()`, dispatches `GetMaterialsQuery`, returns `MaterialListResponse`, logs via Serilog with caller id
- [X] T018 [P] [US1] Create `materialSchemas.ts` in `frontend/src/features/warehouse/schemas/` — Zod schemas for `MaterialRowResponse` and `MaterialListResponse`, typed from `frontend/src/lib/api/materials.d.ts`
- [X] T019 [P] [US1] Create `materialsApi.ts` in `frontend/src/features/warehouse/api/` — `apiFetch` wrapper `getMaterials()` calling `GET /api/warehouse/materials`
- [X] T020 [P] [US1] Extend `warehouseQueryKeys.ts` in `frontend/src/features/warehouse/hooks/` with a materials query key factory (list key, filters key)
- [X] T021 [US1] Create `useMaterials.ts` in `frontend/src/features/warehouse/hooks/` — TanStack Query `useQuery` over `materialsApi.getMaterials` (depends on T018–T020)
- [X] T022 [US1] Create `MaterialsTable.tsx` in `frontend/src/features/warehouse/components/` — renders 5-column table (Material, Owner, Location, Quantity, Quality); Quantity formatted to 2 decimal places; "no material inventory" empty state
- [X] T023 [US1] Create `MaterialsView.tsx` thin route page in `frontend/src/features/warehouse/pages/` — composes `useMaterials` + `MaterialsTable`; no business logic
- [X] T024 [US1] Update `frontend/src/routes/AppRouter.tsx` — replace the `/warehouse/materials` placeholder route with `<MaterialsView />` inside `ProtectedRoute` + `DashboardLayout` (nav item already exists in `navItems.ts`)

**Checkpoint**: User Story 1 complete — authenticated users can view material inventory in default sort order; anonymous users are denied; empty state renders.

---

## Phase 4: User Story 2 — Add Material (Priority: P2)

**Goal**: A Quartermaster (or Admin) can add material by searching `sc.commodities`, picking/defaulting Owner and Quality, entering Location (with suggestions) and Quantity, with increment-on-match and validation of Quantity/Quality.

**Independent Test**: Open the add dialog, select a commodity, complete the fields, save, and confirm a new row appears; repeat with identical Material+Owner+Location+Quality and confirm the existing row's quantity increases instead of a duplicate row.

### Tests for User Story 2 — Write FIRST, confirm FAIL before implementing

- [X] T025 [P] [US2] Create `SearchCommoditiesQueryHandlerTests.cs` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/Materials/SearchCommodities/` — tests: case-insensitive partial match over name and code; excludes soft-deleted commodities; respects `limit`
- [X] T026 [P] [US2] Create `AddMaterialHandlerTests.cs` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/Materials/AddMaterial/` — tests: throws `CommodityNotFoundException` for unknown commodity id; throws `OwnerNotFoundException` for unknown owner id; Owner defaults to caller when omitted; Quality defaults to `500` when omitted; Quantity rounded half-up to 2 places before validation (e.g. `0.004` → rejected as `0.00`); rejects Quantity `<= 0.00`; rejects Quality outside `1..1000`; calls `AddOrIncrementAsync` for a valid request
- [X] T027 [P] [US2] Extend `MaterialInventoryRepositoryTests.cs` in `backend/tests/NajaEcho.Infrastructure.Tests/Warehouse/` — tests: `AddOrIncrementAsync` inserts a new row and returns `IsNew = true` for a new key; a second call with the same `(commodity_id, owner_user_id, location, quality)` increments `quantity` and returns `IsNew = false` with no duplicate row; Quality is never altered by the conflict update
- [X] T028 [P] [US2] Extend `MaterialsEndpointTests.cs` in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/` — tests: `GET /api/warehouse/materials/catalog/search` returns 403 for a non-Quartermaster authenticated user, 200 for Quartermaster/Admin; `POST /api/warehouse/materials` returns 403 for non-Quartermaster, 201 for a new row, 200 for an incremented row, 400 for invalid Quantity/Quality, 404 for unknown commodity/owner
- [X] T029 [P] [US2] Create `AddMaterialDialog.test.tsx` in `frontend/src/features/warehouse/__tests__/` — tests: Owner select defaults to the current user; Quality input defaults to `500`; Location input shows suggestions from existing locations; commodity search restricts selection to `sc.commodities` results (no free-text material entry); validation messages render for Quantity `<= 0` and Quality outside `1..1000`; add control is absent for non-Quartermaster users

### Implementation for User Story 2

- [X] T030 [P] [US2] Create `SearchCommoditiesQuery.cs` + `SearchCommoditiesQueryHandler.cs` in `backend/src/NajaEcho.Application/Features/Warehouse/Materials/SearchCommodities/` — queries `sc.commodities` excluding soft-deleted, `ILIKE` over name/code, `LIMIT`, returns `CommodityResultDto`
- [X] T031 [P] [US2] Create `AddMaterialCommand.cs` + `AddMaterialHandler.cs` + `CommodityNotFoundException`/`OwnerNotFoundException` in `backend/src/NajaEcho.Application/Features/Warehouse/Materials/AddMaterial/` — rounds Quantity half-up to 2 places, validates `> 0.00` and Quality `1..1000`, defaults Owner to caller and Quality to `500`, calls `AddOrIncrementAsync`
- [X] T032 [US2] Create `GetMaterialFiltersQuery.cs` + `GetMaterialFiltersQueryHandler.cs` + `MaterialFiltersDto`/`OwnerOption` in `backend/src/NajaEcho.Application/Features/Warehouse/Materials/GetMaterialFilters/` — calls `IMaterialInventoryRepository.GetMaterialFiltersAsync`, used by the add dialog for Location suggestions (FR-016) and reused in full by US5's filter UI
- [X] T033 [US2] Implement `SearchCommoditiesAsync`, `AddOrIncrementAsync` (`INSERT … ON CONFLICT (commodity_id, owner_user_id, location, quality) DO UPDATE SET quantity = warehouse_material_inventory.quantity + EXCLUDED.quantity, updated_at = EXCLUDED.updated_at` returning `(xmax = 0) AS is_new`), and `GetMaterialFiltersAsync` (distinct Owners + Locations currently present in material inventory) on `MaterialInventoryRepository.cs`
- [X] T034 [US2] Register `SearchCommoditiesQueryHandler`, `AddMaterialHandler`, and `GetMaterialFiltersQueryHandler` as `AddScoped` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`; add `GET /api/warehouse/materials/catalog/search` and `GET /api/warehouse/materials/filters` (both `.RequireAuthorization()`/`.RequireAuthorization(AuthorizationPolicies.Quartermaster)` per `contracts/openapi.yaml`) and `POST /api/warehouse/materials` (`.RequireAuthorization(AuthorizationPolicies.Quartermaster)`) endpoints to `WarehouseEndpoints.cs`, mapping `CommodityNotFoundException`/`OwnerNotFoundException` to RFC-7807 `Results.Problem`
- [X] T035 [P] [US2] Extend `materialSchemas.ts` with Zod schemas for `MaterialFiltersResponse`, `OwnerOption`, `CommodityCatalogResponse`, `CommodityCatalogItem`, `AddMaterialRequest`
- [X] T036 [P] [US2] Extend `materialsApi.ts` with `getMaterialFilters()`, `searchCommodities(search, limit)`, and `addMaterial(request)` wrappers
- [X] T037 [P] [US2] Create `useMaterialFilters.ts` and `useCommoditySearch.ts` in `frontend/src/features/warehouse/hooks/` — TanStack Query `useQuery` for filter options and commodity search respectively
- [X] T038 [P] [US2] Create `useAddMaterial.ts` in `frontend/src/features/warehouse/hooks/` — TanStack Query `useMutation` for `addMaterial`, invalidates the materials list query key on success
- [X] T039 [US2] Create `AddMaterialDialog.tsx` in `frontend/src/features/warehouse/components/` — commodity search/select (no custom entry), Owner select defaulting to caller (sourced from `useMaterialFilters` owners, mirroring `AddInventoryDialog`), Location input with datalist suggestions from `useMaterialFilters` locations, Quantity input, Quality input defaulting to `500`; client-side validation mirrors FR-017/FR-020; uses `useAddMaterial`
- [X] T040 [US2] Wire the Add control into `MaterialsView.tsx` — show "Add Material" button + `AddMaterialDialog` conditional on `useIsQuartermaster()`

**Checkpoint**: User Story 2 complete — Quartermasters can add/increment material; non-Quartermasters see no add control.

---

## Phase 5: User Story 3 — Adjust Material Quantity (Priority: P3)

**Goal**: A Quartermaster (or Admin) can set an existing row's quantity to a new absolute total, rejecting values `<= 0.00`, without touching Quality/Material/Owner/Location.

**Independent Test**: Adjust an existing row to a valid positive value and confirm it updates; attempt `0.00` or a negative value and confirm the row retains its prior quantity with no Quality control offered.

### Tests for User Story 3 — Write FIRST, confirm FAIL before implementing

- [X] T041 [P] [US3] Create `ChangeMaterialQuantityHandlerTests.cs` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/Materials/ChangeMaterialQuantity/` — tests: throws `MaterialRowNotFoundException` for unknown id; rounds the new total half-up to 2 places; rejects `<= 0.00` without calling the repository update; calls `UpdateQuantityAsync` with the rounded absolute value for a valid request
- [X] T042 [P] [US3] Extend `MaterialInventoryRepositoryTests.cs` — tests: `UpdateQuantityAsync` sets quantity to the given absolute value and leaves Quality/CommodityId/OwnerUserId/Location unchanged; the `quantity > 0` check constraint rejects a raw update to `0` or negative at the DB layer
- [X] T043 [P] [US3] Extend `MaterialsEndpointTests.cs` — tests: `PUT /api/warehouse/materials/{id}/quantity` returns 403 for non-Quartermaster, 200 with updated quantity for Quartermaster/Admin, 400 for quantity `<= 0.00`, 404 for unknown id
- [X] T044 [P] [US3] Create `EditMaterialQuantityControl.test.tsx` in `frontend/src/features/warehouse/__tests__/` — tests: renders no Quality field; submitting a value `<= 0` shows a validation message and does not call the mutation; submitting a valid positive value calls the mutation with the absolute value; control is absent for non-Quartermaster users

### Implementation for User Story 3

- [X] T045 [P] [US3] Create `ChangeMaterialQuantityCommand.cs` + `ChangeMaterialQuantityHandler.cs` + `MaterialRowNotFoundException` in `backend/src/NajaEcho.Application/Features/Warehouse/Materials/ChangeMaterialQuantity/` — rounds the new total half-up to 2 places, rejects `<= 0.00`, calls `IMaterialInventoryRepository.UpdateQuantityAsync`
- [X] T046 [US3] Implement `UpdateQuantityAsync` on `MaterialInventoryRepository.cs` — sets `quantity` and `updated_at` only, by id
- [X] T047 [US3] Register `ChangeMaterialQuantityHandler` as `AddScoped` in `DependencyInjection.cs`; add `PUT /api/warehouse/materials/{id}/quantity` endpoint (`.RequireAuthorization(AuthorizationPolicies.Quartermaster)`) to `WarehouseEndpoints.cs`, mapping `MaterialRowNotFoundException` to a 404 `Results.Problem`
- [X] T048 [P] [US3] Extend `materialSchemas.ts` with a Zod schema for `ChangeMaterialQuantityRequest`
- [X] T049 [P] [US3] Extend `materialsApi.ts` with `changeMaterialQuantity(id, quantity)` wrapper
- [X] T050 [P] [US3] Create `useChangeMaterialQuantity.ts` in `frontend/src/features/warehouse/hooks/` — TanStack Query `useMutation`, invalidates the materials list query key on success
- [X] T051 [US3] Create `EditMaterialQuantityControl.tsx` in `frontend/src/features/warehouse/components/` — numeric input + Save/Cancel for absolute quantity set, no Quality field, client-side `> 0.00` validation, uses `useChangeMaterialQuantity`
- [X] T052 [US3] Wire `EditMaterialQuantityControl` into `MaterialsTable.tsx` (or `MaterialsView.tsx`) per row, conditional on `useIsQuartermaster()`

**Checkpoint**: User Story 3 complete — Quartermasters can adjust quantity; invalid adjustments are blocked; control hidden from non-Quartermasters.

---

## Phase 6: User Story 4 — Remove Material (Priority: P3)

**Goal**: A Quartermaster (or Admin) can delete a material row to remove it from active inventory.

**Independent Test**: Delete a material row and confirm it no longer appears in the list.

### Tests for User Story 4 — Write FIRST, confirm FAIL before implementing

- [X] T053 [P] [US4] Create `RemoveMaterialHandlerTests.cs` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/Materials/RemoveMaterial/` — tests: calls `IMaterialInventoryRepository.RemoveAsync` with the given id; propagates not-found behavior for an unknown id
- [X] T054 [P] [US4] Extend `MaterialInventoryRepositoryTests.cs` — tests: `RemoveAsync` deletes the row; the row no longer appears in a subsequent `GetMaterialsAsync` call
- [X] T055 [P] [US4] Extend `MaterialsEndpointTests.cs` — tests: `DELETE /api/warehouse/materials/{id}` returns 403 for non-Quartermaster, 204 for Quartermaster/Admin, 404 for unknown id
- [X] T056 [P] [US4] Create `RemoveMaterialButton.test.tsx` in `frontend/src/features/warehouse/__tests__/` — tests: clicking and confirming calls the remove mutation with the row id; control is absent for non-Quartermaster users

### Implementation for User Story 4

- [X] T057 [P] [US4] Create `RemoveMaterialCommand.cs` + `RemoveMaterialHandler.cs` in `backend/src/NajaEcho.Application/Features/Warehouse/Materials/RemoveMaterial/` — calls `IMaterialInventoryRepository.RemoveAsync`
- [X] T058 [US4] Implement `RemoveAsync` on `MaterialInventoryRepository.cs` — deletes the row by id
- [X] T059 [US4] Register `RemoveMaterialHandler` as `AddScoped` in `DependencyInjection.cs`; add `DELETE /api/warehouse/materials/{id}` endpoint (`.RequireAuthorization(AuthorizationPolicies.Quartermaster)`) to `WarehouseEndpoints.cs`
- [X] T060 [P] [US4] Extend `materialsApi.ts` with `removeMaterial(id)` wrapper
- [X] T061 [P] [US4] Create `useRemoveMaterial.ts` in `frontend/src/features/warehouse/hooks/` — TanStack Query `useMutation`, invalidates the materials list query key on success
- [X] T062 [US4] Create `RemoveMaterialButton.tsx` in `frontend/src/features/warehouse/components/` — confirm-then-remove button, uses `useRemoveMaterial`
- [X] T063 [US4] Wire `RemoveMaterialButton` into `MaterialsTable.tsx` (or `MaterialsView.tsx`) per row, conditional on `useIsQuartermaster()`

**Checkpoint**: User Story 4 complete — Quartermasters can delete rows; non-Quartermasters see no remove control.

---

## Phase 7: User Story 5 — Filter and View Quality (Priority: P3)

**Goal**: Any authenticated user can filter the Materials list by Material text, single-select Owner, single-select Location, and an inclusive Quality range (dual-ended slider, default `1–1000`), combined with AND logic, plus a distinct "no results" empty state; Quality remains read-only after creation.

**Independent Test**: Apply each filter independently and in combination and verify only matching rows display; confirm an empty filter set shows all rows; confirm Quality has no editable control anywhere on the page.

### Tests for User Story 5 — Write FIRST, confirm FAIL before implementing

- [X] T064 [P] [US5] Extend `GetMaterialsQueryHandlerTests.cs` — tests: passes `material`/`ownerUserId`/`location`/`qualityMin`/`qualityMax` query params through to `GetMaterialsAsync`; empty/null filter params are passed through unset
- [X] T065 [P] [US5] Extend `MaterialInventoryRepositoryTests.cs` — tests: `material` filter matches name OR code case-insensitively; `ownerUserId` filter restricts to a single owner; `location` filter restricts to a single location; `qualityMin`/`qualityMax` is an inclusive `BETWEEN`; combining filters applies AND; all-empty filters return all rows
- [X] T066 [P] [US5] Extend `MaterialsEndpointTests.cs` — tests: `GET /api/warehouse/materials?material=...&ownerUserId=...&location=...&qualityMin=...&qualityMax=...` returns only matching rows; `GET /api/warehouse/materials/filters` returns 200 with Owners + Locations derived from current material inventory
- [X] T067 [P] [US5] Create `MaterialsFilters.test.tsx` in `frontend/src/features/warehouse/__tests__/` — tests: Material text input filters by name/code; selecting an Owner/Location replaces rather than accumulates a prior selection; Quality dual-slider defaults to `1–1000` and produces `[min,max]` filter values; Clear resets all filters; a distinct "no results match the current filters" empty state renders when active filters match nothing (vs. the "no material inventory" state when there are zero rows total)
- [X] T068 [P] [US5] Extend `MaterialsTable.test.tsx` — test: Quality is rendered as plain text/read-only for every row, with no editable control under any state

### Implementation for User Story 5

- [X] T069 [US5] Extend `GetMaterialsQuery.cs`/`GetMaterialsQueryHandler.cs` to accept and pass through `material`, `ownerUserId`, `location`, `qualityMin`, `qualityMax` filter params
- [X] T070 [US5] Extend `GetMaterialsAsync` on `MaterialInventoryRepository.cs` to apply `material` (`ILIKE` over commodity name OR code), `ownerUserId` equality, `location` equality, and `quality BETWEEN qualityMin AND qualityMax` filters, all combined with AND; empty/null params ignored
- [X] T071 [US5] Extend `GET /api/warehouse/materials` in `WarehouseEndpoints.cs` to accept and forward the five filter query params per `contracts/openapi.yaml`
- [X] T072 [P] [US5] Extend `materialSchemas.ts` with a Zod schema for the filter form state (material, ownerUserId, location, qualityMin, qualityMax)
- [X] T073 [P] [US5] Extend `materialsApi.ts`'s `getMaterials()` to accept and serialize the five filter query params
- [X] T074 [US5] Extend `useMaterials.ts` to accept filter params and include them in the query key (depends on T072–T073)
- [X] T075 [US5] Create `MaterialsFilters.tsx` in `frontend/src/features/warehouse/components/` — Material text input; single-select Owner and Location combos (sourced from `useMaterialFilters`); dual-ended Quality range slider (using `components/ui/slider.tsx`) defaulting to `1–1000`, optionally paired with numeric min/max inputs; Clear/reset action
- [X] T076 [US5] Wire `MaterialsFilters` into `MaterialsView.tsx` — filter state drives query params passed to `useMaterials`; add the distinct "no results match the current filters" empty state to `MaterialsTable.tsx` (or `MaterialsView.tsx`), separate from the zero-rows empty state
- [X] T077 [US5] Confirm `MaterialsTable.tsx` renders Quality as read-only text only (no input/edit affordance) — adjust if any prior task introduced one

**Checkpoint**: All five user stories complete and independently testable.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Observability and final validation.

- [X] T078 [P] Add structured Serilog logging to `AddMaterialHandler.cs`, `ChangeMaterialQuantityHandler.cs`, and `RemoveMaterialHandler.cs` — log the caller id and operation outcome (created/incremented/adjusted/removed), matching existing warehouse endpoint logging; never log sensitive data
- [X] T079 Run full quickstart.md validation — all scenarios in `specs/014-warehouse-materials/quickstart.md`, confirm all automated tests pass (`dotnet test` from `backend/`, `npm run test:run` from `frontend/`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: T001 → T002 (sequential). T003 independent. No other dependencies. Start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1 completion. T004–T005 parallel. T006 depends on T004–T005. T007 depends on T006. T008–T009 parallel and can run alongside T004–T007.
- **Phase 3 (US1)**: Depends on Phase 2 completion (T007 = migration applied). Tests T010–T013 parallel. T014 parallel with T015. T016 depends on T014–T015. T017 depends on T016. T018–T020 parallel (frontend). T021 depends on T018–T020. T022 depends on T021. T023 depends on T022. T024 depends on T023.
- **Phase 4 (US2)**: Depends on Phase 3 completion. Tests T025–T029 parallel. T030–T032 parallel. T033 depends on T030–T032 (same repository file — sequential within it). T034 depends on T030–T033. T035–T038 parallel (frontend). T039 depends on T035–T038. T040 depends on T039.
- **Phase 5 (US3)**: Depends on Phase 4 completion (writes build on US2's write infrastructure). Tests T041–T044 parallel. T045 independent. T046 depends on T045. T047 depends on T045–T046. T048–T050 parallel. T051 depends on T048–T050. T052 depends on T051.
- **Phase 6 (US4)**: Depends on Phase 4 completion; can run in parallel with Phase 5 by a second developer. Tests T053–T056 parallel. T057 independent. T058 depends on T057. T059 depends on T057–T058. T060–T061 parallel. T062 depends on T060–T061. T063 depends on T062.
- **Phase 7 (US5)**: Depends on Phase 3 (base list) and Phase 4 (filters endpoint) completion; can start once those are done, independent of Phase 5/6. Tests T064–T068 parallel. T069 depends on T064. T070 depends on T069 (same repository file as US1/US2 — sequential). T071 depends on T069–T070. T072–T073 parallel. T074 depends on T072–T073. T075 depends on T074. T076 depends on T075. T077 depends on T076.
- **Phase 8 (Polish)**: Depends on all implementation phases complete. T078 independent. T079 depends on T078.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2. Fully independent — no other story required.
- **US2 (P2)**: Starts after US1 (reuses `MaterialsView`/`MaterialsTable` and introduces the filters endpoint that US2's own add dialog needs for Location suggestions).
- **US3 (P3)**: Starts after US2 (shares write-policy wiring established for Add). Independent of US4/US5.
- **US4 (P3)**: Starts after US2. Independent of US3/US5 — can run in parallel with US3 by a second developer.
- **US5 (P3)**: Starts after US2 (consumes the `GetMaterialFiltersQuery`/endpoint introduced there). Independent of US3/US4.

### Parallel Opportunities Within Phase 2

```
T004 (WarehouseMaterialEntry)        ─┐
T005 (WarehouseMaterialEntryConfig)  ─┤→ T006 (AppDbContext) → T007 (migration)
T008 (IMaterialInventoryRepository)  ─ independent
T009 (MaterialDtos)                  ─ independent
```

### Parallel Opportunities Within Phase 3 (US1)

```
T010 (App unit tests)     ─┐
T011 (Infra tests)        ─┤ parallel
T012 (API endpoint tests) ─┤
T013 (Frontend tests)     ─┘

T018 (Zod schemas)        ─┐
T019 (API client)         ─┤ parallel → T021 (useMaterials) → T022 (Table) → T023 (View) → T024 (Router)
T020 (query keys)         ─┘

T014 (Query + Handler)    ─┐
T015 (Repository impl)    ─┤ → T016 (DI) → T017 (endpoint)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T009)
3. Complete Phase 3: US1 — tests first (T010–T013), then implementation (T014–T024)
4. **STOP and VALIDATE**: Quickstart scenarios A1–A5; run `dotnet test` + `npm run test:run`
5. Authenticated users can view material inventory — ship it.

### Incremental Delivery

1. Setup + Foundational → commit
2. US1 complete → authenticated view works → commit/demo (MVP)
3. US2 complete → add/increment works → commit/demo
4. US3 complete → quantity adjustment works → commit/demo
5. US4 complete → row removal works → commit/demo
6. US5 complete → filters + read-only Quality confirmed → commit/demo
7. Each delivery passes the full test suite without regression

### Parallel Team Strategy (2 developers)

After Phase 4 (US2) is complete:
- **Developer A**: US3 (Phase 5, T041–T052) then US5 (Phase 7, T064–T077)
- **Developer B**: US4 (Phase 6, T053–T063) — fully independent of US3/US5

---

## Notes

- [P] marks tasks that touch different files with no shared dependency — safe to parallelize
- TDD is mandatory (Constitution II): tests MUST be written and confirmed RED before the implementation tasks in the same phase begin
- EF migration (T007) must be applied to the dev DB before any Testcontainers-backed repository test can pass
- `GetMaterialFiltersQuery`/endpoint is introduced once in US2 (for Add-dialog Location suggestions, FR-016) and reused as-is by US5's Owner/Location filter selects — no duplicate endpoint
- Quality is set only at `AddMaterial` time (US2) and is never exposed for editing by `ChangeMaterialQuantity` (US3) or anywhere in the UI (FR-022, FR-023) — T068/T077 guard this explicitly
- `AddOrIncrementAsync`'s conflict target includes `quality`, so quality is never altered on increment (FR-024–FR-026, FR-032)
- The Owner select in `AddMaterialDialog` (T039) draws from `useMaterialFilters` owners, mirroring the existing `AddInventoryDialog` convention in this codebase, not a separate all-registered-users search
- Unique by design: Materials gets its own table/repository/dialog set rather than reusing `warehouse_inventory`/`AddInventoryDialog` — decimal quantity and quality-in-key make sharing those files more complex than parallel ones (see research.md Decision 1)
