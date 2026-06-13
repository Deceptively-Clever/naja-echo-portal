# Implementation Plan: Ship Data Import

**Branch**: `006-ship-data-import` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-ship-data-import/spec.md`

## Summary

Add an **admin-only Data Import page** that lets an admin trigger a server-side import of Star Citizen
ship data from the UEX Corp vehicle feed (`https://api.uexcorp.uk/2.0/vehicles`), browse the imported
records in a paginated table, and open a right-side **shadcn `Sheet`** to view the full 64-field record
for any ship. The page is built around a **tab strip** (one tab per data type) so future data types
(in-game items, prices, etc.) slot in without redesign — this pass implements the **Ships** tab only.

The backend owns the external integration (Constitution III): the SPA never calls UEX directly. A new
`POST /api/admin/ships/import` endpoint fetches the full feed server-side and performs a **transactional
upsert** — new ships inserted, existing ships updated, ships missing from the feed **soft-deleted**, and
previously soft-deleted ships that reappear **auto-reactivated** (FR-009/010/011). Any mid-import failure
**rolls back the whole transaction**, leaving stored data untouched (FR-005). A single-flight lock rejects
concurrent imports with `409 Conflict` (FR-003). Ships are stored with a **hybrid model**: a few promoted,
queryable columns (`uex_id`, `uuid`, `name`, `name_full`, `company_name`, `status`, timestamps) plus the
**full raw record as JSONB**, preserving 100% of feed fields and absorbing upstream schema additions.

**This feature DOES add backend HTTP behaviour**, so an OpenAPI contract is authored before implementation
(Constitution I). Three new admin endpoints are added, and the existing `/api/auth/me` response is extended
with a `roles` array so the SPA can gate the Admin navigation section and route.

**Admin authorization is introduced for the first time** (per planning decision): the existing — but unused
— ASP.NET Identity roles schema is activated. An `Admin` role is **seeded** at startup; **role membership
is assigned manually in the database** (no role-management UI this pass). On sign-in, the user's roles are
read and emitted as role claims into the auth cookie; the admin endpoints and the `/dashboard/admin/*`
routes are protected by an `Admin` authorization policy / role check.

