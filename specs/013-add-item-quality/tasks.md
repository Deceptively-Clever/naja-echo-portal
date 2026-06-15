# Tasks: Add item quality

**Input**: Design documents from `/specs/001-add-item-quality/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included. The constitution requires TDD/test-first coverage for backend and frontend changes.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Lock contract and type-generation inputs for implementation.

- [X] T001 [P] Align quality field semantics in `specs/001-add-item-quality/contracts/openapi.yaml` with final implementation decisions.
- [X] T002 [P] Ensure API type-generation scripts for warehouse contracts are correct in `frontend/package.json`.
- [X] T003 Regenerate frontend API types from updated contracts into `frontend/src/lib/api/warehouse.d.ts` and `frontend/src/lib/api/ship-components.d.ts`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core data-model and shared backend/frontend contracts required by all user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Add `Quality` property to warehouse entity in `backend/src/NajaEcho.Domain/Warehouse/WarehouseInventoryEntry.cs`.
- [X] T005 Add `quality` column mapping + range check constraint in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/WarehouseInventoryEntryConfiguration.cs`.
- [X] T006 Create EF migration for `quality` (`NOT NULL DEFAULT 500` + backfill + check constraint) in `backend/src/NajaEcho.Infrastructure/Persistence/Migrations/`.
- [X] T007 Update EF snapshot for `warehouse_inventory.quality` in `backend/src/NajaEcho.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`.
- [X] T008 [P] Add quality field to shared inventory DTOs in `backend/src/NajaEcho.Application/Features/Warehouse/GetInventory/InventoryRowDto.cs` and `backend/src/NajaEcho.Application/Features/Warehouse/ShipComponents/GetShipComponents/ShipComponentRowDto.cs`.
- [X] T009 [P] Add quality field to API warehouse contracts in `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/WarehouseDtos.cs` and `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/ShipComponentDtos.cs`.
- [X] T010 Add quality parameters to repository abstractions in `backend/src/NajaEcho.Application/Abstractions/IWarehouseInventoryRepository.cs` and `backend/src/NajaEcho.Application/Abstractions/IShipComponentRepository.cs`.

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Create item with explicit quality (Priority: P1) 🎯 MVP

**Goal**: Quartermaster can provide quality (1..1000) when adding warehouse/ship-component items, and the persisted value is returned in list/read surfaces.

**Independent Test**: Submit add requests with explicit quality (e.g., 750) through both add flows and confirm returned/listed rows carry quality 750.

### Tests for User Story 1 ⚠️

