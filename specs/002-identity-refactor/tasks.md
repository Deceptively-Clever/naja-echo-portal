# Tasks: Identity-Backed Authentication Refactor

**Feature**: `002-identity-refactor` | **Branch**: `002-identity-refactor`
**Input**: Design documents from `specs/002-identity-refactor/`
**Prerequisites**: plan.md ✓, spec.md ✓, data-model.md ✓, research.md ✓, contracts/openapi.yaml ✓, quickstart.md ✓

**Testing approach**: TDD — test tasks are marked `⚠️ WRITE FIRST` and MUST be written and confirmed to FAIL before corresponding implementation tasks (Constitution Principle II).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[Story]**: User story label (US1–US5, maps to spec.md)
- Exact file paths are given for every task

---

## Phase 1: Setup

**Purpose**: Add new dependencies and verify project structure before any refactoring begins.

- [X] T001 Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore` NuGet package to `backend/src/NajaEcho.Infrastructure/NajaEcho.Infrastructure.csproj`
- [X] T002 [P] Add `openapi-typescript` as a devDependency and add `"gen:api": "openapi-typescript specs/002-identity-refactor/contracts/openapi.yaml -o src/lib/api/schema.d.ts"` script to `frontend/package.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core Identity plumbing that MUST be complete before any user story endpoint can function.

**⚠️ CRITICAL**: All user story phases depend on this phase completing successfully.

### Retire replaced abstractions

- [X] T003 Remove `IUserRepository.cs` and `IUnitOfWork.cs` from `backend/src/NajaEcho.Application/Abstractions/`
- [X] T004 [P] Remove `UserRepository.cs` and `EfUnitOfWork.cs` from `backend/src/NajaEcho.Infrastructure/Persistence/Repositories/`
- [X] T005 [P] Remove `UserProfile.cs` entity from `backend/src/NajaEcho.Domain/Users/UserProfile.cs`

### Add Application port and DTO

- [X] T006 Create `LocalUser.cs` DTO (`Id: Guid`, `DisplayName: string`, `DiscordUsername: string`) in `backend/src/NajaEcho.Application/Features/Auth/SignInWithDiscord/LocalUser.cs`
- [X] T007 [P] Create `IExternalLoginService.cs` port (`FindOrCreateAsync(DiscordProfile, CancellationToken)`, `GetByIdAsync(Guid, CancellationToken)`) in `backend/src/NajaEcho.Application/Abstractions/IExternalLoginService.cs`

### Add Infrastructure Identity foundation

- [X] T008 Create `ApplicationUser.cs` inheriting `IdentityUser<Guid>` with `DisplayName` (string) and `DiscordUsername` (string) properties in `backend/src/NajaEcho.Infrastructure/Identity/ApplicationUser.cs`
- [X] T009 Update `AppDbContext.cs` to inherit `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` and remove `DbSet<UserProfile>` in `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs`
- [X] T010 Add `ApplicationUserConfiguration.cs` (MaxLength(64) on DisplayName; MaxLength(32) on DiscordUsername; snake_case column names) in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/ApplicationUserConfiguration.cs`

### Migration

- [X] T011 Add EF Core migration `AddIdentitySchema` that creates all Identity tables with snake_case naming and drops `user_profiles` via `dotnet ef migrations add AddIdentitySchema --project backend/src/NajaEcho.Infrastructure --startup-project backend/src/NajaEcho.Api`; review the generated migration file before applying — the `user_profiles` drop is a destructive step requiring PR approval (see plan Complexity Tracking)

### DI and auth configuration

- [X] T012 Update `DependencyInjection.cs` to call `AddIdentityCore<ApplicationUser>`, add EF Core Identity stores, and register `IExternalLoginService → DiscordExternalLoginService` (scoped) in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`
- [X] T013 Update `Program.cs` to: configure `IdentityConstants.ApplicationScheme` cookie (24h `ExpireTimeSpan`; `SlidingExpiration=true`; `OnValidatePrincipal` 7-day absolute-cap check; `__Host-najaecho.auth` name in production / `najaecho.auth` in development; `HttpOnly`; `SameSite=Lax`; `Secure` in production); configure Discord external scheme (`SaveTokens=false`; scope `identify` only; remove the old `OnTicketReceived` principal-rewrite) in `backend/src/NajaEcho.Api/Program.cs`

