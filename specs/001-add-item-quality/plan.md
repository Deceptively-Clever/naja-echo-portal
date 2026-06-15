# Implementation Plan: Add item quality

**Branch**: `feature/001-add-item-quality` | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-add-item-quality/spec.md`

## Summary

Add a `quality` value to warehouse inventory entries so Quartermasters can set item quality when adding
inventory. `quality` is an integer in the inclusive range `1..1000`, defaults to `500`, and is
persisted on inventory rows. This change must cover both warehouse item add flows currently in the
product (`/api/warehouse/items` and `/api/warehouse/ship-components`) and include quality in read/list
responses so existing and new UI surfaces can show and use it consistently.

The approach is a small extension of the existing warehouse vertical slice: add one new column on
`warehouse_inventory`, update application DTOs/repositories/endpoint contracts, and extend frontend
warehouse schemas/forms/tables. Existing rows are safely backfilled to default quality `500`.

## Technical Context

**Language/Version**: C# / .NET `net10.0` backend; TypeScript (strict) frontend.

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core + Npgsql, Serilog; React 19 + Vite,
TanStack Query, React Hook Form + Zod, `openapi-typescript`.

**Storage**: PostgreSQL 16, `public.warehouse_inventory` table (existing) gains `quality` column.

**Testing**: Backend xUnit + FluentAssertions (+ Testcontainers infra tests), frontend Vitest + React
Testing Library.

**Target Platform**: Linux-hosted API + browser SPA.

**Project Type**: Web application (separate backend API + frontend SPA).

**Performance Goals**: Preserve current inventory/ship-components list responsiveness; no additional API
roundtrips for quality defaults.

**Constraints**:
- `quality` must be integer and bounded `1..1000`.
- Server enforces validation for all write paths.
- Default behavior for omitted `quality` is `500`.
- Existing rows without quality are treated as `500` via migration backfill + non-null default.
- API contract changes are required (not UI-only).

**Scale/Scope**: Warehouse inventory feature scope only (`items` + `ship-components` surfaces); no new
permissions, no new warehouse entities.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. API-Contract-First** — Backend HTTP behavior changes (request and response schema additions for
  warehouse endpoints). Contract authored in `contracts/openapi.yaml` before implementation. ✅
- **II. Test-First / TDD** — Plan includes failing tests first for validation bounds, default behavior,
  persistence, and API/UI propagation across item + ship-component flows. ✅
- **III. Frontend/Backend Separation** — Frontend consumes generated contract types; quality remains
  server-authoritative and persisted in backend storage only. ✅
- **IV. Simplicity / YAGNI** — Single-column extension on existing table + existing flows; no extra
  abstraction layers or separate quality entity. ✅
- **V. Observability** — Existing structured warehouse logging remains; quality accepted/rejected paths
  will be logged through current endpoint/handler patterns. ✅
- **VI. Modular Monolith + Clean Architecture** — Changes stay within existing Warehouse feature folders
  in Domain/Application/Infrastructure/API and frontend `features/warehouse`. ✅

**Post-Phase-1 re-check**: still PASS; no constitution violations introduced by design artifacts. ✅

## Project Structure

### Documentation (this feature)

```text
specs/001-add-item-quality/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── openapi.yaml
└── tasks.md             # created later by /speckit-tasks
```

### Source Code (repository root)

```text
backend/src/
├── NajaEcho.Domain/
│   └── Warehouse/
│       └── WarehouseInventoryEntry.cs                     # + Quality
├── NajaEcho.Application/
│   ├── Features/Warehouse/
│   │   ├── GetInventory/InventoryRowDto.cs               # + Quality
│   │   ├── AddInventoryItem/AddInventoryItemCommand.cs   # + Quality with default handling
│   │   └── ShipComponents/GetShipComponents/ShipComponentRowDto.cs  # + Quality
│   └── Abstractions/
│       ├── IWarehouseInventoryRepository.cs              # add/increment signature + quality
│       └── IShipComponentRepository.cs                   # ship add signature + quality
├── NajaEcho.Infrastructure/
│   ├── Warehouse/
│   │   ├── WarehouseInventoryRepository.cs               # select/insert/update projections + quality
│   │   └── ShipComponentRepository.cs                    # ship rows + quality
│   └── Persistence/
│       ├── Configurations/WarehouseInventoryEntryConfiguration.cs   # quality column + check
│       ├── Migrations/<timestamp>_AddWarehouseItemQuality.cs
│       └── Migrations/AppDbContextModelSnapshot.cs
└── NajaEcho.Api/
    └── Features/Warehouse/
        ├── Contracts/WarehouseDtos.cs                    # request/response + quality
        ├── Contracts/ShipComponentDtos.cs                # request/response + quality
        └── WarehouseEndpoints.cs                         # map quality values across endpoints

backend/tests/
├── NajaEcho.Application.Tests/Features/Warehouse/...
├── NajaEcho.Infrastructure.Tests/Warehouse/...
└── NajaEcho.Api.Tests/Features/Warehouse/...

frontend/src/
└── features/warehouse/
    ├── schemas/addItemSchemas.ts                         # quality input validation
    ├── schemas/inventorySchemas.ts                       # quality in row schema
    ├── schemas/shipComponentSchemas.ts                   # quality in row schema
    ├── components/AddInventoryDialog.tsx                 # quality field with default 500
    ├── components/InventoryTable.tsx                     # display quality
    ├── components/ShipComponentsTable.tsx                # display quality
    └── api/warehouseApi.ts + api/shipComponentsApi.ts    # quality request/response payloads
```

**Structure Decision**: Use existing web-app structure and extend the current warehouse vertical slice
in place. No new modules or services are introduced.

## Complexity Tracking

> No constitution violations — section intentionally empty.
