# Implementation Plan: Admin Users Page

**Branch**: `017-admin-users-page` | **Date**: 2026-06-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/017-admin-users-page/spec.md`

## Summary

Add an admin-only **Users** page that lists every authenticated member with their roles (friendly
labels) and registered characters, filterable in real time by auth name / character name / role, and
an **Add Character** action that attaches a character to a member by RSI handle — reusing the
feature-015 RSI scraper but **skipping** the self-registration token-ownership flow (admin authority
is the sole authorization).

The dominant precedents are **feature 002 (identity/roles)** and **feature 015 (character
registration)**. The "add character" path reuses `IRsiCitizenClient.FetchCitizenAsync` and
`ICharacterRepository` (`HandleExistsAsync` / `AddAsync`) verbatim, plus the existing exception
types `HandleAlreadyClaimedException`, `RsiProfileNotFoundException`, and `RsiUnreachableException`
from `Features/Characters/VerifyCharacter`. The only genuinely new behaviour is the admin handler
that omits the pending-token check, and two new failure modes (target-user-not-found, and FR-009
"name not extractable"). The users-list read follows the raw-SQL identity-join precedent in
`MaterialInventoryRepository` (joins `AspNetUsers` / `AspNetUserRoles` / `AspNetRoles` plus
`characters`). The admin endpoint group copies `ShipAdminEndpoints` (`MapGroup(...)
.RequireAuthorization(AuthorizationPolicies.Admin)`). The frontend adds a thin route page, a table +
single filter input + Add-Character dialog under `features/admin/`, TanStack Query hooks, Zod schemas
hand-derived from the contract, a one-line `navItems` entry, and a guarded route under `AdminRoute`.

**No database schema change and no EF Core migration**: the `characters` table, `AspNetUsers`, and
the role join-tables already exist (features 002 + 015). The admin add-character action inserts an
ordinary `Character` whose `OwnerUserId` is the **target member**; the existing
`ux_characters_handle_lower` unique index already enforces FR-006 at the database level.

**API contract changes ARE required** (this is **not** a UI-only feature, so the constitution's
"No API contract changes required" exemption does **not** apply): two new admin endpoints, defined in
[`contracts/openapi.yaml`](./contracts/openapi.yaml) before implementation —

- `GET  /api/admin/users` — Admin-only. Returns the full member roster, each with raw role names and
  registered characters (name + handle); empty role/character arrays are valid (FR-001, FR-002,
  FR-010). Unpaginated; filtering is client-side (FR-003).
- `POST /api/admin/users/{userId}/characters` — Admin-only. Verifies the RSI handle, scrapes the
  name, links a new character to the target member, skipping the token flow (FR-004, FR-005).
  Distinct failures: 400 empty handle, 409 already-claimed (FR-006), 404 RSI-not-found (FR-007) or
  unknown target user, 502 RSI-unreachable (FR-008), 422 name-not-extractable (FR-009).

**Friendly role names (FR-011) are applied client-side** via a small static map in the admin feature
(`Admin` → "Administrator", `Quartermaster` → "Quartermaster"); the API returns raw role names. See
[research.md](./research.md) Decision 5.

## Technical Context

**Language/Version**: C# on .NET (`net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, ASP.NET Core Identity (cookie auth,
`Admin`/`Quartermaster` roles), EF Core + `Npgsql` (snake_case), the existing typed `HttpClient`
`RsiCitizenClient` (AngleSharp HTML parsing) implementing `IRsiCitizenClient`, Serilog. Frontend —
React 19 (Vite), React Router data APIs, Tailwind CSS 4, shadcn/ui (Radix), Lucide, TanStack Query 5,
React Hook Form + Zod, `apiFetch`. **No new backend or frontend dependency is introduced** — the
table/filter/dialog reuse existing `components/ui/` primitives (YAGNI).

**Storage**: PostgreSQL 16. **No migration.** Reads existing `AspNetUsers`, `AspNetUserRoles`,
`AspNetRoles`, and `characters`; writes one `characters` row per admin add (existing table, existing
`owner_user_id` FK and `ux_characters_handle_lower` unique index).