**Checkpoint**: `dotnet ef database update` applies the Identity schema successfully. The application boots with Identity registered. All user story phases may now begin.

---

## Phase 3: User Story 1 — New User Signs In With Discord (Priority: P1) 🎯 MVP

**Goal**: A first-time Discord user completes the OAuth flow; a new `ApplicationUser` row and one `asp_net_user_logins` row are created; the user receives an active session cookie and is redirected to the dashboard root.

**Independent Test**: Simulate a Discord callback with a previously-unseen Discord user ID; assert one new `asp_net_users` row and one `asp_net_user_logins` row; assert session cookie is set; assert redirect location is `/dashboard` (or dashboard root).

> ⚠️ **TDD**: Write and confirm tests T014–T016 FAIL before writing any implementation code (T017–T021).

### Tests for User Story 1

- [X] T014 [P] [US1] Write failing integration test `FirstTimeLogin_CreatesApplicationUser_AndLinksDiscordProvider` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [X] T015 [P] [US1] Write failing integration test `Callback_WithOAuthError_DoesNotCreateUser_Returns302ToAuthError` — assert response is 302; assert `Location` header starts with `/auth/error?reason=`; assert no `asp_net_users` row was created — in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [X] T016 [P] [US1] Write failing integration test `FirstTimeLogin_SessionCookiePresent_NoDiscordTokenInResponse` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`

### Implementation for User Story 1

- [X] T017 [US1] Create `DiscordExternalLoginService.cs` implementing `IExternalLoginService`: `FindOrCreateAsync` calls `UserManager.FindByLoginAsync("Discord", discordId)` → if not found creates `ApplicationUser` and calls `AddLoginAsync`; if found refreshes stale `DisplayName`/`DiscordUsername`; returns `LocalUser`; `GetByIdAsync` wraps `UserManager.FindByIdAsync` in `backend/src/NajaEcho.Infrastructure/Identity/DiscordExternalLoginService.cs`
- [X] T018 [US1] Refactor `SignInWithDiscordHandler.cs` to call `IExternalLoginService.FindOrCreateAsync` in place of `IUserRepository`/`IUnitOfWork` in `backend/src/NajaEcho.Application/Features/Auth/SignInWithDiscord/SignInWithDiscordHandler.cs`
- [X] T019 [US1] Create `SessionStateResponse.cs` discriminated response type (`AuthenticatedSessionResponse` and `AnonymousSessionResponse`) in `backend/src/NajaEcho.Api/Features/Auth/Contracts/SessionStateResponse.cs`
- [X] T020 [P] [US1] Update `CurrentUserResponse.cs` to remove `avatarUrl` and add `DiscordUsername`; fields: `Id`, `DisplayName`, `DiscordUsername` in `backend/src/NajaEcho.Api/Features/Auth/Contracts/CurrentUserResponse.cs`
- [X] T051 [P] [US1] Update `AuthErrorPage.tsx` to read `reason` query param via React Router's `useSearchParams` and map known values (`oauth_error` → "Authorization was denied or could not be verified.", `state_mismatch` → "The sign-in request could not be verified. Please try again.", `server_error` → "An unexpected error occurred. Please try again.") to user-friendly messages; unknown values fall back to the existing generic message in `frontend/src/features/auth/pages/AuthErrorPage.tsx`
- [X] T021 [US1] Update `AuthEndpoints.cs` to add the real `GET /api/auth/discord/callback` endpoint (call `SignInManager.GetExternalLoginInfoAsync`; dispatch `SignInWithDiscordHandler`; call `SignInManager.SignInAsync`; clear external cookie; redirect to `/dashboard` on success; redirect to `/auth/error?reason=<safe-value>` on failure using `oauth_error`, `state_mismatch`, or `server_error`) in `backend/src/NajaEcho.Api/Features/Auth/AuthEndpoints.cs`

**Checkpoint**: First-time Discord login creates one user, links the provider, sets a session cookie, and redirects to the dashboard. Tests T014–T016 pass.

---

## Phase 4: User Story 2 — Returning User Signs In With Discord (Priority: P1)

**Goal**: A Discord user who already has an `ApplicationUser` account signs in again; no duplicate account is created and the same application identity is returned in the session.

**Independent Test**: Call the callback endpoint twice with the same stubbed Discord user ID; assert `asp_net_users` row count remains 1 after the second call; assert `asp_net_user_logins` row count remains 1.

> ⚠️ **TDD**: Write and confirm tests T022–T023 FAIL before any implementation.

### Tests for User Story 2

- [X] T022 [US2] Write failing integration test `ReturningLogin_ReusesExistingUser_NoDuplicateAccountCreated` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [X] T023 [P] [US2] Write failing integration test `ReturningLogin_UpdatesDisplayName_WhenChangedAtDiscord` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`

