# Quickstart & Validation: Ship Data Import

A run/validation guide proving the feature works end-to-end. Implementation details live in `tasks.md`
and the code; this file is about standing it up and verifying each user story.

## Prerequisites

- .NET 10 SDK, Node 20+, Docker (for PostgreSQL and Testcontainers).
- PostgreSQL running (the repo's `docker-compose.yml` provides one) and `ConnectionStrings:Default` set.
- Discord OAuth configured (existing) so you can sign in.
- The new EF migration applied: `dotnet ef database update` from the Infrastructure project (or run the
  API, which applies migrations on startup if so configured).

## Make yourself an admin (manual assignment)

Admin membership is assigned manually this pass. The `Admin` role row is seeded automatically on API
startup; you only need to link your user to it.

1. Sign in once via Discord so your `ApplicationUser` row exists.
2. Find your user id and the Admin role id, then insert the join row (psql):
   ```sql
   -- inspect
   select id, display_name, discord_username from asp_net_users;
   select id, name from asp_net_roles;        -- expect a row named 'Admin'
   -- grant (substitute the two ids)
   insert into asp_net_user_roles (user_id, role_id) values ('<your-user-id>', '<admin-role-id>');
   ```
3. **Sign out and back in.** Roles are baked into the auth cookie at sign-in, so the new role only takes
   effect on your next login (documented caveat).

## Run

```bash
# backend (from backend/src/NajaEcho.Api)
dotnet run
# frontend (from frontend/)
npm install        # picks up @radix-ui/react-tabs if newly added
npm run dev
```

Open the SPA, sign in as your admin user. The sidebar should now show an **Admin** section with a
**Data Import** item. Non-admin users must NOT see it.

## Validate the user stories

### US1 — Trigger an import (P1)
1. Go to Admin → Data Import. The **Ships** tab is selected.
2. Click **Import Ships**. The button shows a loading/disabled state during the run (FR-003 guard).
3. On success a message reports counts (added/updated/…); the table populates (FR-004, SC-001 < 30s).
4. Click **Import Ships** again immediately — it runs again with no cooldown; counts now show mostly
   `updated`. (To see a 409, fire two imports concurrently, e.g. via two quick clicks / two tabs.)
5. Failure path: point the UEX base URL at an unreachable host (config) and import → error message, table
   unchanged (FR-005).

### US2 — View imported ships (P2)
1. With data imported, confirm the table shows **name** and **company name** per row (FR-006).
2. Page through with the pagination controls (default 25/page); verify counts/total pages (SC-002).
3. Empty state: against a fresh DB (no import yet) the page shows the empty state prompting an import
   (FR-007).

### US3 — View full ship detail (P3)
1. Click **View Details** on a row → a **Sheet slides in from the right** (FR-008).
2. Confirm every feed field is listed, including empty ones shown explicitly (US3 scenario 3).
3. Close the sheet → you return to the same page/scroll position in the list (US3 scenario 2, SC-003).
4. For a soft-deleted ship, the detail (and its row) shows a "no longer in source feed" indicator
   (FR-010, US3 scenario 4).

### Soft-delete / reactivate behaviour (FR-009/011, SC-005)
- Simulate a ship leaving the feed (e.g. a fixture/mocked client omitting one `uex_id`) → after import it
  is marked soft-deleted, still visible.
- Re-include it and import again → it auto-reactivates (flag cleared), no manual action (FR-011).

### Authorization (FR-001, defense in depth)
- As a **non-admin**, the Admin nav item is hidden and visiting `/dashboard/admin/data-import` directly is
  blocked by `AdminRoute`.
- Hit the API directly without the Admin role: `GET /api/admin/ships` → **403**; unauthenticated → **401**.

## Tests

```bash
# backend
dotnet test                      # xUnit; includes Testcontainers-Postgres repository tests (needs Docker)
# frontend
npm run test:run                 # Vitest + RTL + MSW
npm run build                    # tsc -b typecheck + vite build
npm run lint
```

Expected coverage:
- **Application**: `ImportShipsHandler` — added/updated/reactivated/soft-deleted counts, rollback on
  failure, zero-record guard.
- **Infrastructure (Testcontainers)**: `ShipRepository` — paging, JSONB round-trip, transactional upsert,
  soft-delete/reactivate.
- **API (WebApplicationFactory)**: admin endpoints — 401/403/200 authz, import 409, list/detail shapes.
- **Frontend**: ships table (rows/empty/pagination/soft-deleted badge), detail sheet (opens, all fields,
  position preserved), import button (loading/success/error/409), nav gating, `AdminRoute`.

## Reference

- Endpoints & shapes: [contracts/openapi.yaml](./contracts/openapi.yaml)
- Entity, status lifecycle, import semantics: [data-model.md](./data-model.md)
- Decisions & rationale: [research.md](./research.md)
