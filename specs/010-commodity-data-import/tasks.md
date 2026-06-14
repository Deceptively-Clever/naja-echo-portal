# Tasks: Commodity Data Import

**Input**: Design documents from `/specs/010-commodity-data-import/`

**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/openapi.yaml ✅ | quickstart.md ✅

**Tests**: Included per constitution Principle II (TDD is NON-NEGOTIABLE — tests are written and
confirmed failing before any production code for that behaviour is written).

**Organization**: Tasks are grouped by user story. Because the commodity import is a single
endpoint/handler/repository operation (the ship import shape, not the multi-category item shape), all
five user stories (US1–US5) are behaviours of that one operation. US1+US4 (both P1) are implemented
in Phase 3 (MVP). US2+US3+US5 (P2 behavioural correctness) are covered in Phase 4. US4 (fail on
source error) and US5 (concurrent prevention) are error-path tests naturally co-located with the main
import tests; they are not given separate implementation phases because the code that satisfies them
(endpoint error mapping, `IImportCoordinator` lock) is written as part of Phase 3.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no blocking dependency)
- **[Story]**: Which user story (US1–US5 from spec.md)
- Exact file paths included in every description

## Path Conventions

- `backend/src/` — .NET project source
- `backend/tests/` — .NET test projects
- `frontend/src/` — React SPA source

---

## Phase 1: Setup

**Purpose**: Register the new API contract in the frontend type-generation pipeline so generated
types are available from the start.

- [X] T001 Add `"gen:api:commodities": "openapi-typescript ../specs/010-commodity-data-import/contracts/openapi.yaml -o src/lib/api/commodities.d.ts"` to the `scripts` block in `frontend/package.json`
- [X] T002 Run `npm run gen:api:commodities` from `frontend/` and commit the generated `frontend/src/lib/api/commodities.d.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entity, EF configuration, migration, abstractions, UEX client, repository,
DI registration, and the admin endpoint group (satisfying FR-018 admin-only access for all calls).

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Domain entities & abstractions (all parallelizable — separate files, no cross-dependencies)

- [X] T003 [P] Create `CommodityStatus` enum (`Active | SoftDeleted`) in `backend/src/NajaEcho.Domain/Commodities/CommodityStatus.cs` — mirrors `ShipStatus` exactly
- [X] T004 [P] Create `Commodity` entity (all promoted fields from data-model.md: `UexId`, `Uuid?`, `Name`, `Code?`, `Slug?`, `Kind?`, `WeightScu?`, `IdParent?`, `IdItem?`, `Wiki?`; raw location strings × 5; parsed `int[]` arrays × 5; 20 `bool` flags; `SourceDateAdded long?`, `SourceDateModified long?`, `SourceDateAddedUtc DateTimeOffset?`, `SourceDateModifiedUtc DateTimeOffset?`; `Status`, `RawData`, `ImportedAt`, `UpdatedAt`, `SoftDeletedAt?`) in `backend/src/NajaEcho.Domain/Commodities/Commodity.cs`
- [X] T005 [P] Create `ICommodityRepository` abstraction (`BulkUpsertAsync(IReadOnlyList<Commodity>, CancellationToken) → (int Inserted, int Updated, int Restored, int SoftDeleted)`) in `backend/src/NajaEcho.Application/Abstractions/ICommodityRepository.cs`
- [X] T006 [P] Create `IUexCommodityClient` abstraction (`FetchAllCommoditiesAsync(CancellationToken) → IReadOnlyList<JsonDocument>`) in `backend/src/NajaEcho.Application/Abstractions/IUexCommodityClient.cs`

### EF Core configuration & migration (sequential — depend on entities above)

- [X] T007 Create `CommodityConfiguration` (table `sc.commodities`, schema `sc`; PK `Id`; explicit `HasColumnName` for every property; `Status HasConversion<string>()` required; `RawData HasColumnType("jsonb")` required; all 5 parsed location arrays map to `integer[]` required; 20 bool columns non-nullable; unique index `ix_commodities_uex_id` on `UexId`; index `ix_commodities_status` on `Status`) in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/CommodityConfiguration.cs`
- [X] T008 Add `DbSet<Commodity> Commodities => Set<Commodity>()` to `AppDbContext` and register `new CommodityConfiguration()` in `OnModelCreating` in `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs`
- [X] T009 Generate EF Core migration `AddCommodities` via `dotnet ef migrations add AddCommodities -p backend/src/NajaEcho.Infrastructure -s backend/src/NajaEcho.Api`; review generated files in `backend/src/NajaEcho.Infrastructure/Persistence/Migrations/` to confirm all columns, bool types, `integer[]` arrays, `jsonb`, indexes, and the unique constraint on `uex_id` are correct

