# Implementation Plan: Discord Authentication

**Branch**: `001-discord-auth` | **Date**: 2026-06-08 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-discord-auth/spec.md`

## Summary

Deliver a thin vertical slice of Discord-based authentication: a public landing page, "Sign in with
Discord" OAuth2 authorization-code flow, server-side session via secure HTTP-only cookies, a
protected dashboard showing display name + avatar, and sign-out. Local user profiles persist in
PostgreSQL via EF Core, keyed by Discord user ID to guarantee idempotent re-login. The backend
exchanges OAuth codes; the frontend never sees Discord tokens. Initial architecture is a modular
monolith with feature-folder + Clean Architecture layering on the backend and feature folders on
the React/TypeScript frontend.

## Technical Context

**Language/Version**: C# / .NET 8 (current LTS) on the backend; TypeScript 5.x on the frontend.

**Primary Dependencies**:
- Backend: ASP.NET Core Web API, EF Core 8 (Npgsql provider), FluentValidation, Microsoft cookie
  authentication, AspNet.Security.OAuth.Discord (community OAuth handler).
- Frontend: React 18, Vite, React Router 6, TanStack Query 5, React Hook Form, Zod, Tailwind CSS 3,
  shadcn/ui, Radix UI primitives, Lucide React.

**Storage**: PostgreSQL 16, accessed via EF Core 8 with code-first migrations.

**Testing**:
- Backend: xUnit, FluentAssertions, Testcontainers for PostgreSQL integration tests,
  WebApplicationFactory for API tests.
- Frontend: Vitest + React Testing Library; MSW for API mocking.

**Target Platform**: Linux containers (backend); modern evergreen browsers (frontend SPA).

**Project Type**: Full-stack web — separate `backend/` (.NET) and `frontend/` (Vite/React) projects.

**Performance Goals**: Sign-in round-trip under 60s including Discord (per SC-001); dashboard load
P95 under 1s after authentication.

**Constraints**:
- Discord access/refresh tokens MUST stay server-side; cookies MUST be HTTP-only, Secure, SameSite=Lax.
- No sensitive auth data in logs.
- Minimum Discord scopes only: `identify` (always), `email` (optional, included in v1).
- No Discord guild/bot scopes.

**Scale/Scope**: Small org (Naja Echo) — dozens to low hundreds of users in initial phase; single
backend instance is sufficient. Designed to scale up later, not optimized for it now.

## Constitution Check

Validated against `.specify/memory/constitution.md` v1.1.0.

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | PASS | OpenAPI contract committed under `contracts/openapi.yaml` before any endpoint code. |
| II. Test-First / TDD | PASS | xUnit + Vitest tests written first; integration tests use Testcontainers. |
| III. Frontend/Backend Separation | PASS | Independently deployable .NET API + React SPA, contract-only coupling, no server-rendered HTML. |
| IV. Simplicity / YAGNI | PASS | Thin vertical slice; no role/admin/multi-provider scope. Cookie auth, not JWT, because no third-party API consumers yet. Discord tokens not persisted (v1 needs no ongoing API access). |
| V. Observability | PASS | Serilog JSON to stdout, correlation IDs via `HttpContext.TraceIdentifier`, `/api/health` endpoint, sensitive-data scrubbers for `code`/`state`/tokens/cookies. |
| VI. Modular Monolith + Clean Architecture | PASS | Four backend projects with enforced dependency direction (Api → App → Domain; Infra → App/Domain). Feature folders inside each layer. Frontend organized by `features/auth/` and `features/dashboard/`. |

No gate violations. No entries needed in Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-discord-auth/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # REST API contract
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit-tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── NajaEcho.Domain/            # Entities, value objects, domain interfaces
│   │   └── Users/
│   │       └── UserProfile.cs
│   ├── NajaEcho.Application/       # Use cases, ports, validators
│   │   ├── Abstractions/           # IUserRepository, IDiscordOAuthClient, IClock, IUnitOfWork
│   │   └── Features/
│   │       └── Auth/
│   │           ├── SignInWithDiscord/   # command, handler, validator, DTO
│   │           ├── GetCurrentUser/      # query, handler, DTO
│   │           └── SignOut/             # command, handler
│   ├── NajaEcho.Infrastructure/    # EF Core, PostgreSQL, Discord OAuth client, Serilog
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/UserProfileConfiguration.cs
│   │   │   ├── Migrations/
│   │   │   └── Repositories/UserRepository.cs
│   │   ├── Discord/
│   │   │   └── DiscordOAuthClient.cs
│   │   └── DependencyInjection.cs
│   └── NajaEcho.Api/               # Endpoints, auth/session config, DTO mapping
│       ├── Features/
│       │   └── Auth/
│       │       ├── AuthEndpoints.cs       # /api/auth/discord/login, /callback, /signout, /me
│       │       └── Contracts/             # request/response DTOs
│       ├── Common/                       # ProblemDetails, exception handler
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    ├── NajaEcho.Domain.Tests/
    ├── NajaEcho.Application.Tests/       # Handler unit tests with fake repos/clients
    ├── NajaEcho.Infrastructure.Tests/    # EF Core + PostgreSQL Testcontainers
    └── NajaEcho.Api.Tests/               # WebApplicationFactory integration tests

frontend/
├── src/
│   ├── features/
│   │   ├── auth/
│   │   │   ├── api/                 # signIn URL, signOut, getCurrentUser (TanStack Query)
│   │   │   ├── components/          # SignInButton, SignOutButton, AuthErrorBanner
│   │   │   ├── hooks/               # useCurrentUser, useSignOut
│   │   │   ├── pages/               # LandingPage, AuthCallbackPage, AuthErrorPage
│   │   │   ├── schemas/             # Zod schemas for API responses
│   │   │   └── ProtectedRoute.tsx
│   │   └── dashboard/
│   │       ├── components/          # UserBadge
│   │       └── pages/               # DashboardPage
│   ├── components/ui/               # shadcn/ui generated components (Button, Card, Avatar, Alert)
│   ├── lib/                         # api client (fetch wrapper), queryClient, utils
│   ├── routes/                      # AppRouter
│   ├── App.tsx
│   └── main.tsx
├── tests/
│   └── setup.ts
├── index.html
├── tailwind.config.ts
├── tsconfig.json
├── vite.config.ts
└── package.json
```

**Structure Decision**: Web-application layout. The backend uses Clean Architecture with feature
folders inside each layer (Domain/Application/Infrastructure/API). The frontend uses feature
folders (`features/auth/`, `features/dashboard/`) with cross-cutting UI primitives under
`components/ui/`. Backend dependencies point inward (API → Application → Domain;
Infrastructure → Application/Domain; Domain depends on nothing).

## Complexity Tracking

No constitutional violations. Table not required.
