# Tasks: Warehouse Item Inventory

**Input**: Design documents from `/specs/011-warehouse-item-inventory/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/openapi.yaml ✅

**Tests**: Included — Constitution II (Test-First / TDD) is NON-NEGOTIABLE. All tests MUST be written and confirmed to FAIL before the production code they exercise is written.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency conflicts)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Exact file paths included in each description

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Role seeding, authorization policy, and frontend type-generation script — prerequisites for all subsequent work.

- [X] T001 Generalize `AdminRoleSeeder` into `RoleSeeder` that seeds both `Admin` and `Quartermaster` roles at startup in `backend/src/NajaEcho.Infrastructure/Identity/RoleSeeder.cs` (update `Program.cs` reference)
- [X] T002 [P] Add `Quartermaster` authorization policy (`RequireRole("Quartermaster","Admin")`) to `backend/src/NajaEcho.Api/Authorization/AuthorizationPolicies.cs`
- [X] T003 [P] Add `gen:api:warehouse` npm script (openapi-typescript from `specs/011-warehouse-item-inventory/contracts/openapi.yaml`) to `frontend/package.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entity, EF configuration and migration, repository contract, DI registration, and generated frontend types. ALL user story work is blocked until this phase is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Create `WarehouseInventoryEntry` domain entity (`Id`, `ItemId`, `OwnerUserId`, `Location`, `Quantity`, `CreatedAt`, `UpdatedAt`) in `backend/src/NajaEcho.Domain/Warehouse/WarehouseInventoryEntry.cs`
- [X] T005 [P] Create `IWarehouseInventoryRepository` interface (list, filters, add-or-increment, update-quantity, remove) in `backend/src/NajaEcho.Application/Abstractions/IWarehouseInventoryRepository.cs`
- [X] T006 [P] Create `WarehouseInventoryEntryConfiguration` (table `warehouse_inventory`, unique index on `(item_id, owner_user_id, location)`, FK constraints, `Quantity >= 1` check, location max length 200) in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/WarehouseInventoryEntryConfiguration.cs`
- [X] T007 Add `DbSet<WarehouseInventoryEntry>` to `AppDbContext` and register configuration in `OnModelCreating` in `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs`, then generate EF migration `AddWarehouseInventory` in `backend/src/NajaEcho.Infrastructure/Persistence/Migrations/`
- [X] T008 Create `WarehouseInventoryRepository` skeleton (implements `IWarehouseInventoryRepository`) and register with `AddScoped` in `backend/src/NajaEcho.Infrastructure/Warehouse/WarehouseInventoryRepository.cs` and `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`
- [X] T009 Run `npm run gen:api:warehouse` in `frontend/` to generate typed API client from `contracts/openapi.yaml`

**Checkpoint**: Migration applied, repository wired, frontend types generated — user story implementation can now begin.

---

## Phase 3: User Story 1 — View Item Inventory (Priority: P1) 🎯 MVP

**Goal**: Any authenticated user can navigate to `/warehouse/items`, see the inventory table (Name, Type, Subtype, Quantity, Owner, Location sorted by Name ascending), and apply one or more optional filters.

**Independent Test**: Sign in as a plain member (no Quartermaster role) → Warehouse → Items: table visible, filters work, no add/edit/remove controls present. Anonymous access → redirect to sign-in.

> **⚠️ Write ALL tests in this section and confirm they FAIL before writing any implementation code.**

### Tests for User Story 1

- [X] T010 Write failing `GetInventoryHandlerTests` covering: filter AND logic, case-insensitive ILIKE for name/location, exact-match for Type/Subtype/Owner, default sort by Name ascending, empty-result set in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/GetInventoryHandlerTests.cs`
- [X] T011 [P] Write failing `GetInventoryFiltersHandlerTests` covering: distinct Types/Subtypes from `ItemCategory`, distinct Owners from inventory rows in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/GetInventoryFiltersHandlerTests.cs`
- [X] T012 [P] Write failing endpoint auth tests for `GET /api/warehouse/items` and `GET /api/warehouse/items/filters` (401 anonymous, 200 authenticated non-QM) in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseEndpointsTests.cs`
- [X] T013 [P] [US1] Write failing Vitest tests for `InventoryTable` (renders columns, empty-inventory state message, no-results-after-filter state) in `frontend/src/features/warehouse/__tests__/InventoryTable.test.tsx`
- [X] T014 [P] [US1] Write failing Vitest tests for `InventoryFilters` (renders dropdowns, applies filter values, calls onFilterChange) in `frontend/src/features/warehouse/__tests__/InventoryFilters.test.tsx`