### Infrastructure implementations

- [X] T010 [P] Implement `UexCommodityClient` (typed `HttpClient`, `GET commodities`, parses `{ "data": [ ... ] }` envelope, throws `InvalidOperationException` when `data` is absent or not an array — same pattern as `UexVehicleClient`) in `backend/src/NajaEcho.Infrastructure/Commodities/UexCommodityClient.cs`
- [X] T011 Implement `CommodityRepository.BulkUpsertAsync`: open transaction; build `incomingByUexId`; load existing rows where `uex_id ∈ incomingIds`; for each incoming: match → update all promoted fields + `RawData` + `UpdatedAt` (if `SoftDeleted` → restore, clear `SoftDeletedAt`, count `restored`; else count `updated`); no match → new `Guid` Id, `Active`, set `ImportedAt`/`UpdatedAt`, add, count `inserted`; then load Active rows `uex_id ∉ incomingIds` → soft-delete + count; `SaveChanges` + commit (global scope — mirrors `ShipRepository.BulkUpsertAsync` exactly, no category scoping) in `backend/src/NajaEcho.Infrastructure/Commodities/CommodityRepository.cs`

### DI registration & endpoint group

- [X] T012 Register in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`: `AddHttpClient<IUexCommodityClient, UexCommodityClient>` (same `UexVehicleClient:BaseUrl` config key); `AddScoped<ICommodityRepository, CommodityRepository>`
- [X] T013 Create `CommodityAdminEndpoints` with `app.MapGroup("/api/admin/commodities").RequireAuthorization(AuthorizationPolicies.Admin)` and a stub `MapPost("/import", ImportCommodities)`; register `app.MapCommodityAdminEndpoints()` in `backend/src/NajaEcho.Api/Program.cs` in `backend/src/NajaEcho.Api/Features/Admin/Commodities/CommodityAdminEndpoints.cs`

**Checkpoint**: Foundation complete. Run `dotnet build` — compiles clean. Run migration — `sc.commodities` table created with correct columns and indexes. The endpoint group is registered; unauthenticated/non-admin requests return 401/403 (FR-018 satisfied).

---

## Phase 3: US1 + US4 — Trigger Import & Fail on Source Error (Priority: P1) 🎯 MVP

**Goal**: Admins can trigger the commodity import from the Commodities tab and receive a completion
summary (US1). If the source is unreachable or returns an invalid shape, the entire import fails with
no changes applied (US4).

**Independent Test**: Navigate to Data Import → Commodities tab → click Import Commodities → observe
success summary with inserted/updated/restored/softDeleted/skipped counts. Verify 403 for non-admin,
409 for a concurrent call, 502 when the source is unreachable or returns an invalid shape.

### Tests — write and confirm FAILING before implementing (US1 + US4)

- [X] T014 [P] [US1] Write `ImportCommoditiesHandlerTests` (mock `IUexCommodityClient`, `ICommodityRepository`, `IImportCoordinator`): (a) happy path — fetched count matches repository call count, summary counts populated, `StartedAt`/`CompletedAt`/`DurationMs` present; (b) flag normalization — verify `GetBool` converts numeric `1` → `true`, `0` → `false`, absent → `false`; (c) ParseIdList — verify `"1, 4, 7"` → `[1, 4, 7]`, empty/null → `[]`, non-numeric tokens discarded; (d) timestamp conversion — valid Unix seconds → non-null `SourceDateAddedUtc`; zero/invalid → null UTC with raw value stored; (e) uuid=null commodity mapped and NOT skipped; (f) price fields never appear as properties on the mapped entity; (g) zero-record feed guard: repository is never called, warning returned (per spec: empty feed → no changes, warning — FR-006 does not apply); (h) feed fetch throws `HttpRequestException` → propagated; (i) invalid shape throws `InvalidOperationException` → propagated (US4); (j) `ImportAlreadyInProgressException` thrown when coordinator returns false (US5 preview) in `backend/tests/NajaEcho.Application.Tests/Features/Commodities/ImportCommoditiesHandlerTests.cs`
- [X] T015 [P] [US1] Write `CommodityRepositoryTests` (Testcontainers PostgreSQL): (a) BulkUpsertAsync inserts new commodities and returns correct `Inserted` count; (b) updates existing commodities (matched by `uex_id`) and advances `UpdatedAt`; (c) restores a `SoftDeleted` commodity that reappears, returns `Restored` count, clears `SoftDeletedAt`; (d) global soft-delete: seed 5 Active, upsert 3 — assert the 2 absent ones have `Status = SoftDeleted` and `SoftDeletedAt` set; (e) `int[]` round-trip: store `IdsStarSystems = [1, 4, 7]`, reload, assert array equals `[1, 4, 7]`; (f) `raw_data` round-trip: store commodity with full source JSON, reload, assert `raw_data` is valid JSON containing all original fields in `backend/tests/NajaEcho.Infrastructure.Tests/Commodities/CommodityRepositoryTests.cs`
- [X] T016 [P] [US1] Write `CommodityAdminEndpointsTests` (in-memory provider + fake coordinator + fake commodity client, per `ShipAdminEndpointsTests`): (a) POST `/api/admin/commodities/import` as admin → 200 with `ImportCommoditiesResult` payload (all summary fields present); (b) POST as non-admin → 403; (c) coordinator `TryAcquire()` returns false → 409 with problem body; (d) client throws `HttpRequestException` → 502 with problem body (US4); (e) client throws `InvalidOperationException` (invalid shape) → 502 with problem body (US4); (f) zero-record feed → 202 with warning field set in `backend/tests/NajaEcho.Api.Tests/Features/Admin/CommodityAdminEndpointsTests.cs`
- [X] T026 [P] [US1] Write `importCommodities.test.tsx` (Vitest + RTL + MSW): (a) Commodities tab renders with Import button; (b) button disabled while pending; (c) success response renders all summary fields (inserted, updated, restored, softDeleted, skipped, fetched) in `role="status"` live region; (d) 409 response shows "already in progress" warning; (e) 502/network error shows failure message in `frontend/src/features/admin/__tests__/importCommodities.test.tsx`

### Implementation — US1 + US4

- [X] T017 [P] [US1] Create `ImportCommoditiesCommand` (no fields — full feed, no parameters) in `backend/src/NajaEcho.Application/Features/Commodities/ImportCommodities/ImportCommoditiesCommand.cs`
- [X] T018 [P] [US1] Create `ImportCommoditiesResult` record (`int Fetched, int Skipped, int Inserted, int Updated, int Restored, int SoftDeleted, DateTimeOffset StartedAt, DateTimeOffset CompletedAt, long DurationMs, string? Warning`) in `backend/src/NajaEcho.Application/Features/Commodities/ImportCommodities/ImportCommoditiesResult.cs`
- [X] T019 [US1] Implement `ImportCommoditiesHandler.HandleAsync`: (1) `coordinator.TryAcquire()` → throw `ImportAlreadyInProgressException` if false; (2) `try { fetch → map+skip → upsert → build result } catch(ImportAlreadyInProgressException) { throw } catch { log, throw } finally { coordinator.Release() }`; mapping: call `MapToCommodity` per record, use `GetBool` for 20 flags, `ParseIdList` for 5 location fields, `GetDateTimeOffset` + raw-value capture for timestamps, skip records missing `id` or empty `name` and increment `skipped`, never map `price_buy`/`price_sell` to entity properties, store full `JsonDocument` as `RawData`; log fetched/skipped/inserted/updated/restored/softDeleted counts; zero-record guard: skip repository call, return 202-appropriate result with warning in `backend/src/NajaEcho.Application/Features/Commodities/ImportCommodities/ImportCommoditiesHandler.cs`
- [X] T020 [P] [US1] Create `ImportCommoditiesResponse` record (mirrors `ImportCommoditiesResult` fields as camelCase JSON-friendly record) in `backend/src/NajaEcho.Api/Features/Admin/Commodities/Contracts/ImportCommoditiesResponse.cs`
- [X] T021 [US1] Register `ImportCommoditiesHandler` with `AddScoped` in `DependencyInjection.cs`; implement the `ImportCommodities` endpoint handler in `CommodityAdminEndpoints`: call `handler.HandleAsync`, on success return `Results.Ok(new ImportCommoditiesResponse(...))` (or `Results.Accepted` if warning set), catch `ImportAlreadyInProgressException` → `Results.Conflict(...)`, catch `HttpRequestException` and `InvalidOperationException` → `Results.Problem(..., statusCode: 502)` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` and `backend/src/NajaEcho.Api/Features/Admin/Commodities/CommodityAdminEndpoints.cs`

