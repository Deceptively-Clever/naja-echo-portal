# Implementation Plan: Warehouse Ship Components Subpage

**Branch**: `012-warehouse-ship-components` | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/012-warehouse-ship-components/spec.md`

## Summary

Add a **Warehouse → Ship Components** sub-page that mirrors the existing **Warehouse → Items**
(Inventory) page (feature 011) but is scoped to inventory rows whose catalog item lives in the
**Systems** section. The table shows **Name, Type, Class, Size, Grade, Quantity, Owner, Location**
(no Section column), sorted by Name → Type → Size → Class → Grade ascending with Unknown values last.
Class, Size, and Grade are **derived** read-only attributes sourced from cached UEX item-attribute
data, not stored on the inventory row.

The new wrinkle versus 011 is the **derived component attributes** and their **lazy UEX fetch +
cache**. A two-table design backs this:

1. **`sc.item_attributes`** — raw cache of every attribute UEX returns from
   `items_attributes?id_item={uexItemId}` (Class, Size, Grade, Volume, Mass, …), one row per
   attribute per item.
2. **`sc.ship_component_attributes`** — typed projection (one row per item) holding `class` (text),
   `size` (int), `grade` (text), and `attributes_fetched_at`.

When a Quartermaster adds an inventory row from the Ship Components context for a Systems item whose
attributes are not yet cached, the add handler fetches attributes from UEX, stores all raw
attributes, and builds/refreshes the typed projection — **without blocking inventory creation** if
the fetch fails (Class/Size/Grade simply remain Unknown).

The dominant precedent is feature **011 (Warehouse Items)**: an owner-scoped `warehouse_inventory`
table, a `/api/warehouse` endpoint group (reads open to authenticated users, writes gated by the
existing `Quartermaster` policy that also admits `Admin`), SQL-projected list/filter queries, the
`AddOrIncrementAsync` race-guarded write, and the `apiFetch` + Zod + TanStack Query frontend
conventions with a data-driven nav. The existing `UexItemClient` (HttpClient against
`https://api.uexcorp.uk/2.0/`) is the precedent for the new UEX attribute client. **Existing
Inventory behaviour is unchanged** — Ship Components adds a parallel, scoped surface.

New backend HTTP behaviour (all under the existing `/api/warehouse` group):

- `GET /api/warehouse/ship-components` — filterable, multi-key-sorted Systems-only inventory list
  with derived Class/Size/Grade (any authenticated user).
- `GET /api/warehouse/ship-components/filters` — selectable Type/Class/Size/Grade/Owner/Location
  options derived from current Ship Component inventory only, including an `unknownClass` /
  `unknownSize` / `unknownGrade` indicator (any authenticated user).
- `GET /api/warehouse/ship-components/catalog/search` — catalog search restricted to **Systems**
  items for the add flow (Quartermaster).
- `POST /api/warehouse/ship-components` — add row (or increment) for a Systems item, triggering the
  lazy UEX attribute fetch/cache (Quartermaster).
- `PUT /api/warehouse/items/{id}/quantity` — **reused as-is** from 011 (Quartermaster).
- `DELETE /api/warehouse/items/{id}` — **reused as-is** from 011 (Quartermaster).

**Schema change IS required**: two new `sc`-schema tables via one EF Core migration. **API contract
changes ARE required**: four new endpoints in the existing `/api/warehouse` group. This is **not** a
UI-only feature.

## Technical Context

**Language/Version**: C# on .NET (`net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql` (snake_case naming),
ASP.NET Core Identity (`Quartermaster`/`Admin` roles + policy), cookie auth, Serilog, `HttpClient`
(typed UEX clients). Frontend — React 19 (Vite), React Router data APIs, Tailwind CSS 4, shadcn/ui
(Radix), Lucide, TanStack Query 5, React Hook Form + Zod, `openapi-typescript` for generated types.

**Storage**: PostgreSQL 16. Two new `sc`-schema tables (`sc.item_attributes`,
`sc.ship_component_attributes`) via one code-first EF migration. `sc.items` (catalog) and
`public.warehouse_inventory` (011) are the precedents; both are referenced by the new read query.

**Testing**: Backend — xUnit, FluentAssertions, in-memory provider + fakes for endpoint/handler tests
(per `WarehouseEndpoints`/`AddInventoryItemHandler` tests), Testcontainers (PostgreSQL) for repository
tests that exercise the new tables, the Systems-only filter, and the unique constraints. A fake
`IUexItemAttributeClient` exercises the lazy-fetch success, already-cached (no re-fetch), and
fetch-failure (non-blocking) paths. Frontend — Vitest + React Testing Library; the existing
`features/warehouse/__tests__` suite is the model.

