# Tasks: Item Data Import

**Input**: Design documents from `/specs/009-item-data-import/`

**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/openapi.yaml ✅ | quickstart.md ✅

**Tests**: Included per constitution Principle II (TDD is NON-NEGOTIABLE — tests are written and
confirmed failing before any production code for that behaviour is written).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.
US4 (Access Control) and the endpoint group setup are folded into Phase 2 (Foundational) because
they are one-line policy calls established when the endpoint group is created — not a separate
workflow slice.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no blocking dependency)
- **[Story]**: Which user story (US1–US5 from spec.md)
- Exact file paths are included in every description

## Path Conventions

- `backend/src/` — .NET project source
- `backend/tests/` — .NET test projects
- `frontend/src/` — React SPA source

---

## Phase 1: Setup

**Purpose**: Register the new API contract in the frontend type-generation pipeline so generated
types are available from the start. No code changes needed in the backend structure (already exists).

- [X] T001 Add `"gen:api:items": "openapi-typescript ../specs/009-item-data-import/contracts/openapi.yaml -o src/lib/api/items.d.ts"` to the `scripts` block in `frontend/package.json`
- [X] T002 Run `npm run gen:api:items` from `frontend/` and commit the generated `frontend/src/lib/api/items.d.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, EF configuration, migration, abstractions, UEX clients, repository
implementations, DI registration, and the admin endpoint group (satisfying US4 admin-only access).

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Domain entities & abstractions (all parallelizable — separate files, no cross-dependencies)

- [X] T003 [P] Create `ItemStatus` enum (`Active | SoftDeleted`) in `backend/src/NajaEcho.Domain/Items/ItemStatus.cs` — mirrors `ShipStatus`
- [X] T004 [P] Create `ItemCategory` entity (all fields from data-model.md) in `backend/src/NajaEcho.Domain/ItemCategories/ItemCategory.cs`
- [X] T005 [P] Create `Item` entity (all promoted fields from data-model.md; no `attributes`, no `screenshot` columns) in `backend/src/NajaEcho.Domain/Items/Item.cs`
- [X] T006 [P] Create `IItemCategoryRepository` abstraction (BulkUpsertAsync, GetAllAsync, GetEligibleAsync, GetLastRefreshedAtAsync) in `backend/src/NajaEcho.Application/Abstractions/IItemCategoryRepository.cs`
- [X] T007 [P] Create `IItemRepository` abstraction (BulkUpsertForCategoryAsync, GetCountByCategoryAsync, GetLastImportedAtByCategoryAsync) in `backend/src/NajaEcho.Application/Abstractions/IItemRepository.cs`
- [X] T008 [P] Create `IUexCategoryClient` abstraction (`FetchAllCategoriesAsync`) in `backend/src/NajaEcho.Application/Abstractions/IUexCategoryClient.cs`
- [X] T009 [P] Create `IUexItemClient` abstraction (`FetchItemsByCategoryAsync(int categoryId)`) in `backend/src/NajaEcho.Application/Abstractions/IUexItemClient.cs`

### EF Core configuration & migration (sequential — depend on entities above)

- [X] T010 Create `ItemCategoryConfiguration` (table `sc.item_categories`, PK, columns, `jsonb raw_data`, unique index on `uex_id`, indexes on `type` and `section`) in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/ItemCategoryConfiguration.cs`
- [X] T011 Create `ItemConfiguration` (table `sc.items`, PK, columns, `HasConversion<string>()` for status, `jsonb raw_data`, unique index on `uuid`, indexes on `uex_id`, `id_category`, `status`) in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/ItemConfiguration.cs`
- [X] T012 Add `DbSet<ItemCategory> ItemCategories` and `DbSet<Item> Items` to `AppDbContext` and apply both configurations in `OnModelCreating` in `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs`
- [X] T013 Generate EF Core migration `AddItemCategoriesAndItems` via `dotnet ef migrations add AddItemCategoriesAndItems -p backend/src/NajaEcho.Infrastructure -s backend/src/NajaEcho.Api`; review the generated files in `backend/src/NajaEcho.Infrastructure/Persistence/Migrations/`

### Infrastructure implementations (parallelizable pairs)

- [X] T014 [P] Implement `UexCategoryClient` (typed `HttpClient`, `FetchAllCategoriesAsync`, parses `{ "data": [...] }` envelope, throws on invalid shape) in `backend/src/NajaEcho.Infrastructure/ItemCategories/UexCategoryClient.cs`
- [X] T015 [P] Implement `UexItemClient` (`FetchItemsByCategoryAsync(int categoryId)`, same envelope pattern, `?id_category={categoryId}` query param) in `backend/src/NajaEcho.Infrastructure/Items/UexItemClient.cs`
- [X] T016 [P] Implement `ItemCategoryRepository` (upsert by `uex_id` in a transaction; compute inserted/updated/unchanged; update `UpdatedAt`) in `backend/src/NajaEcho.Infrastructure/ItemCategories/ItemCategoryRepository.cs`
- [X] T017 Implement `ItemRepository.BulkUpsertForCategoryAsync` (load existing items WHERE `id_category == categoryId`; upsert by `uuid`; restore SoftDeleted rows that reappear; insert new; soft-delete Active rows in this category absent from incoming set; all in one transaction) in `backend/src/NajaEcho.Infrastructure/Items/ItemRepository.cs` — this is the most critical infrastructure task; see data-model.md state-transition table

### DI registration & endpoint group (depends on T014–T017)

- [X] T018 Register in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`: `AddHttpClient<IUexCategoryClient, UexCategoryClient>` (same base URL config key as vehicle client); `AddHttpClient<IUexItemClient, UexItemClient>`; `AddScoped<IItemCategoryRepository, ItemCategoryRepository>`; `AddScoped<IItemRepository, ItemRepository>`
- [X] T019 Create `ItemAdminEndpoints` with `app.MapGroup("/api/admin/items").RequireAuthorization(AuthorizationPolicies.Admin)` and stub registrations for all three endpoints; register `app.MapItemAdminEndpoints()` in `backend/src/NajaEcho.Api/Program.cs` in `backend/src/NajaEcho.Api/Features/Admin/Items/ItemAdminEndpoints.cs`