### Frontend — US1

- [X] T022 [P] [US1] Create `commoditySchemas.ts` (Zod: `importCommoditiesResultSchema` with all summary fields; export `ImportCommoditiesResult` inferred type) in `frontend/src/features/admin/schemas/commoditySchemas.ts`
- [X] T023 [P] [US1] Create `commodityKeys.ts` (query key factory; at minimum `all` and `import()` for mutation cache tagging) in `frontend/src/features/admin/hooks/commodityKeys.ts`
- [X] T024 [P] [US1] Create `commoditiesApi.ts` (`importCommodities(): Promise<ImportCommoditiesResult>` using `apiFetch` + Zod parse against `importCommoditiesResultSchema`) in `frontend/src/features/admin/api/commoditiesApi.ts`
- [X] T025 [P] [US1] Create `useImportCommodities.ts` (TanStack Query `useMutation`, `mutationFn: importCommodities`, no cache invalidation needed in v1 — no commodity list to refresh) in `frontend/src/features/admin/hooks/useImportCommodities.ts`
- [X] T027 [P] [US1] Create `ImportCommoditiesButton.tsx`: button (`Import Commodities`/`Importing…` + `aria-busy`); on success render summary text in a `role="status"` `aria-live="polite"` paragraph (pattern: `${data.inserted} inserted, ${data.updated} updated, ${data.restored} restored, ${data.softDeleted} removed, ${data.skipped} skipped. (${data.fetched} fetched)`); on 409 `ApiError` show warning text; on other error show error text (mirrors `ImportShipsButton.tsx` style) in `frontend/src/features/admin/components/ImportCommoditiesButton.tsx`
- [X] T028 [US1] Create `CommoditiesImportTab.tsx` (brief description of the import, `<ImportCommoditiesButton />`) in `frontend/src/features/admin/components/CommoditiesImportTab.tsx`
- [X] T029 [US1] Add `<TabsTrigger value="commodities">Commodities</TabsTrigger>` and `<TabsContent value="commodities"><CommoditiesImportTab /></TabsContent>` to `frontend/src/features/admin/pages/DataImportPage.tsx`

