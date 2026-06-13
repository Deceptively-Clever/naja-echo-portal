---
description: "Task list for Ship Data Import (006-ship-data-import)"
---

# Tasks: Ship Data Import

**Input**: Design documents from `/specs/006-ship-data-import/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUDED — Constitution Principle II (Test-First / TDD) is non-negotiable for this project, and
the plan specifies tests. Write each test task and confirm it FAILS before implementing.

**Organization**: Tasks are grouped by user story. Phases 1–2 are shared; Phase 3 (US1) is the MVP.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1/US2/US3 for story-phase tasks; Setup/Foundational/Polish have no story label
- Paths are absolute-from-repo-root: `backend/src/...`, `frontend/src/...`

## Path Conventions

Full-stack web app: backend is .NET 10 Clean Architecture (`backend/src/NajaEcho.{Domain,Application,Infrastructure,Api}`,
tests in `backend/tests/NajaEcho.*.Tests`); frontend is React + Vite (`frontend/src`).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: New dependencies and generic, application-agnostic UI primitives.

- [X] T001 [P] Add `@radix-ui/react-tabs` to `frontend/package.json` dependencies and run install
- [X] T002 [P] Add Testcontainers PostgreSQL packages (`Testcontainers.PostgreSql`, `Npgsql`) to `backend/tests/NajaEcho.Infrastructure.Tests/NajaEcho.Infrastructure.Tests.csproj`
- [X] T003 [P] Create generic shadcn `Tabs` primitive (wrapping `@radix-ui/react-tabs`, cva/cn/forwardRef, semantic tokens only) in `frontend/src/components/ui/tabs.tsx`
- [X] T004 [P] Create generic shadcn `Table` primitive (styled HTML table parts, no new dep) in `frontend/src/components/ui/table.tsx`
- [X] T068 [P] Add `openapi-typescript` dev-dependency to `frontend/package.json`, add `gen:api:ships` script (generates `frontend/src/lib/api/ships.d.ts` from `specs/006-ship-data-import/contracts/openapi.yaml`), and run it — generated types are the **canonical** API-boundary type source; Zod schemas validate/narrow them at runtime (Constitution III); must complete before any schema task (T039, T053, T064)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared domain/persistence, the data-access seam, admin authorization, and the frontend admin
shell (nav + guard) that ALL user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Backend — domain, persistence & data-access seam

- [X] T005 [P] Create `ShipStatus` enum (`Active`, `SoftDeleted`) in `backend/src/NajaEcho.Domain/Ships/ShipStatus.cs`
- [X] T006 [P] Create `Ship` entity (promoted props + `RawData` JsonDocument + status + timestamps) in `backend/src/NajaEcho.Domain/Ships/Ship.cs`
- [X] T007 [P] Define `IShipRepository` port (paged list, get-by-id, get-by-uex-id, bulk upsert, soft-delete, transaction scope) in `backend/src/NajaEcho.Application/Abstractions/IShipRepository.cs`
- [X] T008 [P] Define `IUexVehicleClient` port (fetch all vehicles) in `backend/src/NajaEcho.Application/Abstractions/IUexVehicleClient.cs`
- [X] T009 [P] Define `IImportCoordinator` port (single-flight TryAcquire/Release) in `backend/src/NajaEcho.Application/Abstractions/IImportCoordinator.cs`
- [X] T010 Create `ShipConfiguration` (columns, unique index on `uex_id`, `raw_data` → jsonb, status enum conversion) in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/ShipConfiguration.cs` (depends on T006)
- [X] T011 Add `DbSet<Ship> Ships` and apply `ShipConfiguration` in `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs` (depends on T006, T010)
- [X] T012 Generate EF Core migration `AddShips` (+ snapshot update) under `backend/src/NajaEcho.Infrastructure/Persistence/Migrations/` (depends on T011)
- [X] T013 Implement `ShipRepository` (LINQ paging/ordering, get-by-id, get-by-uex-id, transactional upsert, soft-delete/reactivate) in `backend/src/NajaEcho.Infrastructure/Ships/ShipRepository.cs` (depends on T007, T011)
- [X] T014 Register `ShipRepository` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` (depends on T013)
- [X] T015 [P] Testcontainers-Postgres integration tests for `ShipRepository` (paging, jsonb round-trip, transactional upsert, soft-delete + reactivate) in `backend/tests/NajaEcho.Infrastructure.Tests/Ships/ShipRepositoryTests.cs` — write FIRST, confirm red (depends on T002, T013)

### Backend — admin authorization

- [X] T016 [P] Create `AdminRoleSeeder` (idempotently ensure the `Admin` `IdentityRole<Guid>` row exists) in `backend/src/NajaEcho.Infrastructure/Identity/AdminRoleSeeder.cs`
- [X] T017 [P] Create `AuthorizationPolicies` (`Admin` policy name constant + registration helper requiring the `Admin` role) in `backend/src/NajaEcho.Api/Authorization/AuthorizationPolicies.cs`
- [X] T018 Extend `CurrentUserResponse` with `Roles` (string[]) on the authenticated payload in `backend/src/NajaEcho.Api/Features/Auth/Contracts/CurrentUserResponse.cs`
- [X] T019 Populate `roles` in `/api/auth/me` (read `ClaimTypes.Role` from the principal) in `backend/src/NajaEcho.Api/Features/Auth/AuthEndpoints.cs` (depends on T018)
- [X] T067 Sync the canonical auth contract: add `user.roles: string[]` to `/api/auth/me` in `specs/002-identity-refactor/contracts/openapi.yaml` — **MUST precede T020** (Constitution I: contract before implementation)
- [X] T020 In `backend/src/NajaEcho.Api/Program.cs`: emit role claims on sign-in (`UserManager.GetRolesAsync` → `ClaimTypes.Role`) in `OnTicketReceived`, register the `Admin` policy, and run `AdminRoleSeeder` at startup; register `AdminRoleSeeder` in DI (depends on T016, T017, T067)
- [X] T021 [P] API integration test: `/api/auth/me` returns `roles` for an admin-role user and empty roles otherwise (WebApplicationFactory) — write FIRST, confirm red (depends on T020)

### Frontend — admin shell (session roles, guard, navigation)

- [X] T022 [P] Extend `sessionStateSchema` authenticated user with `roles: string[]` in `frontend/src/features/auth/schemas/sessionStateSchema.ts`
- [X] T023 [P] Create `AdminRoute` guard (allows `Admin` role from session; else redirect/forbidden) in `frontend/src/features/auth/AdminRoute.tsx` (depends on T022)
- [X] T024 Extend `NavItem` with optional `group` and add the "Admin" group + "Data Import" item (`access: 'admin'`, path `/dashboard/admin/data-import`) in `frontend/src/features/dashboard/navigation/navItems.ts`
- [X] T025 Update `DashboardNav` to render optional group headings and filter items by `access` against session roles in `frontend/src/features/dashboard/components/DashboardNav.tsx` (depends on T024, T022)
- [X] T026 [P] Apply the same access filtering (and grouping if rendered) to `DashboardMobileNav` in `frontend/src/features/dashboard/components/DashboardMobileNav.tsx` (depends on T024, T022)
- [X] T027 [P] Frontend tests: `AdminRoute` (admin allowed, non-admin blocked) and nav gating (Admin item hidden for non-admin, shown for admin) in `frontend/src/features/admin/__tests__/adminAccess.test.tsx` — write FIRST, confirm red (depends on T023, T025, T026)

**Checkpoint**: Shared data layer, admin authz, and admin nav/guard exist. The Admin section is visible to
admins; the data-import route is not reachable yet (added in US1). User stories can now begin.

---

## Phase 3: User Story 1 - Trigger Ship Data Import (Priority: P1) 🎯 MVP

**Goal**: An admin can trigger a server-side import from the UEX feed; new/changed ships are upserted,
missing ships soft-deleted, reappearing ships reactivated, all transactionally, with a success-count
message and a single-flight guard.

**Independent Test**: As an admin, open Admin → Data Import → Ships, click **Import Ships**, and confirm a
success message with counts; trigger a failing feed and confirm stored data is unchanged; trigger a
concurrent import and confirm a 409 "already in progress" response.

### Tests for User Story 1 (write FIRST, confirm red) ⚠️

- [X] T028 [P] [US1] `ImportShipsHandler` unit tests (added/updated/reactivated/softDeleted/total counts, full rollback on mid-import failure, zero-record guard leaves data unchanged) against `IShipRepository`/`IUexVehicleClient`/`IImportCoordinator` fakes in `backend/tests/NajaEcho.Application.Tests/Features/Ships/ImportShipsHandlerTests.cs`
- [X] T029 [P] [US1] API integration tests for `POST /api/admin/ships/import`: 200 with counts, 409 when an import is in progress, 401 unauth, 403 non-admin, 502 on feed failure (WebApplicationFactory + fake `IUexVehicleClient`) in `backend/tests/NajaEcho.Api.Tests/Features/Admin/ShipAdminEndpointsTests.cs`
- [X] T030 [P] [US1] Frontend tests for `ImportShipsButton` + `useImportShips`: loading/disabled during run, success-count message, error message, 409 in-progress message (Vitest + MSW) in `frontend/src/features/admin/__tests__/importShips.test.tsx`

### Implementation for User Story 1

- [X] T031 [P] [US1] Create `ImportShipsCommand` and `ImportShipsResult` (count fields + optional warning) in `backend/src/NajaEcho.Application/Features/Ships/ImportShips/`
- [X] T032 [US1] Implement `ImportShipsHandler` (acquire coordinator → fetch via `IUexVehicleClient` → zero-record guard → transactional upsert/soft-delete/reactivate via `IShipRepository` → counts; structured Serilog start/counts/failure logging) in `backend/src/NajaEcho.Application/Features/Ships/ImportShips/ImportShipsHandler.cs` (depends on T031, T007, T008, T009, T013)
- [X] T033 [P] [US1] Implement `UexVehicleClient` (typed `HttpClient`, parse `{status, http_code, data:[...]}`, configurable base URL) in `backend/src/NajaEcho.Infrastructure/Ships/UexVehicleClient.cs` (depends on T008)
- [X] T034 [P] [US1] Implement `ImportCoordinator` (singleton `SemaphoreSlim(1,1)`, `TryAcquire()` zero-wait, `Release()`) in `backend/src/NajaEcho.Infrastructure/Ships/ImportCoordinator.cs` (depends on T009)
- [X] T035 [US1] Register `UexVehicleClient` (`AddHttpClient` with base URL from config), `ImportCoordinator` (singleton), and `ImportShipsHandler` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` (depends on T032, T033, T034)
- [X] T036 [P] [US1] Create `ImportShipsResponse` contract in `backend/src/NajaEcho.Api/Features/Admin/Ships/Contracts/ImportShipsResponse.cs`
- [X] T037 [US1] Create `ShipAdminEndpoints` with `POST /api/admin/ships/import` (require `Admin` policy; map coordinator 409, handler success 200 / zero-record 202, feed failure 502) in `backend/src/NajaEcho.Api/Features/Admin/Ships/ShipAdminEndpoints.cs` (depends on T032, T035, T036, T017)
- [X] T038 [US1] Map `ShipAdminEndpoints` in `backend/src/NajaEcho.Api/Program.cs` (depends on T037, T020)
- [X] T039 [P] [US1] Add `shipKeys` query-key factory in `frontend/src/features/admin/hooks/shipKeys.ts` and import-result Zod schema (validates/narrows generated types from `ships.d.ts`) in `frontend/src/features/admin/schemas/shipSchemas.ts` (depends on T068)
- [X] T040 [P] [US1] Add `importShips()` wrapper (apiFetch POST) in `frontend/src/features/admin/api/shipsApi.ts` (depends on T039)
- [X] T041 [US1] Implement `useImportShips` mutation (invalidates the ships list key on success) in `frontend/src/features/admin/hooks/useImportShips.ts` (depends on T040, T039)
- [X] T042 [US1] Implement `ImportShipsButton` (triggers mutation; loading/disabled, success-count, error, 409 messaging) in `frontend/src/features/admin/components/ImportShipsButton.tsx` (depends on T041)
- [X] T043 [US1] Create thin `DataImportPage` (Tabs shell with a "Ships" tab) and `ShipsImportTab` (renders `ImportShipsButton`; table placeholder for US2) in `frontend/src/features/admin/pages/DataImportPage.tsx` and `frontend/src/features/admin/components/ShipsImportTab.tsx` (depends on T003, T042)
- [X] T044 [US1] Wire route `/dashboard/admin/data-import` under `AdminRoute` in `frontend/src/routes/AppRouter.tsx` (depends on T043, T023)

