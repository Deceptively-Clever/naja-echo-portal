# Phase 0 Research: Warehouse Materials Subpage

All Technical Context items were resolved against the existing codebase (features 011, 012, 013) and
the constitution. No `NEEDS CLARIFICATION` markers remain; the two open spec questions were closed in
the 2026-06-15 clarification session (absolute quantity set; single-select Owner/Location filters).

## Decision 1 — Separate `warehouse_material_inventory` table (not reuse of `warehouse_inventory`)

**Decision**: Create a new `public.warehouse_material_inventory` table backed by a new
`WarehouseMaterialEntry` domain entity.

**Rationale**: Materials differ from the Items/Ship-Components inventory in three structural ways that
the existing table cannot absorb cleanly:
1. The catalog FK targets `sc.commodities` (`commodity_id`), not `sc.items` (`item_id`).
2. Quantity is `decimal(18,2)` with a `> 0` rule, vs the integer `>= 1` quantity on
   `warehouse_inventory`.
3. The row-uniqueness key includes **Quality** — `(commodity_id, owner_user_id, location, quality)` —
   whereas `warehouse_inventory`'s unique key is `(item_id, owner_user_id, location)` and its upsert
   *overwrites* quality on conflict. Folding materials into that table would either corrupt the Items
   semantics or require a discriminator plus a conditional unique index, which is more complex than a
   purpose-built table.

**Alternatives considered**:
- *Reuse `warehouse_inventory` with a nullable `commodity_id` and discriminator* — rejected: pollutes
  the Items/Ship-Components hot path, needs partial indexes and CHECKs to keep the two FKs mutually
  exclusive, and conflicts with the differing quantity type and unique key. Violates YAGNI by adding
  conditional complexity to existing, working code.
- *Make quantity integer and scale by 100* — rejected: the spec explicitly requires decimal display
  with 2 places and decimal storage precision (FR-010, Assumptions); native `decimal(18,2)` is the
  honest representation and Postgres/Npgsql support it directly.

## Decision 2 — Decimal quantity precision and rounding

**Decision**: Store quantity as `decimal(18,2)`. In the Application layer, round any incoming quantity
to 2 decimal places using **half-up** (`MidpointRounding.AwayFromZero`) *before* validating `> 0`.
Enforce `quantity > 0` both as FluentValidation/guard logic and as a DB check constraint
(`ck_warehouse_material_inventory_quantity` = `quantity > 0`).