**Checkpoint**: MVP complete. The Commodities tab is live. Import button triggers a fetch, maps and upserts all valid records, returns a summary. Non-admins are rejected (403). Concurrent calls return 409. Source failures return 502 with no changes applied (US4 satisfied). All T014–T016 and T026 tests pass.

---

## Phase 4: US2 + US3 + US5 — Behavioural Correctness (Priority: P2)

**Goal**: Confirm skip-invalid-records (US2), soft-delete/restore lifecycle (US3), and
concurrent-import prevention (US5) are verifiably correct through targeted test scenarios.

**Note**: The implementation code for all three behaviours was written in Phase 3
(`ImportCommoditiesHandler` mapping step for US2; `CommodityRepository.BulkUpsertAsync` for US3;
`IImportCoordinator` for US5). This phase adds focused test scenarios that specifically exercise
these three P2 behaviours in isolation, going beyond the general coverage in T014–T016.

### Tests — write and confirm FAILING before implementing (US2 + US3 + US5)

- [ ] T030 [P] [US2] Extend `ImportCommoditiesHandlerTests` with skip-invalid scenarios: (a) feed contains 10 records where 2 are missing `id`, 1 has empty `name`, 1 has null `name` → assert `Skipped == 4`, repository called with 6 records, `Fetched == 10`; (b) a record with `uuid = null` is NOT skipped and IS passed to the repository; (c) a record with `id = 0` (falsy but numeric) — verify if it's treated as invalid (per spec: `id` must be present and a valid positive integer identity; `0` is not a valid UEX id → skip and count); (d) import continues after all skipped records and remaining valid records are upserted in `backend/tests/NajaEcho.Application.Tests/Features/Commodities/ImportCommoditiesHandlerTests.cs`
- [ ] T031 [P] [US3] Extend `CommodityRepositoryTests` with soft-delete/restore lifecycle: (a) run a full upsert with all 5 commodities → all Active; run a second upsert with only 3 → assert exactly those 2 absent commodities are `SoftDeleted`, `SoftDeletedAt` is set, others unchanged; (b) run a third upsert with all 5 again → assert the 2 previously soft-deleted are `Active`, `SoftDeletedAt` is null, `Restored == 2`; (c) a commodity that is `SoftDeleted` and still absent from the feed remains `SoftDeleted` (not double-deleted) in `backend/tests/NajaEcho.Infrastructure.Tests/Commodities/CommodityRepositoryTests.cs`
- [ ] T032 [P] [US5] Extend `CommodityAdminEndpointsTests` with a concurrent-prevention scenario: configure the fake coordinator so the first `TryAcquire` succeeds and the second returns false; POST import twice in rapid succession; assert the second response is 409 with the expected problem body; after `Release`, a subsequent POST succeeds (200) in `backend/tests/NajaEcho.Api.Tests/Features/Admin/CommodityAdminEndpointsTests.cs`