**Checkpoint**: US1 fully functional — an admin can reach the page, run an import, and see counts; failures
roll back; concurrent imports 409. This is a shippable MVP.

---

## Phase 4: User Story 2 - View Imported Ship Records (Priority: P2)

**Goal**: An admin browses imported ships in a paginated table (name + company name per row, status
indicator, empty state).

**Independent Test**: With ship rows present (seed directly or via US1 import), open the Ships tab and
confirm the table lists name + company, paginates at 25/page, shows a soft-deleted indicator, and shows the
empty state when there is no data.

### Tests for User Story 2 (write FIRST, confirm red) ⚠️

- [X] T045 [P] [US2] `GetShipsHandler` unit tests (paging math, ordering, status mapping) in `backend/tests/NajaEcho.Application.Tests/Features/Ships/GetShipsHandlerTests.cs`
- [X] T046 [P] [US2] API integration tests for `GET /api/admin/ships` (paged envelope shape, empty result, 401/403) appended to `backend/tests/NajaEcho.Api.Tests/Features/Admin/ShipAdminEndpointsTests.cs`
- [X] T047 [P] [US2] Frontend tests for `ShipsTable` + `useShips` (rows show name/company, status badge for soft-deleted, pagination controls, empty state) in `frontend/src/features/admin/__tests__/shipsTable.test.tsx`

