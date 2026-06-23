# Quickstart & Validation: Add Cities Import & Rename Stations to Locations

Feature **018-add-cities-locations** | Date 2026-06-22

Runnable validation scenarios proving the feature end‑to‑end. Reference
[contracts/openapi.yaml](./contracts/openapi.yaml) for exact shapes and [data-model.md](./data-model.md)
for the schema/migration. Do not duplicate implementation here.

## Prerequisites

- Backend running with a PostgreSQL 16 database (Testcontainers for tests, or the dev DB).
- The `AddCitiesAndPolymorphicLocation` migration applied (`dotnet ef database update` from
  `backend/src/NajaEcho.Api`).
- A signed‑in **Admin** session (for the import) and a **Quartermaster**/member session (for warehouse).
- Network access to `https://api.uexcorp.uk/2.0/` from the server (public feed, no auth).

## Setup

```bash
# Backend
cd backend/src/NajaEcho.Api
dotnet ef database update            # applies AddCitiesAndPolymorphicLocation
dotnet run

# Frontend (separate shell)
cd frontend
npm install
npm run dev
```

## Scenario 1 — City import populates the catalog (US1, FR‑002…FR‑006, SC‑001)

1. As an admin, trigger the import on the Data Import admin page (or
   `POST /api/admin/locations/import`).
2. **Expect** a `200` with an `ImportLocationsResponse` whose `cities` block reports non‑zero
   `added`/`total`, and a `skipped` count covering any city whose `id_star_system` is missing/unknown.
3. Query `GET /api/warehouse/locations?search=Area` — **expect** the city "Area 18" present with
   `type: "City"`.
4. Re‑run the import — **expect** the `cities` block now reports mostly `updated` (idempotent upsert),
   `added: 0`.

## Scenario 2 — Empty/unreachable cities source aborts safely (FR‑005, SC‑005)

1. With a fake UEX client returning an empty cities array (integration test), trigger the import.
2. **Expect** `502` and `EmptySourceException("cities")`; **expect zero** rows inserted/updated/deleted
   across systems, stations, and cities (no partial commit).

## Scenario 3 — Unified Location combobox lists stations and cities (US2, FR‑009, SC‑003)

1. Open the **Add** dialog on each warehouse page (Items, Ship Components, Materials).
2. **Expect** a combobox labelled **"Location"** (not "Station").
3. Type `a` — **expect** a single flat alphabetical list interleaving station and city names, with no
   group headers and no per‑row type badge.
4. With both catalogs empty (fresh DB, no import), open the combobox — **expect** an empty‑state message
   and the free‑text location field still usable (Edge Cases, US2 #5).

## Scenario 4 — Saving a city persists the polymorphic reference (US2 #3, FR‑014, SC‑004)

1. In an Add dialog, select a **city** and save.
2. **Expect** the request body to carry `locationId` + `locationType: "City"`.
3. Reload the page (new session) — **expect** the row's Location column shows the city name (read‑path
   `COALESCE(ss.name, ci.name, location)`).
4. Repeat selecting a **station** — **expect** `locationType: "Station"` persisted and displayed.

## Scenario 5 — Edit changes a location across types (US3, FR‑013, SC‑007)

1. Edit an existing row whose location is a station; **expect** the combobox pre‑populated with that
   station's name.
2. Select a **city**, confirm — **expect** the row updates to the city in one modal interaction.
3. Cancel an edit — **expect** the row unchanged.

## Scenario 6 — Migration preserves existing station references (FR‑015, SC‑008)

1. (Integration/Testcontainers) Seed `warehouse_inventory`/`warehouse_material` rows with a
   `station_id` under the pre‑migration schema.
2. Apply `AddCitiesAndPolymorphicLocation`.
3. **Expect** every seeded row now has `location_id = <old station_id>` and `location_type = 'Station'`,
   the `station_id` column gone, and the Location column still rendering the correct station name.

## Scenario 7 — No "Station" text remains (US4, FR‑011, SC‑006)

1. Visit all three warehouse pages and open each Add/Edit/Transfer dialog and each filter.
2. **Expect** no visible "Station" string in any column header, label, placeholder, empty state, or
   tooltip — all read "Location".

## Automated test entry points

- Backend: `dotnet test` — city upsert (skip/soft‑delete/reactivate), empty‑cities abort, combined‑
  locations merge/sort/filter, migration data‑copy (Testcontainers), polymorphic persist + invalid‑type
  rejection, locations endpoint auth.
- Frontend: `npm run test` — combobox interleave/filter/empty‑state, city/station save payloads, Edit
  pre‑populate, and the label‑rename assertions across all three pages.
</content>
