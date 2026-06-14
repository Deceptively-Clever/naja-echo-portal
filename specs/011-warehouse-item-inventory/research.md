# Phase 0 Research: Warehouse Item Inventory

All Technical Context unknowns are resolved below. Each decision records what was chosen, why, and
the alternatives rejected. The dominant input is the existing **Hangar** feature (006), which is the
closest precedent for an owner-scoped relationship entity with authenticated reads and gated writes.

## 1. Inventory entity shape & identity

- **Decision**: New `WarehouseInventoryEntry` in `Domain/Warehouse/` with `Guid Id`, `Guid ItemId`
  (FK → `sc.items`), `Guid OwnerUserId` (portal user), `string Location`, `int Quantity`, plus
  `CreatedAt`/`UpdatedAt`. Stored in the `public` schema. Business identity is the **unique index**
  on (`ItemId`, `OwnerUserId`, `Location`).
- **Rationale**: Mirrors `HangarEntry` (public schema, `Guid Id` surrogate + unique business key,
  references `sc`-schema game data). FR-019 requires same-item-different-owner/location to be separate
  rows; a composite unique key expresses that exactly.
- **Alternatives rejected**: Composite primary key (Item+Owner+Location) — Hangar uses a surrogate
  `Id` + unique index, which keeps the row addressable by a single `{id}` route param for edit/remove
  (FR-023/FR-024) and matches the established pattern. Putting the table in the `sc` schema — `sc` is
  reserved for imported game data; warehouse inventory is portal-owned operational data, like
  `hangar_entries`.

## 2. Location normalization & uniqueness

- **Decision**: Trim leading/trailing whitespace server-side before validating and persisting
  (FR-014). Reject empty/whitespace-only location (FR-013). Uniqueness is **exact match on the
  trimmed value** (case-sensitive).
- **Rationale**: The spec mandates only trimming. Case-insensitive collapsing ("Bay 3" vs "bay 3") is
  not required and would need a normalized/computed column or a functional unique index — added
  complexity beyond the requirement (YAGNI).
- **Alternatives rejected**: Case-insensitive unique index (`lower(location)`) — not required by spec;
  defer until a concrete need appears. Free-form with no uniqueness — violates FR-017 (duplicate-key
  increment).

## 3. Add = increment vs. insert (with concurrency safety)

- **Decision**: `AddInventoryItem` handler resolves the target by (ItemId, OwnerUserId, trimmed
  Location). If a row exists, `Quantity += submitted`. If not, insert. The repository performs this in
  a transaction; on a unique-constraint violation from a concurrent insert, it catches and retries as
  an increment (the DB unique index is the final arbiter).