### Implementation for User Story 2

- [X] T048 [P] [US2] Create `GetShipsQuery` (page, pageSize) and `ShipListItem` in `backend/src/NajaEcho.Application/Features/Ships/GetShips/`
- [X] T049 [US2] Implement `GetShipsHandler` (paged read via `IShipRepository`, default pageSize 25, ordered by name) in `backend/src/NajaEcho.Application/Features/Ships/GetShips/GetShipsHandler.cs` (depends on T048, T007)
- [X] T050 [P] [US2] Create `PagedShipsResponse` and `ShipListItemResponse` contracts in `backend/src/NajaEcho.Api/Features/Admin/Ships/Contracts/`
- [X] T051 [US2] Add `GET /api/admin/ships` (require `Admin` policy; map query → `PagedShipsResponse`) to `backend/src/NajaEcho.Api/Features/Admin/Ships/ShipAdminEndpoints.cs` (depends on T049, T050, T037)
- [X] T052 [US2] Register `GetShipsHandler` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` (depends on T049)
- [X] T053 [P] [US2] Add list-item + paged-response Zod schemas (validate/narrow generated types from `ships.d.ts`) to `shipSchemas.ts`, `getShips()` to `shipsApi.ts`, and `useShips` query hook in `frontend/src/features/admin/hooks/useShips.ts` (depends on T039, T068)
- [X] T054 [US2] Implement `ShipsTable` (columns: name, company, status badge, View Details button; empty state) in `frontend/src/features/admin/components/ShipsTable.tsx` using the `Table` primitive (depends on T004, T053)
- [X] T055 [P] [US2] Implement `ShipsPagination` (prev/next + page info, 25/page) in `frontend/src/features/admin/components/ShipsPagination.tsx` (depends on T053)
- [X] T056 [US2] Integrate `ShipsTable` + `ShipsPagination` into `ShipsImportTab`, replacing the placeholder; manage page state here in `frontend/src/features/admin/components/ShipsImportTab.tsx` (depends on T054, T055, T043)

**Checkpoint**: US1 + US2 work independently — admin can import and browse the paginated list.

---

## Phase 5: User Story 3 - View Full Ship Record Details (Priority: P3)

**Goal**: An admin opens a right-side sheet showing all 64 feed fields for a ship (empty fields shown
explicitly), with a soft-deleted indicator; closing returns to the same list position.

**Independent Test**: With ship rows present, click **View Details** on a row → a right-side sheet shows all
fields including empty ones; a soft-deleted record shows the "no longer in source feed" indicator; closing
returns to the same page/scroll position.

### Tests for User Story 3 (write FIRST, confirm red) ⚠️

- [X] T057 [P] [US3] API integration tests for `GET /api/admin/ships/{id}` (full `fields` map present, 404 unknown id, 401/403) appended to `backend/tests/NajaEcho.Api.Tests/Features/Admin/ShipAdminEndpointsTests.cs`
- [X] T058 [P] [US3] Frontend tests for `ShipDetailSheet` (opens from row, lists all fields incl. empty, soft-deleted indicator, close preserves list page/scroll) in `frontend/src/features/admin/__tests__/shipDetailSheet.test.tsx`

### Implementation for User Story 3

- [X] T059 [P] [US3] Create `GetShipByIdQuery` and `ShipDetail` (id, status, full raw field map) in `backend/src/NajaEcho.Application/Features/Ships/GetShipById/`
- [X] T060 [US3] Implement `GetShipByIdHandler` (load by id via `IShipRepository`, project `raw_data` into the field map) in `backend/src/NajaEcho.Application/Features/Ships/GetShipById/GetShipByIdHandler.cs` (depends on T059, T007)
- [X] T061 [P] [US3] Create `ShipDetailResponse` contract (id, status, `fields` object) in `backend/src/NajaEcho.Api/Features/Admin/Ships/Contracts/ShipDetailResponse.cs`
- [X] T062 [US3] Add `GET /api/admin/ships/{id}` (require `Admin` policy; 404 when missing) to `backend/src/NajaEcho.Api/Features/Admin/Ships/ShipAdminEndpoints.cs` (depends on T060, T061, T037)
- [X] T063 [US3] Register `GetShipByIdHandler` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` (depends on T060)
- [X] T064 [P] [US3] Add detail Zod schema (validates/narrows generated type from `ships.d.ts`) to `shipSchemas.ts`, `getShipById()` to `shipsApi.ts`, and `useShipDetail` hook in `frontend/src/features/admin/hooks/useShipDetail.ts` (depends on T039, T068)
- [X] T065 [US3] Implement `ShipDetailSheet` (existing `Sheet` `side="right"`; render every field, empties explicit; soft-deleted badge) in `frontend/src/features/admin/components/ShipDetailSheet.tsx` (depends on T064)
- [X] T066 [US3] Wire the row **View Details** action to open `ShipDetailSheet` (selected ship state lives in `ShipsImportTab`/`ShipsTable` so the list stays mounted and page/scroll is preserved) in `frontend/src/features/admin/components/ShipsTable.tsx` and `ShipsImportTab.tsx` (depends on T065, T054)