### Implementation for User Story 2

- [X] T024 [US2] Verify and refine the find-existing-user branch in `DiscordExternalLoginService.FindOrCreateAsync` (the `UserManager.FindByLoginAsync` non-null path refreshes stale `DisplayName`/`DiscordUsername` and returns the existing `LocalUser` without creating a new row); update `backend/src/NajaEcho.Infrastructure/Identity/DiscordExternalLoginService.cs` if the refresh logic is incomplete from T017

**Checkpoint**: Two logins with the same Discord identity produce exactly one `ApplicationUser`. Tests T022–T023 pass.

---

## Phase 5: User Story 3 — Authenticated User Checks Session State (Priority: P2)

**Goal**: `GET /api/auth/me` always returns `200 OK` with a discriminated body — `{ "authenticated": true, "user": { id, displayName, discordUsername } }` when signed in, or `{ "authenticated": false }` when anonymous. Frontend `useCurrentUser` hook consumes this endpoint correctly via typed generated schema and `authKeys` factory.

**Independent Test**: Call `/api/auth/me` with and without a session cookie; assert HTTP 200 in both cases; assert body matches the discriminated contract; assert no Discord tokens appear.

> ⚠️ **TDD**: Write and confirm tests T025–T028 FAIL before any implementation.

### Tests for User Story 3

- [X] T025 [P] [US3] Write failing integration test `Me_Returns200WithAuthenticatedBody_WhenSessionExists` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [X] T026 [P] [US3] Update existing test `Me_Returns401_WhenUnauthenticated` → rename to `Me_Returns200WithAnonymousBody_WhenNoSession` and update assertion to expect `200` + `{ "authenticated": false }` body in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [X] T027 [P] [US3] Write failing frontend test for authenticated state — MSW handler returns `{ authenticated: true, user: { id, displayName, discordUsername } }` and assert hook exposes correct session data in `frontend/src/features/auth/hooks/useCurrentUser.test.ts`
- [X] T028 [P] [US3] Write failing frontend test for anonymous state — MSW handler returns `{ authenticated: false }` and assert hook exposes unauthenticated discriminant in `frontend/src/features/auth/hooks/useCurrentUser.test.ts`

### Implementation for User Story 3

