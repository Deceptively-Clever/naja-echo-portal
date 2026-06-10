# Phase 0 Research: Discord Authentication

**Feature**: 001-discord-auth | **Date**: 2026-06-08

## Resolved Decisions

### 1. .NET version

- **Decision**: .NET 8 (current LTS as of 2026-06-08).
- **Rationale**: User specified "latest .NET LTS." .NET 8 LTS support runs through November 2026;
  .NET 10 LTS will release in late 2026 but is not yet available at project start. .NET 8 has full
  EF Core 8 / Npgsql support, native AOT options, and broad library ecosystem.
- **Alternatives**: .NET 9 (STS, not LTS — rejected per user requirement).

### 2. Discord OAuth integration in ASP.NET Core

- **Decision**: Use `AspNet.Security.OAuth.Discord` (community-maintained package from
  `aspnet-contrib`) as the OAuth handler, registered via `AddAuthentication().AddDiscord(...)`,
  paired with cookie authentication as the sign-in scheme.
- **Rationale**: Provides a well-tested ASP.NET Core OAuth handler for Discord that integrates
  with the standard authentication middleware. Handles authorization-code flow, state/CSRF,
  PKCE, and token exchange. Avoids hand-rolling a flow that is easy to get wrong.
- **Alternatives**:
  - Hand-roll OAuth client (`HttpClient` + manual state cookie). Rejected: more surface area for
    security bugs, no real benefit at this scope.
  - IdentityServer / Duende. Rejected: massive overkill for a single-provider login.

### 3. Session mechanism

- **Decision**: ASP.NET Core cookie authentication. Cookie: `HttpOnly`, `Secure`,
  `SameSite=Lax`, sliding expiration (14 days), `__Host-` prefix in production.
- **Rationale**: Same-origin or sibling-origin SPA + API benefits from cookies (server-managed,
  invisible to JS). HTTP-only prevents XSS token theft. SameSite=Lax permits the Discord
  callback redirect while blocking CSRF on state-changing requests. Sign-out call clears it
  server-side.
- **Alternatives**:
  - JWT in localStorage. Rejected: XSS-readable; constitution forbids exposing tokens to frontend.
  - JWT in cookie. Rejected: adds complexity without benefit for a single-app session.

### 4. Storing Discord access/refresh tokens

- **Decision**: Do not persist Discord tokens in v1. The application calls Discord's `/users/@me`
  endpoint exactly once during the callback to read the user profile, then discards the access
  token. Re-authentication on session expiry triggers a new OAuth flow.
- **Rationale**: v1 needs no ongoing Discord API access (no guild sync, no bot, no DMs). Not
  storing tokens removes an entire class of secret-management and rotation work.
- **Alternatives**: Encrypted token store. Deferred until a feature actually requires Discord API
  access beyond login.

### 5. Discord scopes

- **Decision**: Request `identify` (required) and `email` (optional). Store email only when
  Discord returns one and marks it verified.
- **Rationale**: Minimum needed to satisfy FR-004 and FR-010. `email` is included to support
  future user-contact features without requiring users to re-authorize. No `guilds`, no `bot`,
  no `connections`.
- **Alternatives**: `identify` only. Rejected: would require a second consent flow later if email
  becomes useful.

### 6. PostgreSQL provider

- **Decision**: Npgsql.EntityFrameworkCore.PostgreSQL 8.x.
- **Rationale**: De facto EF Core provider for PostgreSQL, actively maintained, supports EF Core 8
  features. Pairs with `dotnet ef` for migrations.
- **Alternatives**: Dapper + raw SQL. Rejected for v1 — EF Core's productivity wins, and
  performance is not a constraint at this scale.

### 7. Backend project layout

- **Decision**: Four projects under `backend/src/`:
  `NajaEcho.Domain`, `NajaEcho.Application`, `NajaEcho.Infrastructure`, `NajaEcho.Api`.
  Each Application/Infrastructure/Api layer organizes by feature folder (e.g., `Features/Auth/`).
- **Rationale**: Matches user-requested Clean Architecture + feature folders. Project boundaries
  enforce dependency direction (Api → Application → Domain; Infrastructure → Application/Domain;
  Domain depends on nothing). xUnit test project per layer.
- **Alternatives**: Single project with namespaces. Rejected: layer rules become advisory rather
  than compiler-enforced.