**Checkpoint**: All three user stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Contract sync, observability verification, and full-suite validation.

- [X] T069 Verify import observability: structured Serilog events (start, source count, added/updated/reactivated/softDeleted, failure) emit with correlation IDs and no secrets, in `ImportShipsHandler`
- [X] T070 Run quickstart.md validation end-to-end (manual admin grant, import, browse, detail, soft-delete/reactivate, authz 401/403)
- [X] T071 [P] Run full suites: `dotnet test` (incl. Testcontainers), `npm run test:run`, `npm run build` (tsc typecheck), `npm run lint`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T068 (type generation) must complete before any Zod schema task (T039, T053, T064).
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories. T067 (contract sync) must precede T020 (role-claim implementation).
- **User Stories (Phases 3–5)**: All depend on Foundational. US1 → US2 → US3 in priority order; US2 and US3
  are independently testable (seed data directly) and only have soft UI-integration touchpoints into the
  shared `ShipsImportTab`.
- **Polish (Phase 6)**: After the desired stories are complete.

### Critical foundational chain (backend data layer)

T005/T006 → T010 → T011 → T012 (migration); T007 + T011 → T013 → T014; T013 + T002 → T015.

### Admin authz chain

T016/T017 + **T067** → T020; T018 → T019; T020 → T021. Frontend: T022 → T023/T025/T026 → T027.