**Checkpoint**: Foundation complete. Run `dotnet build` — should compile clean. Run `./migrate.sh` — migration applies successfully. US4 (admin-only access) is satisfied: the group policy rejects non-admins for all Items endpoints.

---

## Phase 3: US1 — Category Refresh (Priority: P1) 🎯 MVP

**Goal**: Admins can refresh UEX categories from the Items tab and see a result summary with
last-refreshed timestamp. Item import actions are disabled (empty state) until categories exist.

**Independent Test**: Navigate to Data Import → Items tab. Trigger "Refresh Categories". Observe
categories stored, summary shown, last-refreshed timestamp displayed. Verify 403 for non-admin,
409 for concurrent call, 502 on UEX failure.

### Tests — write and confirm FAILING before implementing (US1)

- [X] T020 [P] [US1] Write `RefreshCategoriesHandlerTests` (mock `IUexCategoryClient`, `IItemCategoryRepository`, `IImportCoordinator`): test inserted/updated/unchanged counts, timing fields present, `ImportAlreadyInProgressException` thrown when lock not acquired, feed-failure propagates in `backend/tests/NajaEcho.Application.Tests/Features/ItemCategories/RefreshCategoriesHandlerTests.cs`
- [X] T021 [P] [US1] Write `ItemCategoryRepositoryTests` (Testcontainers PostgreSQL): (a) upsert-by-uex-id inserts new categories, updates existing, returns accurate inserted/updated/unchanged counts; (b) verify `UpdatedAt` advanced; (c) verify `GetLastRefreshedAtAsync` returns `MAX(updated_at)`; (d) **FR-003 atomicity**: seed one valid category, inject a failure mid-upsert (e.g. duplicate constraint violation on a second record), verify the transaction rolled back and no categories were committed; (e) **GetEligibleAsync**: seed categories with mixed `type` values, assert only `type = "item"` rows are returned in `backend/tests/NajaEcho.Infrastructure.Tests/ItemCategories/ItemCategoryRepositoryTests.cs`
- [X] T022 [P] [US1] Write `ItemAdminEndpointsTests` for the refresh endpoint (in-memory provider + fake coordinator + fake category client): admin 200 with summary payload, non-admin 403, coordinator returns false → 409, client throws HttpRequestException → 502 in `backend/tests/NajaEcho.Api.Tests/Features/Admin/ItemAdminEndpointsTests.cs`
- [X] T023 [P] [US1] Write `itemsImportTab.test.tsx` (Vitest + RTL + MSW): Items tab renders; when `GET /api/admin/items/categories` returns empty list, import actions are disabled and empty-state message is shown; when categories exist, last-refreshed timestamp is shown in `frontend/src/features/admin/__tests__/itemsImportTab.test.tsx`

