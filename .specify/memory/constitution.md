<!--
SYNC IMPACT REPORT
==================
Version change: 1.1.0 → 1.2.0  [MINOR: materially expanded frontend architecture and workflow
guidance; new Frontend Conventions section; UI-only contract clarification]

Modified principles:
  - I. API-Contract-First — clarified that UI-only features (no new or changed backend HTTP
    behaviour) do not require artificial OpenAPI contract changes; such features must record
    "No API contract changes required" in their implementation plan.
  - VI. Modular Monolith + Clean Architecture — frontend portion expanded with explicit rules for
    thin route components, feature-owned logic, shared-component criteria, and the split between
    `components/ui/`, `components/shared/`, and feature folders.

Added sections:
  - Frontend Conventions — new top-level section covering shadcn/ui ownership, API client and
    type generation from the OpenAPI contract, TanStack Query server-state conventions, form
    conventions (React Hook Form + Zod), and dashboard shell + navigation architecture.

Materially changed sections:
  - Technology Stack (Frontend) — replaced pinned major versions (React 18, Vite 5, Tailwind
    CSS 3, TanStack Query 5, React Router 6) with a version policy: use the current stable major
    version at project initialization time unless a compatibility issue is documented in the
    implementation plan. Concrete stack choices unchanged.
  - Development Workflow — added the UI-only feature rule: features that do not add or change
    backend endpoints must explicitly record "No API contract changes required" in the plan.

Removed sections:
  - None.

Templates requiring updates:
  - .specify/templates/plan-template.md  ✅ Generic Constitution Check section accommodates the
                                            expanded guidance without edits.
  - .specify/templates/spec-template.md  ✅ No change required (principle-agnostic).
  - .specify/templates/tasks-template.md ✅ No change required (structure-only template).

Follow-up TODOs:
  - ⚠ README.md tech-stack table says "React 18 + TypeScript + Vite"; consider dropping the
    pinned major to match the new version policy (manual, non-blocking).
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

This rule governs all backend HTTP behaviour. Features that introduce no new or changed backend
HTTP behaviour — frontend-only work such as the dashboard shell, navigation, layout, theming,
empty states, and other UI composition — do not require artificial OpenAPI contract changes.
Such features MUST explicitly record "No API contract changes required" in their implementation
plan so the omission is a reviewed decision, not an oversight.

**Rationale**: Decouples frontend and backend work, enables parallel development, and prevents
contract drift between teams — without forcing contract churn on work that never touches the
API boundary.

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
each containing its pages, components, hooks, schemas, API clients, and tests. Feature folders are
the default home for application-specific UI. The following rules apply:

- Route components MUST stay thin. They compose layouts, route params, guards, and feature
  components — nothing more.
- Business logic, data fetching, validation, and transformation logic MUST live in feature-owned
  hooks, schemas, services, or components, never in route components.
- Shared components are allowed only when at least two features need the same behaviour. Until
  then, the component belongs to the feature that uses it.
- Generic shadcn/ui primitives live under `components/ui/` and MUST remain application-agnostic.
- Application-specific compositions built from shadcn/ui primitives belong in `components/shared/`
  or the relevant feature folder, never in `components/ui/`.

**Rationale**: Clean Architecture's enforced dependency direction keeps business logic isolated
from frameworks and persistence, making it testable and replaceable. Feature folders keep related
code physically near, reducing cognitive load and merge conflict surface. Thin routes and
feature-owned logic keep behaviour testable without router scaffolding. A monolith is the right
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

Version policy: for each frontend dependency, use the current stable major version at project
initialization time unless a compatibility issue is documented in the implementation plan.
Subsequent major-version upgrades MUST be verified and intentionally selected — never blind bumps.

- Language: TypeScript. Strict mode required (`strict: true` in tsconfig).
- Framework: React SPA built with Vite.
- Routing: React Router (data router APIs).
- Styling: Tailwind CSS.
- Component system: shadcn/ui (in-repo, generated via CLI) built on Radix UI primitives where
  applicable.
- Icons: Lucide React.
- Data fetching / cache: TanStack Query.
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

## Frontend Conventions

**shadcn/ui ownership**

shadcn/ui components are owned source code once generated into the repository. They MAY be
customized for accessibility, bug fixes, or project-wide styling consistency. Feature-specific
behaviour MUST NOT be embedded directly into `components/ui/` primitives — build a composition in
`components/shared/` or the owning feature folder instead (Principle VI).

**API client and type generation**

Frontend API request and response types MUST be generated from the OpenAPI contract (enforces
Principles I and III). Hand-written duplicate DTO types are forbidden unless the type is purely
frontend view-model state and not part of the API boundary. Feature-level API clients MAY wrap
generated clients to provide ergonomic hooks or feature-specific data shaping, but they MUST NOT
redefine the backend contract.

**Server state (TanStack Query)**

Server state MUST be managed through TanStack Query — not hand-rolled effects or ad-hoc global
stores. Query keys MUST be centralized per feature and SHOULD use stable, typed key factories.
Components SHOULD consume feature hooks rather than calling generated API clients directly.
Mutation success, error, cache invalidation, and optimistic update behaviour MUST be explicit in
the feature plan when relevant.

**Forms**

Forms MUST use React Hook Form for form state and Zod for validation schemas. Validation schemas
live with the feature that owns the form. API DTO schemas and UI form schemas MUST be kept
separate when their shapes differ — do not contort one to serve both. Form components MUST
provide accessible labels, validation messages, disabled/loading states, and submit error
handling.

**Dashboard shell and navigation**

- Authenticated routes MUST render inside the dashboard shell.
- The dashboard shell owns shared layout regions: header, sidebar/navigation, mobile navigation,
  account menu area, and the main content outlet. Features render into the outlet; they do not
  re-implement shell regions.
- Navigation MUST be data-driven from a single source of truth.
- Navigation items SHOULD support label, path, icon, grouping, active matching, and optional
  access rules.
- Desktop navigation and mobile navigation MUST use the same navigation model whenever practical;
  divergence requires a documented reason in the feature plan.

## Development Workflow

- Feature branches follow the Spec Kit naming convention (`###-feature-name`).
- Every feature requires: spec → plan → tasks → implementation, in that order.
- Pull requests MUST reference the relevant spec and pass all CI checks before merge.
- EF Core migrations are versioned and forward-only by default. Destructive migrations (column
  drops, type changes that lose data, constraint removals that allow new invalid data) require
  explicit approval recorded in the PR description.
- The OpenAPI contract for any feature MUST be committed before implementation tasks begin
  (enforces Principle I).
- Features that do not add or change backend endpoints MUST explicitly record "No API contract
  changes required" in the plan. UI-only features still require specs, plans, tasks, and frontend
  tests, but do not require artificial OpenAPI changes.
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

All code reviews MUST verify compliance with Principles I–VI and the Frontend Conventions.
Violations require documented justification in the PR description before approval.

**Version**: 1.2.0 | **Ratified**: 2026-06-08 | **Last Amended**: 2026-06-12