**Target Platform**: Linux server (containerized API) + browser SPA.

**Performance Goals**: List renders within normal page-load time (SC-006); filter changes feel
instant. Ship Component inventory is expected to be in the low thousands of rows at most; a single
indexed SQL projection with server-side filtering suffices — no pagination in v1. Lazy UEX fetch
happens only on first add of an uncached item, off the read path.

**Constraints**:
- Ship Components shows only rows where `sc.items.section = 'Systems'` (FR-007); Section column is
  never shown (FR-008).
- Class/Size/Grade come exclusively from `sc.ship_component_attributes`; missing → "Unknown" in the
  UI (FR-012, FR-014). They are derived/read-only and never editable from this page (FR-024).
- Default sort: Name ↑ → Type ↑ → Size ↑ → Class ↑ → Grade ↑, with Unknown/null component
  attributes sorted **last** (FR-015).
- Filters: Name = case-insensitive partial; Type/Class/Size/Grade/Owner/Location = selectable;
  cross-field AND, within-field OR; empty ignored; options derived from current Ship Component
  inventory only; "Unknown" selectable for Class/Size/Grade only when such rows exist, without
  storing "Unknown" as a DB value (FR-016..FR-022).
- Lazy UEX fetch: only when raw attributes are absent for the item; never re-fetch if present;
  never block inventory creation on fetch failure; log failures via Serilog (UEX behaviour rules).
- Size is stored as **text** in `sc.item_attributes.value` but parsed to **int** for
  `sc.ship_component_attributes.size` (null on parse failure).
- Add from Ship Components restricts catalog selection to Systems items only and rejects non-Systems
  items server-side (FR-023, SC-008).
- Quantity/Owner/Location editing reuse 011 behaviour and endpoints verbatim (FR-025, FR-026).
- Auth identical to 011: reads require authentication; writes require `Quartermaster` (Admin
  admitted); anonymous redirected/401 (FR-003..FR-006).
- Out of scope: Materials page behaviour (nav entry only, placeholder), background/scheduled
  attribute refresh, editing derived attributes, attribute display beyond Class/Size/Grade.

### Verified existing facts (from codebase inspection)

- **Catalog item → UEX id**: `Item` (`sc.items`) has `int UexId` (the UEX item id) and `string?
  Section` / `string? Category`. `UexId` is the lookup key for `items_attributes?id_item=`. If a
  Systems item somehow has no usable UEX id, the fetch is skipped and attributes stay Unknown.
- **UEX client precedent**: `UexItemClient` (`Infrastructure/Items`, `IUexItemClient`) is a typed
  `HttpClient` registered via `AddHttpClient<…>` with base URL `UexVehicleClient:BaseUrl`
  (`https://api.uexcorp.uk/2.0/`), parses the `{ "data": [...] }` envelope, returns
  `IReadOnlyList<JsonDocument>`. The new `IUexItemAttributeClient` / `UexItemAttributeClient`
  mirrors it exactly with `items_attributes?id_item={uexItemId}`.
- **Inventory precedent**: `WarehouseInventoryEntry` (`public.warehouse_inventory`, unique
  `item_id+owner_user_id+location`), `WarehouseInventoryRepository` (SQL-projected list/filters,
  `AddOrIncrementAsync` race guard), `AddInventoryItemHandler` (validates item Active + owner exists,
  trims location, qty ≥ 1). Ship Components reuses the entity, table, and the
  quantity/remove handlers + endpoints unchanged.
- **Auth & caller identity**: `/api/warehouse` group calls `.RequireAuthorization()`; writes add
  `.RequireAuthorization(AuthorizationPolicies.Quartermaster)` (= role `Quartermaster` OR `Admin`).
  Handlers resolve caller via `ClaimTypes.NameIdentifier`. New endpoints follow the same shape in
  `WarehouseEndpoints.cs`.
- **EF config conventions**: `sc`-schema entities configured via `IEntityTypeConfiguration<T>`
  (`ItemConfiguration` is the template: `ToTable(..., schema: "sc")`, explicit snake_case column
  names, `jsonb` for raw, indexes). New configs registered in `AppDbContext.OnModelCreating`; new
  `DbSet`s added there.
