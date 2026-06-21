# Quickstart & Validation: Star Systems & Space Station Import

Runnable validation for feature 016. Proves the catalog import, the station combobox, and the Transfer
action end-to-end. See [data-model.md](./data-model.md) and [contracts/openapi.yaml](./contracts/openapi.yaml)
for shapes; this guide is the run/validate path, not implementation detail.

## Prerequisites

- Backend running locally with PostgreSQL (see `specs/015-character-registration/plan.md` for the standard
  dev setup), migrations applied via `./migrate.sh`.
- Frontend dev server running (`npm run dev` in `frontend/`).
- One **admin** account and one **Quartermaster** account (or one account with both) for the gated routes.
- Outbound network access to `https://api.uexcorp.uk/2.0/` (UEX is unauthenticated).

## Setup

```bash
# Backend: add the migration and apply it
cd backend
dotnet ef migrations add AddStarSystemsAndStationCatalog \
  --project src/NajaEcho.Infrastructure --startup-project src/NajaEcho.Api
./migrate.sh

# Run backend + frontend (separate shells)
dotnet run --project src/NajaEcho.Api
cd ../frontend && npm run dev
```

## Scenario 1 — Trigger the import (US1, FR-001..FR-005)

1. Sign in as an admin → **Data Import** → **Locations** tab → **Import**.
2. **Expect**: HTTP 200 and a summary panel showing **separate** count blocks for Star Systems
   (`added/updated/reactivated/softDeleted/total`) and Space Stations (same + `skipped`).
3. Re-run the import → counts shift to mostly `updated`; `added` ≈ 0 (idempotent upsert).

Backend check via the API directly:

```bash
curl -i -X POST http://localhost:5000/api/admin/locations/import -b cookies.txt
# 200 with { "starSystems": {...}, "spaceStations": { ..., "skipped": N } }
```

DB check (records present, soft-delete column behaves):

```sql
SELECT count(*) FROM sc.star_systems WHERE status = 'Active';
SELECT count(*) FROM sc.space_stations WHERE status = 'Active';
```

**Error/abort path (FR-012, SC-006)**: point `UexVehicleClient:BaseUrl` at an unreachable host (or a stub
returning an empty `data` array) and re-run. **Expect** HTTP 502, a clear error message, and **zero**
change in the row counts above.

**Skip path (FR-013)**: with a station whose `id_star_system` is absent from the systems feed, confirm the
station is not inserted and the summary's `spaceStations.skipped` count increments.

## Scenario 2 — Station combobox on add/edit (US2, FR-006..FR-010)

1. Sign in as a Quartermaster → **Warehouse** → any of Items / Ship Components / Materials → **Add**.
2. Open the **Location** field → **Expect** a searchable dropdown of stations; type a partial name →
   list filters to matching full station names (FR-009, SC-003).
3. Select a station, fill required fields, **Save**.
4. **Expect**: the entry persists with the canonical `stationId` (SC-004). The deprecated free-text
   Location field is still present but optional.

API check:

```bash
curl "http://localhost:5000/api/warehouse/stations?search=ARC&limit=10" -b cookies.txt
# 200 with only is_available && !is_decommissioned stations
```

**Empty-catalog path (US2 scenario 4)**: before any import, open the combobox → it is empty; the free-text
field remains usable.

## Scenario 3 — Transfer a row (US3, FR-011, FR-014, SC-005)

1. In any warehouse table, use the **Transfer** row action → a **modal** opens with the station combobox.
2. Select a destination station → **Confirm**. **Expect** the row's station reference updates; the
   free-text Location is unchanged.
3. Open Transfer on **another** row → **Expect** the combobox pre-selects the **last station** chosen this
   session (FR-014).
4. Open Transfer and **Cancel** → **Expect** the row's location is unchanged (US3 scenario 4).

API check:

```bash
curl -i -X PUT http://localhost:5000/api/warehouse/items/<rowId>/station \
  -H 'Content-Type: application/json' -d '{"stationId":"<stationGuid>"}' -b cookies.txt
# 204; 404 for an unknown rowId; 400 for an unknown stationId
```

## Automated test gates (must be green)

- **Backend unit**: combined import (separate counts), empty/unreachable abort (zero changes),
  unknown-parent skip+count, soft-delete of absent records, station-list filtering, add persists
  `stationId`, transfer updates `station_id` only.
- **Backend integration (Testcontainers)**: unique `uex_id` indexes, star-system→station FK, nullable
  warehouse `station_id` FK.
- **Backend API**: import 200/409/502, station list 200, transfer 204/404/400 + RFC-7807 mapping, admin
  and Quartermaster gates.
- **Frontend**: Locations import tab (summary/empty/error), station combobox filtering, add saves
  `stationId`, Transfer modal open/confirm/cancel + last-selection default.

Run:

```bash
cd backend && dotnet test
cd ../frontend && npm test
```