### Implementation — US1

- [X] T024 [P] [US1] Create `RefreshCategoriesCommand` (no fields) and `RefreshCategoriesResult` (all fields from data-model.md RefreshCategoriesResult) in `backend/src/NajaEcho.Application/Features/ItemCategories/RefreshCategories/`
- [X] T025 [P] [US1] Create `GetCategoriesQuery` (no filters — full list returned; client-side filters in frontend), `GetCategoriesHandler`, and `CategoryListItem` (all selector-context fields including `LocalItemCount` and `LastImportedAt`) in `backend/src/NajaEcho.Application/Features/ItemCategories/GetCategories/`
- [X] T026 [US1] Implement `RefreshCategoriesHandler` (acquire `IImportCoordinator`; fetch via `IUexCategoryClient`; call `IItemCategoryRepository.BulkUpsertAsync`; record `StartedAt`/`CompletedAt`; log counts; release in finally) in `backend/src/NajaEcho.Application/Features/ItemCategories/RefreshCategories/RefreshCategoriesHandler.cs`
- [X] T027 [P] [US1] Create API contract DTOs: `RefreshCategoriesResponse`, `CategoryListItemResponse`, `CategoryListResponse` in `backend/src/NajaEcho.Api/Features/Admin/Items/Contracts/`
- [X] T028 [US1] Register `RefreshCategoriesHandler` and `GetCategoriesHandler` in `DependencyInjection.cs`; wire `GET /categories` → `GetCategoriesHandler` and `POST /categories/refresh` → `RefreshCategoriesHandler` in `ItemAdminEndpoints` with proper error mapping (409, 502) in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` and `backend/src/NajaEcho.Api/Features/Admin/Items/ItemAdminEndpoints.cs`
- [X] T029 [P] [US1] Create `itemSchemas.ts` (Zod: `categoryListItemSchema`, `categoryListSchema`, `refreshCategoriesResultSchema` and their inferred types) in `frontend/src/features/admin/schemas/itemSchemas.ts`
- [X] T030 [P] [US1] Create `itemKeys.ts` (query key factory: `all`, `categories()`, `categoryList()`) in `frontend/src/features/admin/hooks/itemKeys.ts`
- [X] T031 [P] [US1] Create `itemsApi.ts` (`getCategories`, `refreshCategories` using `apiFetch` + Zod parse) in `frontend/src/features/admin/api/itemsApi.ts`
- [X] T032 [P] [US1] Create `useCategories.ts` (TanStack Query `useQuery`, key from `itemKeys.categoryList()`) in `frontend/src/features/admin/hooks/useCategories.ts`
- [X] T033 [P] [US1] Create `useRefreshCategories.ts` (TanStack Query `useMutation`, on success invalidate `itemKeys.categories()`) in `frontend/src/features/admin/hooks/useRefreshCategories.ts`
- [X] T034 [US1] Create `RefreshCategoriesButton.tsx` (button + loading state + summary display with all result fields + error/409 handling, `role="status"` live region) in `frontend/src/features/admin/components/RefreshCategoriesButton.tsx`
- [X] T035 [US1] Create `ItemsImportTab.tsx` (refresh panel with `RefreshCategoriesButton`, last-refreshed display from `useCategories`, disabled/empty-state when no categories, placeholder for import panel) in `frontend/src/features/admin/components/ItemsImportTab.tsx`
- [X] T036 [US1] Add `<TabsTrigger value="items">Items</TabsTrigger>` and `<TabsContent value="items"><ItemsImportTab /></TabsContent>` to `frontend/src/features/admin/pages/DataImportPage.tsx`

**Checkpoint**: Category Refresh is fully functional. Non-admins are rejected. Concurrent calls return 409. UEX failure returns 502. Tab shows empty-state or last-refreshed + category list. All T020–T023 tests pass.

---

## Phase 4: US2 — Single Category Import (Priority: P2)

**Goal**: Admins can select an eligible local category, apply search/section/mining/game filters in
the selector, trigger a single-category item import, and see a complete result summary. UUID-null
items are skipped and counted. Soft-delete and restore work correctly.

**Independent Test**: With categories refreshed, select one `type = "item"` category, import it,
verify summary counts (inserted, updated, unchanged, skipped-no-uuid, soft-deleted, restored). Run
a second import with one item removed from the source stub — verify soft-delete. Re-run with the
item back — verify restore.

### Tests — write and confirm FAILING before implementing (US2 + US5)

- [X] T037 [P] [US2] Write `ImportItemsHandlerTests` (single-category path): mock coordinator, category repo, item repo, item client; test: UUID upsert calls repository with mapped items; uuid-null records filtered before upsert and counted in `ItemsSkippedNoUuid`; handler throws `ImportAlreadyInProgressException` when lock unavailable (US5); unknown categoryId → validation error; non-item category → validation error; UEX client failure → exception propagated in `backend/tests/NajaEcho.Application.Tests/Features/Items/ImportItemsHandlerTests.cs`
- [X] T038 [P] [US2] Write `ItemRepositoryTests` (Testcontainers PostgreSQL): upsert inserts new items by uuid; upsert updates existing by uuid; soft-deletes Active items in this category absent from incoming set; does NOT soft-delete Active items in a DIFFERENT category; restores SoftDeleted items that reappear and sets status Active + clears SoftDeletedAt; uuid-null records passed to upsert are rejected (defensive guard — primary null-UUID filtering is in ImportItemsHandler, not the repo); returns accurate inserted/updated/unchanged/softDeleted/restored counts; **FR-021/FR-022**: assert that stored `raw_data` never contains an `attributes` or `screenshot` key (pass an item record with both fields present and verify they are absent after storage — stripping is done by the handler's map step before calling the repo) in `backend/tests/NajaEcho.Infrastructure.Tests/Items/ItemRepositoryTests.cs`
- [X] T039 [P] [US2] Extend `ItemAdminEndpointsTests`: single-category import 200 with full summary payload; unknown category 400; non-item category 400; lock held 409; UEX client error 502 in `backend/tests/NajaEcho.Api.Tests/Features/Admin/ItemAdminEndpointsTests.cs`
- [X] T040 [P] [US2] Write `categorySelector.test.tsx` (Vitest + RTL): renders all context columns (section, name, type, game-related, mining-related, source modified date, local count, last imported); search filter narrows by name; section filter narrows by section; mining filter narrows; game-related filter narrows; non-item-type rows do not show import action in `frontend/src/features/admin/__tests__/categorySelector.test.tsx`
- [X] T041 [P] [US2] Write `importItems.test.tsx` (Vitest + RTL + MSW): single-category import button triggers mutation with correct categoryUexId; button disabled while pending; on success shows summary with all count fields; on 409 shows concurrency message; on error shows failure message in `frontend/src/features/admin/__tests__/importItems.test.tsx`

### Implementation — US2

- [X] T042 [P] [US2] Create `ImportItemsCommand` (`{ CategoryUexId: int? }`), `ImportItemsResult` (all aggregate fields from data-model.md), and `CategoryImportError` (`{ CategoryUexId, CategoryName?, Message }`) in `backend/src/NajaEcho.Application/Features/Items/ImportItems/`
- [X] T043 [US2] Implement `ImportItemsHandler` (single-category path): acquire lock; validate `CategoryUexId` exists locally and `Type == "item"`; fetch items via `IUexItemClient`; filter uuid-null items (count them); map remaining to `Item` entities (strip `attributes` and `screenshot` from raw data at map time); call `IItemRepository.BulkUpsertForCategoryAsync`; build `ImportItemsResult` with timing; log counts; release lock in finally in `backend/src/NajaEcho.Application/Features/Items/ImportItems/ImportItemsHandler.cs`
- [X] T044 [P] [US2] Create `ImportItemsResponse` DTO and `CategoryImportErrorResponse` in `backend/src/NajaEcho.Api/Features/Admin/Items/Contracts/`
- [X] T045 [US2] Register `ImportItemsHandler` in `DependencyInjection.cs`; wire `POST /import` in `ItemAdminEndpoints` with 400 (validation), 409 (lock), 502 (UEX) error mapping in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` and `backend/src/NajaEcho.Api/Features/Admin/Items/ItemAdminEndpoints.cs`
- [X] T046 [P] [US2] Extend `itemSchemas.ts` with `importItemsResultSchema`, `categoryImportErrorSchema`, `importItemsRequestSchema` and their inferred types in `frontend/src/features/admin/schemas/itemSchemas.ts`
- [X] T047 [P] [US2] Extend `itemsApi.ts` with `importItems({ categoryUexId?: number })` function in `frontend/src/features/admin/api/itemsApi.ts`
- [X] T048 [P] [US2] Create `useImportItems.ts` (TanStack Query `useMutation` accepting optional `categoryUexId`, on success invalidate `itemKeys.categoryList()` for local count refresh) in `frontend/src/features/admin/hooks/useImportItems.ts`
- [X] T049 [P] [US2] Create `CategorySelector.tsx` (renders filterable list with search input, section select, mining toggle, game-related toggle; displays per-category: section, name, type, game-related, mining-related, source modified date, local item count, last imported at; shows import button only for `type = "item"` rows) in `frontend/src/features/admin/components/CategorySelector.tsx`
- [X] T050 [P] [US2] Create `ImportItemsResult.tsx` (display summary: all count fields, timing, derived status badge, per-category error list) in `frontend/src/features/admin/components/ImportItemsResult.tsx` — implemented inline in `ItemsImportTab.tsx`
- [X] T051 [US2] Integrate `CategorySelector`, single-category import action, and `ImportItemsResult` into `ItemsImportTab.tsx`; wire `useImportItems` with the selected `categoryUexId` in `frontend/src/features/admin/components/ItemsImportTab.tsx`

