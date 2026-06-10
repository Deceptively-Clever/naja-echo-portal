---
description: "Task list for Discord Authentication (001-discord-auth)"
---

# Tasks: Discord Authentication

**Input**: Design documents from `/specs/001-discord-auth/`

**Prerequisites**: plan.md ✅ | spec.md ✅ | data-model.md ✅ | contracts/openapi.yaml ✅ | research.md ✅ | quickstart.md ✅

**Tests**: Included — TDD is NON-NEGOTIABLE per constitution Principle II. Tests MUST be written
and confirmed to fail before implementation code is written (Red-Green-Refactor).

**Organization**: Tasks are grouped by user story (US1–US5) to enable independent implementation
and testing of each story. All test tasks within a phase come before implementation tasks.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no shared incomplete dependency)
- **[Story]**: Which user story this task belongs to (US1–US5)
- File paths use project-relative notation from the repository root

---

## Phase 1: Setup

**Purpose**: Scaffold both projects, configure tooling, and verify the dev environment boots.
No feature logic yet.

- [x] T001 Create .NET 8 solution `NajaEcho.sln` with four class-library/web-api projects under `backend/src/`: `NajaEcho.Domain`, `NajaEcho.Application`, `NajaEcho.Infrastructure`, `NajaEcho.Api`
- [x] T002 Add project references enforcing Clean Architecture dependency direction: Api→Application, Api→Infrastructure (DI only), Infrastructure→Application, Infrastructure→Domain, Application→Domain in `backend/NajaEcho.sln`
- [x] T003 [P] Create four xUnit test projects (`NajaEcho.Domain.Tests`, `NajaEcho.Application.Tests`, `NajaEcho.Infrastructure.Tests`, `NajaEcho.Api.Tests`) under `backend/tests/` and add to solution
- [x] T004 [P] Scaffold React + TypeScript frontend project with Vite 5 in `frontend/` (`npm create vite@latest frontend -- --template react-ts`)
- [x] T005 [P] Configure Tailwind CSS 3 in `frontend/tailwind.config.ts` and `frontend/src/index.css`
- [x] T006 [P] Initialize shadcn/ui CLI and generate `Button`, `Card`, `Avatar`, `Alert` components into `frontend/src/components/ui/`
- [x] T007 [P] Configure Vite dev proxy (`/api` → `http://localhost:5080`) in `frontend/vite.config.ts`
- [x] T008 [P] Configure Vitest with React Testing Library and MSW in `frontend/vite.config.ts` and `frontend/src/tests/setup.ts`
- [x] T009 [P] Create `docker-compose.yml` at repo root with a PostgreSQL 16 service (port 5432, db/user/password: `najaecho`)
- [x] T010 [P] Add `.gitignore` entries for `secrets.json` (dotnet user-secrets), `.env*`, `frontend/node_modules`, and `backend/**/bin`, `backend/**/obj`

**Checkpoint**: `dotnet build backend/NajaEcho.sln` succeeds; `cd frontend && npm run dev` starts without errors; `docker compose up -d postgres` starts the database.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain entities, application abstractions, EF Core persistence layer, and the
ASP.NET Core host configuration that every user story depends on.

**⚠️ CRITICAL**: No user story implementation can begin until this phase is complete.

