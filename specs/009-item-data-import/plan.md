# Implementation Plan: Item Data Import

**Branch**: `009-item-data-import` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/009-item-data-import/spec.md`

## Summary

Add an **Items** tab to the existing admin **Data Import** page. The tab gives admins two
manual, independent operations against the UEX API:

1. **Refresh Categories** — fetch `GET /categories`, upsert into a new local `sc.item_categories`
   table, and return a category-refresh summary.
2. **Import Items** — for one selected local category, or for *all* eligible local categories
   (`type = "item"`), fetch `GET /items?id_category={id}` per category, upsert into a new local
   `sc.items` table keyed by `uuid`, soft-delete items absent from the category response (scoped to
   that category only), restore previously soft-deleted items that reappear, and return an
   item-import summary. Item imports **never** call the categories endpoint.

The feature is an additive slice of the existing admin ship-import flow (feature 006). It reuses,
verbatim, the established patterns: the `IImportCoordinator` singleton semaphore (which already
enforces **one import/refresh at a time globally**, so ships, categories, and items all share one
lock), the typed `HttpClient` UEX client registration, the `BulkUpsertAsync` transactional
upsert + soft-delete shape, the `/api/admin/...` group guarded by `AuthorizationPolicies.Admin`,
the per-feature OpenAPI contract + `openapi-typescript` generation, and the `apiFetch` + Zod +
TanStack Query frontend conventions.

**Schema change IS required**: two new tables under the `sc` schema (`item_categories`, `items`),
delivered by one EF Core migration. **API contract changes ARE required**: three new admin
endpoints. This is **not** a UI-only feature.

## Technical Context

**Language/Version**: C# on .NET (repo targets `net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql`
(snake_case naming convention), Serilog. Frontend — React 19 (Vite), React Router data APIs,
Tailwind CSS 4, shadcn/ui (Radix), Lucide, TanStack Query 5, Zod, `openapi-typescript` for
generated types.

**Storage**: PostgreSQL 16. New `sc.item_categories` and `sc.items` tables (code-first EF
migration). `sc.ships` is the established precedent for `sc`-schema game data.

**Testing**: Backend — xUnit, FluentAssertions, in-memory provider for endpoint tests
(`WebApplicationFactory` + fakes, per `ShipAdminEndpointsTests`), Testcontainers (PostgreSQL) for
repository integration tests (per `ShipRepositoryTests`). Frontend — Vitest + React Testing
Library.

**Target Platform**: Linux server (containerized API) + browser SPA.

**Project Type**: Web application (separate backend API + React SPA).

**Performance Goals**: Each operation (refresh, single-category import, all-category import)
completes within a single page interaction with no reload (SC-001, SC-002, SC-003). Detailed
progress UI is explicitly out of scope; a simple running/loading state suffices.

**Constraints**: Item imports MUST NOT call the categories endpoint (FR-… business rule). Only one
operation may run at a time (shared `IImportCoordinator`). Item identity is `uuid`; `uuid = null`
records are skipped and counted, never fatal. Soft-delete is scoped to the imported category only.
Deprecated `attributes` and v1-excluded `screenshot` fields are never imported or stored. No
import-history persistence in v1.

**Scale/Scope**: Categories number in the low hundreds; items per category range from a handful to
a few thousand. All-category import processes every `type = "item"` category sequentially.

### Verified existing facts (from codebase inspection)

- **Concurrency lock**: `IImportCoordinator` (`ImportCoordinator`, registered **singleton** in
  `DependencyInjection.cs`) wraps a `SemaphoreSlim(1,1)`. It is **shared across all imports** — the
  new category-refresh and item-import handlers reuse the *same* instance, so "one at a time"
  spans ships + categories + items for free. `TryAcquire()` returns false → throw
  `ImportAlreadyInProgressException` → endpoint maps to **409 Conflict** (see
  `ShipAdminEndpoints.ImportShips`).
- **UEX client**: typed `HttpClient` registered via
  `services.AddHttpClient<IUexVehicleClient, UexVehicleClient>(...)` with base URL
  `UexVehicleClient:BaseUrl` defaulting to `https://api.uexcorp.uk/2.0/`. Response shape is
  `{ "data": [ ... ] }`; missing/non-array `data` throws `InvalidOperationException`. New clients
  for categories and items follow the identical pattern and base URL.