**Checkpoint**: Single-category import fully functional. Selector filters work. UUID upsert, null-skip, soft-delete, and restore are verified by T037–T038 tests. Concurrency guard (US5) verified by T037 and T039. All T037–T041 tests pass.

---

## Phase 5: US3 — All Category Import (Priority: P3)

**Goal**: Admins can trigger an "Import All" that processes every eligible local category, collects
per-category failures without stopping the run, and shows an aggregated summary with `status =
completedWithErrors` and an error list for any failed categories.

**Independent Test**: With multiple eligible categories, trigger "Import All" while one category's
UEX response is stubbed to fail. Verify: remaining categories import successfully; result
`status = completedWithErrors`; `errors` lists the failed category by id and name; admin can then
import that category individually to retry.

### Tests — write and confirm FAILING before implementing (US3)

- [X] T052 [P] [US3] Extend `ImportItemsHandlerTests` with all-category path: when `CategoryUexId` is null, all eligible categories are processed; per-category failure is caught, recorded in errors, and remaining categories continue; aggregate counts sum across succeeded categories; `status` is `success` / `completedWithErrors` / `failed` based on error count in `backend/tests/NajaEcho.Application.Tests/Features/Items/ImportItemsHandlerTests.cs`
- [X] T053 [P] [US3] Extend `ItemAdminEndpointsTests` with all-category endpoint: no `categoryUexId` in body → all-category path; partial failure → 200 with `status = completedWithErrors` and populated `errors` array in `backend/tests/NajaEcho.Api.Tests/Features/Admin/ItemAdminEndpointsTests.cs`
- [X] T054 [P] [US3] Extend `importItems.test.tsx`: "Import All" button present; triggers mutation with no `categoryUexId`; `completedWithErrors` result shows error list with failed category name in `frontend/src/features/admin/__tests__/importItems.test.tsx`

