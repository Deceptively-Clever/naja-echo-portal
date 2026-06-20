# Implementation Plan: Character Registration & RSI Verification

**Branch**: `015-character-registration` | **Date**: 2026-06-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/015-character-registration/spec.md`

## Summary

Add a **Characters** section to the existing member **Profile** page that lets an authenticated member
prove ownership of a Star Citizen in-game handle and link it to their account. The flow is a
token-on-bio verification:

1. The member starts registration and receives a unique, high-entropy, 30-minute verification token
   to paste into the bio of their public RSI citizen profile.
2. The member submits their handle; the backend fetches
   `https://robertsspaceindustries.com/en/citizens/{handle}`, scrapes the HTML for the token, and on a
   match creates a `Character` record linked to the member — storing the page's RSI **Community Moniker**
   (display name, e.g. `G8trdone` for handle `g8r`) as the character name.
3. The member sees all of their verified characters listed on the Profile page.

The dominant backend precedent is the **warehouse** features (011/012/014): a feature-folder use-case
layout (`Application/Features/<Feature>/<UseCase>/`), Minimal-API endpoints in a single grouped
endpoint class, repositories implementing Application ports, RFC-7807 `Results.Problem` error mapping,
and Serilog logging with the caller id. The dominant **outbound-HTTP** precedent is the UEX clients
(`UexVehicleClient` et al.): a typed `HttpClient` registered via `AddHttpClient<TInterface, TImpl>`
implementing an Application port. The frontend follows the established `apiFetch` + Zod + React Hook
Form + TanStack Query conventions, embedding a new `features/characters/` section into the existing
`ProfilePage`.

New backend HTTP behaviour — a new `/api/characters` group (all endpoints require authentication; each
is scoped to the calling member):

- `GET  /api/characters` — list the caller's verified characters.
- `GET  /api/characters/registration` — return the caller's current non-expired pending token (if any),
  for rehydrating the token + countdown on page load.
- `POST /api/characters/registration` — start registration: return the existing non-expired pending
  token if one exists, otherwise generate a fresh one (FR-010).
- `POST /api/characters/verify` — submit a handle (and optional display name), perform the RSI scrape,
  and create the character on success.

**Schema change IS required**: two new tables — `characters` and `pending_character_registrations` —
via one EF Core migration. **API contract changes ARE required**: four new endpoints in a new
`/api/characters` group, defined in `contracts/openapi.yaml`. This is **not** a UI-only feature, so the
constitution's "No API contract changes required" exemption does **not** apply.

## Technical Context

**Language/Version**: C# on .NET (`net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql` (snake_case naming),
ASP.NET Core Identity (cookie auth; `AspNetUsers` is the owner principal), a typed `HttpClient` for the
RSI scrape (registered via `AddHttpClient`), **AngleSharp** (new dependency) to parse the Community
Moniker out of the RSI HTML, Serilog. Frontend — React 19 (Vite), React Router data APIs, Tailwind CSS 4,
shadcn/ui (Radix), Lucide, TanStack Query 5, React Hook Form + Zod, `apiFetch`. **No new shadcn/ui
primitive is required** — the section reuses existing `card`, `button`, `input`, `badge`, and
toast/sonner primitives already in `components/ui/`. The single new backend package, AngleSharp
(MIT-licensed), is subject to the constitution's licence/security review.

**Storage**: PostgreSQL 16. Two new tables via one code-first EF migration:
- `characters` — `id`, `owner_user_id` (FK → `AspNetUsers`), `name`, `handle`, `created_at`; **unique
  index on `lower(handle)`** to enforce case-insensitive global handle uniqueness (FR-005, SC-002).
- `pending_character_registrations` — `id`, `owner_user_id` (FK → `AspNetUsers`), `token`,
  `expires_at`, `created_at`; **unique index on `owner_user_id`** (at most one pending registration per
  member, enabling token reuse per FR-010).

`AspNetUsers` is referenced read-only as the owner. No external catalog table is involved.