- [X] T011 [P] [US1] Add explicit-quality handler tests in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/AddInventoryItemHandlerTests.cs`.
- [X] T012 [P] [US1] Add explicit-quality API tests for `/api/warehouse/items` in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseEndpointsTests.cs`.
- [X] T013 [P] [US1] Add explicit-quality API tests for `/api/warehouse/ship-components` in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/ShipComponentsEndpointTests.cs`.
- [X] T014 [P] [US1] Add repository persistence/read tests for quality in `backend/tests/NajaEcho.Infrastructure.Tests/Warehouse/WarehouseInventoryRepositoryTests.cs` and `backend/tests/NajaEcho.Infrastructure.Tests/Warehouse/ShipComponentRepositoryTests.cs`.
- [X] T015 [P] [US1] Add frontend add-form and table rendering tests for explicit quality in `frontend/src/features/warehouse/__tests__/AddInventoryDialog.test.tsx`, `frontend/src/features/warehouse/__tests__/InventoryTable.test.tsx`, and `frontend/src/features/warehouse/__tests__/ShipComponentsTable.test.tsx`.

### Implementation for User Story 1

- [X] T016 [US1] Accept optional quality on add requests in `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/WarehouseDtos.cs` and `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/ShipComponentDtos.cs`.
- [X] T017 [US1] Wire quality through add endpoints in `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`.
- [X] T018 [US1] Carry quality through add command/handler logic in `backend/src/NajaEcho.Application/Features/Warehouse/AddInventoryItem/AddInventoryItemCommand.cs` and `backend/src/NajaEcho.Application/Features/Warehouse/AddInventoryItem/AddInventoryItemHandler.cs`.
- [X] T019 [US1] Persist and project quality in repositories in `backend/src/NajaEcho.Infrastructure/Warehouse/WarehouseInventoryRepository.cs` and `backend/src/NajaEcho.Infrastructure/Warehouse/ShipComponentRepository.cs`.
- [X] T020 [US1] Expose quality in API response mapping in `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`.
- [X] T021 [US1] Add quality field to frontend add schema in `frontend/src/features/warehouse/schemas/addItemSchemas.ts`.
- [X] T022 [US1] Include quality in frontend warehouse/ship-component row schemas in `frontend/src/features/warehouse/schemas/inventorySchemas.ts` and `frontend/src/features/warehouse/schemas/shipComponentSchemas.ts`.
- [X] T023 [US1] Add quality input control (explicit entry) in `frontend/src/features/warehouse/components/AddInventoryDialog.tsx`.
- [X] T024 [US1] Render quality in inventory and ship-component tables in `frontend/src/features/warehouse/components/InventoryTable.tsx` and `frontend/src/features/warehouse/components/ShipComponentsTable.tsx`.
- [X] T025 [US1] Pass quality in add API clients/hooks in `frontend/src/features/warehouse/api/warehouseApi.ts`, `frontend/src/features/warehouse/api/shipComponentsApi.ts`, and `frontend/src/features/warehouse/hooks/useAddInventoryItem.ts`.

**Checkpoint**: User Story 1 is independently functional and testable.

---

## Phase 4: User Story 2 - Default quality to 500 when omitted (Priority: P2)

**Goal**: Omitted quality reliably defaults to 500 for new and incremented adds, with existing rows treated as quality 500.

**Independent Test**: Submit add requests without quality through both add flows and verify saved/read quality is 500; verify pre-existing rows are surfaced as 500.

### Tests for User Story 2 ⚠️

- [X] T026 [P] [US2] Add omitted-quality default tests in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/AddInventoryItemHandlerTests.cs`.
- [X] T027 [P] [US2] Add API defaulting tests for omitted quality in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseEndpointsTests.cs` and `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/ShipComponentsEndpointTests.cs`.
- [X] T028 [P] [US2] Add migration/backfill tests for existing rows in `backend/tests/NajaEcho.Infrastructure.Tests/Warehouse/WarehouseInventoryRepositoryTests.cs`.
- [X] T029 [P] [US2] Add frontend default-value tests (`500`) in `frontend/src/features/warehouse/__tests__/AddInventoryDialog.test.tsx` and `frontend/src/features/warehouse/__tests__/ShipComponentsWrite.test.tsx`.

### Implementation for User Story 2

- [X] T030 [US2] Implement omitted-quality defaulting (`500`) in API endpoint request handling in `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`.
- [X] T031 [US2] Implement omitted-quality defaulting (`500`) in application add command/handler in `backend/src/NajaEcho.Application/Features/Warehouse/AddInventoryItem/AddInventoryItemCommand.cs` and `backend/src/NajaEcho.Application/Features/Warehouse/AddInventoryItem/AddInventoryItemHandler.cs`.
- [X] T032 [US2] Ensure repository add/increment logic persists effective quality for insert/update in `backend/src/NajaEcho.Infrastructure/Warehouse/WarehouseInventoryRepository.cs` and `backend/src/NajaEcho.Infrastructure/Warehouse/ShipComponentRepository.cs`.
- [X] T033 [US2] Finalize migration SQL for default + backfill behavior in `backend/src/NajaEcho.Infrastructure/Persistence/Migrations/`.
- [X] T034 [US2] Set frontend form default quality to `500` in `frontend/src/features/warehouse/components/AddInventoryDialog.tsx` and `frontend/src/features/warehouse/schemas/addItemSchemas.ts`.

**Checkpoint**: User Stories 1 and 2 are independently functional and testable.

---

## Phase 5: User Story 3 - Reject out-of-range quality values (Priority: P3)

**Goal**: Values outside 1..1000 are rejected with clear validation errors in backend and frontend.

**Independent Test**: Try quality `0`, `1001`, and non-integer values via UI/API and confirm validation errors and no persisted changes.

### Tests for User Story 3 ⚠️

- [X] T035 [P] [US3] Add backend validation tests for range and integer enforcement in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/AddInventoryItemHandlerTests.cs`.
- [X] T036 [P] [US3] Add API error-response tests for invalid quality values in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseEndpointsTests.cs` and `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/ShipComponentsEndpointTests.cs`.
- [X] T037 [P] [US3] Add frontend form validation tests for invalid quality inputs in `frontend/src/features/warehouse/__tests__/AddInventoryDialog.test.tsx`.

### Implementation for User Story 3

- [X] T038 [US3] Enforce quality range validation in add command/handler path in `backend/src/NajaEcho.Application/Features/Warehouse/AddInventoryItem/AddInventoryItemHandler.cs`.
- [X] T039 [US3] Return clear validation errors for invalid quality in `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`.
- [X] T040 [US3] Enforce quality bounds in database constraint mapping in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/WarehouseInventoryEntryConfiguration.cs`.
- [X] T041 [US3] Enforce frontend quality validation (`int`, min 1, max 1000) in `frontend/src/features/warehouse/schemas/addItemSchemas.ts` and `frontend/src/features/warehouse/components/AddInventoryDialog.tsx`.

