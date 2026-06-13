# Quickstart & Validation: Hangar JSON Import

Validates the feature end-to-end against the [spec](./spec.md), [contract](./contracts/openapi.yaml),
and [data model](./data-model.md). Assumes feature 007 (Hangar) is running and the `sc.ships`
catalog is populated (feature 006).

## Prerequisites

- Backend API running (`backend/`) against a PostgreSQL with `sc.ships` populated and the
  `sc.hangar_entries` table present (migration `AddHangarEntries`). **No new migration is needed.**
- Frontend running (`frontend/`) and an authenticated session (Discord sign-in).
- A HangarXPLOR JSON export file (array of records) — e.g. the sample from the feature request.

## Regenerate frontend types from the contract

```bash
cd frontend
npm run gen:api:hangar-import   # added by this feature → src/lib/api/hangar-import.d.ts
```

## Run the tests (TDD — write first, watch fail, then implement)

```bash
# Backend
cd backend
dotnet test --filter "FullyQualifiedName~Hangar.ImportHangar"      # Application unit tests
dotnet test --filter "FullyQualifiedName~Hangar"                    # incl. endpoint integration

# Frontend
cd frontend
npm run test -- ImportHangar
```

## Manual validation scenarios

### 1. Happy path — replace hangar from export (US1, FR-002/004/008/010)

1. Open **My Hangar**. Note existing ships.
2. Click **Import** → a dialog appears with a **warning** that all existing hangar ships will be
   replaced (SC-005: the file picker is not reachable until the warning is acknowledged).
3. Confirm the warning, select the export `.json`, submit.
4. **Expected**: hangar refreshes to show only matched ships; a summary shows imported vs. skipped
   counts (e.g. "36 imported, 3 skipped"). Org Hangar and the Add-Ship "already owned" flags
   reflect the new state (cache invalidation, research R7).

### 2. Cancel keeps hangar intact (FR-003)

1. Click **Import**, then **Cancel** at the warning.
2. **Expected**: no file prompt proceeds; hangar unchanged.

### 3. Partial match with unidentified / unknown ships (US2, FR-006/007)

1. Import a file containing a record with an `unidentified` field (e.g. `A.T.L.S.`) and/or a name
   not in the catalog.
2. **Expected**: recognized ships import; the summary reports the skipped count and lists the
   unmatched names. `ship_name` is used for matching, falling back to `name`; matching is
   case-insensitive.

### 4. Empty array clears the hangar (edge case)

1. Import `[]`.
2. **Expected**: confirmation that 0 ships were imported; hangar is now empty.

### 5. Duplicate-named records collapse (research R4, supersedes FR-009)

1. Import a file with two records resolving to the same catalog ship.
2. **Expected**: exactly one hangar entry for that ship; `importedShips` counts it once.

### 6. Invalid file is rejected without touching the hangar (US3, FR-011, SC-004)

1. Select a non-JSON file, or JSON that is not an array of records.
2. **Expected**: an inline error appears **before** any API call; the hangar is unchanged. (A
   crafted-but-malformed body sent directly to the API returns `400 ProblemDetails` and changes
   nothing.)

### 7. Atomicity (FR-012)

- Verified by integration test: a forced failure during insert rolls back, leaving the pre-import
  hangar fully intact (no partial/empty state committed).

## Contract reference

`POST /api/hangar/mine/import` with `{ "items": [ { "name": "...", "shipName": "...",
"unidentified": "..."? }, … ] }` → `200 { totalRecords, importedShips, unmatchedRecords,
unmatchedShipNames }`. Requires an authenticated session (`401` otherwise). See
[contracts/openapi.yaml](./contracts/openapi.yaml).
