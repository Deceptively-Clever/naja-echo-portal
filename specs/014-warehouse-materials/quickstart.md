# Quickstart & Validation: Warehouse Materials Subpage

This guide proves feature 014 end-to-end. It references [`spec.md`](./spec.md),
[`data-model.md`](./data-model.md), and [`contracts/openapi.yaml`](./contracts/openapi.yaml) rather
than restating them. Implementation detail belongs in `tasks.md`.

## Prerequisites

- Backend and frontend run locally (see the repo root `README.md` / `docker-compose.yml`).
- PostgreSQL reachable; the new EF migration `AddWarehouseMaterialInventory` is applied
  (`./migrate.sh` or `dotnet ef database update`).
- `sc.commodities` is populated (feature 010 import).
- Test accounts: one **Quartermaster** (or **Admin**) and one **non-Quartermaster** authenticated
  user; the ability to hit a route anonymously.

## Build, migrate, test

```bash
# Backend
dotnet build NajaEchoPortal.sln
dotnet ef migrations add AddWarehouseMaterialInventory \
  --project backend/src/NajaEcho.Infrastructure --startup-project backend/src/NajaEcho.Api
dotnet test                       # Application + Api + Infrastructure tests must be green

# Frontend
cd frontend
npm run gen:api                   # regenerate types from contracts/openapi.yaml (per repo script)
npm test                          # Vitest: Materials* suites green
npm run dev
```

## Validation scenarios (map to acceptance criteria)

### A. View (US1 / SC-001..SC-003, SC-010)
1. As any authenticated user, click **Warehouse → Materials** (≤ 2 clicks). The page loads.
2. The table shows **Material, Owner, Location, Quantity, Quality**; Quantity renders with exactly
   2 decimal places.
3. Rows are ordered Material name ↑ → Quality ↓ → Owner name ↑ → Location ↑.
4. With no rows, a "no material inventory" empty state shows; with filters that match nothing, a
   distinct "no results" empty state shows.
5. Hit `/warehouse/materials` while signed out → same redirect/401 as the Items page.

### B. Add & increment (US2 / SC-004, SC-005, SC-006, SC-007)
6. As Quartermaster, open **Add material**. Owner defaults to you; Quality defaults to **500**.
7. Search and select a commodity (cannot type a custom material). Enter Location — existing
   locations are suggested. Save with Quantity `12.50` → a new row appears showing `12.50`.
8. Add the **same** Material+Owner+Location+Quality with Quantity `2.25` → the existing row becomes
   `14.75`; no duplicate row (verify count unchanged).
9. Change any one of Material/Owner/Location/Quality and save → a separate new row is created.
10. Try Quantity `0`, `-1`, and `0.004` (rounds to `0.00`) → each is blocked with a validation
    message; no row persisted. Try Quality `0` and `1001` → blocked.

### C. Adjust quantity (US3 / SC-005, FR-032)
11. Adjust an existing row to `9.00` (absolute set) → quantity becomes `9.00`; Quality unchanged.
12. Adjust to `0` or `-3` → rejected; row keeps its prior quantity. No quality field is offered.

### D. Remove (US4)
13. Delete a row → it disappears from the list. (Removal is the only way out — confirms FR-019.)

### E. Filter & quality read-only (US5 / SC-008, SC-009)
14. Type in the Material filter → only rows whose name **or** commodity code match remain.
15. Select one Owner, then a different Owner → selection replaces, not accumulates. Same for
    Location.
16. Set the Quality dual-slider to `[300, 700]` → only rows with quality in that inclusive range
    show. Slider defaults to `1–1000`.
17. Combine Material + Owner + Quality range → only rows matching **all** show (AND). Clear all →
    all rows return.
18. Open any existing row → Quality is visible but has no editable control anywhere.

### F. Role gating (SC-011)
19. As a non-Quartermaster authenticated user, the Materials page renders the list but shows **no**
    add, adjust-quantity, or delete controls. Calling the write endpoints directly returns 403.
20. As an Admin (no separate Quartermaster assignment), all write controls are available.

## Contract check

`contracts/openapi.yaml` must validate and match the implemented routes. Regenerated frontend types
must compile with no hand-edited DTOs at the API boundary (constitution III).