- **Upsert + soft-delete shape**: `ShipRepository.BulkUpsertAsync` opens a transaction, loads
  existing rows by key, updates/reactivates matches, inserts new, then soft-deletes Active rows
  absent from the incoming set, all in one `SaveChanges` + commit. Soft-delete uses a `Status`
  enum (`ShipStatus`, stored as string via `HasConversion<string>()`) plus `SoftDeletedAt`,
  `ImportedAt`, `UpdatedAt` timestamps. The items repository mirrors this, but **scopes the
  soft-delete query to the single category being imported**.
- **Schema/config**: `ShipConfiguration` maps `ToTable("ships", schema: "sc")`, `jsonb` raw column,
  unique index on the external id, index on status. `AppDbContext` exposes `DbSet`s and applies
  configurations in `OnModelCreating`. Migrations live in
  `Infrastructure/Persistence/Migrations`; latest is `20260613225055_MoveHangarEntriesToPublicSchema`.
- **Endpoints**: `ShipAdminEndpoints.MapShipAdminEndpoints` →
  `app.MapGroup("/api/admin/ships").RequireAuthorization(AuthorizationPolicies.Admin)`; mapped in
  `Program.cs` (line 251). Admin policy = `RequireRole("Admin")`.
- **DI**: handlers, repositories, and the coordinator are registered explicitly in
  `NajaEcho.Infrastructure/DependencyInjection.cs`. New handlers/repos/clients register there.
- **Frontend**: `features/admin/` owns the Data Import page. `DataImportPage.tsx` uses shadcn
  `Tabs` with a single `ships` tab today — the **Items** tab is added here. Patterns: `apiFetch`
  (`api/shipsApi.ts`), Zod schemas (`schemas/shipSchemas.ts`), TanStack Query key factory
  (`hooks/shipKeys.ts`), mutation hooks with cache invalidation (`hooks/useImportShips.ts`),
  result rendered as a `role="status"` live region (`ImportShipsButton.tsx`). Types generated
  per-feature via `openapi-typescript` scripts in `frontend/package.json`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. API-Contract-First** — Adds **new backend HTTP behaviour** (three admin endpoints).
  **API contract changes ARE required.** `contracts/openapi.yaml` is authored in Phase 1 before
  implementation; frontend types regenerate from it via a new `gen:api:items` script. ✅
- **II. Test-First / TDD** — Failing tests first. Application unit tests (UUID upsert, `uuid=null`
  skip+count, category-scoped soft-delete, restore-on-reappear, `type="item"` eligibility,
  per-category failure isolation in all-category import, summary counts). Repository integration
  tests (Testcontainers): scoped soft-delete does not touch other categories; restore. Endpoint
  tests (in-memory + fakes, per ship pattern): admin-only (403 for non-admin), 409 when lock held,
  502 on UEX failure, summary payloads. Frontend tests: Items tab renders; disabled state + message
  when no categories; refresh summary; single + all-category import summaries; category filters. ✅
- **III. Frontend/Backend Separation** — Frontend consumes types generated from the contract; no
  direct DB access; no server HTML; server is authoritative for upsert/soft-delete decisions. ✅
- **IV. Simplicity / YAGNI** — Reuses the shared `IImportCoordinator`, UEX `HttpClient` pattern,
  and the ship upsert/soft-delete shape. No background jobs (synchronous request is sufficient at
  this scale). No import-history table. No progress streaming. Two new tables only because the data
  is genuinely new. Item summaries are transient DTOs, not persisted. ✅
- **V. Observability** — Both handlers emit structured Serilog logs with correlation ID (counts,
  category ids, durations only — never tokens or full payloads). Per-category failures logged at
  warning level during all-category import. ✅
- **VI. Modular Monolith + Clean Architecture** — New use cases under
  `Application/Features/ItemCategories/RefreshCategories/` and `Application/Features/Items/ImportItems/`;
  new domain entities in `Domain/`; repositories + UEX clients in `Infrastructure/`; endpoints in
  `API/Features/Admin/Items/`; frontend logic in `features/admin/` (thin page, tab + hooks +
  schemas own behaviour). No new shared component (single feature owns the tab). ✅

