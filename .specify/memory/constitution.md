<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.1 → 1.1.0  [MINOR: added Principle VI, resolved deferred tech stack decisions]

Modified principles:
  - V. Observability — expanded to mandate sensitive-data scrubbing and structured request logging

Added principles:
  - VI. Modular Monolith + Clean Architecture (NON-NEGOTIABLE)

Materially expanded sections:
  - Technology Stack — resolved NEEDS CLARIFICATION for database and auth; locked in concrete
    frontend + backend stack including EF Core, Npgsql, Tailwind, shadcn/ui, TanStack Query,
    React Hook Form, Zod, Serilog, Testcontainers, MSW.
  - Development Workflow — added cookie/session security rules, OAuth token handling rules,
    EF Core migration discipline, dependency direction enforcement.

Templates requiring updates:
  - .specify/templates/plan-template.md  ✅ Generic Constitution Check section accommodates the new
                                            principle without edits.
  - .specify/templates/spec-template.md  ✅ No change required (principle-agnostic).
  - .specify/templates/tasks-template.md ✅ No change required (structure-only template).

Deferred TODOs:
  - None — both prior NEEDS CLARIFICATION items resolved.
-->

# NajaEchoPortal Constitution

NajaEchoPortal is a full-stack web application providing org-management utilities for the Naja Echo
organisation in the game Star Citizen. It consists of a .NET backend API and a separate React SPA
frontend, deployed as a modular monolith.

## Core Principles

### I. API-Contract-First (NON-NEGOTIABLE)

Every endpoint MUST be defined in an OpenAPI 3.x contract before any implementation code is
written. The contract is the single source of truth; implementation MUST conform to it, not the
reverse. Breaking contract changes MUST be versioned. No endpoint ships without a corresponding
contract definition reviewed and approved.

**Rationale**: Decouples frontend and backend work, enables parallel development, and prevents
contract drift between teams.

### II. Test-First / TDD (NON-NEGOTIABLE)

Tests MUST be written and confirmed to fail before production code is written. The Red-Green-Refactor
cycle is mandatory. Unit tests cover domain/service logic; integration tests cover API contracts
and cross-layer behaviour. A feature is not considered complete until all tests are green and the
test suite can be run in CI without manual intervention.

**Rationale**: Prevents regression, forces clear requirement thinking, and makes refactoring safe.

### III. Frontend/Backend Separation

The backend (ASP.NET Core Web API) and the frontend (React SPA) are independently deployable
artifacts. They communicate exclusively through the versioned OpenAPI contract (Principle I).
No server-rendered HTML from the backend. No direct database access from the frontend. Shared
types MUST be generated from the contract, not hand-duplicated.

**Rationale**: Allows independent deployment cadences and technology evolution on each tier.

### IV. Simplicity / YAGNI

Every abstraction, dependency, and layer MUST be justified by a current, concrete requirement.
Speculative generalization is forbidden. Three similar lines of code are preferable to a
premature abstraction. Complexity introduced beyond what the task requires MUST be documented
and approved.

**Rationale**: Star Citizen org tooling needs change rapidly; over-engineering creates maintenance
burden that outpaces value.

### V. Observability

All API endpoints MUST emit structured JSON logs via Serilog (or an equivalent structured logger),
written to stdout for container-friendly aggregation. Every request MUST carry a correlation ID
traceable across the frontend request and backend handler. Health-check endpoints MUST be present
and monitored. Sensitive authentication data — including OAuth `code`, `state`, access tokens,
refresh tokens, session cookies, and `Authorization` headers — MUST be scrubbed from all log
output. Observability is not optional polish — it ships with the feature.

**Rationale**: Org tooling is operated by a small team; fast incident diagnosis requires built-in
observability from day one. Token leakage via logs is one of the most common identity-related
breach vectors and is trivially preventable at the logging layer.

### VI. Modular Monolith + Clean Architecture (NON-NEGOTIABLE)

The system is a modular monolith. Microservices are forbidden until a concrete, documented
scaling or organizational constraint demands them.

The backend MUST be organized as four layers with strictly inward-pointing dependencies:

1. **Domain** — entities, value objects, domain rules. Depends on nothing.
2. **Application** — use cases, ports (interfaces), validators, orchestration. Depends only on Domain.
3. **Infrastructure** — EF Core, PostgreSQL, external integrations (e.g., Discord OAuth), logging
   sinks. Implements Application ports. Depends on Application and Domain.
4. **API** — ASP.NET Core endpoints, DTO mapping, authentication/session configuration, HTTP
   concerns. Depends on Application; references Infrastructure only for composition (DI wiring).

These boundaries MUST be enforced by project references (separate .csproj per layer), not by
convention alone. Code within each layer is organized by feature folders (e.g.,
`Application/Features/Auth/SignInWithDiscord/`) keeping commands, queries, handlers, validators,
DTOs, and endpoint mappings co-located.

