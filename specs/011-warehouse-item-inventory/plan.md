# Implementation Plan: Warehouse Item Inventory

**Branch**: `011-warehouse-item-inventory` | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-warehouse-item-inventory/spec.md`

## Summary

Add a **Warehouse** navigation section with an **Items** sub-page (mirroring the existing
**Hangar → My Hangar / Org Hangar** split; Minerals will join the section later). The Items page lets
**any authenticated member** view a filterable inventory table (Name, Type, Subtype, Quantity, Owner,
Location, sorted by Name) and lets a **Quartermaster** (or **Admin**) add inventory rows from the
existing item catalog, change a row's quantity, and remove rows.

The dominant precedent is the **Hangar** feature (006): an owner-scoped relationship entity in the
`public` schema, a `RequireAuthorization()` endpoint group that resolves the caller via
`ClaimTypes.NameIdentifier`, catalog search over imported game data, an add flow guarded by a DB
unique constraint against races, and the `apiFetch` + Zod + TanStack Query frontend conventions with
a data-driven nav. The warehouse reuses these shapes almost verbatim. The single new wrinkle versus
Hangar is **role-gated writes**: a new `Quartermaster` role + authorization policy (Admin inherits),
seeded at startup alongside the existing `Admin` role.

New backend HTTP behaviour:

- `GET /api/warehouse/items` — filterable, sorted inventory list (any authenticated user).
- `GET /api/warehouse/items/filters` — distinct Type/Subtype (from item-category data) and Owner
  options for the filter controls (any authenticated user).
- `GET /api/warehouse/catalog/search` — item-catalog search by name for the add flow (Quartermaster).
- `POST /api/warehouse/items` — add row, or increment quantity when Item + Owner + Location already
  exists (Quartermaster).
- `PUT /api/warehouse/items/{id}/quantity` — replace a row's quantity (Quartermaster).
- `DELETE /api/warehouse/items/{id}` — remove a row; the catalog Item is untouched (Quartermaster).

**Schema change IS required**: one new `public.warehouse_inventory` table (one EF Core migration)
with a unique index on (`item_id`, `owner_user_id`, `location`). **API contract changes ARE
required**: six endpoints in a new `/api/warehouse` group. This is **not** a UI-only feature.

## Technical Context

**Language/Version**: C# on .NET (`net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql` (snake_case naming),
ASP.NET Core Identity (`UserManager`/`RoleManager<IdentityRole<Guid>>`), cookie auth, Serilog.
Frontend — React 19 (Vite), React Router data APIs, Tailwind CSS 4, shadcn/ui (Radix), Lucide,
TanStack Query 5, React Hook Form + Zod, `openapi-typescript` for generated types.

**Storage**: PostgreSQL 16. One new `public.warehouse_inventory` table (code-first EF migration).
`public.hangar_entries` is the precedent for an owner-scoped relationship table in the public schema
referencing imported game data in the `sc` schema.

**Testing**: Backend — xUnit, FluentAssertions, in-memory provider + fakes for endpoint tests (per
`HangarEndpoints` / `ShipAdminEndpointsTests`), Testcontainers (PostgreSQL) for repository tests that
exercise the unique constraint and concurrent increment (the in-memory provider does not enforce the
unique index). Frontend — Vitest + React Testing Library; the Hangar `__tests__` suite is the model.

**Target Platform**: Linux server (containerized API) + browser SPA.

**Performance Goals**: Inventory list renders within 2 s under normal load (SC-001); filter changes
reflect within 1 s (SC-002). Inventory is expected to be in the low thousands of rows at most; a
single indexed query with server-side filtering suffices — no pagination required in v1, but the list
query is written to allow adding it without reshaping the contract.

**Constraints**: Inventory identity is (`item_id`, `owner_user_id`, trimmed `location`). Location is
required, trimmed, free text; uniqueness is exact-match on the trimmed value (case-sensitive — only
trimming is specified by FR-014). Quantity is an integer ≥ 1 (FR-020–FR-022); add **increments**,
edit **replaces** (FR-017/FR-023). Owner is a registered portal user; Owner defaults to the caller on
add (FR-011). Writes require `Quartermaster` or `Admin` (FR-008/FR-027); reads require only
authentication (FR-001). Removing a row never touches the catalog Item (FR-025). Concurrent adds for
the same key produce exactly one row via the DB unique constraint + transactional retry (edge case /
FR-017). Type = Item.Section, Subtype = Item.Category (FR-007). Out of scope: minerals, role-mgmt UI,
audit/history, org-owned inventory, structured locations.

### Verified existing facts (from codebase inspection)

- **Auth & caller identity**: Endpoint groups call `.RequireAuthorization()`; handlers resolve the
  caller with `user.FindFirstValue(ClaimTypes.NameIdentifier)` parsed to `Guid` (see
  `HangarEndpoints.TryGetUserId`). Unauthenticated browser requests are redirected to sign-in by the
  cookie scheme, satisfying FR-001/SC-006 for free.
- **Roles**: `AddRoles<IdentityRole<Guid>>()` is configured in
  `Infrastructure/DependencyInjection.cs`. `AdminRoleSeeder` seeds the `Admin` role at startup
  (`Program.cs` ~217-222). Roles are written into claims as `ClaimTypes.Role` on sign-in
  (`Program.cs` ~173-185) and surfaced to the frontend via `CurrentUserResponse.Roles`
  (`roles: string[]` in `sessionStateSchema.ts`). **The admin policy** is the only policy today:
  `AuthorizationPolicies.Admin = policy.RequireRole("Admin")` (`AuthorizationPolicies.cs`), applied
  via `app.MapGroup(...).RequireAuthorization(AuthorizationPolicies.Admin)`.
- **Owner-scoped entity precedent**: `HangarEntry` (`public` schema, `Guid Id`, `UserId`, `ShipId`,
  `AddedAt`) with a unique index enforced in `HangarEntryConfiguration`; `HangarRepository.AddAsync`
  catches the unique-constraint violation and rethrows a domain exception — the template for the
  warehouse add/increment race guard.
- **Catalog data**: `Item` (`sc` schema) exposes `Name`, `Section` (→ Type), `Category` (→ Subtype),
  and `Status` (`ItemStatus.Active`). `ItemRepository` already filters by category. The catalog
  search mirrors `SearchCatalogShipsHandler` but over `Item`.
- **Filter option source**: `ItemCategory` (`Section`, `Name`) is the FR-007 source for Type/Subtype
  options; `GetCategoriesHandler` is the existing read precedent.
- **Endpoint registration**: endpoints are mapped in `Program.cs` (~252-256:
  `MapAuthEndpoints`/`MapShipAdminEndpoints`/`MapItemAdminEndpoints`/`MapCommodityAdminEndpoints`/
  `MapHangarEndpoints`). The new `MapWarehouseEndpoints` is added here.
- **DI**: handlers, repositories, and clients are registered explicitly in
  `Infrastructure/DependencyInjection.cs` (`AddScoped`). New handlers/repository register there.
- **Frontend nav**: `features/dashboard/navigation/navItems.ts` is the single data-driven nav source;
  `NavItem` supports `label`, `path`, `icon`, `group`, `end`, and `access`. `DashboardNav` groups by
  `group` and filters `access === 'admin'` against `roles`. The **Hangar** group (`My Hangar`,
  `Org Hangar`) is the exact precedent for the new **Warehouse** group.
- **Frontend routing/role gating**: `AppRouter.tsx` redirects `/hangar` → `/hangar/mine` and renders
  views in `DashboardLayout`; `AdminRoute` gates admin routes on `roles.includes('Admin')`. Warehouse
  read pages need only `ProtectedRoute`; write controls are conditionally rendered on
  `roles.includes('Quartermaster') || roles.includes('Admin')`.
- **Frontend feature shape**: `features/hangar/` (api/ hooks/ schemas/ components/ pages/ __tests__)
  with `apiFetch`, per-feature query-key factories, and Zod schemas is the template for
  `features/warehouse/`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. API-Contract-First** — Adds new backend HTTP behaviour (six endpoints). **API contract changes
  ARE required.** `contracts/openapi.yaml` is authored in Phase 1 before implementation; frontend
  types regenerate from it via a new `gen:api:warehouse` script (per the `openapi-typescript`
  precedent). ✅
- **II. Test-First / TDD** — Failing tests first. Application unit tests (add creates row with
  defaults; add increments on duplicate key; edit replaces; quantity ≥ 1 / integer validation;
  location trim + empty rejection; filter AND logic incl. case-insensitive name/location and exact
  Type/Subtype/Owner; sort by Name; remove deletes row only). Repository integration tests
  (Testcontainers): unique-constraint enforcement, concurrent-add yields one row + correct quantity,
  remove leaves Item intact. Endpoint tests (in-memory + fakes, per Hangar): reads allowed for any
  authenticated user; writes 403 for non-Quartermaster/non-Admin; Admin allowed without explicit
  Quartermaster role; 401/redirect for anonymous. Frontend: table renders + empty/no-results states;
  filters; write controls hidden for non-QM; add/edit/remove flows; remembered Owner/Location. ✅
- **III. Frontend/Backend Separation** — Frontend consumes types generated from the contract; no
  direct DB access; no server HTML; server is authoritative for increment-vs-insert, trimming,
  validation, and role enforcement. ✅
- **IV. Simplicity / YAGNI** — Reuses the Hangar owner-scoped entity, unique-constraint race guard,
  catalog-search, and auth-group patterns verbatim. One new table only (genuinely new data). New
  `Quartermaster` policy is one line added to `AuthorizationPolicies`; the seeder is generalized to a
  role list rather than introducing a new mechanism. No pagination, no audit table, no background
  jobs, no structured-location registry — all explicitly out of scope. Filter options reuse existing
  `ItemCategory` data. ✅
- **V. Observability** — Handlers emit structured Serilog logs with correlation ID (caller id, item
  id, owner, action, resulting quantity, row id — never tokens). Per the Hangar logging shape. ✅
- **VI. Modular Monolith + Clean Architecture** — New use cases under
  `Application/Features/Warehouse/*`; new domain entity in `Domain/Warehouse/`; repository in
  `Infrastructure/Warehouse/`; endpoints in `API/Features/Warehouse/`; role policy in `API/
  Authorization/`. Frontend logic in `features/warehouse/` (thin route views; logic in hooks/schemas/
  components). The Warehouse nav group is data-driven via `navItems.ts`; a small shared
  `useHasRole`/`isQuartermaster` helper lives with the warehouse feature (single consumer → not
  promoted to `components/shared`). ✅

No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/011-warehouse-item-inventory/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output (6 endpoints under /api/warehouse)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Warehouse/
│       └── WarehouseInventoryEntry.cs              # Id, ItemId, OwnerUserId, Location, Quantity, timestamps
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   └── IWarehouseInventoryRepository.cs        # list/filters/add-or-increment/update-qty/remove
│   └── Features/
│       └── Warehouse/
│           ├── GetInventory/                       # filtered, sorted list query + row DTO
│           ├── GetInventoryFilters/                # distinct types/subtypes/owners
│           ├── SearchCatalogItems/                 # item-catalog search for add flow
│           ├── AddInventoryItem/                   # add-or-increment command + handler + validator + exceptions
│           ├── ChangeInventoryQuantity/            # replace-quantity command + handler + validator
│           └── RemoveInventoryItem/                # delete command + handler
├── NajaEcho.Infrastructure/
│   ├── Warehouse/
│   │   └── WarehouseInventoryRepository.cs         # queries + transactional add-or-increment race guard
│   ├── Identity/
│   │   └── RoleSeeder.cs                           # generalized from AdminRoleSeeder: seeds Admin + Quartermaster
│   └── Persistence/
│       ├── AppDbContext.cs                         # + DbSet<WarehouseInventoryEntry>
│       ├── Configurations/
│       │   └── WarehouseInventoryEntryConfiguration.cs  # table + unique (item_id, owner_user_id, location)
│       └── Migrations/
│           └── <timestamp>_AddWarehouseInventory.cs
└── NajaEcho.Api/
    ├── Authorization/
    │   └── AuthorizationPolicies.cs                # + Quartermaster = RequireRole("Quartermaster","Admin")
    └── Features/Warehouse/
        ├── WarehouseEndpoints.cs                   # /api/warehouse group (reads open, writes QM-gated)
        └── Contracts/
            └── WarehouseDtos.cs                    # request/response records

backend/tests/
├── NajaEcho.Application.Tests/Features/Warehouse/  # handler/validator unit tests
├── NajaEcho.Infrastructure.Tests/Warehouse/        # repository: unique constraint, concurrent add, remove
└── NajaEcho.Api.Tests/Features/Warehouse/          # endpoint auth + role gating + behaviour

frontend/src/
├── features/warehouse/
│   ├── api/warehouseApi.ts                          # apiFetch wrappers
│   ├── hooks/
│   │   ├── warehouseKeys.ts                         # query key factory
│   │   ├── useInventory.ts / useInventoryFilters.ts # queries
│   │   ├── useCatalogItemSearch.ts                  # add-flow search
│   │   ├── useAddInventoryItem.ts / useChangeQuantity.ts / useRemoveInventoryItem.ts  # mutations
│   │   └── useIsQuartermaster.ts                    # roles.includes('Quartermaster'|'Admin')
│   ├── schemas/                                     # Zod: inventory row, filters, catalog item, forms
│   ├── components/
│   │   ├── InventoryTable.tsx / InventoryFilters.tsx
│   │   ├── AddInventoryDialog.tsx (item search + qty/owner/location)
│   │   ├── EditQuantityControl.tsx / RemoveInventoryButton.tsx
│   │   └── (empty/no-results states)
│   ├── pages/WarehouseItemsView.tsx                 # thin route view
│   └── __tests__/
├── features/dashboard/navigation/navItems.ts        # + Warehouse group (Items)
└── routes/AppRouter.tsx                              # + /warehouse → /warehouse/items + /warehouse/items
```

**Structure Decision**: Web application with the established Clean Architecture layering and frontend
feature folders. The feature is a new vertical slice that closely follows the **Hangar** precedent:
one new owner-scoped `public`-schema table via one migration, one repository, a set of
Application use cases under `Features/Warehouse/`, a new `/api/warehouse` endpoint group (reads open
to authenticated users, writes gated by a new `Quartermaster` policy that also admits `Admin`), and a
new `features/warehouse/` frontend feature surfaced through a data-driven **Warehouse → Items** nav
group. The `Quartermaster` role is seeded at startup by generalizing the existing role seeder.

## Complexity Tracking

> No constitution violations — section intentionally empty.
