<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.0 → 1.0.1  [patch: clarified frontend to React]

Added sections:
  - Core Principles (5 principles)
  - Technology Stack
  - Development Workflow
  - Governance

Templates requiring updates:
  - .specify/templates/plan-template.md  ✅ Constitution Check section is generic — no update required
  - .specify/templates/spec-template.md  ✅ Aligned — no update required
  - .specify/templates/tasks-template.md ✅ Aligned — no update required

Deferred TODOs:
  - None
-->

# NajaEchoPortal Constitution

NajaEchoPortal is a full-stack web application providing org-management utilities for the Naja Echo
organisation in the game Star Citizen. It consists of a .NET backend API and a separate JavaScript
SPA frontend.

## Core Principles

### I. API-Contract-First (NON-NEGOTIABLE)

Every endpoint MUST be defined in an OpenAPI (Swagger) contract before any implementation code is
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

The backend (ASP.NET Core Web API) and the frontend (JavaScript SPA) are independently deployable
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

All API endpoints MUST emit structured logs (JSON) at a minimum. Errors MUST include correlation
IDs traceable across the frontend request and backend handler. Health-check endpoints MUST be
present and monitored. Observability is not optional polish — it ships with the feature.

**Rationale**: Org tooling is operated by a small team; fast incident diagnosis requires built-in
observability from day one.

## Technology Stack

- **Backend**: ASP.NET Core Web API (.NET 8+), C#
- **Frontend**: React — SPA calling the .NET API
- **API specification**: OpenAPI 3.x (Swagger)
- **Testing — backend**: xUnit, FluentAssertions, Testcontainers (integration)
- **Testing — frontend**: Vitest or Jest + Testing Library
- **Database**: NEEDS CLARIFICATION — to be decided per feature (likely PostgreSQL or SQLite)
- **CI**: GitHub Actions (or equivalent); all tests MUST pass before merge
- **Auth**: NEEDS CLARIFICATION — to be decided before first feature that requires identity

All dependency additions MUST be reviewed for licence compatibility and security posture.

## Development Workflow

- Feature branches follow the Spec Kit naming convention (`###-feature-name`).
- Every feature requires: spec → plan → tasks → implementation, in that order.
- Pull requests MUST reference the relevant spec and pass all CI checks before merge.
- Database migrations are versioned and applied forward-only; destructive migrations require
  explicit approval.
- Secrets MUST NOT be committed to the repository; use environment variables or a secrets manager.
- The OpenAPI contract for any feature MUST be committed before implementation tasks begin
  (enforces Principle I).

## Governance

This constitution supersedes all other stated practices. Amendments require:

1. A written rationale explaining what changed and why.
2. A version bump following semantic versioning:
   - MAJOR: principle removal, redefinition, or backward-incompatible governance change.
   - MINOR: new principle or materially expanded guidance.
   - PATCH: clarifications, wording fixes, non-semantic refinements.
3. Updates to all affected templates and this Sync Impact Report.
4. Approval recorded in the commit message.

All code reviews MUST verify compliance with Principles I–V. Violations require documented
justification in the PR description before approval.

**Version**: 1.0.1 | **Ratified**: 2026-06-08 | **Last Amended**: 2026-06-08
