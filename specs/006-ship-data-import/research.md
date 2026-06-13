# Phase 0 Research: Ship Data Import

This document records the decisions that resolve the open questions from the spec and plan, with rationale
and the alternatives considered. All `NEEDS CLARIFICATION` items from Technical Context are resolved here.

## 1. Admin authorization mechanism

- **Decision**: Activate the existing (but unused) ASP.NET Identity roles schema. Seed an `Admin` role at
  startup; emit the signed-in user's roles as `ClaimTypes.Role` claims into the auth cookie on sign-in;
  protect admin endpoints and `/dashboard/admin/*` routes with an `Admin` authorization policy.
- **Rationale**: The DB already has the Identity role tables (`IdentityDbContext<ApplicationUser,
  IdentityRole<Guid>, Guid>` + `AddRoles<>()`); reusing them is the lowest-new-surface way to get real,
  reusable admin authz that every future admin feature can share. Chosen by the user over lighter options.
- **Alternatives considered**:
  - *Config allow-list of admin IDs*: lighter, but a throwaway mechanism we'd have to replace the moment a
    second role appears; doesn't use the schema already in place.
  - *Authenticated-only (defer admin gating)*: contradicts FR-001; rejected.

## 2. Granting admin membership

- **Decision**: Manual database assignment. The `Admin` role row is seeded by code; membership is granted
  by inserting an `AspNetUserRoles` row by hand (documented in `quickstart.md`). No role-management UI.
- **Rationale**: YAGNI — there is no requirement for self-service role management this pass, and the org is
  operated by a small team. Chosen by the user.
- **Alternatives considered**:
  - *Seed admins from configuration (list of Discord IDs)*: reproducible across environments, but adds
    config plumbing not yet needed.
  - *First-user-becomes-admin*: convenient bootstrap, but implicit and risky; rejected.
- **Consequence (documented caveat)**: Roles are written into the cookie at sign-in. A user granted admin
  while already signed in must sign out/in (or wait out the 24h cookie lifetime) before the role applies.
  Acceptable for manual assignment; revisit if/when self-service grants arrive.

## 3. Ship data storage model

- **Decision**: Hybrid — promoted, typed, indexed columns (`uex_id` unique, `uuid`, `name`, `name_full`,
  `company_name`, `status`, `imported_at`, `updated_at`, `soft_deleted_at`) plus `raw_data jsonb` holding
  the verbatim feed record (all 64 fields).
- **Rationale**: Preserves 100% of feed fields (SC-004) and survives upstream schema additions with no
  migration, while still giving fast, sortable, indexable access to the values we actually query and
  display. The detail sheet renders directly from `raw_data`. Chosen by the user.
- **Alternatives considered**:
  - *Full relational mapping of all 64 columns*: maximal queryability, but a large, brittle migration that
    breaks on every upstream schema change — over-engineered for a read-only mirror.
  - *Raw JSONB only*: simplest, but no typed/indexed access even for name/company; weak list querying.

## 4. External feed integration ownership

- **Decision**: The backend fetches the UEX feed server-side via a typed `HttpClient` behind the
  `IUexVehicleClient` port. The SPA only ever calls our own REST API.
- **Rationale**: Constitution III forbids the frontend talking to external services directly. Server-side
  fetch is also required to run the import inside a DB transaction, to keep counts/soft-delete bookkeeping
  authoritative, and to emit structured logs.
- **Alternatives considered**: *Browser fetches UEX, posts to backend* — violates Constitution III, exposes
  CORS/feed quirks to the client, and can't be transactional; rejected.

## 5. Import algorithm: transactional upsert + soft-delete + reactivate

- **Decision**: Single server-side transaction. Match incoming records to stored ships by `uex_id`. Insert
  new (`Active`); update existing promoted columns + `raw_data`; reactivate any matched record that was
  `SoftDeleted`; soft-delete any stored `Active` record absent from the feed. Commit atomically; roll back
  entirely on any error. Guard: if the feed returns zero records, abort before any mutation.
- **Rationale**: Directly satisfies FR-004/005/009/010/011 and the zero-record and mid-failure edge cases.
  A transaction is the simplest correct way to get all-or-nothing semantics (FR-005).
- **Alternatives considered**:
  - *Delete-all-then-insert*: trivially loses soft-delete history and the reactivation signal; a failed run
    could wipe data; rejected.
  - *Per-record autocommit*: can't satisfy all-or-nothing rollback; rejected.

## 6. Concurrency control (single-flight)

- **Decision**: In-memory `SemaphoreSlim(1,1)` behind a singleton `IImportCoordinator`; `TryAcquire()` with
  zero wait. If not acquired → `409 Conflict`. Released in `finally`.