**Testing**: Backend — xUnit, FluentAssertions; Application handler unit tests with a fake
`IRsiCitizenClient` and fake repositories (token reuse vs fresh, token-found success, token-not-found,
token-expired, duplicate-handle, RSI-not-found, RSI-unreachable, verification ordering); at least one
Testcontainers (PostgreSQL) integration test exercising the real tables, the `lower(handle)` unique
index, and the one-pending-per-user unique index; an API endpoint test asserting status codes and
RFC-7807 problem mapping with the `HttpClient` faked. Frontend — Vitest + React Testing Library with
MSW, mirroring the existing feature test suites (token display + countdown, verify success appends to
list, each error message renders, empty state).

**Target Platform**: Linux server (containerized API) + browser SPA.

**Performance Goals**: A registration completes in under 5 minutes excluding RSI-bio editing (SC-001);
the verify round-trip is dominated by a single outbound RSI fetch. The RSI fetch uses a bounded timeout
so a slow/unreachable RSI surfaces as a prompt "try again" error rather than hanging (edge case).
Character lists are single-digit-to-low-tens of rows per member; a single indexed query suffices — no
pagination.

**Constraints**:
- Token is high-entropy and unguessable; 30-minute validity window (FR-001, FR-002, FR-006). Window is
  a single named constant.
- A member has at most one pending registration at a time; re-initiating before expiry returns the same
  token, after expiry generates a fresh one (FR-010, US1 scenarios 2–3).
- Handle uniqueness is **global and case-insensitive** for duplicate detection; the handle is **stored
  exactly as the member typed it** (FR-005, edge cases). Enforced both by an application pre-check (for
  a clean error message) and by the `lower(handle)` DB unique index (for the race guard).
- Duplicate-handle is rejected **regardless of whether the token was found**, so the duplicate check
  runs **before** the RSI fetch (US2 scenario 4).
- Verification reads the public RSI page over plain HTTP scraping; no RSI API key or auth (assumption).
  **Two** values are consumed from the page: token presence, and the **Community Moniker** (display
  name) used as the character `name`. No other RSI profile data (avatar, org, account age) is scraped.
- Character `name` is the **scraped RSI Community Moniker** (e.g. `G8trdone` for handle `g8r`), not a
  member-entered value; it falls back to the handle if the moniker can't be parsed. The member supplies
  only the handle. See [research.md](./research.md) R1/R4. **This narrowly overrides the spec's original
  "member-chosen name" assumption per explicit product-owner direction; update the spec's Assumptions.**
- Out of scope (v1): character deletion/deregistration, re-verification of an already-claimed handle,
  syncing avatar/org/account-age, any admin/cross-member character management.

### Verified existing facts (from codebase inspection)

- **Auth / owner identity**: Minimal-API endpoints take a `ClaimsPrincipal user` and resolve the owner
  via `TryGetUserId` (`user.FindFirstValue(ClaimTypes.NameIdentifier)` → `Guid`). The new endpoints
  reuse this exact helper pattern. `AppDbContext` already maps Identity tables; owner FK targets
  `AspNetUsers`.
- **Endpoint group pattern**: `WarehouseEndpoints.MapWarehouseEndpoints` maps a group with
  `app.MapGroup("/api/...").RequireAuthorization()`, one static handler per route, Serilog `Log.Information`
  with the caller id, and per-exception `catch` blocks returning `Results.Problem(detail, statusCode, title)`.
  A new `CharacterEndpoints.MapCharacterEndpoints` mirrors this for `/api/characters` and is registered
  alongside the existing groups in API composition.
- **Outbound HTTP pattern**: `UexVehicleClient(HttpClient http, ILogger<…> logger)` implements an
  Application port and is registered with `services.AddHttpClient<IUexVehicleClient, UexVehicleClient>(client => …)`
  in `Infrastructure/DependencyInjection.cs`. The RSI client mirrors this: `IRsiCitizenClient` in
  Application (returning an `RsiCitizenPage` with page content + parsed display name, or a
  not-found/unreachable outcome), `RsiCitizenClient` in Infrastructure (fetch + AngleSharp moniker
  parse), registered with a base address of `https://robertsspaceindustries.com/` and a bounded timeout.
- **Persistence pattern**: entities are POCOs in `NajaEcho.Domain/<Feature>/`; an
  `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/` sets snake_case columns,
  check constraints, unique indexes (e.g. `WarehouseMaterialEntryConfiguration` uses `builder.HasIndex(...).IsUnique().HasDatabaseName(...)`),
  and FKs with `OnDelete`. A `DbSet<T>` is added to `AppDbContext`. Migrations are code-first and applied
  via `./migrate.sh`.
