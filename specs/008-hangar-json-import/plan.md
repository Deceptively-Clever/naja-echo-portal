# Implementation Plan: Hangar JSON Import

**Branch**: `008-hangar-json-import` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-hangar-json-import/spec.md`

## Summary

Add an **Import** action to the **My Hangar** view (feature 007) that lets a member replace
their entire hangar from a HangarXPLOR JSON export. The user clicks **Import**, acknowledges a
destructive warning ("all existing hangar ships will be replaced"), selects a `.json` file, and
the system: parses the array, matches each record to the existing `sc.ships` catalog **by name**
(`ship_name` first, falling back to `name`, case-insensitive, Active ships only), skips records
flagged `unidentified` or with no catalog match, then **atomically replaces** all of the member's
`sc.hangar_entries` with the matched set. A summary (imported / unmatched counts) is shown.

**No schema change is required** — the feature reuses the existing `sc.hangar_entries` table and
its `(user_id, ship_id)` unique constraint from feature 007. **No EF migration is required.**

**Reconciliation with spec FR-009**: The spec's FR-009 ("duplicate ship names produce multiple
hangar entries") is **not implementable** against the existing `ux_hangar_entries_user_ship`
unique constraint, which permits a member to own a given catalog ship at most once. This plan
therefore **de-duplicates** matched ships: multiple import records resolving to the same catalog
ship collapse into a single hangar entry. See [research.md](./research.md) decision R4. The spec
should be updated to reflect de-duplication; all other requirements are satisfied as written.

## Technical Context

**Language/Version**: C# on .NET (repo targets `net10.0`) backend; TypeScript (strict) frontend.

**Primary Dependencies**: Backend — ASP.NET Core Minimal APIs, EF Core + `Npgsql`, Serilog.
Frontend — React 19 (Vite), React Router data APIs, Tailwind CSS 4, shadcn/ui (Radix), Lucide,
TanStack Query 5, Zod, `openapi-typescript` for generated types.

**Storage**: PostgreSQL 16. Existing `sc.ships` (catalog, read-only) and existing
`sc.hangar_entries` (ownership, written via replace-all). No new tables, no migration.

**Testing**: Backend — xUnit, FluentAssertions, Testcontainers (PostgreSQL), `WebApplicationFactory`.
Frontend — Vitest + React Testing Library (+ MSW where needed).

**Target Platform**: Linux server (containerized API) + browser SPA.

**Project Type**: Web application (separate backend API + React SPA).

**Performance Goals**: Full import (select → confirm → upload) completes in under 60s (SC-001);
import summary shown immediately after every attempt (SC-003).

**Constraints**: No server-rendered HTML; frontend talks to backend only through the versioned
OpenAPI contract; the warning is unavoidable before any destructive action (SC-005); invalid
files never modify the hangar (SC-004, FR-011); the replace is atomic (FR-012).

**Scale/Scope**: Per-member hangars are small (tens of ships). Import files are small (tens to a
few hundred records). File size capped at 5 MB (assumption).

### Verified existing facts (from codebase inspection)

- **Ownership table**: `sc.hangar_entries` (`HangarEntry`: `Id`, `UserId`, `ShipId`, `AddedAt`),
  mapped by `HangarEntryConfiguration`; created by migration `20260613204646_AddHangarEntries`.
- **Unique constraint**: `ux_hangar_entries_user_ship` on `(user_id, ship_id)` — **one ship per
  member max**. This drives the de-duplication decision (R4).
- **Catalog**: `sc.ships`, PK `id` (`Guid`); matchable column `name`; soft-delete via `status`
  (`ShipStatus.Active`). Hangar reads/writes only surface/accept **Active** ships.
- **Repository port**: `IHangarRepository` (Infrastructure `HangarRepository` uses parameterized
  `db.Database.SqlQuery`/EF). Add/Remove already there; **add a replace-from-import method here**.
- **Transaction pattern**: `await using var tx = await db.Database.BeginTransactionAsync(ct); …
  await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);` (see `ShipRepository.BulkUpsertAsync`).
- **Handlers** are registered explicitly in `NajaEcho.Infrastructure/DependencyInjection.cs`
  (e.g. `services.AddScoped<AddShipToHangarHandler>();`). **Register the new handler there.**
- **Endpoints**: `HangarEndpoints.MapHangarEndpoints` → `MapGroup("/api/hangar")
  .RequireAuthorization()`. Current user via `ClaimsPrincipal.FindFirstValue(NameIdentifier)`
  → `Guid` (`TryGetUserId`). **Add the import route to this group.**
- **Frontend**: `features/hangar/` owns pages/components/hooks/schemas/api/tests. `MyHangarView`
  has the **Add Ship** button — the **Import** button sits beside it. `hangarApi.ts` uses
  `apiFetch` (always `application/json`). Query keys in `hooks/hangarQueryKeys.ts`. Per-feature
  `openapi-typescript` scripts in `frontend/package.json` (`gen:api:hangar`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. API-Contract-First** — Adds **new backend HTTP behaviour** (`POST /api/hangar/mine/import`).
  **API contract changes ARE required.** `contracts/openapi.yaml` is authored in Phase 1 before
  implementation; frontend types regenerate from it. Not a UI-only feature. ✅
- **II. Test-First / TDD** — Failing tests first: Application unit tests (name matching incl.
  `ship_name`/`name` fallback, case-insensitivity, `unidentified` skip, de-dup, summary counts);
  Testcontainers integration test for the endpoint (replace-all atomicity, empty array clears,
  unauthorized); frontend tests (warning gate, invalid-file rejection without API call, success
  summary + cache invalidation). ✅
- **III. Frontend/Backend Separation** — Frontend consumes types generated from the contract; no
  direct DB access; no server HTML; the file is parsed client-side and the matched-by-server
  result is authoritative. ✅
- **IV. Simplicity / YAGNI** — Reuses `sc.hangar_entries` (no new table, no migration). Reuses
  `IHangarRepository`. No merge/append mode. No background job — synchronous request is
  sufficient at this scale. ✅
- **V. Observability** — Endpoint emits structured Serilog logs with correlation ID (counts only,
  never file contents or PII); no sensitive auth data logged. ✅
- **VI. Modular Monolith + Clean Architecture** — New use case under
  `Application/Features/Hangar/ImportHangar/`; repository write in `Infrastructure`; endpoint in
  `API`; frontend logic in `features/hangar/` (thin route unchanged, dialog + hook own behaviour).
  No new shared component (single feature owns it). ✅

No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/008-hangar-json-import/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── openapi.yaml     # Phase 1 output (import endpoint)
├── checklists/
│   └── requirements.md  # (existing)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Application/
│   ├── Abstractions/
│   │   └── IHangarRepository.cs                 # + ReplaceFromImportAsync(...)
│   └── Features/Hangar/ImportHangar/
│       ├── ImportHangarCommand.cs               # (userId, IReadOnlyList<ImportShipRecord>)
│       ├── ImportShipRecord.cs                  # { Name, ShipName?, Unidentified? }
│       ├── ImportHangarHandler.cs               # filter unidentified, resolve effective name,
│       │                                        #   dedupe, call repo, build summary
│       └── ImportHangarResult.cs                # { TotalRecords, ImportedShips,
│                                                #     UnmatchedRecords, UnmatchedShipNames }
├── NajaEcho.Infrastructure/
│   └── Hangar/
│       └── HangarRepository.cs                  # + ReplaceFromImportAsync (match by name +
│                                                #   atomic delete-all-then-insert in one tx)
└── NajaEcho.Api/
    └── Features/Hangar/
        ├── HangarEndpoints.cs                   # + POST /api/hangar/mine/import
        └── Contracts/HangarDtos.cs              # + ImportHangarRequest/Record/Result DTOs

backend/tests/
├── NajaEcho.Application.Tests/Features/Hangar/  # ImportHangarHandler unit tests
└── NajaEcho.Api.Tests/Features/Hangar/          # import endpoint integration tests

frontend/src/
├── features/hangar/
│   ├── pages/MyHangarView.tsx                   # + Import button beside Add Ship
│   ├── components/ImportHangarDialog.tsx        # warning → file picker → confirm → summary
│   ├── hooks/useImportHangar.ts                 # mutation + cache invalidation
│   ├── schemas/hangarImport.ts                  # Zod: file record shape + import result
│   ├── api/hangarApi.ts                         # + importHangar(items)
│   └── __tests__/ImportHangar.test.tsx
└── lib/api/hangar-import.d.ts                   # generated from contracts/openapi.yaml
```

**Structure Decision**: Web application with the established Clean Architecture layering and
frontend feature folders. The feature is an additive slice of the existing **Hangar** feature
(007): one new Application use case (`ImportHangar`), one new repository method, one new endpoint
in the existing group, and one new dialog + hook + schema in `features/hangar/`. The destructive
warning and file parsing live in the dialog; matching and replace-all live behind the repository
port. The **Import** button is added to the existing `MyHangarView` next to **Add Ship**.

## Complexity Tracking

> No constitution violations — section intentionally empty.