**Technical approach**: Backend is .NET 10 Clean Architecture (Domain → Application → Infrastructure → API)
with EF Core + PostgreSQL (snake_case). A new `Ship` domain entity and `ImportShips` / `GetShips` /
`GetShipById` application use cases are added in feature folders, with an `IUexVehicleClient` port
implemented by a typed `HttpClient` in Infrastructure and an `IShipRepository` EF implementation. The
import coordinator (single-flight) and the `Admin` role seeder live in Infrastructure. Frontend adds a
`features/admin/` feature folder (data-import page, ships tab, table, detail sheet, TanStack Query hooks,
Zod schemas mirroring the contract), two generic primitives (`components/ui/tabs.tsx` wrapping
`@radix-ui/react-tabs`, and `components/ui/table.tsx`), an `AdminRoute` guard, and an "Admin" group in the
data-driven navigation. The existing right-side `Sheet` primitive is reused as-is.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`); TypeScript ~5.8 (strict), React 19.2

**Primary Dependencies**:
- Backend (existing): ASP.NET Core Minimal APIs, EF Core 10 + `Npgsql` (snake_case), ASP.NET Core
  Identity (cookie auth, roles), Serilog, FluentValidation (available). **New**: a typed `HttpClient`
  (`Microsoft.Extensions.Http`) for the UEX feed; `System.Text.Json` `JsonDocument`/JSONB mapping
  (no new package — Npgsql maps `JsonDocument`/`string` to `jsonb`).
- Frontend (existing): `react-router-dom` v7, TanStack Query v5, Tailwind v4, shadcn/ui (`sheet` already
  present), `class-variance-authority`/`clsx`/`tailwind-merge`, `lucide-react`, Zod, React Hook Form.
  **New**: `@radix-ui/react-tabs` (MIT, same Radix family already in use) backing a new shadcn `Tabs`
  primitive. A new `Table` primitive (`components/ui/table.tsx`) is styled HTML — no new dependency.

**Storage**: PostgreSQL (existing `AppDbContext`). New `ships` table — hybrid: promoted columns
(`uex_id` unique, `uuid`, `name`, `name_full`, `company_name`, `status`, `imported_at`, `updated_at`,
`soft_deleted_at`) + `raw_data jsonb` holding the complete feed record. New EF Core migration. The `Admin`
role row is seeded (membership assigned manually). No import-history table (clarified out of scope).

**Testing**:
- Backend: xUnit + FluentAssertions (existing). Application unit tests for the import upsert/soft-delete/
  reactivate/rollback algorithm against an `IShipRepository`/`IUexVehicleClient` fake. API integration
  tests via `WebApplicationFactory` (existing pattern) for authz (401/403/200) and the import/list/detail
  contracts. **Repository + JSONB + transactional-rollback integration tests require a real PostgreSQL**
  (EF InMemory models neither JSONB nor transactions) → add **Testcontainers (PostgreSQL)** to
  `NajaEcho.Infrastructure.Tests` (aligns with Constitution II's "real database" integration requirement).
- Frontend: Vitest + RTL + MSW (existing). Tests for the ships table (rows, empty state, pagination,
  soft-deleted indicator), the detail sheet (opens on view, shows all fields, preserves list position),
  the import trigger (loading/disabled, success count, error, 409 in-progress), the admin nav gating, and
  the `AdminRoute` guard.

**Target Platform**: Linux container (backend API) + modern evergreen browsers (SPA served by Vite).

**Project Type**: Web application — full stack (`backend/` + `frontend/`).

**Performance Goals**: Full import (~278 records today) reflected in the table in **under 30 s** (SC-001) —
a single server-side fetch + one batched transactional upsert. Paginated list (default 25/page) returns
promptly via the indexed promoted columns. Detail sheet opens instantly from already-fetched row data.

**Constraints**: Frontend never calls UEX directly (Constitution III) — all external I/O is server-side.
Import is transactional and all-or-nothing (FR-005). Single-flight: no concurrent imports (FR-003). Zero
-record feed responses must NOT wipe stored data (edge case) — guarded before any mutation. Sensitive
auth data stays out of logs (Constitution V). Semantic Tailwind tokens only in new UI; no raw hex.

**Scale/Scope**: Backend — 1 domain entity (`Ship`), 1 enum (`ShipStatus`), 3 use cases (Import/List/
GetById), 1 external-feed port + typed-client impl, 1 repository port + EF impl, 1 import coordinator
(single-flight), 1 `Admin` role seeder, role-claim emission on sign-in, `/api/auth/me` roles extension,
1 EF migration, 1 OpenAPI contract (3 new endpoints + me extension). Frontend — 1 feature folder
(`features/admin`), 2 new generic primitives (`Tabs`, `Table`) + 1 new dependency, 1 `AdminRoute` guard,
nav grouping + Admin section, 1 route, TanStack Query hooks + key factory, Zod schemas, detail sheet
reusing the existing `Sheet`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | ✅ PASS | Feature adds backend endpoints, so an OpenAPI contract (`contracts/openapi.yaml`) is authored **before** implementation: `GET /api/admin/ships`, `GET /api/admin/ships/{id}`, `POST /api/admin/ships/import`, plus the `/api/auth/me` `roles` extension. Implementation conforms to the contract; the contract ships in this PR. |
| II. Test-First / TDD | ✅ PASS | Application unit tests for the import algorithm (upsert/soft-delete/reactivate/rollback/zero-record guard) written first against fakes. API integration tests (`WebApplicationFactory`) for authz + contracts. Repository/JSONB/transaction tests via Testcontainers-Postgres (the "real database" integration test Constitution II requires). Frontend component/hook tests for all new user-facing behaviour. |
| III. Frontend/Backend Separation | ✅ PASS | The SPA never calls UEX — the backend owns the integration behind `IUexVehicleClient`. Frontend talks only to our REST API via the versioned contract. No server-rendered HTML. API-boundary types are **generated** from `contracts/openapi.yaml` via `openapi-typescript` (T068, mandatory Phase 1); Zod schemas in `shipSchemas.ts` validate and narrow those generated types at runtime — they do not re-define the contract shape (Constitution III Frontend Conventions). |
| IV. Simplicity / YAGNI | ✅ PASS | Hybrid JSONB storage avoids 64 hand-mapped columns and future migrations. Single-flight lock is an in-memory semaphore (single-instance deployment) — a Postgres advisory lock is noted as a future need, not built now. Admin grant is manual DB assignment — no role-management UI. Import history is out of scope. Ships are read-only — no edit endpoints. Only one new frontend dependency (`@radix-ui/react-tabs`), justified by the user-requested tabbed layout. |
| V. Observability | ✅ PASS | Import emits structured Serilog events (start, source-record count, added/updated/reactivated/soft-deleted counts, failure) with correlation IDs; no secrets logged. Health endpoint already present. The UEX feed is public (no tokens) so there is nothing sensitive to scrub from the integration. |
| VI. Modular Monolith + Clean Architecture | ✅ PASS | Backend respects inward dependencies: `Ship` entity + `ShipStatus` in **Domain**; `ImportShips`/`GetShips`/`GetShipById` use cases, `IShipRepository` and `IUexVehicleClient` ports in **Application**; EF repo, typed UEX client, import coordinator, role seeder in **Infrastructure**; endpoints + DTO mapping + the `Admin` policy in **API**. Frontend: the new `Tabs`/`Table` primitives are generic and application-agnostic in `components/ui/`; the application-specific data-import composition lives in `features/admin/`. Routes stay thin (compose `AdminRoute` + feature components); data fetching/validation live in feature hooks/schemas. |

**Frontend Conventions check**:
- shadcn/ui ownership — new `Tabs`/`Table` primitives in `components/ui/` are generic (no admin logic embedded); the data-import behaviour lives in `features/admin/`. ✅
- API client/type generation — an OpenAPI contract is authored; API-boundary types are **generated** from `contracts/openapi.yaml` via `openapi-typescript` (T068, mandatory Phase 1 Setup task, output `frontend/src/lib/api/ships.d.ts`). Zod schemas in `shipSchemas.ts` validate and narrow those generated types at runtime; they do not re-define the contract shape (Constitution III Frontend Conventions). ✅
- TanStack Query — ship list/detail are server state behind a feature key factory (`shipKeys`) and feature hooks; the import trigger is a mutation that invalidates the list on success. Components consume hooks, not the fetch layer directly. ✅
- Forms — the import trigger is a single action (button), not a form; no RHF/Zod form needed. Zod is used for response validation. ✅
- Dashboard shell & navigation — the page renders into the shell outlet; the "Admin" section is added to the single data-driven nav source (`navItems`) with an `access: 'admin'` rule and a group label, consumed identically by desktop and mobile nav. ✅

**Gate result: PASS.** The only notable additions — activating Identity roles and adding Testcontainers
for repository tests — are required by the feature (admin-only access) and by Constitution II (real-DB
integration test), respectively. Complexity Tracking not required.

**Post-Design re-check (after Phase 1)**: ✅ PASS — the data model, contract, and quickstart introduce no
new violations. JSONB hybrid storage and the transactional single-flight import are the minimal designs
that satisfy FR-005/009/010/011 and the zero-record edge case. The single new frontend dependency
(`@radix-ui/react-tabs`) is MIT-licensed and in the already-approved Radix family. Role activation reuses
the existing Identity schema (no new auth framework). The in-memory single-flight lock is sufficient for
the current single-instance deployment and is documented as such.

## Project Structure

### Documentation (this feature)

```text
specs/006-ship-data-import/
├── plan.md              # This file
├── research.md          # Phase 0 output — decisions & rationale
├── data-model.md        # Phase 1 output — Ship entity, status lifecycle, import algorithm, role seed
├── quickstart.md        # Phase 1 output — validation/run guide
├── contracts/
│   └── openapi.yaml      # Phase 1 output — 3 admin ship endpoints + /api/auth/me roles extension
├── checklists/
│   └── requirements.md  # Spec quality checklist (created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── NajaEcho.Domain/
│   │   └── Ships/
│   │       ├── Ship.cs                          # NEW — entity: promoted props + RawData (JSON) + status
│   │       └── ShipStatus.cs                    # NEW — enum: Active | SoftDeleted
│   ├── NajaEcho.Application/
│   │   ├── Abstractions/
│   │   │   ├── IShipRepository.cs               # NEW — port: paged list, get by uex id, get by id, bulk upsert/soft-delete
│   │   │   ├── IUexVehicleClient.cs             # NEW — port: fetch all vehicles from the feed
│   │   │   └── IImportCoordinator.cs            # NEW — port: single-flight acquire/release
│   │   └── Features/
│   │       └── Ships/
│   │           ├── ImportShips/
│   │           │   ├── ImportShipsCommand.cs    # NEW
│   │           │   ├── ImportShipsHandler.cs    # NEW — fetch → guard zero → transactional upsert/soft-delete/reactivate
│   │           │   └── ImportShipsResult.cs     # NEW — added/updated/reactivated/softDeleted/total counts
│   │           ├── GetShips/
│   │           │   ├── GetShipsQuery.cs         # NEW — page, pageSize
│   │           │   ├── GetShipsHandler.cs       # NEW
│   │           │   └── ShipListItem.cs          # NEW — id, name, companyName, status
│   │           └── GetShipById/
│   │               ├── GetShipByIdQuery.cs      # NEW
│   │               ├── GetShipByIdHandler.cs    # NEW
│   │               └── ShipDetail.cs            # NEW — id, status, full raw field map
│   ├── NajaEcho.Infrastructure/
│   │   ├── Ships/
│   │   │   ├── ShipRepository.cs                # NEW — EF impl (LINQ paging, transaction)
│   │   │   ├── UexVehicleClient.cs              # NEW — typed HttpClient → parses {status,data:[...]}
│   │   │   └── ImportCoordinator.cs             # NEW — singleton SemaphoreSlim(1,1), TryAcquire
│   │   ├── Identity/
│   │   │   └── AdminRoleSeeder.cs               # NEW — ensures the "Admin" role row exists at startup
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs                  # CHANGE — add DbSet<Ship>; map ships table + jsonb
│   │   │   ├── Configurations/
│   │   │   │   └── ShipConfiguration.cs         # NEW — columns, unique index on uex_id, jsonb mapping
│   │   │   └── Migrations/                       # NEW — AddShips migration (+ snapshot update)
│   │   └── DependencyInjection.cs              # CHANGE — register repo, typed UEX client, coordinator, seeder
│   └── NajaEcho.Api/
│       ├── Features/
│       │   ├── Admin/
│       │   │   └── Ships/
│       │   │       ├── ShipAdminEndpoints.cs    # NEW — map GET list, GET {id}, POST import under /api/admin
│       │   │       └── Contracts/
│       │   │           ├── ShipListItemResponse.cs   # NEW
│       │   │           ├── PagedShipsResponse.cs     # NEW — items + page/pageSize/total/totalPages
│       │   │           ├── ShipDetailResponse.cs     # NEW — id, status, fields (object)
│       │   │           └── ImportShipsResponse.cs    # NEW — counts
│       │   └── Auth/
│       │       └── Contracts/
│       │           └── CurrentUserResponse.cs   # CHANGE — add Roles (string[]) to the authenticated payload
│       ├── Authorization/
│       │   └── AuthorizationPolicies.cs         # NEW — "Admin" role policy constant + registration helper
│       └── Program.cs                           # CHANGE — emit role claims on sign-in; add Admin policy;
│                                                #          run AdminRoleSeeder; map ShipAdminEndpoints
└── tests/
    ├── NajaEcho.Application.Tests/
    │   └── Features/Ships/
    │       └── ImportShipsHandlerTests.cs       # NEW — upsert/soft-delete/reactivate/rollback/zero-record
    ├── NajaEcho.Infrastructure.Tests/
    │   └── Ships/
    │       └── ShipRepositoryTests.cs           # NEW — Testcontainers-Postgres: paging, jsonb, transaction
    └── NajaEcho.Api.Tests/
        └── Features/Admin/
            └── ShipAdminEndpointsTests.cs       # NEW — 401/403/200 authz + import 409 + list/detail shape

