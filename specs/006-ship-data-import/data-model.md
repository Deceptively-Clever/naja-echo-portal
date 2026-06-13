# Phase 1 Data Model: Ship Data Import

Derives the persistent and transferred shapes from the spec's Key Entities and the UEX feed. Storage is the
existing PostgreSQL `AppDbContext` (snake_case). One new table (`ships`), one seeded role row, and one
additive change to the auth session payload.

## Entity: Ship (Domain → `NajaEcho.Domain/Ships/Ship.cs`)

A locally-stored mirror of one UEX vehicle record. Hybrid model: promoted/typed columns for querying and
display + a verbatim JSONB snapshot of the full feed record.

| Property | Type | Column | Notes |
|----------|------|--------|-------|
| `Id` | `Guid` | `id` (PK) | Our surrogate key. |
| `UexId` | `int` | `uex_id` | UEX `id`. **Unique index.** Upsert/identity key. |
| `Uuid` | `string?` | `uuid` | UEX `uuid` (stored for traceability). |
| `Name` | `string` | `name` | UEX `name` (e.g. "100i"). Required. |
| `NameFull` | `string?` | `name_full` | UEX `name_full` (e.g. "Origin 100i"). |
| `CompanyName` | `string?` | `company_name` | UEX `company_name` (manufacturer). |
| `Status` | `ShipStatus` | `status` | `Active` \| `SoftDeleted`. Stored as string or int (EF enum mapping). |
| `RawData` | `JsonDocument` | `raw_data` (`jsonb`) | Verbatim full feed record (all 64 fields). |
| `ImportedAt` | `DateTimeOffset` | `imported_at` | First time we saw this record. |
| `UpdatedAt` | `DateTimeOffset` | `updated_at` | Last import that touched this record. |
| `SoftDeletedAt` | `DateTimeOffset?` | `soft_deleted_at` | Set when soft-deleted; cleared on reactivation. |

**Validation / invariants**:
- `UexId` unique and stable across imports (identity key).
- `Name` non-empty (feed guarantees it; defensively required).
- `RawData` always contains the complete feed record for the ship (no field dropped) → SC-004.
- A `SoftDeleted` ship has `SoftDeletedAt` set; an `Active` ship has it null.

**Indexes**:
- Unique on `uex_id` (upsert lookups + integrity).
- Index on `status` (list filters active vs soft-deleted display).
- Default ordering for the list is `name` (consider an index on `name` if sorting proves hot).

### Enum: ShipStatus (`NajaEcho.Domain/Ships/ShipStatus.cs`)

```
Active        // present in the most recent feed (or reactivated)
SoftDeleted   // previously imported, absent from the latest feed
```

### Status lifecycle (import-driven; no manual transitions)

```
            ┌─────────── ship present in feed ───────────┐
            ▼                                             │
   (new uex_id in feed) ──insert──▶  Active  ──feed still has it──▶ Active (raw_data refreshed)
                                       │
                          feed no longer has it
                                       ▼
                                  SoftDeleted ──uex_id reappears in feed──▶ Active (reactivated,
                                                                            soft_deleted_at cleared)
```

Records are **never hard-deleted** by the import (FR-009). Reactivation is automatic (FR-011). Ships are
**read-only** to admins — there are no field-edit transitions.

## Import semantics (`ImportShips` use case)

Input: the full set of feed records (from `IUexVehicleClient`). Output counts: `added`, `updated`,
`reactivated`, `softDeleted`, `total` (total = active records after the run / records in feed).

Per-run, inside one transaction (see `plan.md` Decision 3 for the ordered steps):
- **added**: incoming `uex_id` not previously stored → insert `Active`.
- **updated**: incoming `uex_id` already stored and `Active` → refresh promoted columns + `raw_data`, set
  `UpdatedAt`.
- **reactivated**: incoming `uex_id` stored but `SoftDeleted` → set `Active`, clear `SoftDeletedAt`, refresh
  data. (Counted separately from `updated` for observability; may be summed in the UI message.)
- **softDeleted**: stored `Active` `uex_id` absent from the feed → set `SoftDeleted`, set `SoftDeletedAt`.
- **Zero-record guard**: empty feed → no transaction, no changes, warning surfaced.
- **Failure**: any exception → full rollback; stored data unchanged (FR-005).

## Transfer shapes (API DTOs)

Mirrors `contracts/openapi.yaml`. Frontend Zod schemas in `features/admin/schemas/shipSchemas.ts` mirror
these 1:1.

### ShipListItemResponse (list rows — FR-006)
```
{ id: string (guid), name: string, companyName: string | null, status: "active" | "softDeleted" }
```

### PagedShipsResponse (list envelope — FR-006/007)
```
{ items: ShipListItemResponse[], page: int, pageSize: int, totalCount: int, totalPages: int }
```
- Default `pageSize` = 25 (assumption). `page` is 1-based.
- Empty `items` with `totalCount: 0` drives the empty state (FR-007).

### ShipDetailResponse (detail sheet — FR-008, US3)
```
{ id: string (guid), status: "active" | "softDeleted", fields: { <all raw feed fields> } }
```
- `fields` is the verbatim `raw_data` object (all 64 keys), so the sheet can render every field, showing
  empty ones explicitly (US3 scenario 3). `status` drives the soft-deleted indicator (US3 scenario 4).

### ImportShipsResponse (import result — FR-004)
```
{ added: int, updated: int, reactivated: int, softDeleted: int, total: int }
```
- On `409` (import already running, FR-003) the body is a ProblemDetails, not this shape.

## Existing entity change: auth session payload

`CurrentUserResponse` (API) and `sessionStateSchema` (frontend) gain a `roles` array so the SPA can gate
the Admin nav section and route. Additive, non-breaking.

```
AuthenticatedSessionResponse.user += roles: string[]   // e.g. ["Admin"]
```

## Seeded reference data

- **Admin role**: `AdminRoleSeeder` ensures an `IdentityRole<Guid>` named `Admin` exists at startup
  (idempotent). **No user is auto-assigned** — membership is inserted manually into `AspNetUserRoles`
  (see `quickstart.md`).

## Persistence notes

- `AppDbContext` adds `DbSet<Ship> Ships`. `ShipConfiguration` maps the table, the `raw_data` → `jsonb`
  column (Npgsql maps `JsonDocument`/`string`), the unique `uex_id` index, and the `status` enum
  conversion.
- New EF Core migration `AddShips` (forward-only; purely additive — no destructive change, so no special
  approval needed per the workflow rules).
- snake_case naming is applied globally (existing `UseSnakeCaseNamingConvention()`), so C# `UexId` →
  `uex_id`, `RawData` → `raw_data`, etc., automatically.
