# Quickstart: Commodity Data Import

Validation guide proving the feature works end-to-end. For field/contract detail see
[data-model.md](./data-model.md) and [contracts/openapi.yaml](./contracts/openapi.yaml).

## Prerequisites

- Backend deps restored; PostgreSQL 16 reachable via the `Default` connection string.
- An `Admin`-role user (see `AdminRoleSeeder`) and a non-admin user for the access checks.
- Frontend deps installed (`npm install` in `frontend/`).
- Docker available (Testcontainers-backed repository tests).

## Setup

```bash
# Backend: apply the new migration (creates sc.commodities)
cd backend
dotnet ef database update \
  --project src/NajaEcho.Infrastructure \
  --startup-project src/NajaEcho.Api

# Frontend: regenerate API types from the new contract
cd ../frontend
npm run gen:api:commodities   # → src/lib/api/commodities.d.ts
```

## Run the apps

```bash
# Terminal 1 — API
cd backend && dotnet run --project src/NajaEcho.Api

# Terminal 2 — SPA
cd frontend && npm run dev
```

## Automated test validation (TDD — write/expect-fail first)

```bash
# Backend
cd backend
dotnet test                                   # all layers
dotnet test --filter FullyQualifiedName~ImportCommoditiesHandlerTests
dotnet test --filter FullyQualifiedName~CommodityRepositoryTests      # Testcontainers
dotnet test --filter FullyQualifiedName~CommodityAdminEndpointsTests

# Frontend
cd ../frontend
npm run test -- importCommodities
```

Expected coverage (maps to spec acceptance criteria):

| Scenario | Spec ref | Where |
|---|---|---|
| Integer flags stored as booleans | US1 #2, FR-009, SC-003 | Handler test |
| Location strings stored raw + parsed `int[]` (trim, drop non-numeric) | US1 #3, FR-012 | Handler + repo test |
| Unix timestamps stored raw + converted UTC; invalid → null converted | US1 #4, FR-013 | Handler test |
| `uuid = null` still imported | US1 #5, FR-010, SC-004 | Handler test |
| Records missing `id`/`name` skipped + counted, import continues | US2, FR-005 | Handler test |
| Pricing fields never promoted (present only in raw_data) | FR-014 | Handler test |
| Soft-delete commodities absent from feed (global) | US3 #1, FR-006 | Repo test |
| Restore soft-deleted commodity on reappearance | US3 #2, FR-007 | Repo test |
| Fail (no changes) on unreachable source / invalid shape | US4, FR-003, SC-007 | Handler + endpoint test |
| Concurrent import rejected (409) | US5, FR-016, SC-006 | Endpoint test |
| Admin-only (403 non-admin, 401 anon) | FR-018 | Endpoint test |
| Import summary counts returned | US1 #6, FR-017 | Endpoint test |

## Manual end-to-end validation

1. Sign in as the **Admin** user → navigate to **Data Import** → open the **Commodities** tab.
2. Click **Import Commodities**.
   - **Expected**: a running/disabled state, then a success summary like
     `Import complete: N inserted, N updated, N restored, N removed, N skipped. (N fetched)`.
3. Inspect the DB:
   ```sql
   SELECT count(*) FROM sc.commodities;
   SELECT name, is_available, is_illegal, ids_planets, source_date_modified_utc
     FROM sc.commodities LIMIT 5;          -- flags are booleans; ids_planets is integer[]
   SELECT raw_data ? 'price_buy' AS price_in_raw   -- true: raw retained
     FROM sc.commodities LIMIT 1;          -- but no price_buy/price_sell column exists
   ```
4. Trigger the import a second time while the first is still running (e.g., double-click or a second
   tab) → **Expected**: the second attempt shows the "already in progress" warning (HTTP 409).
5. Sign in as a **non-admin** and POST `/api/admin/commodities/import` → **Expected**: 403.
6. Point `UexVehicleClient:BaseUrl` at an unreachable host and import → **Expected**: an error
   surfaced in the UI (HTTP 502) and `SELECT count(*) FROM sc.commodities` unchanged (SC-007).

## Done when

- All automated tests green (backend + frontend).
- Manual steps 1–6 behave as described.
- `npm run gen:api:commodities` produces `src/lib/api/commodities.d.ts` with no drift.
