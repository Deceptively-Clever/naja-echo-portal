# Implementation Plan: Commodity Data Import

**Branch**: `010-commodity-data-import` | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-commodity-data-import/spec.md`

## Summary

Add a **Commodities** tab to the existing admin **Data Import** page. The tab gives admins one
manual operation against the UEX API:

1. **Import Commodities** — fetch `GET /commodities`, map each valid record into a new local
   `sc.commodities` table keyed by the UEX `id`, upsert (insert new / update existing / restore
   soft-deleted), soft-delete commodities absent from the source, and return an import summary.

This feature is an additive slice that mirrors, almost verbatim, the existing admin **ship import**
flow (feature 006) — the single-table, single-endpoint, full-feed import with global soft-delete is
the precise precedent (commodities are imported in one full pass, not per-category like items). It
reuses the established patterns: the shared `IImportCoordinator` singleton semaphore (which enforces
**one import/refresh at a time globally**, so ships, categories, items, and commodities all share
one lock), the typed `HttpClient` UEX client registration, the `BulkUpsertAsync` transactional
upsert + soft-delete shape, the `/api/admin/...` group guarded by `AuthorizationPolicies.Admin`, the
per-feature OpenAPI contract + `openapi-typescript` generation, and the `apiFetch` + Zod + TanStack
Query frontend conventions.

Commodity-specific work beyond the ship template:

- **Boolean flag normalization** — 20 UEX integer flag fields (`is_available` … `is_fuel`) are
  promoted to `bool` columns using the established `Number != 0` conversion (the `GetBool` helper
  already used by `UexItemClient`).
- **Location identifier fields** — 5 comma-separated string fields (`ids_star_systems` …
  `ids_orbits`) are stored both raw (text) and parsed into native PostgreSQL `integer[]` columns
  (Npgsql maps `int[]` directly), discarding non-numeric tokens and trimming whitespace.
- **Timestamps** — `date_added` / `date_modified` stored both raw (`bigint`) and as converted UTC
  `timestamptz` (via `DateTimeOffset.FromUnixTimeSeconds`); invalid values keep the raw value with a
  null converted datetime.
- **Pricing exclusion** — `price_buy` / `price_sell` are never promoted to columns (they remain only
  inside the verbatim `raw_data` jsonb, which stores the full source object per FR-008).
- **Relationship identifiers** — `id_parent` / `id_item` stored as plain nullable integers with **no**
  foreign-key constraint or existence check (FR-011).

**Schema change IS required**: one new table `sc.commodities`, delivered by one EF Core migration.
**API contract changes ARE required**: one new admin endpoint. This is **not** a UI-only feature.

## Technical Context

**Language/Version**: C# on .NET (repo targets `net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql` (snake_case
naming convention, native `int[]` array mapping), Serilog. Frontend — React 19 (Vite), React Router
data APIs, Tailwind CSS 4, shadcn/ui (Radix), Lucide, TanStack Query 5, Zod, `openapi-typescript`
for generated types.

**Storage**: PostgreSQL 16. One new `sc.commodities` table (code-first EF migration). `sc.ships` is
the established precedent for a single-table `sc`-schema game-data import.

**Testing**: Backend — xUnit, FluentAssertions, in-memory provider for endpoint tests
(`WebApplicationFactory` + fakes, per `ShipAdminEndpointsTests`), Testcontainers (PostgreSQL) for
repository integration tests (per `ShipRepositoryTests` — required because native `int[]` columns are
not exercised by the in-memory provider). Frontend — Vitest + React Testing Library.

**Target Platform**: Linux server (containerized API) + browser SPA.

**Performance Goals**: A full commodity import (hundreds of records) completes within a single page
interaction with no reload, under 60 seconds under normal network conditions (SC-001). Detailed
progress UI is explicitly out of scope; a simple running/loading state suffices.

**Constraints**: Commodity identity is the UEX `id` (`uex_id`); records missing `id` or `name` are
skipped and counted, never fatal. `uuid = null` is imported normally (UUID is not required — matches
ships, differs from items). Pricing fields are never promoted to columns. Relationship integrity is
not enforced. Soft-delete is global (the full feed is authoritative — a commodity absent from the
feed is soft-deleted). Only one import may run at a time (shared `IImportCoordinator`). No
import-history persistence in v1; no browse/list endpoint in v1 (import-only).

**Scale/Scope**: Commodities number in the low hundreds. The entire feed is fetched and upserted in
one transactional pass.

### Verified existing facts (from codebase inspection)

- **Concurrency lock**: `IImportCoordinator` (`ImportCoordinator`, registered **singleton** in
  `DependencyInjection.cs`) wraps a `SemaphoreSlim(1,1)` and is **shared across all imports**. The
  new commodity handler reuses the *same* instance, so "one at a time" spans ships + categories +
  items + commodities for free. `TryAcquire()` returning false → throw
  `ImportAlreadyInProgressException` → endpoint maps to **409 Conflict** (see
  `ShipAdminEndpoints.ImportShips` / `ItemAdminEndpoints`).
- **UEX client**: typed `HttpClient` registered via `services.AddHttpClient<TInterface, TImpl>(...)`
  with base URL `UexVehicleClient:BaseUrl` defaulting to `https://api.uexcorp.uk/2.0/`. Response
  shape is `{ "data": [ ... ] }`; missing/non-array `data` throws `InvalidOperationException`
  (`UexVehicleClient.FetchAllVehiclesAsync`). The new `UexCommodityClient` calls `GET commodities`
  and follows the identical pattern and base URL.
