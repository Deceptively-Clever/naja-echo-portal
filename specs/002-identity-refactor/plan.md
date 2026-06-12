# Implementation Plan: Identity-Backed Authentication Refactor

**Branch**: `002-identity-refactor` | **Date**: 2026-06-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-identity-refactor/spec.md`

## Summary

Refactor the existing Discord authentication (feature `001-discord-auth`) so that ASP.NET Core
Identity becomes the source of truth for local application users, external-login linkage,
application sign-in, the cookie session, and future-ready roles/claims — while Discord remains the
only external OAuth provider and the user-facing sign-in experience is unchanged. The hand-rolled
`UserProfile` entity, `IUserRepository`, and `EfUnitOfWork` are retired in favor of a custom
`ApplicationUser : IdentityUser<Guid>` and Identity's EF Core stores. Auth orchestration is moved
behind an Application port (`IExternalLoginService`) implemented in Infrastructure with
`UserManager`/`SignInManager`, preserving Clean Architecture dependency direction. The
`/api/auth/me` endpoint becomes an always-200 discriminated session probe per the spec
clarification. Technical approach and trade-offs are detailed in [research.md](./research.md).

## Technical Context

**Language/Version**: C# on .NET 10 (backend); TypeScript 5.x (frontend SPA).

**Primary Dependencies**: ASP.NET Core Identity + `Microsoft.AspNetCore.Identity.EntityFrameworkCore`,
EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL`, `AspNet.Security.OAuth.Discord`, Serilog;
React + Vite + TanStack Query + React Router + Zod, `openapi-typescript` (new dev dependency for
contract-generated types), Vitest + React Testing Library + MSW.

**Storage**: PostgreSQL 16 via EF Core code-first migrations (Identity schema; snake-case naming).

**Testing**: xUnit + FluentAssertions + Testcontainers (PostgreSQL) + `WebApplicationFactory`
(backend); Vitest + RTL + MSW (frontend). No real Discord network calls.

**Target Platform**: Linux server (containerized), behind an nginx reverse proxy; modern browsers.

**Project Type**: Web application (separate `backend/` modular monolith + `frontend/` SPA).

**Performance Goals**: Standard web-app responsiveness; auth endpoints are low-volume. No special
throughput target (small org tool — tens to low hundreds of users).

**Constraints**: Server-session cookie auth (no tokens in the browser); HttpOnly + Secure(prod) +
SameSite=Lax + `__Host-` prefix in prod; no Discord tokens stored or logged; sensitive auth values
scrubbed from logs; forward-only migrations.

**Scale/Scope**: Single Discord org; one external provider; ~6 backend files changed/added per
layer + one migration; auth feature folder on the frontend.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

Evaluated against constitution v1.2.0.

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | PASS | OpenAPI updated to v0.2.0 ([contracts/openapi.yaml](./contracts/openapi.yaml)) before implementation; redirect vs JSON endpoints documented; no tokens in any response. |
| II. Test-First / TDD | PASS (enforced in tasks) | Tasks will write failing unit + integration + frontend tests before implementation; coverage enumerated in quickstart. |
| III. Frontend/Backend Separation | PASS | Independent artifacts; communication only via the versioned contract; frontend TS types generated from OpenAPI (no hand-duplicated DTOs). |
| IV. Simplicity / YAGNI | PASS (with note) | Minimal 2-field `ApplicationUser`; no server-side ticket store; roles tables come free with `IdentityDbContext` and satisfy FR-013 without extra modeling. See Complexity Tracking for the Identity-user-in-Infrastructure note. |
| V. Observability | PASS | Serilog structured logs with correlation id; safe auth milestones; explicit scrubbing of codes/state/tokens/cookies/Authorization headers; `SaveTokens=false` removes a leakage source. |
| VI. Modular Monolith + Clean Architecture | PASS (with documented deviation) | Dependency direction preserved; Identity/EF in Infrastructure; orchestration behind Application port; feature folders retained. The local user entity is an Identity (Infrastructure) type by explicit architecture direction — see Complexity Tracking. |
| Frontend Conventions | PASS | TanStack Query for session; centralized typed `authKeys` factory; components consume feature hooks; generated types from contract + Zod as runtime guard only; auth logic stays in `features/auth/`. |
| Security & Session rules | PASS | Cookie flags, `__Host-` prefix in prod, scope minimized to `identify`, no token storage/exposure, OAuth state/correlation preserved. |
| Development Workflow | PASS | Contract committed before implementation; forward-only migration; the `user_profiles` drop is destructive and is flagged for explicit PR approval (no production data). |