**Testing**: Backend — xUnit + FluentAssertions. Application handler unit tests with fake
`IRsiCitizenClient` / fake repositories: users-list grouping incl. empty roles/characters (FR-010),
add happy path, blank handle, unknown target user, duplicate handle (FR-006), RSI not-found (FR-007),
RSI unreachable (FR-008), RSI 200-but-no-name (FR-009). At least one Testcontainers (PostgreSQL)
integration test for the joined users-with-roles-and-characters read and for the admin insert
honouring the unique handle index. API endpoint tests: `403` for non-admin / `401` unauthenticated on
both routes (FR-001, SC-005), and the full success/failure status + RFC-7807 problem mapping for the
add route. Frontend — Vitest + React Testing Library + MSW: table render with friendly roles +
empty-state cells, single-input filter across auth/character/role + zero-result empty state, Add
Character dialog (inline blank validation, success row update, each distinct error message), and the
non-admin redirect.

**Target Platform**: Linux server (containerized API) + browser SPA.

**Performance Goals**: Roster is small (spec assumption) — the list is one joined query returned
unpaginated and filtered client-side; list readable < 3 s (SC-002), member locatable < 10 s via the
filter (SC-001), add workflow < 60 s (SC-003).

**Constraints**:
- Both endpoints are **Admin-only**, gated by `AuthorizationPolicies.Admin`; the server gate is
  authoritative, the client `AdminRoute` guard is convenience (FR-001, SC-005).
- The admin add **skips** the pending-token / ownership-verification flow used by self-registration
  (spec Assumptions); duplicate handles are blocked before insert (FR-006, SC-004).
- FR-009 is a **distinct** third RSI failure mode (name-not-extractable), separate from not-found
  (FR-007) and unreachable (FR-008).
- Filtering is **client-side**, single combined input matching auth name / character name / role
  (FR-003).
- Friendly role labels are **frontend-only** (FR-011); the API returns raw role names.
- Out of scope (v1): removing/unlinking characters, editing roles, bulk import, activity/login
  history, audit logging (spec Assumptions).

### Verified existing facts (from codebase inspection)

- **RSI client**: `IRsiCitizenClient.FetchCitizenAsync(handle)` returns `object` resolving to
  `RsiCitizenPage(Content, DisplayName)` / `RsiProfileNotFound` / `RsiUnreachable`. Registered via
  `AddHttpClient<IRsiCitizenClient, RsiCitizenClient>` in `Infrastructure/DependencyInjection.cs`.
  Reused as-is; the admin handler treats a `RsiCitizenPage` with blank `DisplayName` as **FR-009**
  (whereas `VerifyCharacterHandler` falls back to the handle).
- **Character repo / entity**: `Character { Id, OwnerUserId, Name(≤100), Handle(≤100), CreatedAt }`,
  table `characters`, FK `owner_user_id → AspNetUsers` (`OnDelete.Cascade`), functional unique index
  `ux_characters_handle_lower`, index `ix_characters_owner_user_id`. `ICharacterRepository` exposes
  `HandleExistsAsync`, `AddAsync` (throws `HandleAlreadyClaimedException` on the unique violation),
  `GetByOwnerAsync`. All reused; **no entity or config change**.
- **Reused exceptions** (`Features/Characters/VerifyCharacter/`): `HandleAlreadyClaimedException`
  (409), `RsiProfileNotFoundException` (404), `RsiUnreachableException` (502). The token-flow
  exceptions (`TokenExpired/TokenNotFound`) are **not** used by the admin path.
- **Identity-join read precedent**: `MaterialInventoryRepository` uses
  `db.Database.SqlQuery<...>` joining `"AspNetUsers"`/`"AspNetUserRoles"`/`"AspNetRoles"` — the users
  list copies this shape, additionally `LEFT JOIN`ing `characters` (empty sets via grouping).
- **`IUserRepository`**: currently `ExistsAsync(userId)` + `GetAllAsync()`. `ExistsAsync` is reused
  for the target-user check; a new `GetUsersWithRolesAndCharactersAsync` read method is added.
