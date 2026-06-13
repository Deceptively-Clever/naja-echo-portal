# Quickstart: Hangar — Validation Guide

Runnable validation scenarios proving the Hangar feature works end-to-end. Details live in
[data-model.md](./data-model.md), [contracts/openapi.yaml](./contracts/openapi.yaml), and
[research.md](./research.md); this file is the run/verify guide only.

## Prerequisites

- PostgreSQL running with the existing `sc.ships` catalog populated (feature 006 import run at
  least once — `POST /api/admin/ships/import`). At least a few ships should have a non-empty
  `raw_data.url_photo` and some with no `url_photo`, to exercise both card backgrounds.
- Backend `AddHangarEntries` migration applied: `dotnet ef database update` from the API project.
- An authenticated session (Discord sign-in) as a normal member.
- Frontend types regenerated from the contract (see below).

## Setup commands

```bash
# Backend (from backend/src/NajaEcho.Api)
dotnet ef database update          # apply AddHangarEntries migration

# Frontend: add a generation script for the hangar contract, then run it
#   "gen:api:hangar": "openapi-typescript ../specs/007-hangar-fleet-view/contracts/openapi.yaml -o src/lib/api/hangar.d.ts"
cd frontend && npm run gen:api:hangar

# Run backend + frontend
dotnet run                         # API
npm run dev                        # SPA
```

## Test commands

```bash
# Backend (TDD: write these first and watch them fail)
cd backend && dotnet test          # Application unit + Infrastructure + Api integration (Testcontainers)

# Frontend
cd frontend && npm run test:run    # Vitest + RTL + MSW
```

## Validation scenarios

### 1. Navigate to Hangar → My Hangar (US1)
- Click **Hangar** in the nav. **Expect**: lands on My Hangar (default sub-view), showing only
  ships the current member owns as cards; ship **name in the top-left** of each card; no owner
  count anywhere. Empty hangar shows the "add your first ship" empty state.

### 2. Card backgrounds use `url_photo` with default fallback (US1 / SC-005)
- **Expect**: cards for ships whose `sc.ships.raw_data.url_photo` is present use that image as the
  card background; cards with missing/empty `url_photo` use the **default** background. Break an
  image URL (DevTools) → card silently falls back to the default background, no error indicator.
  Name/metadata stay readable over any image.

### 3. Search My Hangar by name (US1 / SC-003)
- Type in the search box. **Expect**: only owned ships whose `sc.ships.name` contains the text
  (case-insensitive, partial) remain; updates without full reload; "no ships match" message when
  empty.

### 4. Add Ship dialog searches the catalog (US2 / FR-017–019)
- Click **Add Ship**, type a model name. **Expect**: results come from `sc.ships` (active ships),
  each row showing `name`, and where useful `companyName`, an `url_photo` thumbnail, `scu`, and
  `crew` (crew extracted from `raw_data`). Ships already in your hangar are **marked as owned and
  cannot be added**.

### 5. Add a ship → appears immediately; dialog stays open (US2 / FR-020/021)
- Select an un-owned ship and confirm. **Expect**: success message, **dialog stays open**, and the
  ship now appears in My Hangar and in Org Hangar with you listed as an owner. Re-searching shows
  it as already owned. Adding a duplicate is blocked (**409**).

### 6. Remove a ship (US2 / FR-033–038)
- Hover a My Hangar card → a **remove** icon/button is revealed (not persistently visible).
  Click → confirm prompt → confirm. **Expect**: ship disappears from My Hangar. If you were the
  sole owner it leaves Org Hangar; if others own it, Org Hangar keeps it with a decremented owner
  count. On failure, an error message shows and the ship remains.

### 7. Org Hangar grouping, owner count, hover list (US3 / SC-004)
- Switch to **Org Hangar** (have two members own the same model). **Expect**: that model appears
  **once** with an owner count + person icon in the bottom-right; hovering the count shows the
  list of owner display names. No Add Ship button here.

### 8. Org Hangar filters (US3 / FR-023–026)
- Toggle **My Ships** → only your ships. Open the **member filter** → only members who own ≥1 ship
  are listed; pick one → only that member's ships (and **My Ships toggles off**). Pick **All
  Members** → filter cleared. A member filter with no matches shows the "no ships for that member"
  message.

### 9. Infinite scroll (US4 / SC-006)
- With more ships than one page in either view, scroll to the bottom. **Expect**: more cards load
  automatically, **no pagination controls** at any viewport. Changing search/filter resets to the
  start of the filtered set.

## Field-mapping checks (verify in API responses / tests)

- `name` ← `sc.ships.name`; `companyName` ← `sc.ships.company_name`.
- `urlPhoto` ← `raw_data->>'url_photo'`; `scu` ← `raw_data->>'scu'` (numeric); `crew` ←
  `raw_data->>'crew'` (string). Missing `url_photo` ⇒ default background path on the card.
- `GET /api/hangar/catalog/search` queries `sc.ships` by `name` and returns `alreadyOwned`.
- `ownerCount` = distinct owners of the model; `owners[]` carries display names for the hover list.