- [X] T029 [US3] Create `GetCurrentUserQuery.cs` and `GetCurrentUserHandler.cs` (reads user ID from claims, calls `IExternalLoginService.GetByIdAsync`, returns `LocalUser?`) in `backend/src/NajaEcho.Application/Features/Auth/GetCurrentUser/`
- [X] T030 [US3] Update `GET /api/auth/me` in `AuthEndpoints.cs` to be `AllowAnonymous`, always return `200` with `SessionStateResponse` (authenticated with `CurrentUserResponse` when session exists; anonymous body when not) in `backend/src/NajaEcho.Api/Features/Auth/AuthEndpoints.cs`
- [X] T031 [US3] Run `npm run gen:api` from `frontend/` to generate `schema.d.ts` from the v0.2.0 contract; commit the output to `frontend/src/lib/api/schema.d.ts`
- [X] T032 [P] [US3] Create typed `authKeys` query-key factory (`authKeys.me()` etc.) in `frontend/src/features/auth/hooks/authKeys.ts`
- [X] T033 [P] [US3] Create `sessionStateSchema.ts` Zod runtime guard for the discriminated `SessionState` union (typed to conform to generated `schema.d.ts`) in `frontend/src/features/auth/schemas/sessionStateSchema.ts`
- [X] T034 [US3] Update `authApi.ts` to type the `/api/auth/me` response as `SessionState` using the generated schema and parse with `sessionStateSchema` (remove hand-written `CurrentUserSchema` DTO) in `frontend/src/features/auth/api/authApi.ts`
- [X] T035 [US3] Update `useCurrentUser.ts` to use `authKeys.me()` as the query key and consume the `SessionState` discriminant in `frontend/src/features/auth/hooks/useCurrentUser.ts`

**Checkpoint**: `/api/auth/me` always returns 200 with the correct discriminated body. Frontend hook correctly reflects both authenticated and anonymous states. Tests T025–T028 pass.

---

## Phase 6: User Story 4 — Authenticated User Signs Out (Priority: P2)

**Goal**: `POST /api/auth/signout` is idempotent — clears the Identity session and expires the cookie whether or not a session is active. The frontend sign-out hook invalidates the auth query and routes to the unauthenticated landing.

**Independent Test**: Sign in; call `POST /api/auth/signout`; assert `204`; call `GET /api/auth/me`; assert `{ "authenticated": false }`. Then call `POST /api/auth/signout` again without a session; assert `204` (idempotent).

> ⚠️ **TDD**: Write and confirm tests T036–T038 FAIL before any implementation.

### Tests for User Story 4

- [X] T036 [P] [US4] Write failing integration test `SignOut_ClearsSession_Returns204_SubsequentMeReturnsAnonymous` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [X] T037 [P] [US4] Write failing integration test `SignOut_WithNoSession_Returns204_Idempotent` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [X] T038 [P] [US4] Write failing frontend test for sign-out — MSW; assert auth query is invalidated after sign-out and navigation goes to unauthenticated state in `frontend/src/features/auth/hooks/useSignOut.test.ts`

### Implementation for User Story 4

- [X] T039 [US4] Update `POST /api/auth/signout` in `AuthEndpoints.cs` to remove `RequireAuthorization`, call `SignInManager.SignOutAsync`, and return `204` in all cases (no session is not an error) in `backend/src/NajaEcho.Api/Features/Auth/AuthEndpoints.cs`
- [X] T040 [US4] Update `useSignOut.ts` to invalidate `authKeys.me()` on success and navigate to the unauthenticated landing in `frontend/src/features/auth/hooks/useSignOut.ts`

**Checkpoint**: Sign-out clears the session and returns 204 for both signed-in and anonymous callers. Frontend reflects the post-sign-out unauthenticated state. Tests T036–T038 pass.

---

## Phase 7: User Story 5 — Authenticated Frontend Routes Remain Protected (Priority: P2)

**Goal**: `ProtectedRoute.tsx` reads the `authenticated` discriminant from the `SessionState` response (not `!user`) so the auth guard works correctly with the new discriminated contract.

**Independent Test**: Render `<ProtectedRoute>` with MSW returning `{ "authenticated": false }`; assert redirect to sign-in. Render with `{ "authenticated": true, "user": { ... } }`; assert protected content renders.

> ⚠️ **TDD**: Write and confirm tests T041–T042 FAIL before implementation.

### Tests for User Story 5