- **Rationale**: Directly satisfies FR-017/FR-018 and the concurrency edge case ("exactly one
  increment, preventing duplicate rows"). This is the `HangarRepository.AddAsync` race-guard pattern
  (catch unique violation → domain behaviour) adapted from "reject duplicate" to "merge duplicate".
- **Alternatives rejected**: PostgreSQL `INSERT ... ON CONFLICT DO UPDATE SET quantity = quantity +
  excluded.quantity` — elegant but bypasses EF change tracking and is harder to unit-test against the
  in-memory provider; the transactional read-then-write-with-retry matches the existing codebase
  style and is exercised by a Testcontainers concurrency test. Application-level lock — does not
  protect against multiple instances; the DB constraint does.

## 4. Quantity rules

- **Decision**: `Quantity` is `int`, validated ≥ 1 and integral at the Application boundary
  (FluentValidation, mirroring existing validators). Add **increments** the stored value by the
  submitted amount; edit **replaces** it (FR-023). Non-integer input is rejected at the API edge by
  model binding (the DTO uses `int`) and re-asserted in the validator.
- **Rationale**: FR-020–FR-023. Using `int` makes "whole number" structurally true; the validator
  enforces the ≥ 1 floor with a clear message for the form.
- **Alternatives rejected**: Allowing 0 to mean "remove" — the spec has an explicit separate remove
  flow (US4) and forbids 0 (FR-022).

## 5. Owner model

- **Decision**: `OwnerUserId` references a registered portal user (ASP.NET Core Identity
  `ApplicationUser`, `Guid` key). On add, Owner defaults to the caller (`ClaimTypes.NameIdentifier`)
  but the Quartermaster MAY choose another registered user. The Owner filter and the add-form owner
  picker source their user lists from registered portal users.
- **Rationale**: FR-011/FR-004; "Owner refers exclusively to registered portal users" (Assumptions).
  Org-owned inventory is explicitly out of scope.
- **Alternatives rejected**: Free-text owner — contradicts the "registered portal user" requirement
  and would break the Owner exact-match filter (FR-004).

## 6. Quartermaster role & authorization

- **Decision**: Introduce a `Quartermaster` role. Add a `Quartermaster` authorization policy:
  `RequireRole("Quartermaster", "Admin")` so Admins inherit write access without an explicit
  Quartermaster assignment (FR-027). Generalize `AdminRoleSeeder` into a `RoleSeeder` that seeds both
  `Admin` and `Quartermaster` at startup (same non-fatal startup hook). Role assignment remains
  manual/data-store-only — no management UI (FR-028).
- **Rationale**: Reuses the exact ASP.NET Core Identity role + policy + claim machinery already in
  place (`AddRoles`, `ClaimTypes.Role` on sign-in, `CurrentUserResponse.Roles`). `RequireRole` with
  multiple roles is OR semantics, giving Admin inheritance for free.
- **Alternatives rejected**: A custom authorization handler checking "Quartermaster OR Admin" — the
  built-in multi-role `RequireRole` already does this. Seeding Quartermaster in a separate seeder —
  unnecessary duplication; one generalized seeder is simpler.

## 7. Read vs. write authorization split on one endpoint group

- **Decision**: `app.MapGroup("/api/warehouse").RequireAuthorization()` for the group (authenticated
  baseline, FR-001). Apply `.RequireAuthorization(AuthorizationPolicies.Quartermaster)` per-endpoint
  on the write routes (POST/PUT/DELETE and the catalog-search used only by the add flow).
- **Rationale**: Endpoint-level policy override is the standard Minimal API approach and keeps reads
  open while gating writes (FR-008/SC-005). The Hangar group uses group-level `RequireAuthorization()`
  already; per-endpoint policies layer cleanly on top.
- **Alternatives rejected**: Two separate groups (`/api/warehouse` read, `/api/warehouse/admin`
  write) — fragments a cohesive resource and complicates the contract for no benefit.

## 8. Filtering, sorting, and filter-option sourcing

- **Decision**: Server-side filtering in the list query: Name (`ILIKE %term%`, case-insensitive
  partial), Type = `Item.Section` (exact), Subtype = `Item.Category` (exact), Owner = `OwnerUserId`
  (exact), Location (`ILIKE %term%`). All filters optional; combined with AND (FR-005/FR-006). Default
  sort: Item Name ascending (FR-003). Filter dropdown options come from `GetInventoryFilters`:
  distinct Type/Subtype from `ItemCategory` (`Section`/`Name`, FR-007) and the distinct Owners present
  in inventory.
- **Rationale**: Server-side filtering meets SC-002 with one indexed query and keeps the client thin.
  Sourcing Type/Subtype from `ItemCategory` satisfies FR-007 literally. PostgreSQL `ILIKE` gives
  case-insensitive partial match without `lower()` gymnastics.
- **Alternatives rejected**: Client-side filtering of a full dump — fine at current scale but does not
  scale and duplicates logic; server-side keeps a single source of truth. Sourcing Type/Subtype from
  inventory rows only — would hide valid category values and drifts from FR-007's "item category
  data" wording.

## 9. Pagination

- **Decision**: No pagination in v1; return the full filtered, sorted list. The query and list DTO
  are shaped so a `page`/`pageSize` parameter can be added later without breaking the contract
  (response is an object with an `items` array, not a bare array).
- **Rationale**: Expected inventory size is low thousands at most; a single indexed query renders well
  within SC-001's 2 s. YAGNI on pagination until volume warrants it. The Hangar list DTO already wraps
  items in a paged envelope — the warehouse envelope leaves room to follow suit later.
- **Alternatives rejected**: Implementing pagination now — premature; adds UI and contract surface the
  spec does not require.

## 10. Catalog item search for the add flow

- **Decision**: `SearchCatalogItems` query searches `sc.items` where `Status == Active` and
  `Name ILIKE %term%`, returning Id, Name, Section (Type), Category (Subtype), capped at a sensible
  limit (e.g. 25 results, matching the Hangar catalog-search default page size). Gated by the
  Quartermaster policy.
- **Rationale**: FR-009/FR-010 and the edge case requiring a "manageable" result set. Directly mirrors
  `SearchCatalogShipsHandler`.
- **Alternatives rejected**: Returning the entire catalog to the client for local filtering — the item
  catalog can be large; server-side search with a cap is the established pattern.

## 11. Remembered Owner/Location (client-only)

- **Decision**: After a successful add, retain the submitted Owner and Location in React component
  state for the page lifetime to pre-fill the next add; cleared on reload (FR-015/FR-016). No
  persistence to localStorage/server.
- **Rationale**: "Remembered values live only in the browser's page state" (Assumptions). Component
  state is the simplest mechanism and naturally clears on reload.
- **Alternatives rejected**: localStorage/sessionStorage — would survive reloads, contradicting
  FR-016. Server-side preference — out of scope, over-engineered.

## 12. Frontend navigation & routing

- **Decision**: Add a **Warehouse** `group` to `navItems.ts` with an **Items** entry
  (`path: '/warehouse/items'`, no `access` → visible to all authenticated users), placed after the
  Hangar group. Add routes in `AppRouter.tsx`: `/warehouse` → redirect to `/warehouse/items`, and
  `/warehouse/items` → `WarehouseItemsView` inside `ProtectedRoute`/`DashboardLayout`. Write controls
  within the view render conditionally on `roles.includes('Quartermaster') || roles.includes('Admin')`
  via a `useIsQuartermaster` hook.
- **Rationale**: This is the exact Hangar nav/routing precedent (`group: 'Hangar'`, `/hangar` →
  `/hangar/mine`). Keeping the nav item visible to everyone (read access) while gating controls in the
  UI matches FR-001 vs FR-008. The data-driven nav model is reused with no shell changes
  (constitution Frontend Conventions).
- **Alternatives rejected**: A dedicated `WarehouseRoute` guard like `AdminRoute` — reads are open to
  all authenticated users, so route-level gating is wrong; control-level gating is correct. Adding
  Minerals now — explicitly out of scope; the group is structured to accept it later.

## 13. Testing strategy for the unique constraint

- **Decision**: Repository concurrency/constraint behaviour is verified with Testcontainers
  (PostgreSQL), because the EF in-memory provider does not enforce unique indexes. Handler/validator
  logic and endpoint auth/role gating use the in-memory provider + fakes (per Hangar endpoint tests).
- **Rationale**: Matches the precedent set by the commodity/ship plans (Testcontainers for anything
  the in-memory provider cannot exercise). Constitution II requires at least one real-DB integration
  test through the contract.
- **Alternatives rejected**: In-memory only — would not catch the unique-constraint/race behaviour
  that FR-017 and the concurrency edge case depend on.