- [x] T011 Create `UserProfile` entity with `CreateFromDiscord(DiscordProfile, IClock)` factory and `RecordLogin(DiscordProfile, IClock)` mutation stub in `backend/src/NajaEcho.Domain/Users/UserProfile.cs`
- [x] T012 [P] Create `DiscordProfile` value object (`Id`, `Username`, `GlobalName`, `Avatar`, `Email`, `Verified`) and derived `DisplayName`/admissible-email rules in `backend/src/NajaEcho.Domain/Users/DiscordProfile.cs`
- [x] T013 [P] Create `IUserRepository` interface (`FindByDiscordUserIdAsync`, `FindByIdAsync`, `AddAsync`) in `backend/src/NajaEcho.Application/Abstractions/IUserRepository.cs`
- [x] T014 [P] Create `IDiscordOAuthClient` interface (`GetUserProfileAsync`) in `backend/src/NajaEcho.Application/Abstractions/IDiscordOAuthClient.cs`
- [x] T015 [P] Create `IClock` interface (`UtcNow`) and `IUnitOfWork` interface (`SaveChangesAsync`) in `backend/src/NajaEcho.Application/Abstractions/`
- [x] T016 Create `AppDbContext` with `UserProfile` `DbSet` and `UseSnakeCaseNamingConvention()` in `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs`
- [x] T017 Create `UserProfileConfiguration` (table `user_profiles`, column types, NOT NULL constraints, unique index on `discord_user_id`) in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/UserProfileConfiguration.cs`
- [x] T018 Run `dotnet ef migrations add InitialCreate` to generate the initial migration file under `backend/src/NajaEcho.Infrastructure/Persistence/Migrations/`
- [x] T019 Implement `UserRepository` (`FindByDiscordUserIdAsync`, `FindByIdAsync`, `AddAsync`) using `AppDbContext` in `backend/src/NajaEcho.Infrastructure/Persistence/Repositories/UserRepository.cs`
- [x] T020 [P] Implement `SystemClock` (`IClock`) and `EfUnitOfWork` (`IUnitOfWork`) adapters in `backend/src/NajaEcho.Infrastructure/Persistence/SystemClock.cs` and `EfUnitOfWork.cs`
- [x] T021 Register all Infrastructure services (Npgsql `AppDbContext`, `UserRepository`, `DiscordOAuthClient`, `SystemClock`, `EfUnitOfWork`) in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`
- [x] T022 Configure Serilog JSON sink to stdout with `UseSerilogRequestLogging()` and a destructuring policy that scrubs `code`, `state`, `access_token`, `refresh_token`, `Cookie`, and `Authorization` fields in `backend/src/NajaEcho.Api/Program.cs`
- [x] T023 Configure ASP.NET Core cookie authentication (14-day sliding, `HttpOnly`, `Secure` in prod, `SameSite=Lax`) and `AspNet.Security.OAuth.Discord` handler (scopes: `identify email`) in `backend/src/NajaEcho.Api/Program.cs`
- [x] T024 Add global ProblemDetails exception handler that returns RFC 7807 JSON on unhandled exceptions — no stack traces or internal messages in user-facing output — in `backend/src/NajaEcho.Api/Common/GlobalExceptionHandler.cs` and wire in `Program.cs`
- [x] T025 Add `GET /api/health` minimal API endpoint returning `{ "status": "ok" }` (200) in `backend/src/NajaEcho.Api/Program.cs`
- [x] T026 [P] Create `frontend/src/lib/apiClient.ts` with a base `apiFetch` wrapper (`credentials: 'include'`, `Content-Type: application/json`, throws typed `ApiError` on non-2xx)
- [x] T027 [P] Create `frontend/src/lib/queryClient.ts` with a `QueryClient` instance (`staleTime: 0`, `retry: (count, err) => count < 1 && err.status !== 401`)

**Checkpoint**: `dotnet ef database update` applies migration without errors; `GET http://localhost:5080/api/health` returns `{"status":"ok"}`.

---

## Phase 3: User Story 1 — New User Signs In (Priority: P1) 🎯 MVP

**Goal**: A visitor on the landing page clicks "Sign in with Discord," completes the OAuth flow,
lands on the dashboard with their display name and avatar, and has a new local user profile in the
database.

**Independent Test**: Navigate to `/` as a visitor, click the sign-in button, complete Discord
authorization, and verify the dashboard shows the correct display name and avatar. Confirm exactly
one `user_profiles` row exists for the Discord account. Run `GET /api/auth/me` with the session
cookie and verify a 200 `CurrentUser` JSON response.

### Tests for User Story 1 ⚠️ Write these first — verify they FAIL before implementing

