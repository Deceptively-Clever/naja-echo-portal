# Quickstart / Validation: Admin Users Page

**Feature**: `017-admin-users-page` | **Date**: 2026-06-21

Validates the two user stories end-to-end against the contract in
[`contracts/openapi.yaml`](./contracts/openapi.yaml) and the rules in [`data-model.md`](./data-model.md).
No database migration is required (the `characters`, `AspNetUsers`, and role tables already exist).

## Prerequisites

- Backend running (`dotnet run` from `backend/src/NajaEcho.Api`) against a PostgreSQL instance with
  identity, roles, and at least one member seeded with the `Admin` role.
- Frontend running (`npm run dev` from `frontend/`).
- Signed in via Discord as an account holding the `Admin` role.

## Backend tests

```bash
# From repo root
dotnet test backend                       # full suite (Application + Infrastructure + Api)
# or scoped:
dotnet test backend/tests/NajaEcho.Application.Tests   # GetUsers + AddCharacterForUser handlers
dotnet test backend/tests/NajaEcho.Api.Tests           # endpoint auth + status/problem mapping
```

Expected coverage (must be written failing first — constitution II):

- **GetAdminUsersHandler**: returns all members; member with 0 characters / 0 roles yields empty
  arrays (FR-010); characters and roles are correctly grouped per member.
- **AddCharacterForUserHandler**: happy path creates a character linked to the target user; blank
  handle rejected; unknown target user → not found; duplicate handle → already-claimed; RSI
  not-found / unreachable / no-name each map to their distinct failure (FR-006–FR-009), using a fake
  `IRsiCitizenClient`.
- **Infrastructure (Testcontainers)**: the joined users-with-roles-and-characters read returns
  correct groupings against the real schema; admin add persists a row honouring the
  `ux_characters_handle_lower` unique index.
- **API**: `GET /api/admin/users` and `POST /api/admin/users/{id}/characters` return **403** for a
  non-admin and **401** unauthenticated (FR-001, SC-005); success/failure status codes and
  RFC-7807 problem bodies match the contract.

## Frontend tests

```bash
# From frontend/
npm run test -- adminUsers
```

Expected coverage (Vitest + RTL + MSW):

- Users table renders one row per member with auth name, friendly role labels (FR-011), and each
  member's characters (name + handle); empty character/role cells render a clear empty state, not an
  error (FR-010).
- The single filter input narrows rows by auth name, character name, **and** role simultaneously
  (FR-003); zero matches show an empty-state message.
- Add Character dialog: blank handle shows an inline error before any request (US2 #5); a successful
  add shows the new character in the member's row; duplicate / not-found / unreachable / no-name
  errors each surface their distinct message.
- Non-admin navigating to `/dashboard/admin/users` is redirected (client guard) — server remains
  authoritative.

## Manual walkthrough

### User Story 1 — View users (P1)

1. Navigate to **Users** in the Admin nav group (`/dashboard/admin/users`).
2. Confirm a table lists every member with auth name, friendly roles, and registered characters.
3. Type a character name, then a role, then an auth name into the filter — confirm rows narrow in
   real time with no reload.
4. Sign in as a non-admin and visit `/dashboard/admin/users` — confirm you are redirected and no
   user data loads (network tab shows 403 from `GET /api/admin/users`).

### User Story 2 — Add a character (P2)

1. As admin, click **Add Character** on a member's row.
2. Enter a valid, unregistered RSI handle → submit → the character appears in that member's row.
3. Submit the **same** handle again → blocked with "already claimed" (409).
4. Enter a non-existent handle → "not found" (404). Enter a handle while RSI is unreachable →
   "could not reach RSI" (502). (Optional) a valid page with no extractable name → FR-009 message
   (422).
5. Submit an empty handle → inline validation error, no network request.

## Success criteria mapping

| Criterion | Validated by |
|-----------|--------------|
| SC-001 / SC-002 | Manual US1 steps 1–3; list loads once, filters client-side |
| SC-003 | Manual US2 step 2 |
| SC-004 | Backend duplicate test + manual US2 step 3 (409 before insert) |
| SC-005 | API 401/403 tests + manual US1 step 4 |
| SC-006 | Manual US2 step 2 (no DB intervention needed to attach a character) |