### 8. Validation library

- **Decision**: FluentValidation 11.x for command validators in the Application layer; built-in
  ASP.NET Core model binding + DataAnnotations is sufficient at the API edge for simple DTOs.
- **Rationale**: User specified "server-side validation for all incoming data." FluentValidation
  scales as features grow; for v1 (auth callback has Discord-controlled shape), validators are
  thin but the pattern is in place.
- **Alternatives**: DataAnnotations only. Rejected for forward-compat — first non-auth feature will
  need richer validation.

### 9. Frontend build tool and router

- **Decision**: Vite 5 + React Router 6 (data router APIs).
- **Rationale**: Vite is the standard non-Next React SPA tooling. React Router 6 supports
  data loaders and `<Outlet />`-based protected-route patterns cleanly.
- **Alternatives**: Next.js. Rejected — server-rendered HTML and a Node runtime are explicitly
  out of scope per the frontend/backend-separation principle and user direction (SPA + .NET API).

### 10. TanStack Query usage for auth state

- **Decision**: `useQuery({ queryKey: ['auth','me'], queryFn: getMe })` is the canonical source of
  "current user." `ProtectedRoute` reads it; on 401 redirects to `/`. A small `useSignOut`
  mutation invalidates the `auth/me` cache.
- **Rationale**: Avoids a parallel context/Redux/Zustand auth store. The server cookie is the
  source of truth; the cache mirrors it.
- **Alternatives**: React Context provider. Rejected — duplicates state already in the query cache.

### 11. shadcn/ui setup

- **Decision**: Use shadcn/ui CLI to scaffold `Button`, `Card`, `Avatar`, `Alert` into
  `src/components/ui/`. Customize via Tailwind only.
- **Rationale**: Components live in-repo (not as a dep), making them easy to tweak. Built on
  Radix primitives for accessibility.

### 12. CSRF protection

- **Decision**: `SameSite=Lax` cookies + custom header check (`X-Requested-With: fetch`) on
  state-changing endpoints. State parameter on OAuth flow handled by the AspNet OAuth handler.
- **Rationale**: SameSite=Lax blocks classic CSRF for POST. The header check stops simple
  cross-origin form-encoded posts. ASP.NET Core's antiforgery middleware is also available if a
  later feature needs full token-based protection.
- **Alternatives**: Full antiforgery token round-trip. Deferred — v1 has only `POST /api/auth/signout`
  as a state-changer beyond the OAuth flow, and SameSite=Lax covers it.

### 13. Logging

- **Decision**: Serilog with JSON sink to stdout (container-friendly). Request logging via
  `app.UseSerilogRequestLogging()` with correlation IDs (`HttpContext.TraceIdentifier`).
  Custom destructurer to scrub `code`, `state`, `access_token`, `refresh_token`, `Authorization`
  headers, and `Cookie` headers.
- **Rationale**: Satisfies Principle V (observability) and FR-021 (no sensitive auth data in logs).
- **Alternatives**: Default `Microsoft.Extensions.Logging`. Rejected: harder to enforce structured
  output and scrubbing across third-party loggers.

### 14. Test strategy for auth flow

- **Decision**:
  - Unit tests: handler tests with fake `IDiscordOAuthClient` / `IUserRepository`.
  - Integration tests: `WebApplicationFactory` with a fake OAuth handler registered as the Discord
    scheme (returns canned claims), Testcontainers PostgreSQL for the database.
  - Frontend: Vitest + RTL + MSW to mock `/api/auth/me`, exercise `ProtectedRoute`, `SignInButton`,
    `DashboardPage`.
- **Rationale**: Real OAuth round-trips against Discord in CI are flaky and require live
  credentials. Substituting the auth scheme is the standard ASP.NET Core pattern.

### 15. Local dev orchestration

- **Decision**: `docker-compose.yml` at repo root with PostgreSQL service; backend and frontend
  run on host (`dotnet run`, `npm run dev`). Discord redirect URI:
  `http://localhost:5173/auth/callback` (frontend proxies `/api` to backend on 5080 during dev).
- **Rationale**: Lowest-friction local loop. Production deployment topology is out of scope for
  this plan but documented at a high level in quickstart.

## Open Items

None. All clarifications from the spec and plan have a chosen path.
