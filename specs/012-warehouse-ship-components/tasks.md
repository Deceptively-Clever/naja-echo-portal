# Tasks: Warehouse Ship Components Subpage

**Input**: Design documents from `/specs/012-warehouse-ship-components/`

**Branch**: `012-warehouse-ship-components` | **Date**: 2026-06-14

**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/openapi.yaml ✅ quickstart.md ✅

**Tests**: Included per Constitution II (TDD non-negotiable). Write tests FIRST, confirm they FAIL, then implement.

**Organization**: Tasks grouped by user story to enable independent implementation and delivery.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other [P] tasks in the same phase (different files, no shared dependencies)
- **[Story]**: Which user story this task belongs to (US1/US2/US3)
- Exact file paths included in every task description

---

## Phase 1: Setup

**Purpose**: Wire up the API type-generation script so frontend types can be generated from the committed `contracts/openapi.yaml` before any implementation begins (Constitution I).

- [X] T001 Add `gen:api:ship-components` npm script to `frontend/package.json` pointing at `../specs/012-warehouse-ship-components/contracts/openapi.yaml` (path relative to `frontend/`) with output `src/lib/api/ship-components.d.ts` (mirror the existing `gen:api:*` pattern)
- [X] T002 Run `npm run gen:api:ship-components` from `frontend/` to emit `frontend/src/lib/api/ship-components.d.ts` (commit the generated file)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, EF configuration, migration, and core abstractions that ALL user stories depend on. Nothing in Phase 3+ can begin until this phase is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 [P] Create `ItemAttribute.cs` domain entity in `backend/src/NajaEcho.Domain/Warehouse/` with all fields per data-model.md (Id, ItemId, UexAttributeId, UexItemId, UexCategoryId, UexCategoryAttributeId, AttributeName, Value, Unit, SourceDateAdded, SourceDateModified, FetchedAt)
- [X] T004 [P] Create `ShipComponentAttributes.cs` domain entity in `backend/src/NajaEcho.Domain/Warehouse/` with ItemId PK, Class (string?), Size (int?), Grade (string?), AttributesFetchedAt
- [X] T005 [P] Create `ItemAttributeConfiguration.cs` in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/` — maps to `sc.item_attributes`, snake_case columns, unique index `ux_item_attributes_item_category_attr` on (item_id, uex_category_attribute_id), index `ix_item_attributes_item_id`, FK → `sc.items.id` OnDelete Cascade
- [X] T006 [P] Create `ShipComponentAttributesConfiguration.cs` in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/` — maps to `sc.ship_component_attributes`, item_id as PK, indexes on class/size/grade, FK → `sc.items.id` OnDelete Cascade
- [X] T007 Add `DbSet<ItemAttribute>` and `DbSet<ShipComponentAttributes>` to `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs` and register both configurations in `OnModelCreating`
- [X] T008 Create EF Core migration `AddShipComponentAttributes` via `dotnet ef migrations add AddShipComponentAttributes --project src/NajaEcho.Infrastructure --startup-project src/NajaEcho.Api` from `backend/` and apply to dev DB with `dotnet ef database update`
- [X] T009 [P] Create `IUexItemAttributeClient.cs` abstraction in `backend/src/NajaEcho.Application/Abstractions/` with `Task<IReadOnlyList<JsonDocument>> FetchItemAttributesAsync(int uexItemId, CancellationToken ct)`
- [X] T010 [P] Create `IShipComponentRepository.cs` abstraction in `backend/src/NajaEcho.Application/Abstractions/` declaring all repository methods: `GetShipComponentsAsync`, `GetShipComponentFiltersAsync`, `SearchSystemsCatalogAsync`, `HasCachedAttributesAsync`, `SaveItemAttributesAsync`, `UpsertShipComponentAttributesAsync`
- [X] T011 [P] Create `ShipComponentDtos.cs` in `backend/src/NajaEcho.Api/Features/Warehouse/Contracts/` with all request/response records per contracts/openapi.yaml: `ShipComponentRow`, `ShipComponentListResponse`, `ShipComponentFiltersResponse`, `OwnerOption`, `SystemsCatalogItem`, `SystemsCatalogResponse`, `AddShipComponentRequest`