### Implementation — US3

- [X] T055 [US3] Extend `ImportItemsHandler` with all-category path (branch on `CategoryUexId == null`): load all eligible categories via `IItemCategoryRepository.GetEligibleAsync()`; guard "no eligible categories" as 400; loop per category with individual try/catch; accumulate `CategoryImportError` on failure; sum item counts from succeeded categories; derive `status` (`success` / `completedWithErrors` / `failed`) from error count vs processed count; log per-category failures at Warning level in `backend/src/NajaEcho.Application/Features/Items/ImportItems/ImportItemsHandler.cs`
- [X] T056 [US3] Add "Import All" button and action to `ItemsImportTab.tsx`; wire `useImportItems` with no `categoryUexId`; display `ImportItemsResult` (including `errors` list and `status` badge) in `frontend/src/features/admin/components/ItemsImportTab.tsx`

**Checkpoint**: All-category import functional. Partial failure isolation verified. Aggregated summary with per-category errors displayed. All T052–T054 tests pass.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T057 Verify all Serilog structured log calls in `RefreshCategoriesHandler` and `ImportItemsHandler` log counts/category ids/durations only — no tokens, no full JSON payloads, no PII — by reviewing log output during a manual import run
- [X] T058 Run the full backend test suite (`dotnet test`) and confirm all tests pass; run the frontend test suite (`npm run test`) and confirm all tests pass
- [ ] T059 Execute all manual scenarios from `specs/009-item-data-import/quickstart.md` in order and confirm each expected outcome

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1 (package.json script available). **Blocks all user stories.**
- **Phase 3 (US1, Category Refresh)**: Depends on Phase 2 completion.
- **Phase 4 (US2, Single Category Import)**: Depends on Phase 2 completion; also depends on Phase 3 (categories must exist for import; `GET /categories` endpoint is needed by the frontend selector).
- **Phase 5 (US3, All Category Import)**: Depends on Phase 4 (`ImportItemsHandler` single-category path must be working).
- **Phase 6 (Polish)**: Depends on all story phases complete.