- [X] T041 [P] [US5] Write failing frontend test for unauthenticated redirect — MSW returns `{ authenticated: false }`; assert `<ProtectedRoute>` redirects to sign-in in `frontend/src/features/auth/ProtectedRoute.test.tsx`
- [X] T042 [P] [US5] Write failing frontend test for authenticated render — MSW returns `{ authenticated: true, user: { ... } }`; assert protected content is rendered in `frontend/src/features/auth/ProtectedRoute.test.tsx`

### Implementation for User Story 5

- [X] T043 [US5] Update `ProtectedRoute.tsx` to read `session.authenticated === true` (using the `SessionState` discriminant from `useCurrentUser`) in place of `!user` null-check in `frontend/src/features/auth/ProtectedRoute.tsx`

**Checkpoint**: Auth guard blocks unauthenticated access and passes authenticated users through. Tests T041–T042 pass.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Observability, log-scrubbing verification, security hardening, dead-code cleanup, and full suite validation.

- [X] T044 Add structured Serilog log milestones (login started, external login received, local user created/linked/found, sign-in succeeded, sign-out completed, auth failure with safe non-sensitive reason) to `backend/src/NajaEcho.Api/Features/Auth/AuthEndpoints.cs` and `backend/src/NajaEcho.Infrastructure/Identity/DiscordExternalLoginService.cs`
- [X] T045 [P] Write integration test `AuthFlow_DoesNotEmitSensitiveValues_InLogs` (assert OAuth code, state, access token, Authorization header, and cookie value are absent from captured Serilog output) in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [X] T046 [P] Review `IDiscordOAuthClient.cs` and `DiscordOAuthClient.cs` for usage after the refactor; remove both if unused in `backend/src/NajaEcho.Application/Abstractions/IDiscordOAuthClient.cs` and `backend/src/NajaEcho.Infrastructure/Discord/DiscordOAuthClient.cs`
- [X] T047 [P] Verify `__Host-` cookie prefix activates only when `ASPNETCORE_ENVIRONMENT=Production` (check the environment-conditional cookie name in `Program.cs`) in `backend/src/NajaEcho.Api/Program.cs`
- [X] T048 [P] Run full backend test suite and confirm all tests pass: `cd backend && dotnet test`
- [X] T049 [P] Run full frontend test suite and confirm all tests pass: `cd frontend && npm run test:run`
- [ ] T050 Run quickstart.md Scenarios A–G manually (or via end-to-end harness) to validate the full auth flow end-to-end per `specs/002-identity-refactor/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1. **BLOCKS all user story phases.**
- **US1 (Phase 3)**: Depends on Foundational. First P1 story — begin here after Phase 2.
- **US2 (Phase 4)**: Depends on Phase 3. `DiscordExternalLoginService` from T017 is the shared implementation; US2 tests can be written in parallel with US1 implementation.
- **US3 (Phase 5)**: Depends on Foundational. Can start test-writing (T025–T028) in parallel with US1/US2 if working with multiple agents.
- **US4 (Phase 6)**: Depends on Foundational. Independent of US1/US2/US3; shares `AuthEndpoints.cs` file but touches only the signout endpoint.
- **US5 (Phase 7)**: Depends on US3 (`SessionState` type from T033/T035 must exist before updating `ProtectedRoute.tsx`).
- **Polish (Final)**: Depends on all user story phases complete.

### Cross-story dependencies (detailed)

- **US2 → US1**: The find-existing-user path in `DiscordExternalLoginService` is part of the same `FindOrCreateAsync` implementation begun in T017. US2 verifies that path — it does not introduce new files.
- **US5 → US3**: `ProtectedRoute.tsx` reads `session.authenticated` from the `SessionState` union type introduced in T033/T035.
- **US4**: Fully independent — `POST /api/auth/signout` changes do not affect any other user story's code path.

### Within each phase

- Retire tasks T003–T005 are independent files — run in parallel.
- Port tasks T006–T007 are independent — run in parallel.
- Identity foundation tasks T008 → T009 → T010 are sequential (ApplicationUser must exist before AppDbContext references it).
- T011 (migration) must follow T009/T010.
- T012 (DI registration) must follow T008.
- T013 (Program.cs auth config) depends on T012.
- All test-writing tasks within a phase marked [P] are safe to write in parallel (different test method names in the same file — coordinate naming to avoid merge conflicts).

### Parallel opportunities (summary)

| Group | Tasks |
|-------|-------|
| Retire old code | T003, T004, T005 |
| Application port + DTO | T006, T007 |
| US1 failing tests | T014, T015, T016 |
| US3 failing tests | T025, T026, T027, T028 |
| US3 frontend scaffolding (after T031) | T032, T033 |
| US4 failing tests | T036, T037, T038 |
| US5 failing tests | T041, T042 |
| Polish (different files) | T044, T045, T046, T047 |
| Final test runs | T048, T049 |

---

## Parallel Execution Example: Phase 2 (Foundational)

```bash
# Step 1 — retire old code in parallel:
Task A: Remove IUserRepository.cs and IUnitOfWork.cs
Task B: Remove UserRepository.cs and EfUnitOfWork.cs
Task C: Remove UserProfile.cs