**Checkpoint**: P2 behaviours verified. Skip-invalid (US2) confirmed with mixed valid/invalid feed. Soft-delete/restore lifecycle (US3) confirmed via repository integration tests. Concurrent import prevention (US5) confirmed at the endpoint level. All T030–T032 tests pass.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [ ] T033 Verify all Serilog structured log calls in `ImportCommoditiesHandler` log counts/durations only — no tokens, no full JSON payloads, no PII — by reviewing log output during a manual import run against the real UEX endpoint
- [ ] T034 Run the full backend test suite (`dotnet test`) and confirm all tests pass; run the frontend test suite (`npm run test`) and confirm all tests pass
- [ ] T035 Execute all manual scenarios from `specs/010-commodity-data-import/quickstart.md` in order and confirm each expected outcome

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1. **Blocks all user story work.**
- **Phase 3 (US1+US4, P1)**: Depends on Phase 2 completion.
- **Phase 4 (US2+US3+US5, P2)**: Depends on Phase 3 (extends existing handler and repo tests;
  implementation code already present from Phase 3).
- **Phase 5 (Polish)**: Depends on Phases 3 and 4 complete.

### Within Phase 2 (internal ordering)

```
T003–T006 in parallel (entities + abstractions — no cross-dependencies)
↓
T007 (EF config — depends on T003/T004)
↓
T008 (AppDbContext — depends on T007)
↓
T009 (migration — depends on T008)
↓
T010, T011 in parallel (infrastructure impls — depend on entities + abstractions)
↓
T012 (DI — depends on T010, T011)
↓
T013 (endpoint group + Program.cs — depends on T012)
```

### Within Phase 3 (internal ordering)