### Within Phase 2 (internal ordering)

```
T003–T009 in parallel
↓
T010, T011 in parallel (depend on T003/T004)
↓
T012 (AppDbContext — depends on T010, T011)
↓
T013 (migration — depends on T012)
↓
T014–T017 in parallel (infrastructure impls — depend on entities and abstractions)
↓
T018 (DI — depends on T014–T017)
↓
T019 (endpoint group — depends on T018)
```

### Within each user story phase (internal ordering)

```
Tests (TXxx [P]) in parallel — write first, confirm failing
↓
Application types (Commands, Results) — parallelizable among themselves [P]
↓
Application handler
↓
API DTOs [P] + DI + endpoint wiring (in parallel for DI/DTOs)
↓
Frontend schemas [P] + keys [P] + api [P] (in parallel)
↓
Frontend hooks [P] (depend on api functions)
↓
Frontend components (depend on hooks)
↓
Frontend integration into page/tab
```

---

## Parallel Execution Examples

### Phase 2 — Foundational first batch

```
T003: ItemStatus enum
T004: ItemCategory entity
T005: Item entity
T006: IItemCategoryRepository
T007: IItemRepository
T008: IUexCategoryClient
T009: IUexItemClient
(all simultaneously — separate files, no cross-deps)
```

### Phase 3 — US1 tests first, then backend and frontend in parallel

```
# Test tasks (write and confirm failing):
T020: RefreshCategoriesHandlerTests
T021: ItemCategoryRepositoryTests
T022: ItemAdminEndpointsTests (refresh)
T023: itemsImportTab.test.tsx
(all simultaneously)

# After tests written:
T024: RefreshCategoriesCommand/Result  ──┐
T025: GetCategoriesQuery/Handler/Item  ──┤ parallel
T027: API contract DTOs               ──┘
↓
T026: RefreshCategoriesHandler (needs T024)
T028: DI + endpoint wiring (needs T025, T026, T027)
↓
T029: itemSchemas.ts  ──┐
T030: itemKeys.ts     ──┤ parallel
T031: itemsApi.ts     ──┘
↓
T032: useCategories.ts  ──┐ parallel
T033: useRefreshCategories.ts ──┘
↓
T034: RefreshCategoriesButton.tsx
T035: ItemsImportTab.tsx
T036: DataImportPage.tsx
```

---

## Implementation Strategy

### MVP First (US4 + US1 only)

1. Complete Phase 1 (Setup) — 2 tasks
2. Complete Phase 2 (Foundational) — 17 tasks — foundation ready
3. Complete Phase 3 (US1: Category Refresh + US4 coverage) — 17 tasks
4. **STOP and VALIDATE**: Refresh categories, verify admin-only, verify 409/502 handling
5. Deploy or demo — categories are manageable; import tab is live

### Incremental Delivery

1. Setup + Foundational → migrate, compile clean
2. Phase 3 (US1) → Category Refresh works; Items tab live with empty state + refresh
3. Phase 4 (US2) → Single-category import works with full selector and summary
4. Phase 5 (US3) → Import All works with partial-failure handling
5. Phase 6 → Polish, logging check, quickstart validation

---

## Notes

- `[P]` tasks touch different files and have no incomplete dependencies — safe to parallelize.
- `[US#]` maps each task to its spec user story for traceability.
- **TDD is mandatory per the project constitution** — every test task must be written and confirmed failing before the implementation tasks for that story begin.
- `ItemRepository.BulkUpsertForCategoryAsync` (T017) is the most complex infrastructure task — the category-scope predicate is what differentiates it from `ShipRepository.BulkUpsertAsync`; test it thoroughly in T038.
- Strip `attributes` and `screenshot` at the map step inside `ImportItemsHandler` (T043), not inside the repository — the handler owns the "what to store" decision.
- The shared `IImportCoordinator` singleton covers ships + categories + items — no new lock needed.
- Commit after each checkpoint to keep history clean and bisectable.