**Checkpoint**: Foundation complete — all three user story phases can begin.

---

## Phase 3: User Story 1 — View Ship Components Inventory (Priority: P1) 🎯 MVP

**Goal**: Any authenticated user can navigate to Warehouse → Ship Components and see a Systems-only inventory table with 8 columns (Name, Type, Class, Size, Grade, Quantity, Owner, Location), default multi-key sort, Unknown displayed for null attributes, and auth redirect for anonymous users.

**Independent Test**: Navigate to the Ship Components page as an authenticated user and confirm the table shows only Systems-section rows, all 8 columns (no Section column), correct default sort, and Unknown in null attribute cells. Hit the route while signed out and confirm login redirect.

### Tests for User Story 1 — Write FIRST, confirm FAIL before implementing

- [X] T012 [P] [US1] Create `GetShipComponentsQueryHandlerTests.cs` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/ShipComponents/GetShipComponents/` — tests: Systems-only scoping (non-Systems rows excluded), derived Class/Size/Grade from ShipComponentAttributes (null Size → null in DTO, null Class/Grade → null in DTO), multi-key sort (Name ↑ → Type ↑ → Size ↑ NULLS LAST → Class ↑ NULLS LAST → Grade ↑ NULLS LAST)
- [X] T013 [P] [US1] Create `ShipComponentRepositoryTests.cs` in `backend/tests/NajaEcho.Infrastructure.Tests/Warehouse/` (Testcontainers/PostgreSQL) — tests: sc.item_attributes unique constraint on (item_id, uex_category_attribute_id) blocks duplicate insert; sc.ship_component_attributes PK enforces one row per item; Systems-only list query returns only Systems rows with correct attribute left-join projection
- [X] T014 [P] [US1] Create `ShipComponentsEndpointTests.cs` in `backend/tests/NajaEcho.Api.Tests/Features/Warehouse/` — tests: GET /api/warehouse/ship-components returns 200 for authenticated user; returns 401/redirect for anonymous; response contains only Systems-section rows; null Class/Size/Grade are serialized as JSON null
- [X] T015 [P] [US1] Create `ShipComponentsTable.test.tsx` in `frontend/src/features/warehouse/__tests__/` — tests: renders 8 columns (Name, Type, Class, Size, Grade, Quantity, Owner, Location) with no Section column; renders "Unknown" for null class/size/grade; renders empty-inventory state when items array is empty

### Implementation for User Story 1

- [X] T016 [P] [US1] Create `GetShipComponentsQuery.cs` (query + row DTO) and `GetShipComponentsQueryHandler.cs` in `backend/src/NajaEcho.Application/Features/Warehouse/ShipComponents/GetShipComponents/` — handler calls `IShipComponentRepository.GetShipComponentsAsync`, maps nullable Class/Size/Grade to DTO
- [X] T017 [P] [US1] Implement `ShipComponentRepository.cs` in `backend/src/NajaEcho.Infrastructure/Warehouse/` — `GetShipComponentsAsync`: joins `warehouse_inventory` → `sc.items` (WHERE section = 'Systems') → `sc.ship_component_attributes` (LEFT JOIN) → `AspNetUsers`; applies server-side filters; ORDER BY name, category, size NULLS LAST, class NULLS LAST, grade NULLS LAST
- [X] T018 [US1] Register `IShipComponentRepository` / `ShipComponentRepository` as `AddScoped` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`; register `GetShipComponentsQueryHandler` as `AddScoped`
- [X] T019 [US1] Add `GET /api/warehouse/ship-components` endpoint to `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs` — `.RequireAuthorization()`, accepts filter query params (name, type[], class[], size[], grade[], ownerUserId[], location[], unknownClass, unknownSize, unknownGrade), dispatches `GetShipComponentsQuery`, returns `ShipComponentListResponse`
- [X] T020 [P] [US1] Create `shipComponentSchemas.ts` in `frontend/src/features/warehouse/schemas/` — Zod schemas for `ShipComponentRow` (nullable class/size/grade) and `ShipComponentListResponse`, typed from `frontend/src/lib/api/ship-components.d.ts`
- [X] T021 [P] [US1] Create `shipComponentsApi.ts` in `frontend/src/features/warehouse/api/` — `apiFetch` wrapper for `getShipComponents(params)` calling `GET /api/warehouse/ship-components` with typed query params
- [X] T022 [P] [US1] Extend `warehouseQueryKeys.ts` in `frontend/src/features/warehouse/hooks/` with ship-component query key factory (list key with filter params)
- [X] T023 [US1] Create `useShipComponents.ts` in `frontend/src/features/warehouse/hooks/` — TanStack Query `useQuery` over `shipComponentsApi.getShipComponents`, depends on T020/T021/T022
- [X] T024 [US1] Create `ShipComponentsTable.tsx` in `frontend/src/features/warehouse/components/` — renders 8-column table (Name, Type, Class, Size, Grade, Quantity, Owner, Location); displays "Unknown" for null class/size/grade; empty-inventory state
- [X] T025 [US1] Create `ShipComponentsView.tsx` thin route view in `frontend/src/features/warehouse/pages/` — composes `useShipComponents` + `ShipComponentsTable`; no business logic
- [X] T026 [US1] Update `frontend/src/features/dashboard/navigation/navItems.ts` — add Ship Components (path `/warehouse/ship-components`) and Materials placeholder (path `/warehouse/materials`) to the Warehouse group
- [X] T027 [US1] Update `frontend/src/routes/AppRouter.tsx` — add `/warehouse/ship-components` route (renders `ShipComponentsView`) and `/warehouse/materials` placeholder route inside `ProtectedRoute` + `DashboardLayout`