### Implementation for User Story 1

- [X] T015 [US1] Implement `GetInventoryQuery`, `GetInventoryHandler`, `InventoryRowDto` (joined from `WarehouseInventoryEntry` + `Item` + user display name) in `backend/src/NajaEcho.Application/Features/Warehouse/GetInventory/`
- [X] T016 [P] [US1] Implement `GetInventoryFiltersQuery`, `GetInventoryFiltersHandler`, `InventoryFiltersDto` (distinct Types/Subtypes from `ItemCategory`; distinct Owners from inventory) in `backend/src/NajaEcho.Application/Features/Warehouse/GetInventoryFilters/`
- [X] T017 [US1] Implement list and filters read methods on `WarehouseInventoryRepository` (ILIKE for name/location, exact-match for type/subtype/owner, ordered by `Item.Name`) in `backend/src/NajaEcho.Infrastructure/Warehouse/WarehouseInventoryRepository.cs`
- [X] T018 [US1] Create `WarehouseDtos.cs` (US1 records: `InventoryRow`, `InventoryListResponse`, `InventoryFiltersResponse`, `OwnerOption`) in `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/WarehouseDtos.cs`
- [X] T019 [US1] Create `WarehouseEndpoints.cs` with `/api/warehouse` group (`RequireAuthorization()`), `GET /items` and `GET /items/filters` mapped with handler dispatch and response mapping in `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`
- [X] T020 [US1] Register `app.MapWarehouseEndpoints()` in `backend/src/NajaEcho.Api/Program.cs` alongside existing endpoint registrations
- [X] T021 [P] [US1] Create `warehouseKeys.ts` typed query-key factory in `frontend/src/features/warehouse/hooks/warehouseKeys.ts`
- [X] T022 [P] [US1] Create `warehouseApi.ts` with `apiFetch` wrappers for `GET /api/warehouse/items` and `GET /api/warehouse/items/filters` in `frontend/src/features/warehouse/api/warehouseApi.ts`
- [X] T023 [US1] Create Zod schemas for `InventoryRow`, `InventoryListResponse`, `InventoryFiltersResponse`, and filter-state in `frontend/src/features/warehouse/schemas/inventorySchemas.ts`
- [X] T024 [US1] Implement `useInventory.ts` and `useInventoryFilters.ts` TanStack Query hooks in `frontend/src/features/warehouse/hooks/`
- [X] T025 [US1] Implement `InventoryTable.tsx` (columns: Name, Type, Subtype, Quantity, Owner, Location; empty-inventory state; no-results state; QM action columns stubbed but hidden) in `frontend/src/features/warehouse/components/InventoryTable.tsx`
- [X] T026 [P] [US1] Implement `InventoryFilters.tsx` (Name text input, Type/Subtype/Owner dropdowns populated from `useInventoryFilters`, Location text input; controlled) in `frontend/src/features/warehouse/components/InventoryFilters.tsx`
- [X] T027 [US1] Implement `WarehouseItemsView.tsx` thin route view (composes `InventoryFilters` + `InventoryTable`; no QM write controls yet) in `frontend/src/features/warehouse/pages/WarehouseItemsView.tsx`
- [X] T028 [US1] Add Warehouse nav group (Items entry, `path: '/warehouse/items'`, no `access` restriction) after the Hangar group in `frontend/src/features/dashboard/navigation/navItems.ts`
- [X] T029 [US1] Add `/warehouse` → redirect `/warehouse/items` and `/warehouse/items` → `WarehouseItemsView` inside `ProtectedRoute`/`DashboardLayout` in `frontend/src/routes/AppRouter.tsx`

**Checkpoint**: Any authenticated user can view the inventory table and apply filters. Anonymous access redirects to sign-in. No write controls are visible. User Story 1 is independently verifiable.

---

## Phase 4: User Story 2 — Add Item to Inventory (Priority: P2)

**Goal**: A Quartermaster can search the item catalog, fill in quantity/owner/location, and submit — creating a new inventory row or incrementing an existing row's quantity for the same Item + Owner + Location. Owner and Location are remembered for subsequent adds within the same page session.

**Independent Test**: Sign in as Quartermaster → add-item flow → new location creates new row → same location increments → different owner/location creates separate rows → remembered fields pre-fill second add → page reload clears remembered fields. Non-QM member: add button absent.