### User Story Dependencies

- **US1 (P1)**: After Foundational. Self-contained import pipeline + page + route.
- **US2 (P2)**: After Foundational. Reuses `ShipAdminEndpoints` (created in US1, T037) and `ShipsImportTab`
  (T043) — sequence US2 after US1 for those shared files; otherwise independent.
- **US3 (P3)**: After Foundational. Reuses `ShipAdminEndpoints` (T037), `ShipsTable` (T054), and
  `ShipsImportTab` — sequence after US2 for the table/tab integration points.

### Within Each User Story

- Tests written and failing before implementation.
- Application command/query + handler → API contract → endpoint → Program mapping.
- Frontend: schema/key/api → hook → component → page/route integration.

---

## Parallel Opportunities

- **Setup**: T001, T002, T003, T004, T068 all parallel (T068 must complete before T039/T053/T064).
- **Foundational**: T005/T006/T007/T008/T009 parallel (distinct files); T016/T017 parallel; T022/T023
  parallel with the backend authz tasks. The data-layer chain (T010→T011→T012→T013→T014) is sequential.
- **US1 tests**: T028, T029, T030 parallel. **US1 impl**: T033 and T034 parallel; T031 parallel with them.
- **US2 tests**: T045, T046, T047 parallel. **US3 tests**: T057, T058 parallel.
- Across stories: once Foundational is done, US1/US2/US3 backend use cases can be built by different
  developers in parallel; the shared `ShipAdminEndpoints.cs` and `ShipsImportTab.tsx` are the only
  serialization points.

