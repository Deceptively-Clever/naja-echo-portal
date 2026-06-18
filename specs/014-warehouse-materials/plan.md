# Implementation Plan: Warehouse Materials Subpage

**Branch**: `014-warehouse-materials` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/014-warehouse-materials/spec.md`

## Summary

Add a **Warehouse → Materials** sub-page — the third Warehouse surface alongside **Items** (011)
and **Ship Components** (012). It tracks crafting-material inventory whose catalog source is the
existing **`sc.commodities`** table (not `sc.items`). The table shows **Material, Owner, Location,
Quantity, Quality**, sorted by Material name ↑ → Quality ↓ → Owner name ↑ → Location ↑.

The two new wrinkles versus 011/012 are:

1. **Decimal quantity** — material quantity is a `decimal(18,3)` greater than `0.000` (rounded
   half-up to 3 places), where Items/Ship Components use integer quantity ≥ 1.
2. **Quality is part of the row-uniqueness key** — a material row is unique by **(Material, Owner,
   Location, Quality)**. In 011/013, Quality exists on `warehouse_inventory` but is **not** part of
   its unique key. Because the keys differ (commodity vs item, decimal qty, quality-in-key),
   Materials gets its **own table** rather than reusing `warehouse_inventory`.

The dominant precedent is feature **011/012**: an owner-scoped inventory table, an endpoint group
under `/api/warehouse`, reads open to any authenticated user, writes gated by the existing
`Quartermaster` policy (which also admits `Admin`), SQL-projected list/filter queries, an
`AddOrIncrementAsync` race-guarded upsert, and the `apiFetch` + Zod + TanStack Query frontend
conventions with a data-driven nav. Quality is sourced from the same 1–1000 integer rule already
established in 013. **Existing Items and Ship Components behaviour is unchanged** — Materials adds a
parallel, scoped surface.

New backend HTTP behaviour (all under the existing `/api/warehouse` group):

- `GET  /api/warehouse/materials` — filterable, multi-key-sorted material inventory list (any
  authenticated user).
- `GET  /api/warehouse/materials/filters` — selectable Owner/Location options plus the known
  Location list, derived from current material inventory only (any authenticated user).
- `GET  /api/warehouse/materials/catalog/search` — commodity search restricted to `sc.commodities`
  for the add flow (Quartermaster).
- `POST /api/warehouse/materials` — add row (or increment an existing matching row) for a commodity
  (Quartermaster).
- `PUT  /api/warehouse/materials/{id}/quantity` — absolute set of a row's quantity, rejecting
  ≤ 0.00 (Quartermaster).
- `DELETE /api/warehouse/materials/{id}` — remove a row (Quartermaster).

**Schema change IS required**: one new `warehouse_material_inventory` table via one EF Core
migration. **API contract changes ARE required**: six new endpoints in the existing `/api/warehouse`
group. This is **not** a UI-only feature, so the "No API contract changes required" exemption does
not apply.

## Technical Context

**Language/Version**: C# on .NET (`net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql` (snake_case
naming), ASP.NET Core Identity (`Quartermaster`/`Admin` roles + policy), cookie auth, Serilog.
Frontend — React 19 (Vite), React Router data APIs, Tailwind CSS 4, shadcn/ui (Radix), Lucide,
TanStack Query 5, React Hook Form + Zod, `openapi-typescript` for generated types. **One new
shadcn/ui primitive — `slider` (Radix Slider)** — is needed for the dual-ended Quality range filter
and is not yet generated into `components/ui/`.

**Storage**: PostgreSQL 16. One new table `warehouse_material_inventory` (default `public` schema,
matching `warehouse_inventory`) via one code-first EF migration. `sc.commodities` (catalog) and
`AspNetUsers` (owner) are referenced by the new read query. `sc.commodities` is read-only here.

**Testing**: Backend — xUnit, FluentAssertions, in-memory provider + fakes for endpoint/handler
tests (per `WarehouseEndpoints`/`AddInventoryItemHandler` tests), Testcontainers (PostgreSQL) for
repository tests exercising the new table, the upsert-on-conflict key, the decimal-quantity check
constraint, and the quality range. Frontend — Vitest + React Testing Library; the existing
`features/warehouse/__tests__` suite is the model.

**Target Platform**: Linux server (containerized API) + browser SPA.

**Performance Goals**: List renders within normal page-load time (SC-002); filter changes feel
instant. Material inventory is expected to be in the low thousands of rows at most; a single indexed
SQL projection with server-side filtering suffices — no pagination in v1.

**Constraints**:
- Material is sourced exclusively from `sc.commodities`; no custom materials (FR-007, FR-008).
- Quantity is `decimal`, displayed at exactly 3 places, must be `> 0.000` on add and adjust; excess
  precision is rounded half-up to 3 places before validation/storage; a value rounding to `0.000` is
  rejected (FR-010, FR-017, FR-018; spec edge case).
- Quality is an integer `1..1000`, defaults to `500`, set only at creation, never editable
  afterward — correction is delete-and-re-add (FR-020..FR-023).
- Row uniqueness key = **(Material, Owner, Location, Quality)**; a matching add increments quantity,
  any difference creates a new row (FR-024..FR-026).
- Quantity is never a soft-delete; removal is row deletion only (FR-019, FR-033).
- Default sort: Material name ↑ → Quality ↓ → Owner name ↑ → Location ↑ (FR-042).
- Filters: Material = case-insensitive partial over commodity **name and code**; Owner =
  single-select; Location = single-select from known locations; Quality = inclusive `[min,max]`
  range via dual-ended slider defaulting to `1–1000`; cross-field AND; empty ignored (FR-035..FR-041).
- Location is free text with suggestions drawn from locations currently used across material
  inventory (FR-015, FR-016); the Location filter draws from the same runtime set.
- Auth identical to 011/012: reads require authentication; writes require `Quartermaster` (Admin
  admitted); anonymous redirected/401 (FR-003..FR-006).
- No audit history in v1 (FR-034). Out of scope: reservation/allocation/crafting consumption,
  transaction history, commodity import/custom creation, Quartermaster role assignment, zero-quantity
  rows.

### Verified existing facts (from codebase inspection)

- **Commodity catalog**: `Commodity` (`sc.commodities`) exposes `Guid Id`, `string Name`, and
  `string? Code` — name+code are the searchable/display fields the spec calls for. `Status` /
  `SoftDeletedAt` exist; the catalog search should exclude soft-deleted commodities.
- **Inventory precedent**: `WarehouseInventoryEntry` (`public.warehouse_inventory`, unique
  `item_id+owner_user_id+location`, `int Quantity`, `int Quality = 500`, check constraints
  `quantity >= 1` and `quality between 1 and 1000`). `WarehouseInventoryRepository` does the
  SQL-projected list/filters, an `INSERT … ON CONFLICT … DO UPDATE` upsert returning `(xmax = 0) AS
  is_new`, an `UpdateQuantityAsync`, and a `RemoveAsync`. The Materials repository mirrors this with
  `commodity_id`, `decimal` quantity, the conflict target **`(commodity_id, owner_user_id, location,
  quality)`**, and a `quantity > 0` (not `>= 1`) check constraint.
- **Endpoints**: `WarehouseEndpoints.MapWarehouseEndpoints` maps the `/api/warehouse` group with
  reads open and writes `.RequireAuthorization(AuthorizationPolicies.Quartermaster)`. Materials adds
  six sibling routes in the same file/group, each logging via Serilog with the caller id and mapping
  domain exceptions to RFC-7807 `Results.Problem` responses, exactly as the Items/Ship-Components
  handlers do.
- **Frontend scaffolding already present**: `navItems.ts` already contains
  `{ label: 'Materials', path: '/warehouse/materials', group: 'Warehouse' }`, and `AppRouter.tsx`
  has a placeholder route `<Route path="/warehouse/materials" … >Materials coming soon.</…>`. The
  placeholder is replaced by the real `MaterialsView` page; the nav item is reused as-is.
- **Frontend feature precedent**: `features/warehouse/` already owns `pages/WarehouseItemsView.tsx`
  and `pages/ShipComponentsView.tsx` plus matching `components/`, `hooks/`, `api/`, `schemas/`, and
  `__tests__/`. Materials extends this same feature folder; no new top-level feature is created.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | PASS | Six new endpoints are defined in `contracts/openapi.yaml` before implementation. This is not a UI-only feature, so the contract-exemption clause does not apply and is not invoked. |
| II. Test-First / TDD | PASS | Plan mandates failing tests first: Application handler unit tests (add/increment, decimal rounding, quality range, quantity > 0, owner/commodity existence), at least one Testcontainers integration test through the real table (upsert-on-conflict-with-quality, check constraints), and frontend component/hook tests mirroring the existing warehouse suite. |
| III. Frontend/Backend Separation | PASS | Frontend consumes only `/api/warehouse/materials*`; request/response types are generated from the OpenAPI contract via `openapi-typescript`. No server-rendered HTML, no DB access from the SPA. |
| IV. Simplicity / YAGNI | PASS | Reuses 011/012 patterns wholesale. A separate table is justified by three concrete differences (commodity FK, decimal quantity, quality-in-unique-key), not speculation. No audit/history, reservation, or pagination added (all explicitly out of scope). Only one new UI primitive (`slider`) — required by FR-038. |
| V. Observability | PASS | Each endpoint emits structured Serilog logs with the caller id and operation outcome, matching existing warehouse endpoints. No new sensitive data is introduced. |
| VI. Modular Monolith + Clean Architecture | PASS | Domain entity in `NajaEcho.Domain/Warehouse`; use cases in `NajaEcho.Application/Features/Warehouse/Materials/<UseCase>/`; EF config, migration, and repository in `NajaEcho.Infrastructure`; endpoints in `NajaEcho.Api`. Dependencies point inward only. Frontend logic lives in feature-owned hooks/schemas; the route component stays thin; the `slider` primitive stays application-agnostic in `components/ui/`. |

**Result**: PASS — no violations. Complexity Tracking table not required.

## Project Structure

### Documentation (this feature)

```text
specs/014-warehouse-materials/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output — the six new endpoints
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Warehouse/
│       └── WarehouseMaterialEntry.cs                 # new entity (commodity_id, owner, location, decimal qty, quality)
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   └── IMaterialInventoryRepository.cs            # new port
│   └── Features/Warehouse/Materials/
│       ├── GetMaterials/                              # query + handler + MaterialRowDto
│       ├── GetMaterialFilters/                        # query + handler + MaterialFiltersDto + OwnerOption
│       ├── SearchCommodities/                         # query + handler + CommodityResultDto
│       ├── AddMaterial/                               # command + handler + CommodityNotFound/OwnerNotFound exceptions
│       ├── ChangeMaterialQuantity/                    # command + handler + MaterialRowNotFoundException
│       └── RemoveMaterial/                            # command + handler
├── NajaEcho.Infrastructure/
│   ├── Warehouse/
│   │   └── MaterialInventoryRepository.cs             # SQL-projected reads + upsert + update + remove
│   └── Persistence/
│       ├── Configurations/
│       │   └── WarehouseMaterialEntryConfiguration.cs # table, decimal precision, check constraints, unique index
│       └── Migrations/
│           └── <ts>_AddWarehouseMaterialInventory.cs  # one new migration
└── NajaEcho.Api/
    └── Features/Warehouse/
        ├── WarehouseEndpoints.cs                      # +6 material routes (edited)
        └── Contracts/
            └── MaterialDtos.cs                        # request/response records for the material endpoints