> **⚠️ Write ALL tests in this section and confirm they FAIL before writing any implementation code.**

### Tests for User Story 2

- [X] T030 [US2] Write failing `AddInventoryItemHandlerTests` covering: new row created with correct fields and defaults; existing row incremented (not duplicated); separate rows for different owner; separate rows for different location; location trimmed before save; 404 for unknown `ItemId`; 404 for unknown `OwnerUserId` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/AddInventoryItemHandlerTests.cs`
- [X] T031 [P] [US2] Write failing `AddInventoryItemValidatorTests` covering: empty location rejected; whitespace-only location rejected; `Quantity < 1` rejected; `Quantity = 0` rejected in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/AddInventoryItemValidatorTests.cs`
- [X] T032 [P] [US2] Write failing `SearchCatalogItemsHandlerTests` covering: name ILIKE search returns `Name`, `Type`, `Subtype`; results capped at 25; only `Active` items returned in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/SearchCatalogItemsHandlerTests.cs`
- [X] T033 [P] [US2] Write failing Testcontainers repository integration tests: unique-constraint prevents duplicate insert; concurrent adds for same key yield exactly one row with correct summed quantity in `backend/tests/NajaEcho.Infrastructure.Tests/Warehouse/WarehouseInventoryRepositoryTests.cs`
- [X] T034 [P] [US2] Extend `WarehouseEndpointsTests` with failing tests for `POST /api/warehouse/items` (403 for authenticated non-QM, 201 QM new row, 200 QM increment, Admin inherits QM) and `GET /api/warehouse/catalog/search` (403 non-QM, 200 QM) in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseEndpointsTests.cs`
- [X] T035 [P] [US2] Write failing Vitest tests for `AddInventoryDialog` (QM sees add button; form defaults owner to current user, qty to 1; remembered owner/location pre-fill second add; non-QM: add button absent) in `frontend/src/features/warehouse/__tests__/AddInventoryDialog.test.tsx`

### Implementation for User Story 2

- [X] T036 [US2] Implement `AddInventoryItemCommand`, `AddInventoryItemHandler` (add-or-increment via repository), `AddInventoryItemValidator` (location non-empty after trim, qty ≥ 1), and domain exceptions (`ItemNotFoundException`, `OwnerNotFoundException`) in `backend/src/NajaEcho.Application/Features/Warehouse/AddInventoryItem/`
- [X] T037 [P] [US2] Implement `SearchCatalogItemsQuery`, `SearchCatalogItemsHandler`, `CatalogItemResultDto` (name ILIKE, Active only, cap 25) in `backend/src/NajaEcho.Application/Features/Warehouse/SearchCatalogItems/`
- [X] T038 [US2] Implement `AddOrIncrementAsync` in `WarehouseInventoryRepository` (transactional read-then-write; catch unique-constraint violation on concurrent insert and retry as increment; trim location before resolve and persist) in `backend/src/NajaEcho.Infrastructure/Warehouse/WarehouseInventoryRepository.cs`
- [X] T039 [US2] Add `AddInventoryItemRequest`, `CatalogItemResult` to `WarehouseDtos.cs`; add `POST /items` (Quartermaster policy) and `GET /catalog/search` (Quartermaster policy) to `WarehouseEndpoints.cs` in `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/WarehouseDtos.cs` and `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`
- [X] T040 [US2] Create Zod schemas for add-item form (`AddItemFormSchema`) and catalog item result in `frontend/src/features/warehouse/schemas/addItemSchemas.ts`
- [X] T041 [P] [US2] Implement `useCatalogItemSearch.ts` TanStack Query hook (search term debounced, keyed via `warehouseKeys`) in `frontend/src/features/warehouse/hooks/useCatalogItemSearch.ts`
- [X] T042 [P] [US2] Implement `useAddInventoryItem.ts` mutation hook (invalidates inventory query on success) in `frontend/src/features/warehouse/hooks/useAddInventoryItem.ts`
- [X] T043 [P] [US2] Implement `useIsQuartermaster.ts` hook (`roles.includes('Quartermaster') || roles.includes('Admin')`) in `frontend/src/features/warehouse/hooks/useIsQuartermaster.ts`
- [X] T044 [US2] Implement `AddInventoryDialog.tsx` (catalog search combobox; form with Owner defaulting to current user, Quantity defaulting to 1, required Location; remembered Owner/Location in component state; validation error messages; disabled/loading states) in `frontend/src/features/warehouse/components/AddInventoryDialog.tsx`
- [X] T045 [US2] Wire `AddInventoryDialog` into `WarehouseItemsView.tsx` conditionally rendered via `useIsQuartermaster` in `frontend/src/features/warehouse/pages/WarehouseItemsView.tsx`

