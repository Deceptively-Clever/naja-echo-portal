# Quickstart: Identity-Backed Authentication Refactor

**Feature**: `002-identity-refactor` | **Date**: 2026-06-12

This guide validates that the Identity-backed auth refactor works end-to-end. It is a run/validate
guide, not implementation detail — code lives in the implementation phase and `tasks.md`.

---

## Prerequisites

- .NET 10 SDK, Node.js (for the SPA), Docker (for PostgreSQL + Testcontainers).
- A Discord application (existing one from feature 001 is reused) with redirect URI
  `http://localhost:5080/api/auth/discord/callback` registered.
- PostgreSQL running via `docker-compose up -d` from the repo root.

### Local Discord OAuth configuration (secrets, never committed)

Configure the backend with user secrets (preferred for local dev) — see
`backend/src/NajaEcho.Api` (UserSecretsId is already set in the csproj):

```bash
cd backend/src/NajaEcho.Api
dotnet user-secrets set "Discord:ClientId" "<your-client-id>"
dotnet user-secrets set "Discord:ClientSecret" "<your-client-secret>"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=najaecho;Username=najaecho;Password=najaecho"
```

`Frontend:Origin` defaults to `http://localhost:5173`. Only the `identify` scope is requested.

---

## Setup

```bash
# 1. Apply migrations (creates Identity schema, drops user_profiles)
cd backend/src/NajaEcho.Api
dotnet ef database update --project ../NajaEcho.Infrastructure

# 2. Run the backend
dotnet run

# 3. Run the frontend (separate terminal)
cd frontend
npm install
npm run dev

# 4. (When the contract changes) regenerate frontend API types from the OpenAPI contract
npm run gen:api    # openapi-typescript -> src/lib/api/schema.d.ts
```

---

## Manual validation scenarios

Map directly to the spec's user stories and acceptance criteria.

### Scenario A — New user signs in (User Story 1)
1. Open `http://localhost:5173`, click **Sign in with Discord**.
2. Authorize on Discord.
3. **Expected**: redirected to the dashboard root; a new `asp_net_users` row exists with an
   `asp_net_user_logins` row `(LoginProvider="Discord", ProviderKey=<discord id>)`. No
   `asp_net_user_tokens` row (tokens not stored).

### Scenario B — Returning user signs in (User Story 2)
1. Sign out, then sign in again with the same Discord account.
2. **Expected**: no new `asp_net_users` row — the existing user is reused. Row count unchanged.

### Scenario C — Session probe while authenticated (User Story 3)
1. While signed in, in the browser console:
   `fetch('/api/auth/me', {credentials:'include'}).then(r=>r.json()).then(console.log)`
2. **Expected**: `200` with `{ authenticated: true, user: { id, displayName, discordUsername } }`.
   No token fields present.

### Scenario D — Session probe while anonymous (User Story 3)
1. In a private window with no session, fetch `/api/auth/me` as above.
2. **Expected**: `200` with `{ authenticated: false }` — NOT a 401.

### Scenario E — Sign out (User Story 4)
1. While signed in, click **Sign out**.
2. **Expected**: session cookie expired; redirected to the unauthenticated landing; a subsequent
   `/api/auth/me` returns `{ authenticated: false }`; protected action endpoints return 401.

### Scenario F — Protected routes (User Story 5)
1. While anonymous, navigate directly to `/dashboard`.
2. **Expected**: redirected to the landing page (auth guard blocks access).
3. While authenticated, navigate to `/dashboard`. **Expected**: dashboard renders, no re-auth.

### Scenario G — No token / secret leakage (SC-006)
1. Inspect the `/api/auth/me` body, the session cookie (DevTools → Application → Cookies), and the
   backend console logs during a full login.
2. **Expected**: no OAuth code, state, access token, refresh token, Authorization header, or cookie
   value appears in any response body or log line. Log lines show milestones only (login started,
   external login succeeded, local user linked, sign-in succeeded, sign-out completed, auth failure).

---

## Automated test validation

```bash
# Backend (unit + integration; Testcontainers spins up PostgreSQL)
cd backend
dotnet test

# Frontend (Vitest + RTL + MSW)
cd frontend
npm run test:run
```

**Backend integration coverage** (WebApplicationFactory; Discord external login result is stubbed —
no real Discord calls): first-time login creates a user, links the provider key; returning login
reuses the user and creates no duplicate; `/api/auth/me` returns the authenticated body without
tokens; `/api/auth/me` returns `{ authenticated: false }` when anonymous; sign-out clears the
session; protected action endpoints reject anonymous requests with 401; sensitive values are absent
from captured logs where practical.

**Frontend coverage** (MSW mocks `/api/auth/me` and `/api/auth/signout`): auth state loading;
authenticated state renders protected content; anonymous state redirects; sign-out invalidates the
auth query and routes to the unauthenticated state.

---

## Cookie matrix (dev vs prod) — validation reference

| Setting | Development | Production |
|---------|-------------|------------|
| Cookie name | `najaecho.auth` | `__Host-najaecho.auth` |
| `Secure` | off (plain http) | on (always) |
| `HttpOnly` | on | on |
| `SameSite` | Lax | Lax |
| Idle timeout (`ExpireTimeSpan`) | 24h | 24h |
| Sliding renewal | on | on |
| Absolute max (validated) | 7 days | 7 days |

If sign-in fails to persist a session in dev, confirm the cookie is **not** using the `__Host-`
prefix over plain HTTP (the prefix requires `Secure`, rejected on `http://localhost`).