**Checkpoint**: User Story 1 complete — authenticated users can view Systems-only inventory with 8 columns, correct sort, Unknown cells, and auth redirect for anonymous.

---

## Phase 4: User Story 2 — Filter Ship Components (Priority: P2)

**Goal**: Authenticated users can narrow the Ship Components list by Name (partial, case-insensitive), Type/Class/Size/Grade/Owner/Location (multi-select OR within field, AND across fields), with "Unknown" appearing as a selectable option for Class/Size/Grade when null rows exist, and a clear/reset action.

**Independent Test**: Apply each filter type individually and in combination; verify correct AND/OR semantics. Apply "Unknown" Class filter and confirm only null-class rows appear. Click Clear and confirm full list returns.

### Tests for User Story 2 — Write FIRST, confirm FAIL before implementing

- [X] T028 [P] [US2] Create `GetShipComponentFiltersQueryHandlerTests.cs` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/ShipComponents/GetShipComponentFilters/` — tests: returns distinct Type/Class/Size/Grade/Owner/Location drawn only from Systems inventory; unknownClass=true when at least one Systems row has null class; unknownSize/Grade same logic; empty inventory → all lists empty + all Unknown flags false
- [X] T029 [P] [US2] Add filter integration tests to `ShipComponentRepositoryTests.cs` — `GetShipComponentsAsync` with name partial match (case-insensitive), multi-value type OR, multi-field AND (type + class), unknownClass sentinel returns only null-class rows, empty filter params returns all rows
- [X] T030 [P] [US2] Add filter endpoint tests to `ShipComponentsEndpointTests.cs` — GET /api/warehouse/ship-components/filters: 200 with correct option lists and Unknown flags; GET /ship-components with ?unknownClass=true returns only null-class rows; multi-value ?type=A&type=B returns rows for either type
- [X] T031 [P] [US2] Create `ShipComponentsFilters.test.tsx` in `frontend/src/features/warehouse/__tests__/` — tests: Unknown option only visible in Class/Size/Grade filter dropdowns when unknownClass/Size/Grade is true; selecting Unknown filter and submitting produces correct API call; Clear button resets all filter state; no-results empty state displays when filtered items list is empty

### Implementation for User Story 2

- [X] T032 [P] [US2] Create `GetShipComponentFiltersQuery.cs` + `GetShipComponentFiltersQueryHandler.cs` in `backend/src/NajaEcho.Application/Features/Warehouse/ShipComponents/GetShipComponentFilters/` — calls `IShipComponentRepository.GetShipComponentFiltersAsync`, maps to `ShipComponentFiltersResponse`
- [X] T033 [US2] Implement `GetShipComponentFiltersAsync` method on `ShipComponentRepository.cs` in `backend/src/NajaEcho.Infrastructure/Warehouse/` — queries distinct Type/Class/Size/Grade/Owner/Location over Systems inventory; sets unknownClass/Size/Grade boolean flags
- [X] T034 [US2] Implement filter parameters on `GetShipComponentsAsync` in `ShipComponentRepository.cs` — name partial (ILIKE), type/class/grade/location multi-value OR (ANY), size multi-value OR, unknownClass/Size/Grade sentinel (IS NULL), all AND across fields; empty params ignored
- [X] T035 [US2] Register `GetShipComponentFiltersQueryHandler` as `AddScoped` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`; add `GET /api/warehouse/ship-components/filters` endpoint to `WarehouseEndpoints.cs` (`.RequireAuthorization()`, returns `ShipComponentFiltersResponse`)
- [X] T036 [P] [US2] Extend `shipComponentSchemas.ts` with Zod schemas for `ShipComponentFiltersResponse`, `OwnerOption`, and the filter form state shape
- [X] T037 [P] [US2] Extend `shipComponentsApi.ts` with `getShipComponentFilters()` calling `GET /api/warehouse/ship-components/filters`
- [X] T038 [P] [US2] Create `useShipComponentFilters.ts` in `frontend/src/features/warehouse/hooks/` — TanStack Query `useQuery` for filter options
- [X] T039 [P] [US2] Create `ShipComponentsFilters.tsx` in `frontend/src/features/warehouse/components/` — Name text input, multi-select dropdowns for Type/Class/Size/Grade/Owner/Location; Unknown option shown only when unknownClass/Size/Grade flag is true; Clear/reset action
- [X] T040 [US2] Wire `ShipComponentsFilters` and `useShipComponentFilters` into `ShipComponentsView.tsx` — filter state drives query params passed to `useShipComponents`; add no-results empty state when filtered list is empty