**Checkpoint**: Quartermaster can add and increment inventory. Non-QM members see no write controls. Concurrent add race is handled. User Story 2 is independently verifiable.

---

## Phase 5: User Story 3 — Change Item Quantity (Priority: P3)

**Goal**: A Quartermaster can edit the quantity on an existing inventory row. The submitted value **replaces** (not increments) the existing quantity. Values of 0, negative, or non-integer are rejected.

**Independent Test**: Sign in as Quartermaster → edit an existing row's quantity to a new whole number ≥ 1 → value replaced (not added). Setting 0 or non-integer → rejected; original value unchanged. Non-QM member: no edit control visible.

> **⚠️ Write ALL tests in this section and confirm they FAIL before writing any implementation code.**

### Tests for User Story 3

- [X] T046 [US3] Write failing `ChangeInventoryQuantityHandlerTests` covering: quantity is replaced (not incremented); `UpdatedAt` bumped; 404 for missing row in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/ChangeInventoryQuantityHandlerTests.cs`
- [X] T047 [P] [US3] Write failing `ChangeInventoryQuantityValidatorTests` covering: `Quantity < 1` rejected; `Quantity = 0` rejected in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/ChangeInventoryQuantityValidatorTests.cs`
- [X] T048 [P] [US3] Extend `WarehouseEndpointsTests` with failing tests for `PUT /api/warehouse/items/{id}/quantity` (403 non-QM, 200 QM with updated row, 404 missing row, Admin inherits QM) in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseEndpointsTests.cs`
- [X] T049 [P] [US3] Write failing Vitest tests for `EditQuantityControl` (QM sees inline edit; submitting valid value updates display; submitting 0 or empty shows error; non-QM: control absent) in `frontend/src/features/warehouse/__tests__/EditQuantityControl.test.tsx`

### Implementation for User Story 3

- [X] T050 [US3] Implement `ChangeInventoryQuantityCommand`, `ChangeInventoryQuantityHandler` (replaces quantity, bumps `UpdatedAt`), `ChangeInventoryQuantityValidator` (qty ≥ 1) in `backend/src/NajaEcho.Application/Features/Warehouse/ChangeInventoryQuantity/`
- [X] T051 [US3] Implement `UpdateQuantityAsync` in `WarehouseInventoryRepository` (load by id, replace `Quantity`, bump `UpdatedAt`, 404 guard) in `backend/src/NajaEcho.Infrastructure/Warehouse/WarehouseInventoryRepository.cs`
- [X] T052 [US3] Add `ChangeInventoryQuantityRequest` to `WarehouseDtos.cs`; add `PUT /items/{id}/quantity` (Quartermaster policy) to `WarehouseEndpoints.cs` in `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/WarehouseDtos.cs` and `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`
- [X] T053 [P] [US3] Create Zod schema for change-quantity form (`ChangeQuantityFormSchema`, integer ≥ 1) in `frontend/src/features/warehouse/schemas/changeQuantitySchemas.ts`
- [X] T054 [P] [US3] Implement `useChangeQuantity.ts` mutation hook (invalidates inventory query on success) in `frontend/src/features/warehouse/hooks/useChangeQuantity.ts`
- [X] T055 [US3] Implement `EditQuantityControl.tsx` (inline edit field; replace semantics clearly communicated; validation error; loading/disabled state) in `frontend/src/features/warehouse/components/EditQuantityControl.tsx`
- [X] T056 [US3] Wire `EditQuantityControl` into the Quantity cell of `InventoryTable.tsx` conditionally via `useIsQuartermaster` in `frontend/src/features/warehouse/components/InventoryTable.tsx`

**Checkpoint**: Quartermaster can replace row quantities. Zero/non-integer input rejected. Non-QM sees no edit control. User Story 3 is independently verifiable.

---

## Phase 6: User Story 4 — Remove Inventory Row (Priority: P3)

**Goal**: A Quartermaster can remove an inventory row. The `WarehouseInventoryEntry` is deleted; the underlying `sc.items` catalog item is **never** touched.

**Independent Test**: Sign in as Quartermaster → remove a row → it disappears from the list → re-search the catalog to confirm the item still exists. Non-QM member: no remove control visible.

> **⚠️ Write ALL tests in this section and confirm they FAIL before writing any implementation code.**

### Tests for User Story 4

- [X] T057 [US4] Write failing `RemoveInventoryItemHandlerTests` covering: row is deleted; 404 for missing row; no side-effect on the `sc.items` catalog record in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/RemoveInventoryItemHandlerTests.cs`
- [X] T058 [P] [US4] Extend Testcontainers integration tests: remove deletes the `warehouse_inventory` row and the `sc.items` row remains intact in `backend/tests/NajaEcho.Infrastructure.Tests/Warehouse/WarehouseInventoryRepositoryTests.cs`
- [X] T059 [P] [US4] Extend `WarehouseEndpointsTests` with failing tests for `DELETE /api/warehouse/items/{id}` (403 non-QM, 204 QM success, 404 missing row, Admin inherits QM) in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseEndpointsTests.cs`
- [X] T060 [P] [US4] Write failing Vitest tests for `RemoveInventoryButton` (QM sees button; confirm dialog appears; cancel leaves row; confirm triggers delete and row disappears; non-QM: button absent) in `frontend/src/features/warehouse/__tests__/RemoveInventoryButton.test.tsx`

### Implementation for User Story 4

- [X] T061 [US4] Implement `RemoveInventoryItemCommand`, `RemoveInventoryItemHandler` (delete row, 404 guard, no catalog touch) in `backend/src/NajaEcho.Application/Features/Warehouse/RemoveInventoryItem/`
- [X] T062 [US4] Implement `RemoveAsync` in `WarehouseInventoryRepository` (delete by `Id`, 404 guard; FK RESTRICT ensures catalog row is never affected) in `backend/src/NajaEcho.Infrastructure/Warehouse/WarehouseInventoryRepository.cs`
- [X] T063 [US4] Add `DELETE /items/{id}` (Quartermaster policy, returns 204) to `WarehouseEndpoints.cs` in `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`
- [X] T064 [P] [US4] Implement `useRemoveInventoryItem.ts` mutation hook (invalidates inventory query on success) in `frontend/src/features/warehouse/hooks/useRemoveInventoryItem.ts`
- [X] T065 [US4] Implement `RemoveInventoryButton.tsx` (confirm dialog before delete; loading state during mutation; accessible label) in `frontend/src/features/warehouse/components/RemoveInventoryButton.tsx`
- [X] T066 [US4] Wire `RemoveInventoryButton` into the action column of `InventoryTable.tsx` conditionally via `useIsQuartermaster` in `frontend/src/features/warehouse/components/InventoryTable.tsx`

**Checkpoint**: Quartermaster can remove rows. Catalog items are unaffected. Non-QM sees no remove control. All four user stories are independently verifiable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Observability, full test-suite validation, and end-to-end quickstart verification.

- [X] T067 [P] Add structured Serilog logging to all six warehouse handlers (emit caller id, item id, owner id, action name, resulting quantity, row id — never tokens or sensitive data) following the Hangar logging shape in `backend/src/NajaEcho.Application/Features/Warehouse/`
- [X] T068 [P] Run the complete automated test suite and confirm all tests pass: `dotnet test backend/tests/NajaEcho.Application.Tests`, `dotnet test backend/tests/NajaEcho.Infrastructure.Tests`, `dotnet test backend/tests/NajaEcho.Api.Tests`, `cd frontend && npm run test -- warehouse`
- [X] T069 Run all six quickstart.md validation scenarios end-to-end (read access, anonymous redirect, add & increment, write authorization, validation, change quantity, remove) per `specs/011-warehouse-item-inventory/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 completion — **BLOCKS all user stories**.
- **User Stories (Phases 3–6)**: All depend on Phase 2 completion.
  - US2 (Phase 4) depends on the `useIsQuartermaster` hook pattern established in Phase 3, but the backend components are independent.
  - US3 and US4 (Phases 5–6) depend on the inventory table established in Phase 3 for UI wiring; backend components are independent.
  - Stories can proceed in parallel by different developers once Phase 2 is complete.
