# Phase 0 Research: Commodity Data Import

All Technical Context items were resolvable from existing codebase precedent (features 006 ships,
009 items). No external/unknown technology choices remained. The decisions below record the
commodity-specific choices and the precedent each one follows.

## Decision 1 — Durable identity = UEX `id` (`uex_id`)

- **Decision**: Key the local `sc.commodities` table on the UEX `id` field, stored as `uex_id` with a
  unique index. `uuid` is stored as a nullable column and is **not** part of identity.
- **Rationale**: Spec FR-004/FR-010 require `id` as the durable import identity and explicitly allow
  `uuid = null`. This matches the **ship** precedent (`Ship.UexId` unique, `Ship.Uuid` nullable) and
  differs from items (which key on `uuid`).
- **Alternatives considered**: Keying on `uuid` (rejected — nullable in source, not durable);
  composite keys (rejected — `id` is globally unique in the UEX feed).

## Decision 2 — Single-table, full-feed, global soft-delete

- **Decision**: One `sc.commodities` table. `CommodityRepository.BulkUpsertAsync` mirrors
  `ShipRepository.BulkUpsertAsync` exactly: transactional insert/update/restore + soft-delete of any
  Active row whose `uex_id` is absent from the incoming full feed. Soft-delete is **global** (not
  scoped), because the commodities feed is fetched in one full pass and is authoritative.
- **Rationale**: Spec FR-006/FR-007 and User Story 3. The commodities endpoint returns the complete
  catalog in one call (`GET /commodities`), exactly like `GET /vehicles` for ships — so the global
  soft-delete used for ships is the correct shape, not the category-scoped variant used for items.
- **Alternatives considered**: Category-scoped soft-delete (rejected — commodities are not imported
  per-category); hard delete (rejected — spec mandates soft delete to preserve history/integrity).
- **Soft-delete model**: `CommodityStatus { Active, SoftDeleted }` enum stored as string via
  `HasConversion<string>()`, plus `SoftDeletedAt`/`ImportedAt`/`UpdatedAt` timestamps — identical to
  `ShipStatus`.

## Decision 3 — Boolean flag normalization (20 flags → bool columns)

- **Decision**: Promote each of the 20 UEX integer flags (`is_available`, `is_available_live`,
  `is_visible`, `is_extractable`, `is_mineral`, `is_raw`, `is_pure`, `is_refined`, `is_refinable`,
  `is_harvestable`, `is_buyable`, `is_sellable`, `is_temporary`, `is_illegal`, `is_volatile_qt`,
  `is_volatile_time`, `is_inert`, `is_explosive`, `is_buggy`, `is_fuel`) to a non-nullable `bool`
  column. Conversion uses the existing `GetBool` helper logic: JSON `true`/`false` → as-is, JSON
  number → `value != 0`, missing → `false`.
- **Rationale**: Spec FR-009/FR-015 and SC-003 require integer flags stored as booleans and all
  records preserved regardless of flag values, so future filtering can use them. Dedicated columns
  (vs. only jsonb) make the flags directly queryable. The `GetBool` helper already exists in
  `UexItemClient`.
- **Alternatives considered**: Storing flags only in `raw_data` jsonb (rejected — SC-003 requires no
  raw integer flag in the local data and the data must be filterable); a bit-flags integer (rejected
  — opaque, harder to query and read).

## Decision 4 — Location identifier fields: raw text + native PostgreSQL `int[]`

- **Decision**: For each of `ids_star_systems`, `ids_planets`, `ids_moons`, `ids_poi`, `ids_orbits`,
  store two columns: the raw comma-separated string (`text`, nullable) and a parsed
  `integer[]` (non-null, empty array when source is null/empty). Npgsql maps C# `int[]` directly to
  PostgreSQL `integer[]`. Parsing splits on `,`, trims whitespace, and discards tokens that are not
  valid integers.
- **Rationale**: Spec FR-012 requires both the raw string and parsed IDs available locally, and
  explicitly forbids location join tables for v1. Native `int[]` is queryable (supports `@>`
  containment for future filtering) and avoids a join-table abstraction the spec defers.
- **Alternatives considered**: Join tables (rejected — explicitly out of scope); storing parsed IDs
  only inside jsonb (rejected — not first-class/queryable); comma-string only (rejected — spec
  requires parsed values be available). **Testing note**: native arrays are not faithfully exercised
  by the EF in-memory provider, so array round-trip is verified in the Testcontainers repository
  test (Constitution II).