- [x] T028 [P] [US1] Write `UserProfileTests`: `CreateFromDiscord` sets all fields correctly; `DiscordProfile.DisplayName` prefers `GlobalName` over `Username`; email is stored only when verified in `backend/tests/NajaEcho.Domain.Tests/Users/UserProfileTests.cs`
- [x] T029 [P] [US1] Write `SignInWithDiscordHandlerTests` (new-user path): fake `IUserRepository` returns `null` → handler calls `CreateFromDiscord`, calls `AddAsync` and `SaveChangesAsync`, returns new user ID in `backend/tests/NajaEcho.Application.Tests/Features/Auth/SignInWithDiscordHandlerTests.cs`
- [x] T030 [P] [US1] Write `UserRepositoryTests` with Testcontainers PostgreSQL: `FindByDiscordUserIdAsync` returns `null` for unknown ID; `AddAsync` + `SaveChangesAsync` persists a row retrievable by `FindByDiscordUserIdAsync` in `backend/tests/NajaEcho.Infrastructure.Tests/Persistence/UserRepositoryTests.cs`
- [x] T031 [P] [US1] Write `AuthEndpointsTests` (login redirect): `GET /api/auth/discord/login` returns 302 with `Location` beginning with `https://discord.com/oauth2/authorize` and sets the OAuth correlation cookie in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T032 [P] [US1] Write `AuthEndpointsTests` (callback success): with a fake Discord OAuth scheme returning canned claims, the callback creates a `user_profiles` row, sets the session cookie, and returns 302 to `/dashboard` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T033 [P] [US1] Write `AuthEndpointsTests` (me endpoint): `GET /api/auth/me` with a valid session cookie returns 200 with `CurrentUser` JSON containing `id`, `displayName`, and optional `avatarUrl` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T034 [P] [US1] Write Vitest test: `useCurrentUser` returns user data when `GET /api/auth/me` responds 200 (MSW handler); returns `undefined` and does not retry on 401 in `frontend/src/features/auth/hooks/useCurrentUser.test.ts`
- [x] T035 [P] [US1] Write Vitest test: `LandingPage` renders a heading and a "Sign in with Discord" button; the button navigates to `/api/auth/discord/login` in `frontend/src/features/auth/pages/LandingPage.test.tsx`
- [x] T036 [P] [US1] Write Vitest test: `DashboardPage` displays `displayName` text and `Avatar` with the user's `avatarUrl`; shows fallback initial when `avatarUrl` is null — both cases backed by MSW mock of `GET /api/auth/me` in `frontend/src/features/dashboard/pages/DashboardPage.test.tsx`

### Implementation for User Story 1

- [x] T037 [US1] Implement `SignInWithDiscordCommand` (input: Discord callback context), `SignInWithDiscordResult` DTO (output: `UserId`, `DisplayName`), and `SignInWithDiscordHandler` (create-new-user path only; upsert added in US2) in `backend/src/NajaEcho.Application/Features/Auth/SignInWithDiscord/`
- [x] T038 [US1] Implement `GetCurrentUserQuery` (input: `userId` Guid), `CurrentUserDto` (output: `Id`, `DisplayName`, `AvatarRef`), and `GetCurrentUserHandler` (reads `sub` claim, calls `FindByIdAsync`) in `backend/src/NajaEcho.Application/Features/Auth/GetCurrentUser/`
- [x] T039 [US1] Implement `DiscordOAuthClient.GetUserProfileAsync` (reads Discord identity claims already resolved by the OAuth handler, maps to `DiscordProfile`) in `backend/src/NajaEcho.Infrastructure/Discord/DiscordOAuthClient.cs`
- [x] T040 [US1] Implement `GET /api/auth/discord/login` endpoint (trigger `ChallengeAsync` with Discord scheme) and `GET /api/auth/discord/callback` endpoint (invoke `SignInWithDiscordHandler`, call `SignInAsync` with user claims, redirect to `/dashboard`) in `backend/src/NajaEcho.Api/Features/Auth/AuthEndpoints.cs`
- [x] T041 [US1] Implement `GET /api/auth/me` endpoint with `[Authorize]` and configure the authentication challenge to return 401 JSON (not a redirect) for API routes; add `CurrentUserResponse` DTO with resolved `avatarUrl` construction in `backend/src/NajaEcho.Api/Features/Auth/AuthEndpoints.cs` and `backend/src/NajaEcho.Api/Features/Auth/Contracts/CurrentUserResponse.cs`
- [x] T042 [P] [US1] Create Zod schema `CurrentUserSchema` (id: uuid, displayName: string, avatarUrl: string nullable) in `frontend/src/features/auth/schemas/currentUserSchema.ts`
- [x] T043 [P] [US1] Implement `authApi.ts`: `getSignInUrl()` returns `'/api/auth/discord/login'`; `getCurrentUser()` calls `GET /api/auth/me` and validates response with `CurrentUserSchema` in `frontend/src/features/auth/api/authApi.ts`
- [x] T044 [US1] Implement `useCurrentUser` hook (`useQuery({ queryKey: ['auth','me'], queryFn: getCurrentUser, retry: ... })`) in `frontend/src/features/auth/hooks/useCurrentUser.ts`
- [x] T045 [US1] Create `SignInButton` component (renders as an anchor `href={getSignInUrl()}` styled with `Button` from shadcn/ui) in `frontend/src/features/auth/components/SignInButton.tsx`
- [x] T046 [US1] Create `LandingPage` with application heading, short description, and `SignInButton` in `frontend/src/features/auth/pages/LandingPage.tsx`
- [x] T047 [US1] Create `UserBadge` component (`Avatar` with `avatarUrl` or fallback first-initial + `displayName` text) in `frontend/src/features/dashboard/components/UserBadge.tsx`
- [x] T048 [US1] Create `DashboardPage` displaying `UserBadge` from `useCurrentUser` with a sign-out placeholder section in `frontend/src/features/dashboard/pages/DashboardPage.tsx`
- [x] T049 [US1] Create `AppRouter` with `BrowserRouter`, public `/` route (`LandingPage`), and `/dashboard` route (`DashboardPage`, unprotected placeholder until US4) in `frontend/src/routes/AppRouter.tsx`
- [x] T050 [US1] Wire `App.tsx` to render `AppRouter` inside `QueryClientProvider` in `frontend/src/App.tsx`