- **Repository pattern**: repositories (e.g. `MaterialInventoryRepository(AppDbContext db)`) implement an
  Application `Abstractions` port; reads may use `db.Database.SqlQuery<...>` projections or LINQ. The
  character/pending repositories are small and use straight LINQ/`DbSet` operations.
- **Profile page**: `frontend/src/features/dashboard/pages/ProfilePage.tsx` is the existing profile route
  (`/dashboard/profile` in `AppRouter.tsx`). It renders an Account card and consumes
  `useCurrentUser()`. The new Characters section is a `features/characters/` component embedded into this
  page; the route stays thin and lives in `dashboard`. No nav change required (Profile is reached via the
  account menu, not `navItems.ts`).
- **Frontend feature pattern**: `features/warehouse/` owns `api/` (`apiFetch` wrappers), `hooks/`
  (TanStack Query hooks + a `queryKeys` factory), `schemas/` (Zod), `components/`, `pages/`, and
  `__tests__/`. The new `features/characters/` folder follows the same shape.
- **Zod schema approach (constitution note)**: Frontend request/response types in
  `features/characters/schemas/` are hand-written Zod schemas derived from and reviewed against
  `contracts/openapi.yaml`. This is consistent with the established `features/warehouse/` pattern
  and satisfies the constitution's Frontend Conventions intent — the OpenAPI contract remains the
  single source of truth; schemas are manually verified against it during code review. No additional
  code-generation tool is introduced (YAGNI — Principle IV).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | PASS | Four new endpoints are defined in `contracts/openapi.yaml` before implementation. Not a UI-only feature, so the contract-exemption clause is not invoked. |
| II. Test-First / TDD | PASS | Plan mandates failing tests first: Application handler unit tests (token reuse/fresh, found/not-found/expired, duplicate handle, RSI not-found/unreachable, check ordering) with fake `IRsiCitizenClient`; ≥1 Testcontainers integration test through the real tables (the `lower(handle)` unique index and one-pending-per-user index); API endpoint tests for status/problem mapping; frontend component/hook tests mirroring existing suites. |
| III. Frontend/Backend Separation | PASS | Frontend consumes only `/api/characters*` via `apiFetch`; no server-rendered HTML, no DB access from the SPA. Request/response shapes are governed by the OpenAPI contract. |
| IV. Simplicity / YAGNI | PASS | Reuses warehouse + UEX-client patterns wholesale. Token-on-bio scrape is the spec's chosen mechanism; no RSI API, no deletion/re-verify, no pagination, no new UI primitive. The one new dependency (AngleSharp) is justified by a concrete requirement — structured extraction of the Community Moniker (R1/R4) — not speculation, and is confined to the Infrastructure RSI client. Two tables justified by two distinct lifecycles (durable character vs. ephemeral pending token). |
| V. Observability | PASS | Each endpoint emits structured Serilog logs with the caller id and outcome. **No token, handle-page HTML, or RSI response body is logged** — only the handle, the parsed display name, the outcome, and counts. The verification token is treated as a secret in logs. |
| VI. Modular Monolith + Clean Architecture | PASS | Domain entities in `NajaEcho.Domain/Characters`; use cases in `NajaEcho.Application/Features/Characters/<UseCase>/`; EF config, migration, repositories, and `RsiCitizenClient` in `NajaEcho.Infrastructure`; endpoints in `NajaEcho.Api`. `IRsiCitizenClient` is an Application port implemented in Infrastructure — dependencies point inward only. Frontend logic lives in feature-owned hooks/schemas; the Profile route stays thin. |

**Result**: PASS — no violations. Complexity Tracking table not required.

## Project Structure

### Documentation (this feature)