No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/009-item-data-import/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output (3 admin endpoints)
├── checklists/
│   └── requirements.md  # (existing)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   ├── ItemCategories/
│   │   └── ItemCategory.cs                         # local category entity
│   └── Items/
│       ├── Item.cs                                 # local item entity (uuid identity)
│       └── ItemStatus.cs                           # Active | SoftDeleted (mirrors ShipStatus)
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   ├── IItemCategoryRepository.cs              # upsert categories; list local; list eligible
│   │   ├── IItemRepository.cs                      # category-scoped BulkUpsertAsync
│   │   ├── IUexCategoryClient.cs                   # FetchAllCategoriesAsync()
│   │   └── IUexItemClient.cs                       # FetchItemsByCategoryAsync(categoryId)
│   └── Features/
│       ├── ItemCategories/
│       │   ├── RefreshCategories/
│       │   │   ├── RefreshCategoriesCommand.cs
│       │   │   ├── RefreshCategoriesHandler.cs     # lock, fetch, upsert, summary
│       │   │   └── RefreshCategoriesResult.cs      # fetched/inserted/updated/unchanged/failed + timing
│       │   └── GetCategories/
│       │       ├── GetCategoriesQuery.cs
│       │       ├── GetCategoriesHandler.cs         # local categories for the selector
│       │       └── CategoryListItem.cs
│       └── Items/
│           └── ImportItems/
│               ├── ImportItemsCommand.cs           # { CategoryUexId? }  (null ⇒ all eligible)
│               ├── ImportItemsHandler.cs           # orchestrates single + all-category, per-cat
│               │                                   #   failure isolation, aggregate summary
│               ├── ImportItemsResult.cs            # aggregate item-import summary + per-cat errors
│               └── CategoryImportError.cs          # { CategoryUexId, CategoryName, Message }
├── NajaEcho.Infrastructure/
│   ├── ItemCategories/
│   │   ├── ItemCategoryRepository.cs
│   │   └── UexCategoryClient.cs
│   ├── Items/
│   │   ├── ItemRepository.cs                       # category-scoped upsert + soft-delete + restore
│   │   └── UexItemClient.cs
│   └── Persistence/
│       ├── AppDbContext.cs                         # + DbSet<ItemCategory>, DbSet<Item>
│       ├── Configurations/
│       │   ├── ItemCategoryConfiguration.cs        # sc.item_categories
│       │   └── ItemConfiguration.cs                # sc.items
│       └── Migrations/
│           └── <timestamp>_AddItemCategoriesAndItems.cs
└── NajaEcho.Api/
    └── Features/Admin/Items/
        ├── ItemAdminEndpoints.cs                   # 3 endpoints under /api/admin/items
        └── Contracts/
            ├── RefreshCategoriesResponse.cs
            ├── CategoryListItemResponse.cs
            └── ImportItemsResponse.cs

backend/tests/
├── NajaEcho.Application.Tests/Features/
│   ├── ItemCategories/RefreshCategoriesHandlerTests.cs
│   └── Items/ImportItemsHandlerTests.cs
├── NajaEcho.Infrastructure.Tests/Items/ItemRepositoryTests.cs
└── NajaEcho.Api.Tests/Features/Admin/ItemAdminEndpointsTests.cs

frontend/src/
├── features/admin/
│   ├── pages/DataImportPage.tsx                    # + Items tab trigger + content
│   ├── components/
│   │   ├── ItemsImportTab.tsx                      # refresh panel + category selector + import actions
│   │   ├── RefreshCategoriesButton.tsx             # action + refresh summary
│   │   ├── CategorySelector.tsx                    # search/section/mining/game filters + context
│   │   └── ImportItemsResult.tsx                   # item-import summary incl. per-category errors
│   ├── hooks/
│   │   ├── itemKeys.ts                             # query key factory
│   │   ├── useCategories.ts                        # list local categories
│   │   ├── useRefreshCategories.ts                 # mutation + invalidation
│   │   └── useImportItems.ts                       # mutation (single | all) + invalidation
│   ├── api/itemsApi.ts                             # apiFetch wrappers
│   ├── schemas/itemSchemas.ts                      # Zod: category list, refresh result, import result
│   └── __tests__/
│       ├── itemsImportTab.test.tsx
│       ├── categorySelector.test.tsx
│       └── importItems.test.tsx
└── lib/api/items.d.ts                              # generated from contracts/openapi.yaml
```

**Structure Decision**: Web application with the established Clean Architecture layering and
frontend feature folders. The feature is an additive slice of the existing admin **Data Import**
feature (006): two new Application use-case areas (`ItemCategories`, `Items`), two new repositories
and two new UEX typed clients, three new endpoints in a new `/api/admin/items` group, two new
`sc` tables via one migration, and a new **Items** tab (tab content + selector + hooks + schemas)
in `features/admin/`. The concurrency guarantee comes for free from the shared singleton
`IImportCoordinator`. Category refresh and item import are deliberately separate endpoints/handlers
so item import never touches the categories endpoint.

## Complexity Tracking

> No constitution violations — section intentionally empty.