**Gate result**: PASS. One justified deviation recorded in Complexity Tracking; one destructive
migration step flagged for PR approval. No unresolved clarifications (spec clarifications complete).

## Project Structure

### Documentation (this feature)

```text
specs/002-identity-refactor/
├── plan.md              # This file
├── research.md          # Phase 0 output — decisions D1–D9
├── data-model.md        # Phase 1 output — Identity entities + port DTOs
├── quickstart.md        # Phase 1 output — validation guide
├── contracts/
│   └── openapi.yaml      # Phase 1 output — auth API v0.2.0
├── checklists/
│   └── requirements.md   # Spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 output — created by /speckit-tasks (NOT here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Users/
│       └── DiscordProfile.cs                 # KEEP — framework-free external-profile value object
│                                             # (UserProfile.cs retired)
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   ├── IExternalLoginService.cs          # NEW — port (FindOrCreateAsync / GetByIdAsync)
│   │   ├── IClock.cs                          # KEEP
│   │   └── IDiscordOAuthClient.cs             # review/retire if unused after refactor
│   │   # IUserRepository.cs / IUnitOfWork.cs  # REMOVE (Identity owns persistence)
│   └── Features/Auth/
│       ├── SignInWithDiscord/                # orchestrates via IExternalLoginService
│       │   ├── SignInWithDiscordCommand.cs
│       │   ├── SignInWithDiscordHandler.cs
│       │   └── LocalUser.cs                   # NEW — port output DTO
│       └── GetCurrentUser/
│           ├── GetCurrentUserQuery.cs
│           └── GetCurrentUserHandler.cs       # returns id/displayName/discordUsername
├── NajaEcho.Infrastructure/
│   ├── Identity/
│   │   ├── ApplicationUser.cs                 # NEW — IdentityUser<Guid> + DisplayName, DiscordUsername
│   │   └── DiscordExternalLoginService.cs     # NEW — IExternalLoginService via UserManager
│   ├── Persistence/
│   │   ├── AppDbContext.cs                    # CHANGE — IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
│   │   ├── Configurations/                    # ApplicationUser config (DisplayName/DiscordUsername lengths)
│   │   └── Migrations/                        # NEW — AddIdentitySchema (creates Identity tables, drops user_profiles)
│   │       # Repositories/UserRepository.cs, EfUnitOfWork.cs  # REMOVE
│   ├── Discord/DiscordOAuthClient.cs          # review/retire if unused
│   └── DependencyInjection.cs                 # CHANGE — AddIdentityCore + stores + IExternalLoginService
└── NajaEcho.Api/
    ├── Program.cs                            # CHANGE — Identity cookie scheme, external scheme,
    │                                         #          24h/sliding/7-day session, scope=identify,
    │                                         #          SaveTokens=false, /api 401 JSON, me anonymous
    └── Features/Auth/
        ├── AuthEndpoints.cs                  # CHANGE — login (challenge), callback (SignInManager
        │                                     #          external -> find/create/link -> SignInAsync),
        │                                     #          me (always 200 discriminated), signout
        └── Contracts/
            ├── SessionStateResponse.cs        # NEW — discriminated { authenticated, user? }
            └── CurrentUserResponse.cs         # CHANGE — id, displayName, discordUsername (drop avatarUrl)

backend/tests/                                 # update auth unit + integration tests (see quickstart)

frontend/src/
├── lib/api/schema.d.ts                        # NEW — generated from openapi.yaml (openapi-typescript)
└── features/auth/
    ├── api/authApi.ts                         # CHANGE — me returns SessionState; types from generated schema
    ├── schemas/sessionStateSchema.ts          # NEW/REPLACE — Zod runtime guard for the discriminated union
    │                                          #   (currentUserSchema.ts updated: discordUsername, no avatarUrl)
    ├── hooks/
    │   ├── authKeys.ts                         # NEW — centralized typed query-key factory
    │   ├── useCurrentUser.ts                   # CHANGE — use authKeys; consume SessionState
    │   └── useSignOut.ts                       # CHANGE — invalidate via authKeys
    ├── ProtectedRoute.tsx                      # CHANGE — read `authenticated` discriminant
    └── components/                             # SignInButton/SignOutButton unchanged behavior
```