- **Upsert + soft-delete shape**: `ShipRepository.BulkUpsertAsync` opens a transaction, loads
  existing rows by key, updates/reactivates matches, inserts new, then soft-deletes Active rows
  absent from the incoming set — all in one `SaveChanges` + commit. Soft-delete uses a `Status` enum
  (`ShipStatus`, stored as string via `HasConversion<string>()`) plus `SoftDeletedAt`, `ImportedAt`,
  `UpdatedAt` timestamps. `CommodityRepository.BulkUpsertAsync` mirrors this **globally** (the ship
  shape exactly, not the category-scoped item variant).
- **Mapping helpers**: `UexItemClient` already contains the exact helper set needed — `GetString`,
  `GetInt`, `GetNullableInt`, `GetBool` (handles `Number != 0` and JSON true/false),
  `GetDateTimeOffset` (Unix seconds → `DateTimeOffset`). These are the templates for the commodity
  mapper; a new `ParseIdList` helper handles the comma-separated → `int[]` parsing.
- **Schema/config**: `ShipConfiguration` maps `ToTable("ships", schema: "sc")`, `jsonb` raw column,
  unique index on the external id, index on status. `AppDbContext` exposes `DbSet`s and applies
  configurations in `OnModelCreating`. Migrations live in
  `Infrastructure/Persistence/Migrations`; latest is `20260614061222_AddItemCategoryUexIdIndex`.
- **Endpoints**: admin groups use
  `app.MapGroup("/api/admin/...").RequireAuthorization(AuthorizationPolicies.Admin)`; mapped in
  `Program.cs` (lines 252–253: `MapShipAdminEndpoints`, `MapItemAdminEndpoints`). Admin policy =
  `RequireRole("Admin")`. `ImportAlreadyInProgressException` → 409; `HttpRequestException` → 502.
- **DI**: handlers, repositories, and clients are registered explicitly in
  `NajaEcho.Infrastructure/DependencyInjection.cs`. New handler/repo/client register there.
- **Frontend**: `features/admin/` owns the Data Import page. `DataImportPage.tsx` uses shadcn `Tabs`
  with `ships` and `items` tabs today — the **Commodities** tab is added here. The **ship** slice is
  the precedent for a single-button full import: `ImportShipsButton.tsx` (action + `role="status"`
  summary), `hooks/useImportShips.ts` (mutation + `invalidateQueries`), `hooks/shipKeys.ts` (key
  factory), `api/shipsApi.ts` (`apiFetch`), `schemas/shipSchemas.ts` (Zod). Types are generated
  per-feature via `openapi-typescript` scripts in `frontend/package.json`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. API-Contract-First** — Adds **new backend HTTP behaviour** (one admin endpoint).
  **API contract changes ARE required.** `contracts/openapi.yaml` is authored in Phase 1 before
  implementation; frontend types regenerate from it via a new `gen:api:commodities` script. ✅
- **II. Test-First / TDD** — Failing tests first. Application unit tests (flag→bool normalization,
  location string→`int[]` parse incl. whitespace/non-numeric handling, Unix→UTC timestamp
  conversion incl. invalid value, skip+count records missing `id`/`name`, `uuid=null` imported,
  price fields never promoted, global soft-delete + restore-on-reappear, empty-feed soft-deletes
  all, summary counts, fail on invalid shape). Repository integration tests (Testcontainers): global
  soft-delete; restore; `int[]` round-trip; jsonb raw round-trip. Endpoint tests (in-memory +
  fakes, per ship pattern): admin-only (403 for non-admin), 409 when lock held, 502 on UEX failure /
  invalid shape, summary payload. Frontend tests: Commodities tab renders; import summary; 409
  warning; error state. ✅
- **III. Frontend/Backend Separation** — Frontend consumes types generated from the contract; no
  direct DB access; no server HTML; server is authoritative for upsert/soft-delete decisions. ✅