- **Admin endpoint precedent**: `ShipAdminEndpoints` maps `app.MapGroup("/api/admin/ships")
  .RequireAuthorization(AuthorizationPolicies.Admin)` and returns `Results.Ok` / `Results.Problem`.
  `AuthorizationPolicies.Admin` = `RequireRole("Admin")`. Endpoint groups are registered in
  `Program.cs` (`app.MapShipAdminEndpoints();` …) — a new `app.MapUserAdminEndpoints();` is added.
  `CharacterEndpoints.VerifyCharacter` is the precedent for mapping each RSI failure to its problem
  status; the admin add mirrors it minus the token cases plus the FR-009 / unknown-user cases.
- **Frontend admin feature**: `features/admin/` already owns `pages/`, `components/`, `hooks/`
  (TanStack Query + key factories), `api/` (`apiFetch` wrappers), `schemas/` (Zod derived from the
  contract), and `__tests__/`. `DataImportPage` + `useImportShips` + `shipKeys.ts` + `shipsApi.ts` +
  `shipSchemas.ts` are the precedent the new page/hooks/api/schemas copy.
- **Navigation + guard**: `navItems.ts` is the single data-driven source (`{ label, path, icon,
  access, group }`); the Admin group already contains a `access: 'admin'` Data Import item. `AdminRoute`
  guards `/dashboard/admin/*` by `session.user.roles.includes('Admin')`. The Users page mounts at
  `/dashboard/admin/users` under `AdminRoute` in `AppRouter.tsx` (spec's `/admin/users` normalized to
  the existing admin nesting — research Decision 7).
- **Zod-from-contract convention**: frontend request/response types are hand-written Zod schemas
  reviewed against `contracts/openapi.yaml`; no codegen tool (established admin/warehouse pattern).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | PASS | Two new admin endpoints defined in `contracts/openapi.yaml` before implementation. Not UI-only (new backend HTTP behaviour), so the exemption clause is correctly **not** invoked. |
| II. Test-First / TDD | PASS | Plan mandates failing tests first: handler unit tests (list grouping incl. empty sets; add happy path + blank/unknown-user/duplicate/not-found/unreachable/no-name); ≥1 Testcontainers integration test (joined read + unique-handle insert); API auth + status/problem tests; frontend table/filter/dialog/redirect tests. |
| III. Frontend/Backend Separation | PASS | SPA consumes only the new `/api/admin/users…` routes via `apiFetch`; RSI scrape is server-side only; no server-rendered HTML, no DB access from the SPA; shapes governed by the OpenAPI contract. **Approved deviation — hand-written Zod schemas**: Constitution Frontend Conventions requires types generated from the OpenAPI contract; this project uses hand-written Zod schemas reviewed against `contracts/openapi.yaml` instead of a codegen tool (`openapi-zod-client` or similar). This is the established pattern across the existing admin and warehouse features and is retained here for consistency. The Zod schemas are manually reviewed against the contract on every PR; any contract change must be accompanied by a matching schema update. If the codebase adopts codegen in a future feature, this deviation should be remediated. |
| IV. Simplicity / YAGNI | PASS | Reuses the RSI client, character repo + exceptions, identity-join read pattern, admin-endpoint pattern, and existing UI primitives wholesale. **No new dependency, no migration.** Friendly-name map is a tiny client-side constant. Out-of-scope items (unlink, role editing, bulk import, audit) explicitly excluded. Only genuinely new code: the admin add handler (token-flow omitted), the users-list read, two new exceptions, and the frontend feature surface. |
| V. Observability | PASS | Both endpoints/handlers emit structured Serilog logs with caller id, target user id, and outcome (mirroring `CharacterEndpoints`). No secrets involved; RSI HTML is not logged at info level — only handle + outcome. |
| VI. Modular Monolith + Clean Architecture | PASS | New use cases in `NajaEcho.Application/Features/Admin/Users/...`; read method on the existing `IUserRepository` port implemented in `NajaEcho.Infrastructure/Identity`; endpoints in `NajaEcho.Api/Features/Admin/Users`. Dependencies point inward only (handlers depend on Application ports; Infrastructure implements them). Frontend logic lives in feature-owned hooks/schemas; the route component stays thin. |

**Result**: PASS — no violations. Complexity Tracking table intentionally empty.

## Project Structure

### Documentation (this feature)