- **Polish (Phase 7)**: Depends on all user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Can start immediately after Foundational — no story dependencies.
- **US2 (P2)**: Can start after Foundational. Shares `WarehouseEndpointsTests.cs` and `WarehouseInventoryRepository.cs` with US1 — coordinate file edits.
- **US3 (P3)**: Can start after Foundational. Extends same endpoint and repository files.
- **US4 (P3)**: Can start after Foundational. Extends same endpoint and repository files.

### Within Each User Story

1. Write failing tests first — confirm RED before any production code.
2. Implement domain/application layer (commands, handlers, validators).
3. Implement infrastructure layer (repository methods).
4. Implement API layer (DTOs, endpoint mapping).
5. Implement frontend (schemas → hooks → components → view wiring).
6. Confirm all tests GREEN.

### Parallel Opportunities

- **Phase 1**: T002 and T003 can run in parallel with T001.
- **Phase 2**: T005 and T006 can run in parallel with T004; T008 depends on T004+T005+T006+T007.
- **US1 tests**: T011–T014 can run in parallel with T010.
- **US1 implementation**: T016 parallel with T015; T021+T022 parallel with T023 (within frontend).
- **US2 tests**: T031–T035 can run in parallel with T030.
- **US2 implementation**: T037+T043 parallel with T036; T041+T042+T043 parallel on frontend.
- **US3 tests**: T047–T049 can run in parallel with T046.
- **US4 tests**: T058–T060 can run in parallel with T057.
- **Phase 7**: T067 and T068 can run in parallel.

