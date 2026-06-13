# Implementation Plan: Hangar

**Branch**: `007-hangar-fleet-view` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-hangar-fleet-view/spec.md`

## Summary

Members get a **Hangar** area with two sub-views — **My Hangar** (the ships the current
member owns) and **Org Hangar** (a derived, aggregated view of every member's hangar,
grouped by unique ship model with an owner count and hover owner list). Both views render a
shared visual **card gallery** with infinite scroll, name search, and a card that uses the
ship image as its background (default background when absent or on image load failure). My
Hangar adds an **Add Ship** dialog (search the ship catalog by name, duplicate-proof) and a
hover **remove** action.

**Ship data source of truth is the existing `sc.ships` catalog (feature 006); no new ship
catalog table is created.** A new ownership table links members to catalog ships by the
catalog's primary key. Card/search fields come from `sc.ships`: `name` and `company_name`
are promoted columns; `url_photo`, `scu`, and `crew` are extracted from the `raw_data`
(`jsonb`) verbatim UEX feed object.

## Technical Context

**Language/Version**: C# on .NET (repo targets `net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql.EntityFrameworkCore.PostgreSQL`, FluentValidation, Serilog. Frontend — React 19 (Vite 8), React Router data APIs, Tailwind CSS 4, shadcn/ui (Radix), Lucide, TanStack Query 5, React Hook Form + Zod, `openapi-typescript` 7 for generated types.

**Storage**: PostgreSQL 16. Existing `sc.ships` (catalog, read-only here). New ownership table for hangar entries, EF Core code-first migration (additive).

**Testing**: Backend — xUnit, FluentAssertions, Testcontainers (PostgreSQL), `WebApplicationFactory`. Frontend — Vitest + React Testing Library + MSW.

**Target Platform**: Linux server (containerized API) + browser SPA.

**Project Type**: Web application (separate backend API + React SPA).

**Performance Goals**: My Hangar visible within 2s under normal conditions (SC-001); search results update within 500ms of stop-typing (SC-003); infinite scroll with no pagination controls (SC-006).

**Constraints**: No server-rendered HTML; frontend talks to backend only through the versioned OpenAPI contract; generated types only. Card readability required with or without a background image.

**Scale/Scope**: Single organization (Naja Echo). One deployment = one org; "organization members" = all authenticated `ApplicationUser`s in this version. Catalog is on the order of a few thousand ship rows; per-member hangars are small (tens of ships).

### Verified existing facts (from codebase inspection)

- **Catalog table**: `sc.ships`, mapped by `NajaEcho.Domain.Ships.Ship` via `ShipConfiguration`.
- **Catalog identity / ownership key**: primary key is **`sc.ships.id` (`Guid`)**. `uex_id` (`int`) is a unique business key. **Hangar ownership references `sc.ships.id`.**
- **Promoted columns available**: `name`, `company_name` (also `name_full`, `uuid`, `status`, timestamps).
- **`url_photo`, `scu`, `crew` are NOT columns** — they are keys inside `raw_data` (`jsonb`), the verbatim UEX feed object (all 64 fields). Per 006 research, values are stored without coercion (`crew` stays a string).
- **Catalog has soft-delete**: `status` / `soft_deleted_at`. Hangar reads MUST only surface `Active` catalog ships in search; existing entries pointing at a soft-deleted ship are handled per research.
- **Member model**: `NajaEcho.Infrastructure.Identity.ApplicationUser : IdentityUser<Guid>` with `DisplayName`. No separate Organization entity exists yet.
- **Current user in endpoints**: `ClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)` → user `Guid` (see `AuthEndpoints.Me`).
- **Navigation**: data-driven from `frontend/src/features/dashboard/navigation/navItems.ts` (`NavItem` supports `label`, `path`, `icon`, `group`, `access`, `end`).
- **Type generation**: per-feature `openapi-typescript` scripts in `frontend/package.json` (`gen:api*`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. API-Contract-First** — Hangar adds new backend HTTP behaviour (list/search/add/remove). **API contract changes ARE required.** `contracts/openapi.yaml` is authored in Phase 1 before implementation. *Not* a UI-only feature. ✅
- **II. Test-First / TDD** — Plan mandates failing tests first: Application unit tests (read models, raw_data extraction, duplicate guard), Testcontainers-backed integration test per endpoint, frontend component/hook tests. ✅
- **III. Frontend/Backend Separation** — Frontend consumes generated types from the contract; no direct DB access; no server HTML. ✅
- **IV. Simplicity / YAGNI** — Reuse `sc.ships` (no new catalog table). One additive ownership table. Org Hangar is a derived query, not a stored aggregate. Single-org assumption avoids a premature Organization model. ✅
- **V. Observability** — Endpoints emit structured logs with correlation IDs via existing Serilog setup; no sensitive auth data logged. ✅
- **VI. Modular Monolith + Clean Architecture** — Backend feature folders under `Application/Features/Hangar/*`, ownership entity in `Domain`, EF/repository in `Infrastructure`, endpoints in `API`. Frontend `features/hangar/` owns pages, components, hooks, schemas, api, tests; thin route component; shared card lives in the feature until a second feature needs it. ✅

No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/007-hangar-fleet-view/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output (Hangar endpoints)
├── checklists/          # (existing)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Hangar/
│       └── HangarEntry.cs                      # ownership entity (UserId, ShipId, AddedAt)
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   └── IHangarRepository.cs                # read/write port
│   └── Features/Hangar/
│       ├── GetMyHangar/                        # query + handler + ShipCard read model
│       ├── GetOrgHangar/                       # query + handler + grouped card + owners
│       ├── SearchCatalogShips/                 # query + handler (name search, alreadyOwned)
│       ├── GetOwningMembers/                   # query + handler (members owning >=1 ship)
│       ├── AddShipToHangar/                    # command + handler + validator (dup guard)
│       └── RemoveShipFromHangar/               # command + handler
├── NajaEcho.Infrastructure/
│   ├── Hangar/
│   │   └── HangarRepository.cs                 # EF + Npgsql jsonb extraction
│   └── Persistence/
│       ├── Configurations/HangarEntryConfiguration.cs
│       └── Migrations/<timestamp>_AddHangarEntries.cs
└── NajaEcho.Api/
    └── Features/Hangar/
        ├── HangarEndpoints.cs                  # MapGroup("/api/hangar")
        └── Contracts/                          # request/response DTOs

backend/tests/
├── NajaEcho.Application.Tests/Features/Hangar/ # read-model + dup-guard unit tests
├── NajaEcho.Infrastructure.Tests/Hangar/       # raw_data extraction + repo tests
└── NajaEcho.Api.Tests/Features/Hangar/         # endpoint integration tests (Testcontainers)

frontend/src/
├── features/hangar/
│   ├── pages/HangarPage.tsx                    # thin route: tabs + outlet
│   ├── pages/MyHangarView.tsx
│   ├── pages/OrgHangarView.tsx
│   ├── components/ShipCardGallery.tsx          # shared grid + infinite scroll
│   ├── components/ShipCard.tsx                 # shared card (image bg / default bg)
│   ├── components/AddShipDialog.tsx
│   ├── components/OwnerCountBadge.tsx          # count + hover owner list (org only)
│   ├── components/RemoveShipButton.tsx         # hover overlay + confirm
│   ├── hooks/                                  # TanStack Query hooks + key factory
│   ├── schemas/                                # Zod view-model schemas
│   ├── api/                                    # feature client over generated types
│   └── __tests__/
└── lib/api/hangar.d.ts                         # generated from contracts/openapi.yaml
```

**Structure Decision**: Web application with the established Clean Architecture layering
(`Domain` → `Application` → `Infrastructure` → `API`) and frontend feature folders. The
Hangar feature adds one ownership aggregate (`HangarEntry`) and one frontend feature folder
(`features/hangar/`). The card grid and `ShipCard` are **shared between My Hangar and Org
Hangar** as feature-owned components (single feature owns both, so they stay in
`features/hangar/`, not `components/shared/`). A new nav item is appended to `navItems.ts`.

## Complexity Tracking

> No constitution violations — section intentionally empty.