- **Rationale**: Matches FR-003 ("only one import at a time") with minimal machinery for the current
  single-instance deployment. No cooldown is enforced (clarified) — the lock is the sole guard.
- **Alternatives considered**: *PostgreSQL advisory lock* — needed only for multi-instance deployments;
  documented as a future upgrade, not built now (YAGNI).

## 7. Upsert identity key & quirky field types

- **Decision**: Use UEX `id` (integer) as the stable upsert key; also store `uuid`. Store all feed values
  verbatim in `raw_data` without coercion (`is_*` stay int 0/1, dates stay unix epoch ints, `crew` stays a
  string). Format for humans only at the presentation layer.
- **Rationale**: Empirically all 278 records share an identical 64-key shape and a unique `id`. Faithful
  storage guarantees no data loss (SC-004) and keeps the importer simple and robust to type weirdness.
- **Alternatives considered**: *Coerce types on import* (bool/datetime/number) — lossy and brittle if the
  feed sends unexpected values; rejected in favor of store-raw / format-on-read.

## 8. Backend integration tests need a real database

- **Decision**: Add **Testcontainers (PostgreSQL)** to `NajaEcho.Infrastructure.Tests` for repository
  tests (paging, JSONB round-trip, transactional rollback, soft-delete/reactivate). Keep Application
  algorithm tests fast against fakes; keep API tests on the existing `WebApplicationFactory` pattern.
- **Rationale**: The existing test stack uses EF InMemory, which models neither `jsonb` nor transactions —
  exactly the behaviours this feature depends on. Constitution II also requires at least one real-database
  integration test. Testcontainers is the standard, CI-friendly way to get a real Postgres per test run.
- **Alternatives considered**: *EF InMemory* — cannot represent JSONB or transaction rollback; would give
  false confidence; rejected for the repository layer.

## 9. OpenAPI contract & frontend type strategy

- **Decision**: Author `contracts/openapi.yaml` for the three new admin endpoints plus the `/api/auth/me`
  `roles` extension (Constitution I). Generate TypeScript API-boundary types from the contract via
  `openapi-typescript` (T068, mandatory Phase 1 task, output `frontend/src/lib/api/ships.d.ts`). Feature-
  owned Zod schemas in `shipSchemas.ts` **validate and narrow** those generated types at runtime — they do
  not re-define or duplicate the contract shape.
- **Rationale**: Constitution III Frontend Conventions mandate that API request/response types be generated
  from the OpenAPI contract; hand-written API-boundary types are forbidden. The generated types are the
  canonical shape source. Zod is used for runtime parsing/validation (rejecting malformed responses) and for
  ergonomic type inference in components — a complementary role, not a replacement. The existing auth-feature
  Zod pattern pre-dates the v1.2.0 Constitution requirement and will be migrated separately; this feature
  must comply with the current Constitution.
- **Alternatives considered**: *Hand-written Zod schemas as the sole type source* — the pre-v1.2.0 pattern;
  now explicitly prohibited by Constitution III Frontend Conventions for API-boundary types. Rejected.

## 10. UI building blocks (tabs, table, detail sheet)

- **Decision**: Add a generic `Tabs` primitive (`components/ui/tabs.tsx`, wrapping the already-installed
  `@radix-ui/react-tabs`) for the page's data-type tab strip, and a generic `Table` primitive
  (`components/ui/table.tsx`, styled HTML, no new dependency) for the ship list. Reuse the existing `Sheet`
  primitive (`side="right"`) for the detail view.
- **Rationale**: Tabs are the user-requested layout and the natural extensibility seam for future data
  types (FR-012). `@radix-ui/react-tabs` is already a dependency in `package.json` — note: confirm at
  implementation time; if absent it is MIT and in the approved Radix family. The shadcn Table is plain
  styled markup (no Radix). The right-side Sheet already exists, so the detail view needs no new primitive.
- **Alternatives considered**: *Build bespoke tab/table markup in the feature folder* — violates the
  shadcn-primitive convention (Constitution VI / Frontend Conventions) and isn't reusable; rejected.

## 11. Navigation: Admin group + access gating

- **Decision**: Extend the single data-driven nav source (`navItems`) with an optional `group` label and
  use the existing `access?` field (`access: 'admin'`) on the Data Import item. `DashboardNav` renders
  optional group headings and filters items whose `access` the current session lacks. Desktop and mobile
  nav consume the same model.
- **Rationale**: Constitution's Frontend Conventions require a single source of truth for navigation that
  supports grouping and optional access rules — the `NavItem` type already reserves `access?`. This is the
  intended extension point.
- **Alternatives considered**: *Separate hard-coded admin nav* — duplicates the nav model and diverges
  desktop/mobile; rejected.
