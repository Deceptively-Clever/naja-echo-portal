# Quickstart: Discord Authentication

**Feature**: 001-discord-auth | **Audience**: developer running the slice locally and validating
the user journeys from `spec.md`.

This guide validates the feature end-to-end. It does **not** include implementation code — see
`plan.md`, `data-model.md`, and `contracts/openapi.yaml` for the design, and `tasks.md` (generated
by `/speckit-tasks`) for the implementation steps.

## Prerequisites

1. **Discord application**: Create a Discord application at <https://discord.com/developers/applications>.
   - Add OAuth2 redirect: `http://localhost:5080/api/auth/discord/callback`.
   - Note the Client ID and Client Secret.
2. **.NET 8 SDK** installed (`dotnet --version` ≥ 8.0).
3. **Node 20+** and **npm 10+**.
4. **Docker** available for the PostgreSQL container.

## One-time setup

1. Start PostgreSQL:

   ```bash
   docker compose up -d postgres
   ```

2. Configure backend secrets (do NOT commit):

   ```bash
   cd backend/src/NajaEcho.Api
   dotnet user-secrets set Discord:ClientId "<your-client-id>"
   dotnet user-secrets set Discord:ClientSecret "<your-client-secret>"
   dotnet user-secrets set ConnectionStrings:Default "Host=localhost;Database=najaecho;Username=najaecho;Password=najaecho"
   ```

3. Apply EF Core migrations:

   ```bash
   dotnet ef database update --project ../NajaEcho.Infrastructure --startup-project .
   ```

4. Install frontend dependencies:

   ```bash
   cd ../../../frontend
   npm install
   ```

## Run

In two terminals:

```bash
# terminal 1
cd backend/src/NajaEcho.Api
dotnet run            # listens on http://localhost:5080
```

```bash
# terminal 2
cd frontend
npm run dev           # listens on http://localhost:5173, proxies /api → 5080
```

## Validation scenarios

Each scenario maps to a user story in `spec.md`. Run them in order.

### US1 — New user sign-in (P1)

1. Open <http://localhost:5173/> in a private window. **Expected**: landing page with "Sign in with Discord."
2. Click the button. **Expected**: redirect to Discord consent screen requesting only `identify` and `email`.
3. Approve. **Expected**: returned to `/dashboard` showing your Discord display name and avatar.
4. Inspect DB:

   ```bash
   docker compose exec postgres psql -U najaecho -d najaecho -c "select id, discord_user_id, display_name, created_at_utc, last_login_at_utc from user_profiles;"
   ```

   **Expected**: exactly one row for your account.

### US2 — Returning user, no duplicate (P2)

1. Sign out (see US3), then sign in again with the same Discord account.
2. Re-run the SQL query. **Expected**: still exactly one row; `last_login_at_utc` updated, `created_at_utc` unchanged.
3. Change your Discord display name in Discord settings, sign out, sign in again.
4. Re-run the SQL query. **Expected**: `display_name` matches new Discord value; `last_updated_at_utc` advanced.

### US3 — Sign out (P3)

1. While signed in, click the sign-out control on the dashboard.
2. **Expected**: returned to landing page, no user badge shown.
3. Try to navigate directly to `/dashboard`. **Expected**: redirected back to `/`.

### US4 — Unauthenticated access blocked (P4)

1. In a fresh private window, navigate to `http://localhost:5173/dashboard`.
2. **Expected**: redirected to `/`; no dashboard content visible in the DOM.
3. `curl -i http://localhost:5080/api/auth/me`. **Expected**: HTTP 401 with `application/problem+json`.

### US5 — Authorization denied / cancelled (P5)

1. Click sign-in, then on the Discord consent screen click **Cancel**.
2. **Expected**: returned to a friendly auth-error page with a "Try again" button. No new row created.
3. Re-run the SQL query. **Expected**: same row count as before the attempt.

## Tests

```bash
# backend — all layers
dotnet test backend/NajaEcho.sln

# frontend
cd frontend && npm test
```

Test coverage requirements (per plan.md → Constitution Check):
- Handlers (Application layer) unit-tested with fake repository and Discord client.
- `IUserRepository` integration test against PostgreSQL Testcontainer covers idempotent upsert.
- `WebApplicationFactory` test substitutes the Discord auth scheme to verify the full callback →
  cookie → `/api/auth/me` round-trip and the unauthenticated redirect.
- `ProtectedRoute`, `SignInButton`, `DashboardPage` covered by Vitest + RTL + MSW.

## Tearing down

```bash
docker compose down -v   # removes volume; DB starts empty next time
```

## What's NOT covered here

- Production deployment (containers, secrets manager, reverse proxy, prod cookie domain).
- Role-based authorization, admin pages, multi-provider login — all out of scope per `spec.md`.