```text
specs/015-character-registration/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output — the four new endpoints
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Characters/
│       ├── Character.cs                              # new entity (owner, name, handle, created_at)
│       └── PendingCharacterRegistration.cs           # new entity (owner, token, expires_at) + Create/IsExpired
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   ├── ICharacterRepository.cs                   # new port (list, add, handle-exists)
│   │   ├── IPendingRegistrationRepository.cs         # new port (get-by-owner, upsert, remove)
│   │   └── IRsiCitizenClient.cs                      # new outbound-HTTP port → RsiCitizenPage(content, displayName) | not-found | unreachable
│   └── Features/Characters/
│       ├── GetCharacters/                            # query + handler + CharacterDto
│       ├── GetRegistration/                          # query + handler + PendingRegistrationDto
│       ├── StartRegistration/                        # command + handler (reuse-or-create token)
│       └── VerifyCharacter/                          # command + handler + domain exceptions
│           # TokenExpiredException, TokenNotFoundException, HandleAlreadyClaimedException,
│           # RsiProfileNotFoundException, RsiUnreachableException
├── NajaEcho.Infrastructure/
│   ├── Characters/
│   │   ├── CharacterRepository.cs                    # LINQ reads/writes; case-insensitive handle check
│   │   ├── PendingRegistrationRepository.cs          # get/upsert/remove by owner
│   │   └── RsiCitizenClient.cs                       # typed HttpClient scrape (200 → page content + AngleSharp-parsed moniker, 404 → not-found, error → unreachable)
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   ├── CharacterConfiguration.cs             # table, columns, lower(handle) unique index, owner FK
│   │   │   └── PendingCharacterRegistrationConfiguration.cs # table, columns, unique owner index, FK
│   │   └── Migrations/
│   │       └── <ts>_AddCharacterRegistration.cs      # one new migration (two tables)
│   └── DependencyInjection.cs                        # +AddHttpClient<IRsiCitizenClient, RsiCitizenClient>, +repo registrations (edited)
└── NajaEcho.Api/
    └── Features/Characters/
        ├── CharacterEndpoints.cs                     # /api/characters group: list, get/start registration, verify
        └── Contracts/
            └── CharacterDtos.cs                      # request/response records (StartResponse, VerifyRequest, CharacterResponse, …)
        # + registration of MapCharacterEndpoints in API composition (Program/endpoint wiring, edited)

backend/tests/
├── NajaEcho.Application.Tests/Features/Characters/   # StartRegistration + VerifyCharacter handler unit tests (fake RSI client + repos)
├── NajaEcho.Infrastructure.Tests/Characters/         # Testcontainers test: lower(handle) unique + one-pending-per-user
└── NajaEcho.Api.Tests/Features/Characters/CharacterEndpointTests.cs  # status codes + RFC-7807 mapping

frontend/src/
└── features/characters/
    ├── api/charactersApi.ts                          # apiFetch wrappers: list, getRegistration, startRegistration, verify
    ├── hooks/
    │   ├── characterQueryKeys.ts                     # typed key factory
    │   ├── useCharacters.ts                          # list query
    │   ├── useRegistration.ts                        # pending-token query (for countdown rehydrate)
    │   ├── useStartRegistration.ts                   # mutation → sets/returns token
    │   └── useVerifyCharacter.ts                     # mutation → invalidates character list, surfaces errors
    ├── schemas/characterSchemas.ts                   # Zod: verify form (handle only), API response parsing
    ├── components/
    │   ├── CharacterRegistrationSection.tsx          # the section embedded into ProfilePage (composes the below)
    │   ├── CharacterList.tsx                          # verified characters + empty state
    │   ├── RegistrationTokenCard.tsx                 # token display, copy-to-clipboard, 30-min countdown
    │   └── VerifyCharacterForm.tsx                   # handle input only, submit, error messages
    └── __tests__/                                    # component/hook tests (MSW)

frontend/src/features/dashboard/pages/ProfilePage.tsx  # renders <CharacterRegistrationSection /> below Account card (edited)
```

**Structure Decision**: Create a new `features/characters/` frontend feature and a new
`/api/characters` backend group + `Features/Characters/` use-case folders, rather than overloading an
existing feature. The Characters section is **embedded into the existing `ProfilePage`** (which stays in
`features/dashboard` and remains a thin route), satisfying the spec's "add a Characters section to the
profile page". Backend layering follows the four-project Clean Architecture split already in place; the
RSI scrape reuses the established typed-`HttpClient` + Application-port pattern from the UEX clients.

## Complexity Tracking

> No constitution violations — table intentionally empty.