- **DI**: handlers/repositories/clients registered explicitly in
  `Infrastructure/DependencyInjection.cs` (`AddScoped` / `AddHttpClient`). New ones register there.
- **Frontend nav**: `features/dashboard/navigation/navItems.ts` is the single data-driven source;
  the **Warehouse** group currently has only **Items**. Add **Ship Components** and **Materials**
  (placeholder) to the group.
- **Frontend routing**: `AppRouter.tsx` already redirects `/warehouse` → `/warehouse/items` inside
  `ProtectedRoute` + `DashboardLayout`. Add `/warehouse/ship-components` (and a `/warehouse/materials`
  placeholder route). Write controls are conditionally rendered via the existing
  `useIsQuartermaster` hook.
- **Frontend feature shape**: `features/warehouse/` (api/ hooks/ schemas/ components/ pages/
  __tests__) with `apiFetch`, per-feature query-key factories, and Zod schemas. Ship Components adds
  sibling files in the **same** `features/warehouse/` feature folder (it is the same feature area),
  reusing `AddInventoryDialog`, `EditQuantityControl`, `RemoveInventoryButton`, and `useIsQuartermaster`.
- **Type generation**: each feature contract has a `gen:api:*` script in `frontend/package.json`
  emitting `src/lib/api/*.d.ts`. Add `gen:api:ship-components` for this contract.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. API-Contract-First** — Adds new backend HTTP behaviour (four new endpoints; two reused).
  **API contract changes ARE required.** `contracts/openapi.yaml` is authored in Phase 1 before
  implementation; frontend types regenerate via a new `gen:api:ship-components` script. ✅
- **II. Test-First / TDD** — Failing tests first. Application unit tests: Systems-only scoping;
  derived Class/Size/Grade projection incl. Unknown handling; Size text→int parse (incl. parse
  failure → null); multi-key sort with Unknown-last; filter AND/OR + explicit Unknown filtering;
  add restricted to Systems items (non-Systems rejected); lazy fetch only when uncached; no re-fetch
  when cached; fetch failure does not block create + leaves attributes null. Repository integration
  tests (Testcontainers): table/unique-constraint enforcement on `sc.item_attributes` and
  `sc.ship_component_attributes`; upsert/refresh of projection; Systems-only list query. Endpoint
  tests (in-memory + fakes): reads allowed for any authenticated user; writes 403 for
  non-Quartermaster/non-Admin; Admin allowed; 401/redirect anonymous; non-Systems add rejected.
  Frontend: table renders 8 columns (no Section) + Unknown cells; empty vs no-results states;
  filters incl. Unknown option visibility + clear/reset; write controls hidden for non-QM; add flow
  restricted to Systems items. ✅
- **III. Frontend/Backend Separation** — Frontend consumes types generated from the contract; no
  direct DB access; no server HTML. Server is authoritative for Systems scoping, derivation,
  sort/Unknown semantics, filter option derivation, lazy fetch, and role enforcement. ✅
- **IV. Simplicity / YAGNI** — Reuses the 011 inventory entity, table, write handlers/endpoints
  (quantity/remove) and the `UexItemClient` HttpClient pattern verbatim. Only genuinely new data
  (raw attribute cache + typed projection) gets new tables. The Ship Components feature shares the
  existing `features/warehouse/` frontend folder and reuses the existing dialogs/controls — no new
  modal pattern. No background refresh, no attribute-display expansion, no Materials behaviour, no
  pagination — all out of scope. Authorization reuses the existing `Quartermaster` policy unchanged. ✅
- **V. Observability** — Handlers/clients emit structured Serilog logs with correlation ID (caller
  id, item id, uex id, owner, action, fetched attribute count, parse outcomes, fetch
  failure/skip reason — never tokens). Per the existing warehouse logging shape. ✅
- **VI. Modular Monolith + Clean Architecture** — New use cases under
  `Application/Features/Warehouse/ShipComponents/*`; new domain entities in
  `Domain/Warehouse/` (`ItemAttribute`, `ShipComponentAttributes`); new abstractions
  (`IUexItemAttributeClient`, `IItemAttributeRepository`/`IShipComponentAttributesRepository` or an
  extension of the warehouse repository) in `Application/Abstractions`; infrastructure repository +
  UEX client in `Infrastructure/Warehouse` and `Infrastructure/Items`; endpoints in the existing
  `API/Features/Warehouse/`. Frontend logic stays in `features/warehouse/` (thin route view; logic
  in hooks/schemas/components); nav is data-driven via `navItems.ts`. Dependencies point inward. ✅

