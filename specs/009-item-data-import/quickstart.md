# Quickstart & Validation: Item Data Import

Validation guide for `009-item-data-import`. Proves the feature end-to-end. Implementation details
live in [plan.md](./plan.md), [data-model.md](./data-model.md), and
[contracts/openapi.yaml](./contracts/openapi.yaml).

## Prerequisites

- Local PostgreSQL running (via `docker-compose.yml`).
- Backend able to reach `https://api.uexcorp.uk/2.0/` (or a configured `UexVehicleClient:BaseUrl`
  override pointing at a stub).
- A signed-in user holding the `Admin` role (see `AdminRoleSeeder`).

## Setup

```bash
# Backend: apply the new migration (run from repo root)
./migrate.sh                # or: dotnet ef database update -p backend/src/NajaEcho.Infrastructure -s backend/src/NajaEcho.Api

# Frontend: regenerate types from the new contract, then run the SPA
cd frontend
npm run gen:api:items       # new script → src/lib/api/items.d.ts
npm run dev
```

## Backend tests

```bash
cd backend
dotnet test                 # all suites
# focused:
dotnet test --filter "FullyQualifiedName~ImportItemsHandlerTests"
dotnet test --filter "FullyQualifiedName~RefreshCategoriesHandlerTests"
dotnet test --filter "FullyQualifiedName~ItemRepositoryTests"
dotnet test --filter "FullyQualifiedName~ItemAdminEndpointsTests"
```

## Frontend tests

```bash
cd frontend
npm run test -- itemsImportTab categorySelector importItems
```

## Manual verification scenarios

Map directly to the spec acceptance criteria. Perform as an admin on the Data Import page → **Items** tab.

1. **Items tab is admin-only** (AC-1, AC-2)
   - As admin: the **Items** tab is visible and selectable.
   - As a non-admin (or via direct API call): `GET/POST /api/admin/items/**` returns **403**.

2. **Empty state disables import** (AC-4)
   - With no local categories: import actions are disabled and a message explains categories must be
     refreshed first; no last-refreshed timestamp is shown.

3. **Category refresh** (AC-3, AC-15)
   - Click **Refresh Categories** → the UEX categories endpoint is called → summary shows fetched /
     inserted / updated / unchanged / failed + start/end/duration. Last-refreshed timestamp now shows.
   - Re-run refresh: inserted drops, updated/unchanged reflect no-op churn.
   - Simulate a UEX failure (bad base URL / stub 500): error shown, **no** partial category data stored.

4. **Category selector context & filters** (AC-5, FR-006, FR-007)
   - Selector lists categories with section, name, type, game-related, mining-related, source modified
     date, and local import state.
   - Search by name, filter by section, toggle mining-related and game-related → list narrows correctly.
   - Only `type = "item"` categories expose an import action.

5. **Single-category import** (AC-5, AC-7, AC-14)
   - Select one item category → import → summary shows items fetched / inserted / updated /
     unchanged-or-skipped / skipped-no-uuid / soft-deleted / failed + timing.
   - Imported active items are available to normal app use; soft-deleted ones are hidden.

6. **UUID rules** (AC-7, AC-8)
   - Items with a uuid insert (new) or update (known uuid).
   - Items with `uuid = null` are skipped and counted in `itemsSkippedNoUuid`; the import still succeeds.

7. **Category-scoped soft-delete + restore** (AC-9, AC-10)
   - Import category A. Re-import A with one item removed at the source → that item is soft-deleted;
     items in category B are untouched.
   - Re-import A with the removed item back → it is restored (active) and updated.

8. **All-category import with partial failure** (AC-6, AC-11, AC-12)
   - Trigger **Import All** with at least two eligible categories, one of which fails (e.g. stub a
     malformed `data` for one category) → remaining categories still import; result `status` is
     `completedWithErrors` and `errors` lists the failed category for manual retry.

9. **Concurrency guard** (AC-13)
   - Start any import/refresh, then immediately trigger another action → blocked in the UI; a direct
     concurrent API call returns **409**.

## Expected end-state

- `sc.item_categories` populated; `sc.items` populated with active rows for imported categories.
- Soft-deleted items remain in `sc.items` with `status = SoftDeleted` and a `soft_deleted_at` value.
- `attributes` and `screenshot` never appear in `sc.items.raw_data` or as columns.
- No import-history rows anywhere (out of scope); summaries are response-only.
