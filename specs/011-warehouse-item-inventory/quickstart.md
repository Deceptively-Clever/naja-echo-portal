# Quickstart: Warehouse Item Inventory

Validation guide proving the feature works end-to-end. See [spec.md](./spec.md) for requirements,
[data-model.md](./data-model.md) for the schema, and [contracts/openapi.yaml](./contracts/openapi.yaml)
for the API.

## Prerequisites

- PostgreSQL running (`docker-compose up -d` from repo root).
- Backend deps restored; frontend deps installed (`npm install` in `frontend/`).
- The item catalog populated (run the admin **Data Import → Items** import at least once) so the
  add-flow catalog search returns results.
- At least three test users: an anonymous (signed-out) browser, a plain authenticated member, and a
  user with the `Quartermaster` role. Admin should also be tested to confirm inheritance.

## Setup

```bash
# 1. Apply migrations (creates public.warehouse_inventory; seeds Admin + Quartermaster roles at startup)
./migrate.sh                       # or: dotnet ef database update -p backend/src/NajaEcho.Infrastructure -s backend/src/NajaEcho.Api

# 2. Regenerate frontend types from the contract
cd frontend && npm run gen:api:warehouse

# 3. Run backend + frontend
dotnet run --project backend/src/NajaEcho.Api    # terminal 1
cd frontend && npm run dev                        # terminal 2
```

## Assign the Quartermaster role (data-store only — no UI, FR-028)

```sql
-- Replace <user-guid>. The Quartermaster role row is seeded at startup.
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT '<user-guid>', "Id" FROM "AspNetRoles" WHERE "NormalizedName" = 'QUARTERMASTER';
```

The user must sign out/in for the role claim to appear in the session.

## Validation scenarios

### 1. Read access for any authenticated member (US1 / FR-001..FR-007)
- Sign in as a plain member → sidebar shows a **Warehouse** group with an **Items** link.
- Open **Warehouse → Items**: table shows Name, Type, Subtype, Quantity, Owner, Location, sorted by
  Name ascending.
- Apply filters individually and combined: partial Name (case-insensitive), Type (exact), Subtype
  (exact), Owner, partial Location (case-insensitive). Confirm AND logic narrows results.
- Confirm no add/edit/remove controls are visible.
- Empty inventory / no-results filters show their respective empty-state messages.

### 2. Anonymous redirect (FR-001 / SC-006)
- In a signed-out browser, navigate to `/warehouse/items` → redirected to sign-in; no data exposed.
- `curl` the API without a session cookie → `401`:
  ```bash
  curl -i http://localhost:5080/api/warehouse/items
  ```

### 3. Add & increment (US2 / FR-008..FR-019)
- As Quartermaster, open the add flow, search the catalog by name (results show Name, Type, Subtype),
  select an item. Owner defaults to the signed-in user; Quantity defaults to 1.
- Submit with a new Location → new row appears.
- Submit the **same** Item + Owner + Location again with quantity N → the existing row's quantity
  increases by N (no duplicate row).
- Submit the same item with a **different** owner, then a **different** location → separate rows.
- After a successful add, add a second item → Owner and Location are pre-filled from the previous add.
- Reload the page → remembered Owner/Location are cleared (FR-016).

### 4. Write authorization (FR-008 / FR-027 / SC-005)
- As a plain member, the add/edit/remove controls are absent. Direct API calls are rejected:
  ```bash
  # Expect 403 for a non-Quartermaster, non-Admin authenticated session:
  curl -i -X POST http://localhost:5080/api/warehouse/items \
    -H 'Content-Type: application/json' --cookie '<member-session>' \
    -d '{"itemId":"<item-guid>","quantity":1,"location":"Bay 1"}'
  ```
- As an **Admin** (without explicit Quartermaster role), the same write succeeds (inheritance).

### 5. Validation (FR-013/FR-014, FR-020..FR-022)
- Add with empty / whitespace-only location → rejected with an error.
- Add with location `"  Bay 3  "` then add `"Bay 3"` to the same item/owner → treated as the same row
  (trimmed), quantity increments.
- Add or edit with quantity `0`, negative, or non-integer → rejected; nothing changes.

### 6. Change quantity (US3 / FR-023)
- As Quartermaster, edit a row's quantity to a whole number ≥ 1 → value is **replaced** (not added).
- Setting `0` or a non-integer → rejected; original value unchanged.

### 7. Remove (US4 / FR-024/FR-025)
- As Quartermaster, remove a row → it disappears from the list.
- Confirm the underlying catalog item still exists (re-search it in the add flow).

## Automated tests

```bash
# Backend
dotnet test backend/tests/NajaEcho.Application.Tests
dotnet test backend/tests/NajaEcho.Infrastructure.Tests   # Testcontainers: unique constraint + concurrent add
dotnet test backend/tests/NajaEcho.Api.Tests              # auth + role gating + behaviour

# Frontend
cd frontend && npm run test -- warehouse
```

## Success criteria mapping

| Criterion | Validated by |
|-----------|--------------|
| SC-001 list < 2 s | Scenario 1 |
| SC-002 filter < 1 s | Scenario 1 |
| SC-003 add < 60 s | Scenario 3 |
| SC-004 repeat add < 15 s | Scenario 3 (remembered fields) |
| SC-005 writes blocked for non-QM | Scenario 4 |
| SC-006 anonymous redirected | Scenario 2 |