```text
specs/017-admin-users-page/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (no schema change)
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output — two new admin endpoints
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   └── IUserRepository.cs                            # + GetUsersWithRolesAndCharactersAsync (edited)
│   └── Features/Admin/Users/
│       ├── GetUsers/
│       │   ├── GetUsersQuery.cs
│       │   ├── GetUsersHandler.cs                        # roster via IUserRepository read
│       │   ├── AdminUserDto.cs                           # Id, AuthName, Roles[], Characters[]
│       │   └── AdminUserCharacterDto.cs                  # Id, Name, Handle
│       └── AddCharacterForUser/
│           ├── AddCharacterForUserCommand.cs             # TargetUserId, Handle
│           ├── AddCharacterForUserHandler.cs             # reuse IRsiCitizenClient + ICharacterRepository; NO token flow
│           ├── UserNotFoundException.cs                  # new (404)
│           └── CharacterNameUnavailableException.cs      # new (422, FR-009)
│           # reuses HandleAlreadyClaimedException / RsiProfileNotFoundException /
│           # RsiUnreachableException from Features/Characters/VerifyCharacter
├── NajaEcho.Infrastructure/
│   ├── Identity/UserRepository.cs                        # + GetUsersWithRolesAndCharactersAsync (raw-SQL identity join; edited)
│   └── DependencyInjection.cs                            # + GetUsersHandler, AddCharacterForUserHandler (edited)
└── NajaEcho.Api/
    ├── Features/Admin/Users/
    │   ├── UserAdminEndpoints.cs                         # GET /api/admin/users, POST /api/admin/users/{userId}/characters (Admin)
    │   └── Contracts/
    │       ├── AdminUserListResponse.cs                  # + AdminUserResponse, AdminUserCharacterResponse
    │       └── AddCharacterRequest.cs
    └── Program.cs                                        # + app.MapUserAdminEndpoints(); (edited)

backend/tests/
├── NajaEcho.Application.Tests/Features/Admin/Users/      # GetUsers grouping; AddCharacterForUser (happy + all failure modes)
├── NajaEcho.Infrastructure.Tests/Identity/               # Testcontainers: joined roster read + unique-handle admin insert
└── NajaEcho.Api.Tests/Features/Admin/Users/              # 401/403 gates + add status/RFC-7807 mapping

frontend/src/
├── features/admin/
│   ├── pages/AdminUsersPage.tsx                          # thin route: composes filter + table + add dialog
│   ├── components/
│   │   ├── UsersTable.tsx                                # auth name, friendly roles, characters; empty-state cells
│   │   ├── UsersFilter.tsx                               # single input → client-side filter across all three fields
│   │   └── AddCharacterDialog.tsx                        # RHF + Zod; inline blank validation; distinct error messages
│   ├── hooks/
│   │   ├── userKeys.ts                                   # query-key factory
│   │   ├── useAdminUsers.ts                              # roster query
│   │   └── useAddCharacterForUser.ts                     # mutation → invalidate users
│   ├── api/usersApi.ts                                   # apiFetch wrappers (getAdminUsers, addCharacterForUser)
│   ├── schemas/userSchemas.ts                            # Zod derived from contract
│   ├── lib/roleDisplayNames.ts                           # friendly role map (FR-011)
│   └── __tests__/adminUsers.test.tsx                     # table/filter/dialog/error + non-admin redirect
├── features/dashboard/navigation/navItems.ts            # + { label:'Users', path:'/dashboard/admin/users', access:'admin', group:'Admin' } (edited)
└── routes/AppRouter.tsx                                  # + <Route path="/dashboard/admin/users" .../> under <AdminRoute/> (edited)
```

**Structure Decision**: Add a new `Application/Features/Admin/Users` slice (GetUsers +
AddCharacterForUser) and a `NajaEcho.Api/Features/Admin/Users` endpoint group, mirroring the existing
`Features/Admin/Ships` admin slice. The users-list read is added to the existing `IUserRepository`
port (implemented in `Infrastructure/Identity/UserRepository`) following the raw-SQL identity-join
precedent. No new Domain entity and no migration — the feature reuses the `Character` aggregate and
the identity tables. The frontend lives entirely in the existing `features/admin/` folder with a thin
route page mounted under `AdminRoute`, plus one data-driven `navItems` entry. Backend layering
follows the established four-project Clean Architecture split.

## Complexity Tracking

> No constitution violations — table intentionally empty.