**Checkpoint**: User Story 2 complete — all filter types work independently and in combination; Unknown filter works; clear resets all filters.

---

## Phase 5: User Story 3 — Quartermaster Manages Ship Component Inventory (Priority: P3)

**Goal**: A Quartermaster (or Admin) can add a new Ship Component inventory row (catalog search restricted to Systems items; derived fields non-editable; lazy UEX attribute fetch/cache on first add; fetch failure non-blocking), edit quantity/owner/location (reused 011 endpoints), and delete rows. Non-Quartermasters see no write controls.

**Independent Test**: As Quartermaster, add a row for a Systems item with no cached attributes, confirm the row appears and `sc.item_attributes` + `sc.ship_component_attributes` are populated. Add same item again and confirm no second UEX call. Kill the UEX host and add another item — row still creates, attributes stay Unknown. Attempt non-Systems item add → 422. As non-QM, confirm no add/edit/delete controls visible.

### Tests for User Story 3 — Write FIRST, confirm FAIL before implementing

- [X] T041 [P] [US3] Create `AddShipComponentHandlerTests.cs` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/ShipComponents/AddShipComponent/` — tests: (a) rejects non-Systems item with domain error; (b) rejects inactive item with domain error; (c) lazy fetch triggered when no cached attributes exist for item; (d) no re-fetch when `sc.item_attributes` rows already present for item; (e) fetch failure is caught, logged, and does not abort inventory creation — row created with null attributes; (f) AddOrIncrementAsync called for valid add
- [X] T042 [P] [US3] Create `SearchSystemsCatalogQueryHandlerTests.cs` in `backend/tests/NajaEcho.Application.Tests/Features/Warehouse/ShipComponents/SearchSystemsCatalog/` — tests: returns only Systems-section items; case-insensitive partial name match; limit param respected; non-Systems items excluded; inactive Systems items excluded (status ≠ Active does not appear in results)
- [X] T043 [P] [US3] Add attribute upsert integration tests to `ShipComponentRepositoryTests.cs` — `SaveItemAttributesAsync` inserts rows and respects unique (item_id, uex_category_attribute_id) constraint (no error on duplicate, upsert semantics); `UpsertShipComponentAttributesAsync` inserts then updates typed projection; `HasCachedAttributesAsync` returns false when no item_attributes rows exist, true when present; Size text→int parse: attribute_name "Size" with valid numeric string → correct int in projection; non-numeric or absent "Size" value → null size in projection
- [X] T044 [P] [US3] Add write endpoint tests to `ShipComponentsEndpointTests.cs` — POST /api/warehouse/ship-components: 403 for authenticated non-QM; 200/201 for Quartermaster; 200/201 for Admin; 422 when itemId references non-Systems item; 422 when itemId references inactive item; GET /ship-components/catalog/search: 403 for non-QM, 200 for QM with Systems-only results
- [X] T045 [P] [US3] Create `ShipComponentsWrite.test.tsx` in `frontend/src/features/warehouse/__tests__/` — tests: add/edit/delete controls absent for non-Quartermaster authenticated user; add dialog catalog search returns only Systems items; Class/Size/Grade/Name/Type fields are read-only in add dialog; form requires Quantity ≥ 1 and non-empty Location

### Implementation for User Story 3

- [X] T046 [P] [US3] Create `UexItemAttributeClient.cs` in `backend/src/NajaEcho.Infrastructure/Items/` — typed `HttpClient` implementing `IUexItemAttributeClient`; calls `items_attributes?id_item={uexItemId}` on `UexVehicleClient:BaseUrl`; parses `{ "data": [...] }` envelope; returns `IReadOnlyList<JsonDocument>`; mirrors `UexItemClient` pattern exactly
- [X] T047 [P] [US3] Create `SearchSystemsCatalogQuery.cs` + `SearchSystemsCatalogQueryHandler.cs` in `backend/src/NajaEcho.Application/Features/Warehouse/ShipComponents/SearchSystemsCatalog/` — queries `sc.items` WHERE section = 'Systems' AND status = Active (Active filter prevents inactive items from appearing in the add dialog before the add handler rejects them), ILIKE name, LIMIT, returns `SystemsCatalogResponse`
- [X] T048 [P] [US3] Create `AddShipComponentCommand.cs` + `AddShipComponentHandler.cs` in `backend/src/NajaEcho.Application/Features/Warehouse/ShipComponents/AddShipComponent/` — validates item Active + Systems section (422 otherwise); checks `HasCachedAttributesAsync`; if no cache and UexId > 0: calls `IUexItemAttributeClient.FetchItemAttributesAsync`, calls `SaveItemAttributesAsync`, calls `UpsertShipComponentAttributesAsync` (Class/Size/Grade mapped from attribute_name, Size parsed to int); any UEX/parse failure caught + Serilog warning logged, inventory creation continues; calls `AddOrIncrementAsync`
- [X] T049 [US3] Implement `SearchSystemsCatalogAsync`, `HasCachedAttributesAsync`, `SaveItemAttributesAsync`, `UpsertShipComponentAttributesAsync` methods on `ShipComponentRepository.cs` in `backend/src/NajaEcho.Infrastructure/Warehouse/` — upsert for item_attributes respects unique (item_id, uex_category_attribute_id); projection upsert keyed by item_id
- [X] T050 [US3] Register `IUexItemAttributeClient` / `UexItemAttributeClient` via `AddHttpClient<>` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` with the existing `UexVehicleClient:BaseUrl` base address; register `AddShipComponentHandler` and `SearchSystemsCatalogQueryHandler` as `AddScoped`
- [X] T051 [US3] Add `GET /api/warehouse/ship-components/catalog/search` (`.RequireAuthorization(AuthorizationPolicies.Quartermaster)`, query params: search, limit) and `POST /api/warehouse/ship-components` (`.RequireAuthorization(AuthorizationPolicies.Quartermaster)`, body: `AddShipComponentRequest`) endpoints to `backend/src/NajaEcho.Api/Features/Warehouse/WarehouseEndpoints.cs`
- [X] T052 [P] [US3] Extend `shipComponentsApi.ts` with `searchSystemsCatalog(search, limit)` and `addShipComponent(request)` apiFetch wrappers
- [X] T053 [P] [US3] Create `useSystemsCatalogSearch.ts` in `frontend/src/features/warehouse/hooks/` — TanStack Query `useQuery` (or enabled-when-open pattern) for Systems catalog search
- [X] T054 [P] [US3] Create `useAddShipComponent.ts` in `frontend/src/features/warehouse/hooks/` — TanStack Query `useMutation` for `addShipComponent`, invalidates ship-components list query key on success
- [X] T055 [US3] Extend `frontend/src/features/warehouse/components/AddInventoryDialog.tsx` to accept a `scope` prop (or equivalent) that, when set to `"ship-components"`, wires `useSystemsCatalogSearch` for item selection (Systems-only results), renders Name/Type/Class/Size/Grade as read-only derived preview fields, and exposes only Quantity/Owner/Location as editable inputs; uses `useAddShipComponent` mutation — no new dialog file; the existing dialog handles both scopes
- [X] T056 [US3] Wire Quartermaster write controls into `ShipComponentsView.tsx` — show Add button + `AddInventoryDialog` (Systems scope), `EditQuantityControl`, and `RemoveInventoryButton` per row conditional on `useIsQuartermaster()`; reuse existing 011 components/hooks for edit/remove (they already target the correct `PUT /items/{id}/quantity` and `DELETE /items/{id}` endpoints)