The frontend MUST be organized by feature folders (e.g., `features/auth/`, `features/dashboard/`),
each containing its pages, components, hooks, schemas, API clients, and tests. Cross-cutting UI
primitives live separately under `components/ui/`. Page-level route components MUST stay thin and
delegate behaviour to feature components and hooks.

**Rationale**: Clean Architecture's enforced dependency direction keeps business logic isolated
from frameworks and persistence, making it testable and replaceable. Feature folders keep related
code physically near, reducing cognitive load and merge conflict surface. A monolith is the right
default at this scale; splitting prematurely is a known anti-pattern.

## Technology Stack

**Backend**

- Language/runtime: C# on .NET 8 (current LTS through November 2026); migrate to next LTS when available.
- Web framework: ASP.NET Core Web API (REST). gRPC and GraphQL are out of scope unless justified.
- ORM: Entity Framework Core 8 with `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Database: PostgreSQL 16 (or current supported major), managed via EF Core code-first migrations.
- Validation: FluentValidation for command/query validators in the Application layer; built-in
  ASP.NET Core model binding handles trivial DTO shape checks at the API edge.
- Authentication: ASP.NET Core cookie authentication for browser sessions; OAuth2 authorization-code
  flow for third-party identity providers (Discord is the first and only provider in v1).
- Logging: Serilog with the JSON sink to stdout; request logging via `UseSerilogRequestLogging`
  with sensitive-data destructuring policies.
- Testing: xUnit, FluentAssertions, Testcontainers (PostgreSQL), `WebApplicationFactory` for
  integration tests, fakes for OAuth handlers and repositories.

**Frontend**

- Language: TypeScript 5.x. Strict mode required (`strict: true` in tsconfig).
- Framework: React 18 SPA built with Vite 5.
- Routing: React Router 6 (data router APIs).
- Styling: Tailwind CSS 3.
- Component system: shadcn/ui (in-repo, generated via CLI) built on Radix UI primitives.
- Icons: Lucide React.
- Data fetching / cache: TanStack Query 5.
- Forms / validation: React Hook Form + Zod schemas.
- Testing: Vitest + React Testing Library; MSW for API mocking.

**Security & Session Rules (cross-cutting)**

- Browser sessions MUST use server-managed cookies that are `HttpOnly`, `Secure` (production),
  `SameSite=Lax`, and prefixed `__Host-` in production.
- Third-party access tokens and refresh tokens MUST NOT be exposed to the frontend, persisted in
  the browser, or placed in session claims visible to the client.
- OAuth scopes MUST be minimized to what the feature genuinely requires.
- Secrets MUST NOT be committed to the repository. Local development uses `dotnet user-secrets`
  or `.env` files git-ignored by default; deployed environments use a secrets manager.
- Sensitive authentication data MUST NOT appear in log output (enforces Principle V).

**CI**

- GitHub Actions (or equivalent). All backend and frontend tests MUST pass before merge.
- The OpenAPI contract for any feature MUST be committed and reviewable in the same PR as (or
  before) the implementation that satisfies it.

All dependency additions MUST be reviewed for licence compatibility and security posture.

## Development Workflow

- Feature branches follow the Spec Kit naming convention (`###-feature-name`).
- Every feature requires: spec → plan → tasks → implementation, in that order.
- Pull requests MUST reference the relevant spec and pass all CI checks before merge.
- EF Core migrations are versioned and forward-only by default. Destructive migrations (column
  drops, type changes that lose data, constraint removals that allow new invalid data) require
  explicit approval recorded in the PR description.
- The OpenAPI contract for any feature MUST be committed before implementation tasks begin
  (enforces Principle I).
- Backend project references MUST preserve the dependency direction defined in Principle VI.
  Adding a reference from Domain → Infrastructure (or any other inward-pointing violation) MUST
  fail review.
- Each feature implementation MUST include the minimum tests required by Principle II:
  - Domain/Application unit tests for new use cases and rules.
  - At least one integration test covering the API contract end-to-end through the real database
    (Testcontainers-backed).
  - Frontend component/hook tests for new user-facing behaviour.

## Governance

This constitution supersedes all other stated practices. Amendments require:

1. A written rationale explaining what changed and why.
2. A version bump following semantic versioning:
   - MAJOR: principle removal, redefinition, or backward-incompatible governance change.
   - MINOR: new principle or materially expanded guidance.
   - PATCH: clarifications, wording fixes, non-semantic refinements.
3. Updates to all affected templates and this Sync Impact Report.
4. Approval recorded in the commit message.

All code reviews MUST verify compliance with Principles I–VI. Violations require documented
justification in the PR description before approval.

**Version**: 1.1.0 | **Ratified**: 2026-06-08 | **Last Amended**: 2026-06-08
