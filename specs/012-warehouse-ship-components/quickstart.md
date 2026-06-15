# Quickstart & Validation: Warehouse Ship Components

Validation guide for `012-warehouse-ship-components`. See [data-model.md](./data-model.md),
[contracts/openapi.yaml](./contracts/openapi.yaml), and [research.md](./research.md) for details.

## Prerequisites

- PostgreSQL 16 reachable via the `Default` connection string (Docker compose or local).
- Backend: .NET SDK (`net10.0`). Frontend: Node + the workspace deps installed.
- Catalog data imported (`sc.items` / `sc.item_categories`) including at least a few **Systems**
  section items with valid `uex_id` values (use the Data Import admin page if empty).
- A user with the **Quartermaster** (or **Admin**) role for write checks, and a plain authenticated
  user for read-only checks.

## Setup

```bash
# Backend: apply the new migration
cd backend
dotnet ef database update --project src/NajaEcho.Infrastructure --startup-project src/NajaEcho.Api

# Frontend: regenerate API types from the new contract (script added by this feature)
cd ../frontend
npm run gen:api:ship-components
```

Run the apps:

```bash
# Backend (from backend/)
dotnet run --project src/NajaEcho.Api
# Frontend (from frontend/)
npm run dev
```

## Automated tests (authoritative)

```bash
# Backend (from backend/)
dotnet test
# Frontend (from frontend/)
npm run test:run
```

Expected coverage (Constitution II — write these failing first):

- **Application**: Systems-only scoping; derived Class/Size/Grade incl. Unknown (null) handling;
  Size text→int parse incl. parse-failure→null; multi-key sort with Unknown last; filter AND/OR +
  explicit Unknown; add rejects non-Systems items; lazy fetch only when uncached; no re-fetch when
  cached; fetch failure does not block create and leaves attributes null.
- **Infrastructure (Testcontainers)**: `sc.item_attributes` unique (item_id, uex_category_attribute_id);
  `sc.ship_component_attributes` PK/upsert; Systems-only list projection joins.
- **API**: reads allowed for any authenticated user; writes 403 for non-QM/non-Admin; Admin allowed;
  401/redirect anonymous; non-Systems add → 422.
- **Frontend**: 8-column table (no Section) + Unknown cells; empty vs no-results states; filters incl.
  Unknown option visibility + clear/reset; write controls hidden for non-QM; add restricted to
  Systems items; derived fields non-editable.

## Manual validation scenarios

1. **View scope (US1)** — As any authenticated user, go to **Warehouse → Ship Components**. Confirm
   the table shows only Systems-section rows, columns in order *Name, Type, Class, Size, Grade,
   Quantity, Owner, Location*, **no Section column**, default sort Name→Type→Size→Class→Grade, and
   missing Class/Size/Grade render as **Unknown**.
2. **Unauthorized (US1/FR-003)** — Hit `/warehouse/ship-components` while signed out → redirected to
   sign-in.
3. **Filters (US2)** — Type a Name substring (case-insensitive partial). Select multiple Type values
   (OR within field) plus a Class value (AND across fields). Confirm only matching rows show. Where
   rows have missing attributes, confirm **Unknown** appears as a selectable option and filtering by
   it returns exactly those rows. Click **Clear** → full list returns. Filter to empty result →
   "no results match" empty state.
4. **Add lazy-fetch (US3 + UEX)** — As Quartermaster, add a row for a Systems item with **no** cached
   attributes. Confirm the catalog search shows **only Systems items**, that Name/Type/Class/Size/
   Grade preview is **read-only**, and only Quantity/Owner/Location are editable. After save, confirm
   `sc.item_attributes` is populated and `sc.ship_component_attributes` has the typed Class/Size/Grade
   (Size as int). Add a second row for the **same** item → confirm no second UEX call (cache hit).
5. **Add fetch-failure non-blocking** — Point the UEX base URL at an unreachable host (or use the fake
   client in tests) and add a Systems item → the inventory row is still created; Class/Size/Grade show
   **Unknown**; a Serilog warning is emitted; inventory creation is not aborted.
6. **Non-Systems guard (SC-008)** — Attempt `POST /api/warehouse/ship-components` with a non-Systems
   itemId → 422; no row created.
7. **Edit / delete (US3, reused 011 endpoints)** — Change a row's quantity and delete a row from the
   Ship Components page; confirm the table updates and the catalog item is untouched.
8. **Role gating (US3)** — As a non-Quartermaster authenticated user, confirm no add/edit/delete
   controls appear. As Admin, confirm full Quartermaster controls without a separate role.
9. **Nav (FR-001)** — Confirm the **Warehouse** group lists **Inventory, Ship Components, Materials**
   (Materials is a placeholder/stub) and Ship Components is reachable in ≤ 2 clicks.

## Expected outcomes (success criteria)

- SC-002/SC-003: only Systems rows; all 8 columns present, Section absent.
- SC-004: five-level default sort on load, Unknown last.
- SC-005/SC-006: filters return correct AND/OR results with no perceptible delay.
- SC-007/SC-008: QM add/edit/delete without leaving the page; 100% of adds restricted to Systems.
- SC-009/SC-010: anonymous blocked; non-QM see no write controls.