### Parallel Example: User Story 1

```bash
# Tests first (parallel):
Task: "ImportShipsHandler unit tests in backend/tests/.../ImportShipsHandlerTests.cs"   # T028
Task: "Import endpoint integration tests in backend/tests/.../ShipAdminEndpointsTests.cs" # T029
Task: "ImportShipsButton tests in frontend/src/features/admin/__tests__/importShips.test.tsx" # T030

# Then independent infra impls (parallel):
Task: "Implement UexVehicleClient in backend/src/NajaEcho.Infrastructure/Ships/UexVehicleClient.cs" # T033
Task: "Implement ImportCoordinator in backend/src/NajaEcho.Infrastructure/Ships/ImportCoordinator.cs" # T034
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (CRITICAL) → 3. Phase 3 US1 → 4. **STOP & VALIDATE** the import
flow end-to-end (success counts, rollback on failure, 409 concurrency) → 5. Demo/deploy.

### Incremental Delivery

Foundation → US1 (import, MVP) → US2 (browse list) → US3 (detail sheet). Each story is a shippable
increment that doesn't break the previous ones.

---

## Notes

- [P] = different files, no incomplete dependencies. [Story] maps tasks to US1/US2/US3.
- Confirm every test fails before implementing (Constitution II).
- The EF migration (T012) is additive/forward-only — no destructive-change approval needed.
- Admin membership is granted manually in the DB (quickstart.md); only the `Admin` role row is seeded.
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