# Step 2 — add Application port + DTO in parallel:
Task D: Create LocalUser.cs
Task E: Create IExternalLoginService.cs

# Step 3 — Identity foundation (sequential):
T008 (ApplicationUser.cs) → T009 (AppDbContext.cs) → T010 (ApplicationUserConfiguration.cs)
  → T011 (migration) → T012 (DI) → T013 (Program.cs)
```

## Parallel Execution Example: User Story 3 (Session Check)

```bash
# Step 1 — failing tests in parallel (write first!):
Task: Write Me_Returns200WithAuthenticatedBody_WhenSessionExists
Task: Rename/rewrite Me_Returns401_WhenUnauthenticated
Task: Write frontend test for authenticated state
Task: Write frontend test for anonymous state

# Step 2 — backend implementation:
T029 (GetCurrentUserHandler) → T030 (update me endpoint)

# Step 3 — generate types + frontend scaffolding:
T031 (gen:api → schema.d.ts)
  → T032 (authKeys.ts) ║ T033 (sessionStateSchema.ts)  [parallel]
    → T034 (authApi.ts) → T035 (useCurrentUser.ts)
```

---

## Implementation Strategy

### MVP First (P1 user stories: US1 + US2 only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks everything)
3. Complete Phase 3: US1 — New User Signs In
4. Complete Phase 4: US2 — Returning User Signs In
5. **STOP and VALIDATE**: T014–T016 + T022–T023 pass; run Scenarios A + B from quickstart.md
6. Deploy / demo if ready

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready; database schema applied
2. Phase 3 + 4 → Discord sign-in fully functional (MVP)
3. Phase 5 → Session probe correct; frontend auth hook updated
4. Phase 6 → Sign-out idempotent and correct
5. Phase 7 → Frontend route guard updated
6. Final Phase → Observability, dead-code cleanup, full validation

### Single-developer sequence (recommended)

Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 → Phase 7 → Final Phase

---

## Notes

- `[P]` marks tasks that touch different files and have no incomplete dependencies — safe to parallelize within that phase.
- `[US#]` provides full traceability back to spec.md user stories.
- Tests MUST fail before implementation — do not skip the Red phase (Constitution Principle II).
- Commit after each checkpoint to keep the branch in a known-good state.
- The `user_profiles` drop in T011 is a **destructive migration step** — include explicit sign-off in the PR description per the Development Workflow rule.
- Do not apply `dotnet ef database update` until the generated migration file has been reviewed (T011 produces a `.cs` file to read before running).
- `schema.d.ts` (T031) is generated output — do not hand-edit after generation; run `gen:api` again if the contract changes.
- `IDiscordOAuthClient` (T046) may or may not be dead code after the refactor; verify before deleting.