**Checkpoint**: All three user stories complete and independently testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Observability, final validation, and cleanup.

- [X] T057 [P] Add structured Serilog logging to `AddShipComponentHandler.cs` — log on attribute fetch attempt (item id, uex id), fetch success (attribute count), fetch failure (reason, item id, uex id), and cache hit (skipping fetch); never log tokens or auth data (Constitution V)
- [X] T058 [P] Add structured Serilog logging to `UexItemAttributeClient.cs` — log fetch start (uex item id), success (row count), and failure (status code/exception message) at appropriate Serilog levels
- [X] T059 Run full quickstart.md validation — all 9 manual scenarios from `specs/012-warehouse-ship-components/quickstart.md`, confirm all automated tests pass (`dotnet test` from `backend/`, `npm run test:run` from `frontend/`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: T001 → T002 (sequential). No other dependencies. Start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1 completion. T003–T006 are parallel. T007 depends on T003–T006. T008 depends on T007. T009–T011 are parallel and can run alongside T003–T008 once Phase 1 is done.
- **Phase 3 (US1)**: Depends on Phase 2 completion (T008 = migration applied). Tests T012–T015 can run in parallel. Implementation T016–T019 sequential within backend. T020–T022 parallel (frontend schemas/API/keys). T023 depends on T020–T022. T024 depends on T020+T023. T025 depends on T024+T023. T026–T027 parallel (different files).
- **Phase 4 (US2)**: Depends on Phase 3 completion. Tests T028–T031 parallel. T032 parallel with T033–T034. T033–T034 implement on `ShipComponentRepository` (same file — sequential). T035 depends on T032–T034. T036–T039 parallel (different files). T040 depends on T038–T039.
- **Phase 5 (US3)**: Depends on Phase 2 completion (can start parallel with Phase 3 after Phase 2 if separate developer). Tests T041–T045 parallel. T046–T048 parallel. T049 depends on T046–T048. T050 depends on T046. T051 depends on T049–T050. T052 parallel. T053–T054 parallel (depend on T052). T055 depends on T053–T054. T056 depends on T055.
- **Phase 6 (Polish)**: Depends on all implementation phases complete. T057–T058 parallel. T059 depends on T057–T058.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2. Fully independent — no other story required.
- **US2 (P2)**: Starts after Phase 2. Extends US1 (filters feed into the same list endpoint and view). Should start after US1 Phase 3 is complete.
- **US3 (P3)**: Starts after Phase 2. Backend fully independent of US2. Frontend wires into the same view as US1/US2. Can be developed in parallel with US2 by a second developer if desired.

### Parallel Opportunities Within Phase 2

```
T003 (ItemAttribute entity)     ─┐
T004 (ShipComponentAttributes)  ─┤→ T007 (AppDbContext) → T008 (migration)
T005 (ItemAttributeConfig)      ─┤
T006 (ShipComponentAttrConfig)  ─┘
T009 (IUexItemAttributeClient)  ─ independent
T010 (IShipComponentRepository) ─ independent
T011 (ShipComponentDtos)        ─ independent
```

### Parallel Opportunities Within Phase 3 (US1)

```
T012 (App unit tests)    ─┐
T013 (Infra tests)       ─┤ parallel
T014 (API endpoint tests)─┤
T015 (Frontend tests)    ─┘

T020 (Zod schemas)       ─┐
T021 (API client)        ─┤ parallel → T023 (useShipComponents) → T024 (Table) → T025 (View)
T022 (query keys)        ─┘

T016 (Query + Handler)   ─┐
T017 (Repository impl)   ─┤ parallel → T018 (DI) → T019 (endpoint)
T026 (navItems.ts)       ─┘
T027 (AppRouter.tsx)     ─ parallel with T026
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T002)
2. Complete Phase 2: Foundational (T003–T011)
3. Complete Phase 3: US1 — tests first (T012–T015), then implementation (T016–T027)
4. **STOP and VALIDATE**: Quickstart scenario 1 + 2; run `dotnet test` + `npm run test:run`
5. Authenticated users can view Systems-only inventory — ship it.

### Incremental Delivery

1. Setup + Foundational → commit
2. US1 complete → authenticated view works → commit/demo (MVP)
3. US2 complete → filters work → commit/demo
4. US3 complete → Quartermaster write flow works → commit/demo
5. Each delivery passes full test suite without regression

### Parallel Team Strategy (2 developers)

After Phase 2 is complete:
- **Developer A**: US1 (Phase 3, T012–T027) then US2 (Phase 4, T028–T040) 
- **Developer B**: US3 (Phase 5, T041–T056) — backend is independent; frontend wires into the view after Developer A's ShipComponentsView.tsx is in place

---

## Notes

- [P] marks tasks that touch different files with no shared dependency — safe to parallelize
- TDD is mandatory (Constitution II): tests MUST be written and confirmed RED before the implementation tasks in the same phase begin
- EF migration (T008) must be applied to the dev DB before any Testcontainers-backed repository test can pass (tests create their own container, but the migration SQL must exist in the project)
- `AddOrIncrementAsync` is reused from feature 011 — do not reimplement; the `AddShipComponentHandler` calls through `IShipComponentRepository` which delegates to the existing warehouse repository
- `PUT /api/warehouse/items/{id}/quantity` and `DELETE /api/warehouse/items/{id}` are reused from 011 verbatim — no new endpoint tasks for those
- The `AddInventoryDialog` reuse (T055) adapts the existing dialog, not a fork — Systems-scope and derived-field preview are the only differences
- Unknown/null handling: server returns null JSON values; frontend renders "Unknown"; "Unknown" is never stored as a string in the DB
- Unknown filter sentinel: the `unknownClass`/`unknownSize`/`unknownGrade` boolean query params on GET /ship-components are the mechanism — no sentinel string stored