- **IV. Simplicity / YAGNI** — Reuses the shared `IImportCoordinator`, the UEX `HttpClient` pattern,
  and the ship upsert/soft-delete shape verbatim. No background jobs (synchronous request suffices
  at this scale). No import-history table. No progress streaming. No browse/list endpoint (not
  required by the spec — import-only). One new table only because the data is genuinely new. Location
  IDs use native `int[]` rather than join tables (explicitly deferred by the spec). The import
  summary is a transient DTO, not persisted. ✅
- **V. Observability** — The handler emits structured Serilog logs with correlation ID (record
  counts, durations, status — never tokens or full payloads). Skipped-record counts logged. ✅
- **VI. Modular Monolith + Clean Architecture** — New use case under
  `Application/Features/Commodities/ImportCommodities/`; new domain entity in `Domain/Commodities/`;
  repository + UEX client in `Infrastructure/`; endpoint in `API/Features/Admin/Commodities/`;
  frontend logic in `features/admin/` (thin page, tab + button + hook + schema own behaviour). No
  new shared component (single feature owns the tab). ✅

No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/010-commodity-data-import/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output (1 admin endpoint)
├── checklists/
│   └── requirements.md  # (existing)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Commodities/
│       ├── Commodity.cs                              # local commodity entity (uex_id identity)
│       └── CommodityStatus.cs                        # Active | SoftDeleted (mirrors ShipStatus)
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   ├── ICommodityRepository.cs                   # global BulkUpsertAsync
│   │   └── IUexCommodityClient.cs                    # FetchAllCommoditiesAsync()
│   └── Features/
│       └── Commodities/
│           └── ImportCommodities/
│               ├── ImportCommoditiesCommand.cs       # (no parameters — full feed)
│               ├── ImportCommoditiesHandler.cs       # lock, fetch, map+skip, upsert, summary
│               └── ImportCommoditiesResult.cs        # fetched/skipped/inserted/updated/restored/
│                                                     #   softDeleted + timing
├── NajaEcho.Infrastructure/
│   ├── Commodities/
│   │   ├── CommodityRepository.cs                    # global upsert + soft-delete + restore
│   │   └── UexCommodityClient.cs                     # GET commodities; mapping helpers
│   └── Persistence/
│       ├── AppDbContext.cs                           # + DbSet<Commodity>
│       ├── Configurations/
│       │   └── CommodityConfiguration.cs             # sc.commodities (bools, int[] arrays, jsonb)
│       └── Migrations/
│           └── <timestamp>_AddCommodities.cs
└── NajaEcho.Api/
    └── Features/Admin/Commodities/
        ├── CommodityAdminEndpoints.cs                # 1 endpoint under /api/admin/commodities
        └── Contracts/
            └── ImportCommoditiesResponse.cs

backend/tests/
├── NajaEcho.Application.Tests/Features/
│   └── Commodities/ImportCommoditiesHandlerTests.cs  # mapping, skip, flags, parse, summary
├── NajaEcho.Infrastructure.Tests/Commodities/
│   └── CommodityRepositoryTests.cs                   # global soft-delete, restore, int[]/jsonb
└── NajaEcho.Api.Tests/Features/Admin/
    └── CommodityAdminEndpointsTests.cs               # admin-only, 409, 502, summary

frontend/src/
├── features/admin/
│   ├── pages/DataImportPage.tsx                      # + Commodities tab trigger + content
│   ├── components/
│   │   ├── CommoditiesImportTab.tsx                  # description + import action
│   │   └── ImportCommoditiesButton.tsx              # action + import summary (role="status")
│   ├── hooks/
│   │   ├── commodityKeys.ts                          # query key factory
│   │   └── useImportCommodities.ts                   # mutation + invalidation
│   ├── api/commoditiesApi.ts                         # apiFetch wrapper
│   ├── schemas/commoditySchemas.ts                   # Zod: import result
│   └── __tests__/
│       └── importCommodities.test.tsx
└── lib/api/commodities.d.ts                          # generated from contracts/openapi.yaml
```

**Structure Decision**: Web application with the established Clean Architecture layering and frontend
feature folders. The feature is an additive slice of the existing admin **Data Import** feature: one
new Application use-case area (`Commodities/ImportCommodities`), one repository, one UEX typed client,
one endpoint in a new `/api/admin/commodities` group, one `sc` table via one migration, and a new
**Commodities** tab (tab content + button + hook + schema) in `features/admin/`. The concurrency
guarantee comes for free from the shared singleton `IImportCoordinator`. The single-table, full-feed,
global-soft-delete shape follows the **ship** import precedent exactly.

## Complexity Tracking

> No constitution violations — section intentionally empty.