frontend/
├── src/
│   ├── components/
│   │   └── ui/
│   │       ├── sheet.tsx                        # EXISTS — reused (side="right") for ship detail
│   │       ├── tabs.tsx                         # NEW — generic shadcn Tabs (wraps @radix-ui/react-tabs)
│   │       └── table.tsx                        # NEW — generic shadcn Table primitives (styled HTML)
│   ├── features/
│   │   └── admin/
│   │       ├── pages/
│   │       │   └── DataImportPage.tsx           # NEW — thin: Tabs strip; renders ShipsImportTab
│   │       ├── components/
│   │       │   ├── ShipsImportTab.tsx           # NEW — import button + ShipsTable + state
│   │       │   ├── ShipsTable.tsx               # NEW — name, company, status badge, View Details; pagination
│   │       │   ├── ShipDetailSheet.tsx          # NEW — right Sheet listing all raw fields (empty shown)
│   │       │   ├── ImportShipsButton.tsx        # NEW — triggers mutation; loading/disabled/success/error/409
│   │       │   └── ShipsPagination.tsx          # NEW — page controls (default 25/page)
│   │       ├── api/
│   │       │   └── shipsApi.ts                  # NEW — apiFetch wrappers (list, detail, import)
│   │       ├── hooks/
│   │       │   ├── shipKeys.ts                  # NEW — query key factory
│   │       │   ├── useShips.ts                  # NEW — paged list query
│   │       │   ├── useShipDetail.ts             # NEW — detail query (optional; row carries summary)
│   │       │   └── useImportShips.ts            # NEW — mutation; invalidates list on success
│   │       ├── schemas/
│   │       │   └── shipSchemas.ts               # NEW — Zod: list item, paged response, detail, import result
│   │       └── __tests__/                        # NEW — ShipsTable / ShipDetailSheet / ImportShipsButton / nav-gating / AdminRoute tests
│   ├── features/auth/
│   │   ├── ProtectedRoute.tsx                   # EXISTS — unchanged
│   │   ├── AdminRoute.tsx                       # NEW — requires 'admin' role; else redirect/forbidden
│   │   └── schemas/sessionStateSchema.ts        # CHANGE — add roles: string[] to authenticated user
│   ├── features/dashboard/
│   │   ├── navigation/navItems.ts               # CHANGE — add "Admin" group + Data Import item (access:'admin')
│   │   └── components/DashboardNav.tsx          # CHANGE — render optional group headings; filter by access
│   └── routes/AppRouter.tsx                     # CHANGE — add /dashboard/admin/data-import under AdminRoute
└── frontend/src/lib/api/ships.d.ts             # NEW — generated types from contracts/openapi.yaml (T068, mandatory Phase 1; canonical API-boundary types)
```

**Structure Decision**: Full-stack change. The backend follows the established Clean Architecture
feature-folder layout (`Features/Ships/<UseCase>/…` in Application, `Features/Admin/Ships/…` in API),
adding the `Ship` aggregate to Domain and the EF/HTTP/coordinator/seeder implementations to Infrastructure
behind Application ports. The frontend adds a dedicated `features/admin/` folder owning the data-import
page, the ships tab, table, detail sheet, hooks, and schemas; the two new generic primitives live in
`components/ui/`; the existing right-side `Sheet` is reused; navigation is extended in its single
data-driven source. Routes remain thin, composing the new `AdminRoute` guard with feature components.

## Key Technical Decisions

1. **Backend owns the UEX integration; SPA never calls it directly** (Constitution III). `POST
   /api/admin/ships/import` fetches the feed server-side via a typed `HttpClient` (`IUexVehicleClient`).
   This keeps the all-or-nothing transaction, soft-delete bookkeeping, and structured logging on the
   server, and means CORS/feed-auth changes never touch the browser.

2. **Hybrid storage — promoted columns + raw JSONB** (planning decision). The `ships` table carries
   typed, indexed columns for the values we query/sort/display (`uex_id` unique, `uuid`, `name`,
   `name_full`, `company_name`, `status`, timestamps) plus `raw_data jsonb` holding the verbatim
   64-field record. This preserves 100% of feed data (SC-004), absorbs upstream field additions without a
   migration, and powers the detail sheet directly from `raw_data`. Identity key for upsert is the UEX
   `id` (stable integer, present and unique across all 278 records; `uuid` is also stored).

3. **Transactional, all-or-nothing import** (FR-005). Algorithm: (a) acquire single-flight lock or return
   409; (b) fetch the full feed — on transport/parse failure, abort with an error and **no** DB writes;
   (c) **zero-record guard** — if the feed returns an empty `data` array, abort with a warning and leave
   data unchanged (never soft-delete everything); (d) open an EF transaction; (e) upsert each incoming
   ship by `uex_id` (insert new as `Active`; update existing promoted columns + `raw_data`, and if it was
   `SoftDeleted`, set `Active` and clear `soft_deleted_at` → **reactivation**, FR-011); (f) any `Active`
   ship whose `uex_id` is absent from the feed → `SoftDeleted` with `soft_deleted_at` (FR-009); (g) commit
   and return counts; any exception → rollback (data untouched) and surface an error; (h) release the lock
   in `finally`.

4. **Single-flight concurrency via an in-memory semaphore** (FR-003, YAGNI). `IImportCoordinator` is a
   singleton wrapping `SemaphoreSlim(1,1)`; `TryAcquire()` (zero timeout) returns false when an import is
   already running → endpoint responds `409 Conflict`. This is correct for the current single-instance
   deployment; a Postgres advisory lock would be required for multi-instance and is documented as a future
   need, not built now. **No cooldown** between imports (clarified) — the lock is the only guard.

5. **Activate ASP.NET Identity roles; seed the role, assign membership manually** (planning decisions).
   The Identity roles schema already exists. `AdminRoleSeeder` ensures the `Admin` role row exists at
   startup (idempotent). Membership is granted by inserting an `AspNetUserRoles` row by hand (no UI this
   pass — documented in quickstart). On sign-in (`Program.cs` `OnTicketReceived`), the user's roles are
   loaded via `UserManager` and emitted as `ClaimTypes.Role` claims into the auth cookie. The admin
   endpoints and routes require the `Admin` policy. **Caveat (documented)**: because roles are baked into
   the cookie at sign-in, a freshly-granted admin must sign out/in (or wait for the 24h cookie cycle) for
   the role to take effect — acceptable for a manual-assignment first pass.

6. **`/api/auth/me` extended with `roles`** so the SPA can drive UI from server truth. The authenticated
   session payload gains `roles: string[]`; the frontend `sessionStateSchema` mirrors it, the nav filters
   the Admin section by `access: 'admin'`, and `AdminRoute` guards `/dashboard/admin/*`. This is the one
   change to the existing auth contract (002) and is reflected in this feature's OpenAPI delta.

7. **Defense in depth: server is the source of truth for authz.** The frontend nav-gating and `AdminRoute`
   are UX only; every admin endpoint independently enforces the `Admin` policy (401 unauthenticated, 403
   authenticated-but-not-admin). A non-admin who navigates directly to the API gets 403 regardless of the
   SPA.

8. **Tabbed page for extensibility** (FR-012, user direction). `DataImportPage` renders a shadcn `Tabs`
   strip; this pass wires only the **Ships** tab. Adding a future data type = a new tab + a new feature
   module, no layout rework. New generic `Tabs` primitive (`components/ui/tabs.tsx`, `@radix-ui/react-tabs`)
   and `Table` primitive (`components/ui/table.tsx`, styled HTML, no dependency) are added in the shadcn
   house style (`cva`/`cn`/`forwardRef`, semantic tokens only).

9. **Right-side detail Sheet reuses the existing primitive.** `ShipDetailSheet` uses the existing
   `Sheet` with `side="right"` (already supported) and lists every field from `raw_data`; fields with no
   value render as explicitly empty (US3 scenario 3), and a badge marks soft-deleted records (US3 scenario
   4). Closing returns to the list with page/scroll preserved because the list is never unmounted — the
   sheet is an overlay and pagination state lives in the table component (SC-003 / US3 scenario 2).

10. **Pagination** (FR-006, default 25/page). The list endpoint takes `page`/`pageSize` and returns
    `{ items, page, pageSize, totalCount, totalPages }`; the table renders rows (name, company, status
    badge, View Details) with prev/next controls and an empty state (FR-007). Search/filter is out of
    scope (assumption).

11. **Faithful raw storage; presentation-layer formatting.** The feed's quirks (`is_*` as int 0/1, unix
    timestamps, some numbers as strings like `crew: "1"`) are stored verbatim in `raw_data` (no lossy
    coercion → SC-004). The detail sheet formats for readability on the client; the promoted columns are
    only the clean string/int values we sort and display.

12. **Real-database tests for the repository.** EF InMemory cannot model JSONB or transactional rollback,
    so `ShipRepositoryTests` use **Testcontainers-PostgreSQL** to exercise paging, JSONB round-trip, the
    transactional upsert, and soft-delete/reactivate — satisfying Constitution II's real-DB integration
    requirement. Application-layer algorithm tests stay fast against fakes; API tests use the existing
    `WebApplicationFactory` pattern.

## Complexity Tracking

No constitution violations requiring justification. The two additions called out above — activating the
pre-existing Identity roles schema and adding Testcontainers to the Infrastructure test project — are
mandated by the feature's admin-only requirement and by Constitution II's real-database integration-test
rule, respectively. Section intentionally otherwise empty.