backend/tests/
├── NajaEcho.Application.Tests/Features/Warehouse/Materials/   # handler unit tests
└── NajaEcho.Api.Tests/Features/Warehouse/MaterialsEndpointTests.cs
   (+ Infrastructure/repository Testcontainers test for the new table)

frontend/src/
├── components/ui/
│   └── slider.tsx                                     # new shadcn primitive (Radix Slider)
├── features/warehouse/
│   ├── api/materialsApi.ts                            # apiFetch wrappers over generated types
│   ├── hooks/
│   │   ├── useMaterials.ts
│   │   ├── useMaterialFilters.ts
│   │   ├── useCommoditySearch.ts
│   │   ├── useAddMaterial.ts
│   │   ├── useChangeMaterialQuantity.ts
│   │   ├── useRemoveMaterial.ts
│   │   └── warehouseQueryKeys.ts                      # extended with material keys (edited)
│   ├── schemas/materialSchemas.ts                     # Zod add/adjust/filter schemas (decimal qty, quality)
│   ├── components/
│   │   ├── MaterialsTable.tsx
│   │   ├── MaterialsFilters.tsx                       # material search, owner, location, quality dual-slider
│   │   ├── AddMaterialDialog.tsx
│   │   ├── EditMaterialQuantityControl.tsx
│   │   └── RemoveMaterialButton.tsx
│   ├── pages/MaterialsView.tsx                        # thin route page
│   └── __tests__/                                     # Materials* component/hook tests
└── routes/AppRouter.tsx                               # placeholder route → <MaterialsView /> (edited)
```

**Structure Decision**: Extend the existing `features/warehouse` frontend feature and the existing
`/api/warehouse` backend group rather than creating new top-level modules. Backend layering follows
the four-project Clean Architecture split already in place. The only genuinely new persistence
artifact is the `warehouse_material_inventory` table; everything else mirrors the 011/012 shapes.

## Complexity Tracking

> No constitution violations — table intentionally empty.