## Decision 5 — Timestamps: raw `bigint` + converted UTC `timestamptz`

- **Decision**: For `date_added` and `date_modified`, store the raw Unix integer (`bigint`) and a
  converted UTC value (`timestamptz`, nullable). Conversion uses
  `DateTimeOffset.FromUnixTimeSeconds`. A zero/invalid raw value keeps the raw column populated and
  leaves the converted column null.
- **Rationale**: Spec FR-013 requires both raw and converted values. Unix-seconds interpretation
  matches the existing `GetDateTimeOffset` helper used for ship/item `date_added`/`date_modified`.
- **Alternatives considered**: Storing only the converted datetime (rejected — spec requires the raw
  value too); failing the record on a bad timestamp (rejected — timestamps are not required identity
  fields; only `id`/`name` gate a record).

## Decision 6 — Pricing exclusion

- **Decision**: Do not create `price_buy` / `price_sell` columns and do not map them. They remain
  only inside the verbatim `raw_data` jsonb (which stores the full source object per FR-008).
- **Rationale**: Spec FR-014 — pricing comes from a separate future source. `raw_data` is the
  verbatim source object for troubleshooting/forward-compat, so the fields are retained there without
  being promoted to first-class columns.
- **Alternatives considered**: Stripping price fields from `raw_data` (rejected — FR-008 wants the
  full source object verbatim); promoting them now (rejected — explicitly excluded).

## Decision 7 — Relationship identifiers without integrity

- **Decision**: Store `id_parent` and `id_item` as nullable `integer` columns. No foreign keys, no
  existence checks.
- **Rationale**: Spec FR-011 — referenced records need not exist; relationship integrity deferred to
  a future feature.
- **Alternatives considered**: FK constraints to commodities/items (rejected — explicitly deferred,
  and the referenced item table may not contain the row at import time).

## Decision 8 — Record validation: skip vs. fail

- **Decision**: A source record missing `id` or `name` is skipped and counted (`skipped`); the import
  continues. The whole import fails only when the endpoint is unreachable (`HttpRequestException`) or
  the response shape is invalid (missing/non-array `data` → `InvalidOperationException`). On failure,
  nothing is committed (the upsert transaction never runs because mapping happens before the
  repository call).
- **Rationale**: Spec FR-003/FR-005, User Stories 2 & 4, SC-007. Mirrors the ship handler's
  fetch-then-map-then-upsert ordering, which guarantees no partial writes on a source failure.
- **Endpoint mapping**: `ImportAlreadyInProgressException` → **409**; `HttpRequestException` → **502
  Bad Gateway**; invalid-shape `InvalidOperationException` → **502 Bad Gateway** (caught explicitly so
  an invalid source shape is reported as an upstream failure rather than a generic 500 — a small,
  deliberate improvement over the ship endpoint, consistent with "fail the import").
- **Alternatives considered**: Failing the whole import on any bad record (rejected — spec requires
  resilience to individual bad records); committing valid records before a fetch error (impossible by
  construction — fetch precedes upsert).

## Decision 9 — Concurrency: reuse shared `IImportCoordinator`

- **Decision**: The handler acquires the existing singleton `IImportCoordinator` semaphore; if it is
  held, throw `ImportAlreadyInProgressException` → 409.
- **Rationale**: Spec FR-016, User Story 5. The coordinator is already a shared singleton, so "one
  import at a time" automatically spans ships, categories, items, and commodities with no new code.
- **Alternatives considered**: A commodity-specific lock (rejected — would allow a commodity import to
  run concurrently with a ship import against shared infrastructure; the global guarantee is simpler
  and safer).

## Decision 10 — No browse/list endpoint or import history in v1

- **Decision**: Ship only the import endpoint (`POST /api/admin/commodities/import`). No GET
  list/detail endpoint, no import-history table, no progress UI.
- **Rationale**: Spec scope and Assumptions — the spec's user stories cover only the import action and
  its completion summary. Adding browse endpoints now would violate YAGNI (Constitution IV).
- **Alternatives considered**: Mirroring the ship GET list/detail endpoints (rejected — no spec
  requirement; can be added by a later feature when a commodities screen exists).