**Rationale**: Matches FR-017/FR-018 and the spec edge case ("rounded to 2 places half-up before
validation and storage; a value that rounds to 0.00 is rejected"). Rounding before the `> 0` check
ensures `0.004` → `0.00` is rejected. The DB constraint is the last-line guarantee (SC-005) the same
way `quantity >= 1` guards the Items table.

**Alternatives considered**: Round-half-to-even (banker's) — rejected: spec explicitly says half-up.

## Decision 3 — Upsert with quality in the conflict target

**Decision**: Mirror `WarehouseInventoryRepository.AddOrIncrementAsync` with an
`INSERT … ON CONFLICT (commodity_id, owner_user_id, location, quality) DO UPDATE SET quantity =
warehouse_material_inventory.quantity + EXCLUDED.quantity, updated_at = EXCLUDED.updated_at`,
returning `(xmax = 0) AS is_new`. Quality is part of the conflict target, so it is **never** updated
on conflict (FR-022, FR-032) — only quantity and `updated_at` change.

**Rationale**: A single atomic statement is the established race-guard (referenced in the 012 plan and
implemented in 011). Including quality in the conflict target is exactly what makes
"same Material+Owner+Location+Quality → increment; any difference → new row" correct (FR-024..FR-026)
without a read-modify-write race.

**Alternatives considered**: Read-then-insert/update in app code — rejected: known race; the existing
repo already proves the upsert approach.

## Decision 4 — Absolute quantity set on adjust

**Decision**: `PUT /api/warehouse/materials/{id}/quantity` takes the **new total** quantity (absolute
set), rounds half-up to 2 places, rejects `<= 0.00`, and never touches Quality/Material/Owner/Location
(FR-030..FR-032). Mirror `UpdateQuantityAsync`.

**Rationale**: Resolved by the 2026-06-15 clarification ("Absolute set — the Quartermaster enters the
new total quantity directly"). Rejecting `<= 0` enforces "no zeroing-out as soft delete" (FR-031,
FR-019) — removal must use DELETE.

## Decision 5 — Default sort and filters

**Decision**: Server-side ORDER BY `commodity.name ASC, quality DESC, owner.display_name ASC,
location ASC` (FR-042). Filters: Material = `ILIKE %term%` over `commodity.name OR commodity.code`;
Owner = single `owner_user_id` equality; Location = single `location` equality from the known set;
Quality = `quality BETWEEN min AND max` inclusive; cross-field AND; null/empty params ignored. The
filters endpoint returns the distinct Owners (id + display name) and Locations currently present in
material inventory.

**Rationale**: Direct application of FR-035..FR-042 and the single-select clarification. Reuses the
`{param}::type IS NULL OR …` SQL pattern already in `WarehouseInventoryRepository`. Material search
over both name and code satisfies FR-035. Quality range is a numeric BETWEEN — no need to enumerate
quality options.

**Alternatives considered**: Client-side filtering/sorting — rejected: inconsistent with 011/012 which
filter and sort in SQL; server-side keeps payloads small and behaviour identical across pages.

## Decision 6 — `slider` shadcn primitive for the Quality range filter

**Decision**: Generate the shadcn `slider` component into `components/ui/slider.tsx` (Radix
`@radix-ui/react-slider`) and use it in two-thumb mode for the `[min,max]` Quality range, defaulting
to `1–1000`, optionally paired with numeric min/max inputs (FR-038, US5 scenario 5). Keep the
primitive application-agnostic; the dual-ended wiring and 1–1000 defaults live in `MaterialsFilters`.

**Rationale**: No range/slider primitive exists in `components/ui/` today; Radix Slider supports
multiple thumbs natively, which is the standard shadcn pattern for a min/max range. Per the
constitution, the primitive stays generic and the feature-specific composition lives in the feature
folder. Generation via the shadcn CLI keeps it consistent with the other owned primitives.

**Alternatives considered**: Two numeric inputs only — rejected: spec mandates a dual-ended slider
(numeric inputs are the optional accompaniment, not the primary control).

## Decision 7 — Auth, roles, and unauthorized behaviour

**Decision**: Reuse the existing setup verbatim — the `/api/warehouse` group `RequireAuthorization()`
for reads; `.RequireAuthorization(AuthorizationPolicies.Quartermaster)` on the four write/search
routes (the policy already admits `Admin`). Frontend gates controls with the existing
`useIsQuartermaster` hook; anonymous access redirects/401s exactly as the Items page does via
`ProtectedRoute`.

**Rationale**: FR-003..FR-006 explicitly require parity with the Items page; the infrastructure
already exists, so no new role plumbing is built (spec Assumptions).

## Decision 8 — Frontend types from the OpenAPI contract

**Decision**: Add the six endpoints to `contracts/openapi.yaml`; regenerate the shared types via
`openapi-typescript`; have `materialsApi.ts` wrap `apiFetch` using the generated request/response
types. UI-only view-model state (filter form state) may use local Zod schemas in
`schemas/materialSchemas.ts`.

**Rationale**: Constitution Principles I and III — generated types only, no hand-duplicated DTOs at
the API boundary. Mirrors `warehouseApi.ts` / `shipComponentsApi.ts`.

## Open Questions

None. The spec's two clarifications resolved the only behavioural ambiguities; all remaining choices
are mechanical applications of existing, verified codebase patterns.