**Structure Decision**: Web application (Option 2). The existing `backend/` modular monolith
(Domain/Application/Infrastructure/API, one .csproj per layer) and `frontend/` SPA are retained.
The refactor adds an `Identity/` folder under Infrastructure for the Identity user and external
login service, an `IExternalLoginService` port under Application/Abstractions, and keeps all auth
code in the existing `Features/Auth/` feature folders on both tiers. No new projects are introduced.

## Complexity Tracking

| Violation / Deviation | Why Needed | Simpler Alternative Rejected Because |
|-----------------------|------------|--------------------------------------|
| Local application user entity (`ApplicationUser`) lives in **Infrastructure** as an Identity type, not in Domain | ASP.NET Core Identity couples the user entity to `IdentityUser<TKey>` and EF Core stores; the architecture constraints explicitly require Identity/EF in Infrastructure and forbid Identity dependencies in Domain. The Domain stays Identity-free (keeps `DiscordProfile`); the Application depends only on the `IExternalLoginService` port and the plain `LocalUser` DTO, so dependency direction is preserved. | A Domain `User` entity mapped to an `IdentityUser` 1:1 was rejected — it duplicates identity state across two tables, reintroduces the custom repository the refactor retires, and adds a join with no current requirement (YAGNI). |
| `IdentityDbContext` brings role/claim tables that are unused this feature | FR-013 requires the model to support future roles/claims; these tables ship for free with `IdentityDbContext` and require no extra modeling or migration work beyond the single Identity migration. | Stripping the role tables to a users-only schema was rejected — it would need custom store configuration now and a second migration later, more work than leaving Identity's defaults in place. |
| Destructive migration step: drop `user_profiles` | The custom table is fully replaced by the Identity schema and holds no production data (spec assumption). Leaving it would create a dead table and contradict "Identity is the source of truth." | Keeping `user_profiles` was rejected for the reasons above. The drop is flagged for explicit approval in the PR description per the Development Workflow rule on destructive migrations. |

## Phase 0 — Outline & Research

Complete. See [research.md](./research.md) for decisions D1–D9 (user model, port placement,
external-login flow, cookie/session policy, tokens/scopes, `/api/auth/me` shape, migration
strategy, frontend type generation, CORS/cookie risks) and the full delta table vs feature 001.
All NEEDS CLARIFICATION items were resolved during `/speckit-clarify` (no open questions remain).

## Phase 1 — Design & Contracts

Complete. Artifacts generated:
- [data-model.md](./data-model.md) — `ApplicationUser`, `AspNetUserLogins` linkage, role/claim
  tables, port DTOs, and the retired-items list.
- [contracts/openapi.yaml](./contracts/openapi.yaml) — auth API v0.2.0 with the discriminated
  `SessionState` response, redirect-vs-JSON endpoint documentation, and the no-token invariant.
- [quickstart.md](./quickstart.md) — end-to-end manual scenarios mapped to user stories, automated
  test coverage summary, and the dev-vs-prod cookie matrix.
- Agent context (`CLAUDE.md`) updated to reference this plan.

## Phase 2 — Next step

`/speckit-tasks` will derive the ordered, test-first task list (`tasks.md`) from these artifacts.
This plan command does not create `tasks.md`.