**Checkpoint**: All US1 tests are green. Run quickstart.md scenario US1 manually and verify new visitor reaches dashboard with name and avatar; confirm exactly one `user_profiles` row in the database.

---

## Phase 4: User Story 2 — Returning User, No Duplicate (Priority: P2)

**Goal**: A returning visitor signs in with the same Discord account without creating a duplicate
local profile. Changed Discord profile fields are updated on re-login.

**Independent Test**: Sign in twice with the same Discord account. Query `user_profiles` and
confirm one row; `last_login_at_utc` advances on the second login. Simulate a display-name change
between logins and verify `display_name` updates.

### Tests for User Story 2 ⚠️ Write these first — verify they FAIL before implementing

- [x] T051 [P] [US2] Write `SignInWithDiscordHandlerTests` (update path): fake repository returns an existing `UserProfile` → handler calls `RecordLogin`, does NOT call `AddAsync`, calls `SaveChangesAsync` in `backend/tests/NajaEcho.Application.Tests/Features/Auth/SignInWithDiscordHandlerTests.cs`
- [x] T052 [P] [US2] Write `UserProfileTests` (`RecordLogin`): `LastLoginAtUtc` always advances; `DisplayName`, `AvatarRef`, `Email` update only when changed; `LastUpdatedAtUtc` advances only when a non-login field changes in `backend/tests/NajaEcho.Domain.Tests/Users/UserProfileTests.cs`
- [x] T053 [P] [US2] Write `AuthEndpointsTests` (idempotent callback): two callbacks with the same `DiscordUserId` produce exactly one `user_profiles` row; second callback advances `last_login_at_utc` in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`

### Implementation for User Story 2

- [x] T054 [US2] Implement `UserProfile.RecordLogin(DiscordProfile, IClock)`: advance `LastLoginAtUtc`; if `DisplayName`, `AvatarRef`, or `Email` differ from current values update them and advance `LastUpdatedAtUtc` in `backend/src/NajaEcho.Domain/Users/UserProfile.cs`
- [x] T055 [US2] Extend `SignInWithDiscordHandler` to upsert: call `FindByDiscordUserIdAsync`; if `null` → create new (US1 path); if found → call `RecordLogin`; always call `SaveChangesAsync` in `backend/src/NajaEcho.Application/Features/Auth/SignInWithDiscord/SignInWithDiscordHandler.cs`

**Checkpoint**: All US2 tests are green. Run quickstart.md scenario US2 and verify one row, updated timestamps, and updated display name after a simulated profile change.

---

## Phase 5: User Story 3 — User Signs Out (Priority: P3)

**Goal**: An authenticated user clicks sign out, their session is cleared, and they are returned to
the landing page. They can no longer access the dashboard.

**Independent Test**: Sign in, navigate to the dashboard, click sign out, verify redirect to `/`,
then confirm `GET /api/auth/me` returns 401 and `GET /dashboard` redirects to `/`.

### Tests for User Story 3 ⚠️ Write these first — verify they FAIL before implementing

- [x] T056 [P] [US3] Write `AuthEndpointsTests` (signout with session): `POST /api/auth/signout` with a valid session cookie returns 204 and the response `Set-Cookie` header expires the session cookie in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T057 [P] [US3] Write `AuthEndpointsTests` (signout without session): `POST /api/auth/signout` without a session cookie returns 204 (idempotent — no error) in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T058 [P] [US3] Write Vitest test: `useSignOut` mutation calls `POST /api/auth/signout` (MSW), on success invalidates `['auth','me']` query and navigates to `/` in `frontend/src/features/auth/hooks/useSignOut.test.ts`
- [x] T059 [P] [US3] Write Vitest test: `DashboardPage` renders a sign-out button; clicking it triggers `useSignOut` in `frontend/src/features/dashboard/pages/DashboardPage.test.tsx`

### Implementation for User Story 3

- [x] T060 [US3] Implement `SignOutCommand` and `SignOutHandler` (calls `HttpContext.SignOutAsync` for both cookie and Discord schemes; no-op if not authenticated) in `backend/src/NajaEcho.Application/Features/Auth/SignOut/`
- [x] T061 [US3] Add `POST /api/auth/signout` endpoint (allow anonymous, invoke `SignOutHandler`, return 204) to `backend/src/NajaEcho.Api/Features/Auth/AuthEndpoints.cs`
- [x] T062 [P] [US3] Add `signOut()` function (`POST /api/auth/signout`) to `frontend/src/features/auth/api/authApi.ts`
- [x] T063 [US3] Implement `useSignOut` hook (`useMutation({ mutationFn: signOut, onSuccess: () => { queryClient.invalidateQueries(['auth','me']); navigate('/') } }`) in `frontend/src/features/auth/hooks/useSignOut.ts`
- [x] T064 [US3] Create `SignOutButton` component (calls `useSignOut().mutate()` on click) in `frontend/src/features/auth/components/SignOutButton.tsx`
- [x] T065 [US3] Add `SignOutButton` to `DashboardPage` and wire `useCurrentUser` loading/error states in `frontend/src/features/dashboard/pages/DashboardPage.tsx`

**Checkpoint**: All US3 tests are green. Run quickstart.md scenario US3 and verify sign-out clears session and `/dashboard` becomes inaccessible.

---

## Phase 6: User Story 4 — Unauthenticated Access Blocked (Priority: P4)

**Goal**: A visitor without a session is always redirected to `/` when they attempt to access a
protected page directly. The API returns 401 JSON (not a redirect) for protected endpoints.

**Independent Test**: Without signing in, navigate to `/dashboard` in the browser — confirm
redirect to `/`. Run `curl -i http://localhost:5080/api/auth/me` without a cookie — confirm 401
`application/problem+json`.

### Tests for User Story 4 ⚠️ Write these first — verify they FAIL before implementing

- [x] T066 [P] [US4] Write `AuthEndpointsTests` (me endpoint unauthenticated): `GET /api/auth/me` without a session cookie returns 401 with `Content-Type: application/problem+json` — not a 302 redirect in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T067 [P] [US4] Write Vitest test: `ProtectedRoute` renders children when `useCurrentUser` returns a user; renders `<Navigate to="/" />` when `useCurrentUser` returns `undefined` and is not loading in `frontend/src/features/auth/ProtectedRoute.test.tsx`
- [x] T068 [P] [US4] Write Vitest test: `AppRouter` — navigating to `/dashboard` while `useCurrentUser` returns `undefined` (MSW returns 401) renders the `LandingPage` in `frontend/src/routes/AppRouter.test.tsx`

### Implementation for User Story 4

- [x] T069 [US4] Confirm `[Authorize]` attribute is on `GET /api/auth/me` and configure `OnRedirectToLogin` / `OnRedirectToAccessDenied` cookie events to return 401 JSON for requests with `Accept: application/json` or path starting with `/api` in `backend/src/NajaEcho.Api/Program.cs`
- [x] T070 [US4] Create `ProtectedRoute` component: reads `useCurrentUser`; renders `<Outlet />` (or `children`) when authenticated; renders `<Navigate to="/" replace />` when not loading and user is absent in `frontend/src/features/auth/ProtectedRoute.tsx`
- [x] T071 [US4] Wrap `/dashboard` route with `ProtectedRoute` in `frontend/src/routes/AppRouter.tsx`

**Checkpoint**: All US4 tests are green. Run quickstart.md scenario US4 and verify unauthenticated browser and curl behaviors.

---

## Phase 7: User Story 5 — Auth Failure / Cancellation (Priority: P5)

**Goal**: When Discord authorization fails (denied, cancelled, error, forged state), the visitor
reaches a friendly error page with a retry option. No partial user profile is created.

**Independent Test**: Deny authorization on Discord's consent screen. Verify the frontend shows
a user-friendly error message and a "Try again" link. Verify no new `user_profiles` row was
created in the database.

### Tests for User Story 5 ⚠️ Write these first — verify they FAIL before implementing

- [x] T072 [P] [US5] Write `AuthEndpointsTests` (callback with error param): `GET /api/auth/discord/callback?error=access_denied&state=...` returns 302 to `/auth/error?reason=access_denied`; no `user_profiles` row is created in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T073 [P] [US5] Write `AuthEndpointsTests` (forged state): callback with a state param that does not match the correlation cookie is rejected by the OAuth handler and redirected to the error page; no user row created in `backend/tests/NajaEcho.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T074 [P] [US5] Write Vitest test: `AuthErrorPage` renders a user-friendly message (no internal details) and a "Try again" link pointing to `/` in `frontend/src/features/auth/pages/AuthErrorPage.test.tsx`
- [x] T075 [P] [US5] Write Vitest test: `AuthCallbackPage` renders a loading spinner and no user content in `frontend/src/features/auth/pages/AuthCallbackPage.test.tsx`

### Implementation for User Story 5

- [x] T076 [US5] Add error-handling branch in `GET /api/auth/discord/callback`: detect `error` query param (or OAuth handler failure event `OnRemoteFailure`); redirect to `/auth/error?reason=access_denied` (or generic `auth_error`); abort before invoking `SignInWithDiscordHandler` in `backend/src/NajaEcho.Api/Features/Auth/AuthEndpoints.cs`
- [x] T077 [US5] Create `AuthCallbackPage` (shows a loading/spinner state; the browser is only here briefly during the OAuth redirect round-trip) in `frontend/src/features/auth/pages/AuthCallbackPage.tsx`
- [x] T078 [US5] Create `AuthErrorPage` with user-friendly heading ("Sign-in could not be completed"), generic explanation, and a "Try again" `Button` linking to `/` — no `reason` param rendered to the user in `frontend/src/features/auth/pages/AuthErrorPage.tsx`
- [x] T079 [US5] Add `/auth/callback` (AuthCallbackPage) and `/auth/error` (AuthErrorPage) routes to `AppRouter` in `frontend/src/routes/AppRouter.tsx`

**Checkpoint**: All US5 tests are green. Run quickstart.md scenario US5: deny auth on Discord, confirm the error page appears, confirm no DB row was created.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Accessibility, CORS, end-to-end test coverage confirmation, and cleanup.

- [x] T080 [P] Add CORS policy in `backend/src/NajaEcho.Api/Program.cs` allowing the Vite dev origin (`http://localhost:5173`) with credentials; configure a named policy and apply to all API routes
- [x] T081 [P] Add `aria-label` attributes and keyboard accessibility (`role`, `tabIndex`) to `SignInButton`, `SignOutButton`, and `UserBadge` in `frontend/src/features/auth/components/` and `frontend/src/features/dashboard/components/`
- [x] T082 [P] Create MSW handlers file `frontend/src/tests/handlers.ts` exporting stubs for all five auth endpoints (`/api/auth/discord/login`, `/callback`, `/api/auth/signout`, `/api/auth/me` authenticated, `/api/auth/me` unauthenticated) for reuse across all frontend tests
- [x] T083 Run `dotnet test backend/NajaEcho.sln` and resolve any failing tests
- [x] T084 Run `cd frontend && npm test` and resolve any failing Vitest tests
- [x] T085 Run all five quickstart.md validation scenarios (US1–US5) end-to-end and confirm each passes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1 completion. **BLOCKS all user stories.**
- **Phase 3 (US1)**: Depends on Phase 2 completion. Tests before implementation.
- **Phase 4 (US2)**: Depends on Phase 3 completion (shares `SignInWithDiscordHandler`; US2 extends it).
- **Phase 5 (US3)**: Depends on Phase 3 (needs session established by US1). Can start after US3 tests pass.
- **Phase 6 (US4)**: Depends on Phase 3 (`/api/auth/me` must exist) and Phase 5 (needs sign-out to test full cycle). Tests can be written after US3.
- **Phase 7 (US5)**: Depends on Phase 3 (callback endpoint must exist). Tests and implementation are largely independent of US2–US4.
- **Phase 8 (Polish)**: Depends on all user story phases being complete and green.

### User Story Dependencies (Summary)

| Story | Depends On | Can Parallel With |
|-------|-----------|-------------------|
| US1 (P1) | Phase 2 complete | — |
| US2 (P2) | US1 complete | — |
| US3 (P3) | US1 complete | US2 (no shared files in this phase) |
| US4 (P4) | US1 + US3 complete | US2 |
| US5 (P5) | US1 complete | US2, US3 |

### Within Each User Story

1. Write tests first → confirm they fail → implement → confirm they pass.
2. Domain/Application tasks before Infrastructure/API tasks.
3. Backend tasks before frontend tasks that depend on the API contract.
4. Tasks marked `[P]` within the same phase can run in parallel.

---

## Parallel Examples: US1

```text
# All US1 test tasks can launch together (different files, same foundation):
T028 UserProfileTests (domain)
T029 SignInWithDiscordHandlerTests (application)
T030 UserRepositoryTests (infrastructure/Testcontainers)
T031 AuthEndpointsTests — login redirect
T032 AuthEndpointsTests — callback success
T033 AuthEndpointsTests — /me endpoint
T034 useCurrentUser.test.ts (frontend)
T035 LandingPage.test.tsx (frontend)
T036 DashboardPage.test.tsx (frontend)

# Backend implementation sequence (ordered):
T037 → T038 → T039 → T040 → T041

# Frontend implementation (can parallel with backend after T042):
T042 schema  ─┐
T043 api     ─┤ all parallel
              ├→ T044 hook → T045 SignInButton → T046 LandingPage
              └→ T047 UserBadge → T048 DashboardPage
T049 AppRouter → T050 App.tsx
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (**CRITICAL** — blocks everything).
3. Complete Phase 3: User Story 1 (tests → implementation).
4. **STOP and VALIDATE**: A new visitor can sign in and reach the dashboard. One `user_profiles` row exists.
5. Demo if ready.

### Incremental Delivery

1. Setup + Foundational → infrastructure ready.
2. US1 → new-user sign-in works end-to-end (MVP).
3. US2 → idempotent re-login works (trust the profile).
4. US3 → users can sign out (session lifecycle complete).
5. US4 → protected routes enforced in browser (security boundary).
6. US5 → failure paths handled gracefully (robustness complete).
7. Polish → accessibility, CORS, final confirmation.

### Notes

- `[P]` tasks within the same phase have no file conflicts and can run in parallel.
- Each story phase ends with a Checkpoint that maps directly to a quickstart.md scenario.
- TDD is NON-NEGOTIABLE (constitution Principle II): every implementation task must have a
  corresponding preceding test task. Do not start T037 until T028–T036 are written and failing.
- Commit after each phase or logical group (see post-hook `/speckit-git-commit`).