No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/012-warehouse-ship-components/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output (4 new endpoints under /api/warehouse)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Warehouse/
│       ├── ItemAttribute.cs                         # raw UEX attribute cache row (sc.item_attributes)
│       └── ShipComponentAttributes.cs               # typed projection (sc.ship_component_attributes)
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   ├── IUexItemAttributeClient.cs               # FetchItemAttributesAsync(uexItemId)
│   │   └── IShipComponentRepository.cs              # list/filters/catalog-search(Systems) + attribute cache read/upsert
│   └── Features/
│       └── Warehouse/
│           └── ShipComponents/
│               ├── GetShipComponents/               # Systems-only list query + row DTO (derived Class/Size/Grade)
│               ├── GetShipComponentFilters/          # selectable Type/Class/Size/Grade/Owner/Location + Unknown flags
│               ├── SearchSystemsCatalog/             # catalog search restricted to Systems items
│               └── AddShipComponent/                 # add command + handler (Systems guard + lazy attribute fetch/cache)
├── NajaEcho.Infrastructure/
│   ├── Items/
│   │   └── UexItemAttributeClient.cs                # GET items_attributes?id_item={uexItemId}
│   ├── Warehouse/
│   │   └── ShipComponentRepository.cs               # Systems-only SQL projection, filter options, attribute upsert
│   └── Persistence/
│       ├── AppDbContext.cs                          # + DbSet<ItemAttribute>, DbSet<ShipComponentAttributes>
│       ├── Configurations/
│       │   ├── ItemAttributeConfiguration.cs        # sc.item_attributes + unique (item_id, uex_category_attribute_id)
│       │   └── ShipComponentAttributesConfiguration.cs  # sc.ship_component_attributes PK/unique item_id
│       └── Migrations/
│           └── <timestamp>_AddShipComponentAttributes.cs
└── NajaEcho.Api/
    └── Features/Warehouse/
        ├── WarehouseEndpoints.cs                    # + ship-components list/filters/catalog/add (reuse qty/remove)
        └── Contracts/
            └── ShipComponentDtos.cs                 # request/response records

backend/tests/
├── NajaEcho.Application.Tests/Features/Warehouse/ShipComponents/  # projection, sort, filters, Systems guard, lazy fetch
├── NajaEcho.Infrastructure.Tests/Warehouse/                       # attribute upsert, unique constraints, Systems list
└── NajaEcho.Api.Tests/Features/Warehouse/                         # ship-components endpoint auth + behaviour

frontend/src/
├── features/warehouse/
│   ├── api/shipComponentsApi.ts                     # apiFetch wrappers for the 4 new endpoints
│   ├── hooks/
│   │   ├── useShipComponents.ts / useShipComponentFilters.ts
│   │   ├── useSystemsCatalogSearch.ts               # add-flow search (Systems only)
│   │   ├── useAddShipComponent.ts                   # add mutation (reuses change/remove from 011)
│   │   └── warehouseQueryKeys.ts                    # + ship-component keys
│   ├── schemas/shipComponentSchemas.ts             # Zod: row, filters, catalog, add form
│   ├── components/
│   │   ├── ShipComponentsTable.tsx / ShipComponentsFilters.tsx
│   │   └── (reuse AddInventoryDialog / EditQuantityControl / RemoveInventoryButton with Systems scope + derived fields)
│   ├── pages/ShipComponentsView.tsx                 # thin route view
│   └── __tests__/                                    # table, filters, add-scope, empty states
├── features/dashboard/navigation/navItems.ts        # + Ship Components + Materials (placeholder) in Warehouse group
└── routes/AppRouter.tsx                              # + /warehouse/ship-components (+ /warehouse/materials placeholder)
```

**Structure Decision**: Web application with the established Clean Architecture layering and frontend
feature folders. Ship Components is a scoped extension of the existing **Warehouse** vertical slice
(feature 011): it reuses the `warehouse_inventory` table and the quantity/remove write path
verbatim, adds two new `sc`-schema attribute tables via one migration, adds a UEX attribute client
modelled on `UexItemClient`, adds Systems-scoped read/add use cases under
`Features/Warehouse/ShipComponents/`, extends the `/api/warehouse` endpoint group with four new
endpoints, and adds Ship Components UI inside the same `features/warehouse/` frontend folder surfaced
through the data-driven **Warehouse → Ship Components** nav entry (Materials added as a placeholder).

## Complexity Tracking

> No constitution violations — section intentionally empty.