**Checkpoint**: All user stories are independently functional and testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency, documentation, and end-to-end validation.

- [X] T042 [P] Update quality-field documentation references in `specs/001-add-item-quality/quickstart.md` and `README.md`.
- [X] T043 [P] Regenerate and verify frontend API type outputs in `frontend/src/lib/api/warehouse.d.ts` and `frontend/src/lib/api/ship-components.d.ts`.
- [X] T044 Run full backend and frontend test suites from quickstart commands using `backend/NajaEcho.slnx` and `frontend/package.json`, then resolve failures in impacted warehouse files.
- [X] T045 Execute manual quickstart validation scenarios and record outcomes in `specs/001-add-item-quality/quickstart.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies.
- **Phase 2 (Foundational)**: Depends on Phase 1; blocks all user stories.
- **Phase 3 (US1)**: Depends on Phase 2; MVP.
- **Phase 4 (US2)**: Depends on US1 data-path completion (T016-T020, T025) so default behavior builds on explicit-path wiring.
- **Phase 5 (US3)**: Depends on US1/US2 add-flow wiring.
- **Phase 6 (Polish)**: Depends on all user story phases.

### User Story Dependencies

- **US1 (P1)**: Can start after foundational phase; no dependency on other user stories.
- **US2 (P2)**: Depends on US1 add-path implementation but is independently testable once complete.
- **US3 (P3)**: Depends on add-path implementation and is independently testable once complete.

### Within Each User Story

- Write tests first and confirm failing state.
- Implement backend contract/handler/repository path.
- Implement frontend schema/form/rendering path.
- Re-run story-specific tests before moving on.

### Parallel Opportunities

- Phase 1 contract and script checks (`T001`, `T002`) can run in parallel.
- Phase 2 DTO/contract updates (`T008`, `T009`) can run in parallel.
- Within US1, backend and frontend test tasks (`T011`-`T015`) can run in parallel.
- Within US1 implementation, frontend schema/table tasks (`T021`-`T024`) can run in parallel after API payload shape is stable.
- Within US2 and US3, backend and frontend test tasks can run in parallel (`T026`-`T029`, `T035`-`T037`).

---

## Parallel Example: User Story 1

```bash
# Run US1 test authoring in parallel:
Task: "T011 Add explicit-quality handler tests in backend/tests/NajaEcho.Application.Tests/Features/Warehouse/AddInventoryItemHandlerTests.cs"
Task: "T012 Add explicit-quality API tests in backend/tests/NajaEcho.Api.Tests/Features/Warehouse/WarehouseEndpointsTests.cs"
Task: "T015 Add explicit-quality frontend tests in frontend/src/features/warehouse/__tests__/AddInventoryDialog.test.tsx"

# Run US1 frontend implementation tasks in parallel after payload wiring:
Task: "T021 Add quality field to frontend add schema in frontend/src/features/warehouse/schemas/addItemSchemas.ts"
Task: "T022 Include quality in frontend row schemas in frontend/src/features/warehouse/schemas/inventorySchemas.ts"
Task: "T024 Render quality in frontend tables in frontend/src/features/warehouse/components/InventoryTable.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational)
3. Complete Phase 3 (US1)
4. Validate US1 independently via explicit-quality add/read scenarios

### Incremental Delivery

1. Deliver US1 (explicit quality)
2. Deliver US2 (default handling/backfill)
3. Deliver US3 (strict validation and error feedback)
4. Finish with Phase 6 polish

### Parallel Team Strategy

1. One developer completes Phase 1-2 foundations.
2. Then split:
   - Dev A: backend US1/US2
   - Dev B: frontend US1/US2
   - Dev C: US3 validation + cross-cutting tests

---

## Notes

- Every task uses the required checklist format with Task ID and explicit file path.
- `[P]` markers only appear where work is parallelizable.
- User-story tasks include `[US1]`, `[US2]`, or `[US3]` labels for traceability.