```
Tests (T014, T015, T016, T026) in parallel — write first, confirm failing
↓
T017, T018 in parallel (Command + Result records — no dependencies)
↓
T019 (Handler — depends on T017, T018)
↓
T020, T021 in parallel (API DTO + DI/endpoint wiring — T020 independent; T021 depends on T019, T020)
↓
T022, T023, T024 in parallel (schemas, keys, api functions)
↓
T025 (hook — depends on T024)
↓
T027 (ImportCommoditiesButton — depends on T025; T026 test must already be failing)
↓
T028 (CommoditiesImportTab — depends on T027)
↓
T029 (DataImportPage — depends on T028)
```

### Within Phase 4 (internal ordering)

```
T030, T031, T032 in parallel (all extend separate test files — no cross-dependencies)
```

---

## Parallel Execution Examples

### Phase 2 — first batch (T003–T006)

```
T003: CommodityStatus enum
T004: Commodity entity (all promoted fields)
T005: ICommodityRepository abstraction
T006: IUexCommodityClient abstraction
(all simultaneously — separate files, no dependencies)
```

### Phase 3 — tests first, then backend and frontend in parallel

```
# Test tasks (write and confirm failing — all simultaneously):
T014: ImportCommoditiesHandlerTests
T015: CommodityRepositoryTests
T016: CommodityAdminEndpointsTests
T026: importCommodities.test.tsx

# After tests written:
T017: ImportCommoditiesCommand  ──┐ parallel
T018: ImportCommoditiesResult   ──┘
↓
T019: ImportCommoditiesHandler
↓
T020: ImportCommoditiesResponse ──┐ parallel
T021: DI + endpoint wiring      ──┘
↓
T022: commoditySchemas.ts  ──┐
T023: commodityKeys.ts     ──┤ parallel
T024: commoditiesApi.ts    ──┘
↓
T025: useImportCommodities.ts
↓
T027: ImportCommoditiesButton.tsx
↓
T028: CommoditiesImportTab.tsx
↓
T029: DataImportPage.tsx
```

---

## Implementation Strategy

### MVP First (US1 + US4)

1. Complete Phase 1 (Setup) — 2 tasks
2. Complete Phase 2 (Foundational) — 11 tasks — domain entity, migration, repo, client, DI, endpoint
3. Complete Phase 3 (US1+US4: core import trigger + source-failure handling) — 16 tasks
4. **STOP and VALIDATE**: Import runs, summary is shown, admin-only enforced, 409/502 handled correctly
5. Deploy/demo — Commodities tab is live

### Incremental Delivery

1. Setup + Foundational → migrate, compile clean
2. Phase 3 (US1+US4) → full MVP: import works, source errors handled, frontend complete
3. Phase 4 (US2+US3+US5) → behavioural correctness verified by targeted test scenarios
4. Phase 5 → polish, logging check, quickstart validation

---

## Notes

- `[P]` tasks touch different files and have no incomplete dependencies — safe to parallelize.
- `[US#]` maps each task to its spec user story for traceability.
- **TDD is mandatory per the project constitution** — every test task must be written and confirmed failing before the implementation tasks for that story begin.
- `CommodityRepository.BulkUpsertAsync` (T011) mirrors `ShipRepository.BulkUpsertAsync` **globally** (not the category-scoped `ItemRepository` variant) — the full feed is the authority, and a commodity absent from the feed is soft-deleted regardless of any scoping.
- The `GetBool`, `GetInt`, `GetNullableInt`, `GetString`, `GetDateTimeOffset` helpers already exist in `UexItemClient`/`ImportItemsHandler` and are the exact templates for the commodity mapping in `ImportCommoditiesHandler`. Add a `ParseIdList(JsonElement, string)` helper that splits on `,`, trims, and discards non-numeric tokens.
- Pricing fields (`price_buy`, `price_sell`) must never be assigned to any property of `Commodity` — they remain only in the verbatim `RawData` `JsonDocument` (FR-014). The `Commodity` entity has no such properties, so omitting them in the map step is sufficient.
- The shared `IImportCoordinator` singleton spans ships + categories + items + commodities — no new lock is needed and no registration changes other than the one added in T012.
- Commit after each phase checkpoint to keep history clean and bisectable.