---

## Parallel Example: User Story 1 Tests

```bash
# Run all US1 tests together (all should FAIL at this point — that's correct):
dotnet test backend/tests/NajaEcho.Application.Tests --filter "GetInventory"
dotnet test backend/tests/NajaEcho.Application.Tests --filter "GetInventoryFilters"
dotnet test backend/tests/NajaEcho.Api.Tests --filter "Warehouse"
cd frontend && npm run test -- InventoryTable InventoryFilters
```

## Parallel Example: User Story 2 Infrastructure + Application Tests

```bash
# Run concurrently after T030–T035 written (all should FAIL):
dotnet test backend/tests/NajaEcho.Application.Tests --filter "AddInventoryItem|SearchCatalogItems"
dotnet test backend/tests/NajaEcho.Infrastructure.Tests --filter "WarehouseInventoryRepository"
dotnet test backend/tests/NajaEcho.Api.Tests --filter "Warehouse"
cd frontend && npm run test -- AddInventoryDialog
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003).
2. Complete Phase 2: Foundational (T004–T009) — **critical blocker**.
3. Complete Phase 3: User Story 1 (T010–T029).
4. **STOP and VALIDATE**: Any authenticated user can view the inventory table and apply filters. Anonymous redirect works. No write controls present.
5. Demo / deploy the read-only inventory page.

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready.
2. Phase 3 (US1) → Read-only inventory live — **demo-able MVP**.
3. Phase 4 (US2) → Quartermaster can add and increment inventory.
4. Phase 5 (US3) → Quartermaster can correct quantities.
5. Phase 6 (US4) → Quartermaster can remove stale rows.
6. Phase 7 → Observability confirmed, all tests green, quickstart validated.

### Parallel Team Strategy

With multiple developers (once Phase 2 is complete):

- **Developer A**: US1 backend (T010–T020) → US1 frontend (T021–T029).
- **Developer B**: US2 backend (T030–T039).
- **Developer C**: US2 frontend (T040–T045) → US3 (T046–T056).

---

## Notes

- `[P]` tasks touch different files and have no intra-phase dependency conflicts.
- `[Story]` label maps each task to a specific user story for traceability.
- Every user story is independently completable and testable.
- **TDD is mandatory** (Constitution II): confirm tests FAIL before writing production code; do not skip the Red phase.
- `WarehouseEndpointsTests.cs` is extended across phases — coordinate if working in parallel.
- `WarehouseInventoryRepository.cs` is extended across phases — coordinate if working in parallel.
- Location trimming (`Location.Trim()`) MUST happen in the Application layer (handler or validator) before the repository call, not in the repository itself.
- The unique-constraint retry in `AddOrIncrementAsync` is the concurrency safety mechanism — do not replace it with an application-level lock.
- The `Quartermaster` policy uses `RequireRole("Quartermaster","Admin")` OR semantics — Admin inherits without explicit Quartermaster role assignment (FR-027).
- Remembered Owner/Location values live in React component state only — never `localStorage`, `sessionStorage`, or server (FR-015/FR-016).
