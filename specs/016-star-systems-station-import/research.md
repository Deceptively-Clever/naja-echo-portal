# Phase 0 Research: Star Systems & Space Station Import

All NEEDS CLARIFICATION items from the Technical Context are resolved below. The feature sits squarely on
existing precedents; most "research" is confirming the precedent and pinning the external UEX schema.

## R1 — UEX field schema for star systems and space stations

**Decision**: Map the promoted columns directly from the live UEX field names; persist the full raw record
as `jsonb` for forward-compatibility.

**Findings (verified live against `https://api.uexcorp.uk/2.0/`, 2026-06-20):**

`GET /2.0/star_systems` — each `data[]` record:

```json
{
  "id": 68, "id_faction": 0, "id_jurisdiction": 0,
  "name": "Stanton", "code": "ST",
  "is_available": 1, "is_available_live": 1, "is_visible": 1, "is_default": 1,
  "wiki": "https://starcitizen.tools/Stanton_system",
  "date_added": 1682371194, "date_modified": 1734643829,
  "faction_name": null, "jurisdiction_name": null
}
```

Promoted: `id` (→ `uex_id`), `name`, `code`, `is_available`, `is_visible`. Rest → `raw_data`.

`GET /2.0/space_stations` — each `data[]` record (abridged):

```json
{
  "id": ..., "id_star_system": ..., "id_planet": ..., "id_orbit": ..., "id_moon": ..., "id_city": ...,
  "name": "...", "nickname": "...",
  "is_available": 1, "is_available_live": 1, "is_visible": 1, "is_landable": 1, "is_decommissioned": 0,
  "has_refinery": 0, "has_trade_terminal": 1, "has_cargo_center": 0, "has_clinic": 0, "has_shops": 1,
  "has_refuel": 1, "has_repair": 1, "has_docking_port": 1, "...": "...",
  "star_system_name": "...", "date_added": ..., "date_modified": ...
}
```

Promoted: `id` (→ `uex_id`), `id_star_system` (resolved to local `star_system_id`), `name`, `nickname`,
`is_available`, `is_decommissioned`, `is_landable`, and the capability flags actually used by the spec
text (`has_refinery`, `has_trade_terminal`). Rest → `raw_data`.

**Rationale**: The promoted columns are exactly those the spec, the list filter (FR-007), and the combobox
label (FR-009) need; everything else is preserved in `raw_data` so future features (capability filtering,
pad types, etc.) need no re-import. Flags are `0/1` integers in the source — store as `bool` in the
promoted columns.

**Notes / corrections to the spec wording:**
- The spec's example capability "trading post" maps to UEX `has_trade_terminal`; there is **no**
  `is_trading_post` field. The data model uses `has_trade_terminal`.
- "decommissioned status" = UEX `is_decommissioned`; "availability" = UEX `is_available`. The list filter
  is `is_available = true AND is_decommissioned = false` (FR-007).

**Alternatives considered**: Persisting only promoted columns (rejected — loses forward data, would force
re-imports). Importing the full location hierarchy (planets/moons/cities) (rejected — explicitly out of
scope).

## R2 — One combined import endpoint vs. two separate endpoints

**Decision**: One admin endpoint, `POST /api/admin/locations/import`, that imports star systems **then**
space stations in a single transactional run and returns a summary with **separate** count blocks per
entity.

**Rationale**: US1 describes a single admin action ("triggers the star systems and space station import …
fetches all star systems … then fetches all space stations"). Stations reference star systems, so they
**must** be imported in order within one unit of work to (a) make the parent map available and (b) keep
the all-or-nothing rollback guarantee (FR-012). Acceptance scenarios 1 & 2 are both satisfied within the
one run; FR-005's "separate counts" requirement is met by the per-entity summary blocks. This also matches
the one-button-per-tab UX of the existing `DataImportPage`.

**Alternatives considered**: Two endpoints / two buttons (rejected — splits the transaction, risks a
station import against a stale/partial system catalog, and complicates the single-action UX and the empty/
abort semantics).

## R3 — Empty or unreachable source = error (deviation from ships import)

**Decision**: Treat an empty record set **or** an unreachable/error source as a failure: abort the run,
commit no changes, surface a clear error. Map to HTTP **502 Bad Gateway** at the endpoint (matching the
existing UEX-failure mapping in `ShipAdminEndpoints`), with a distinct title for the empty case.

**Rationale**: Direct from the 2026-06-20 clarification and FR-012/SC-006 — an empty UEX result is a
"suspicious source condition", not a legitimate empty catalog, and must not wipe the catalog via the
soft-delete-absent step. This **diverges** from `ImportShipsHandler`, which returns a warning and a
zero-count result on an empty feed; the new handler throws (`EmptySourceException`) before any write. The
whole run is inside one transaction, so an exception rolls back cleanly.

**Alternatives considered**: Reusing the ship "warning, zero changes" behavior (rejected — on an empty
feed the soft-delete-absent pass would soft-delete the entire existing catalog, the exact data-loss the
clarification guards against).

## R4 — Skipping stations with an unresolved parent star system

**Decision**: Build an in-memory `uex_id → local Guid` map of star systems (post-upsert, within the same
transaction). For each incoming station, resolve `id_star_system` against the map; if absent, **skip** the
record and increment a `skipped` counter reported in the summary (FR-013). Skipped records are neither
inserted nor cause a failure.

**Rationale**: FR-013 + the edge case require referential integrity without aborting the whole run for one
orphan record. Importing systems first guarantees the map reflects the current source. The `star_system_id`
FK is `OnDelete.Restrict` against soft-deleted (never hard-deleted) parents, so the FK never dangles.

**Alternatives considered**: Inserting stations with a null parent (rejected — `star_system_id` is a
required FK by design; a station with no system is meaningless). Failing the run on the first orphan
(rejected — one bad upstream record shouldn't block the whole catalog).

## R5 — Warehouse station reference: nullable FK alongside the retained free-text column

**Decision**: Add a nullable `station_id` FK to **both** `warehouse_inventory` and
`warehouse_material_inventory`; keep the existing `location` string. `station_id` is canonical going
forward; `location` is deprecated, retained for backward compatibility, and **never written** by the
Transfer action.

**Rationale**: FR-010, SC-007, and the clarifications. Nullable + additive = a non-destructive,
forward-only migration; existing rows keep their free-text `location` and are **not** migrated
(assumption). `warehouse_inventory` backs both item inventory and ship components, so a single column +
single items-transfer endpoint covers two of the three UI features; `warehouse_material_inventory` covers
the third.

**Alternatives considered**: Replacing `location` outright (rejected — destructive, breaks existing rows,
violates the spec's backward-compat requirement and would need migration that is out of scope). A separate
join table (rejected — YAGNI; a single nullable FK models "this entry is at this station" exactly).

## R6 — "Last station this session" default for the Transfer modal

**Decision**: Track the last-selected station as **client-side session state** in the warehouse feature
(lifted state / small context, e.g. `useLastTransferStation`), not persisted server-side. The Transfer
modal pre-selects it when present (FR-014).

**Rationale**: The clarification scopes the default to "the current session" and "the last station they
selected" — a UX convenience, not durable data. Keeping it client-side avoids a needless schema/endpoint
addition (YAGNI) and naturally resets between sessions.

**Alternatives considered**: Persisting a per-user "last station" server-side (rejected — out of scope,
adds state with no stated durability requirement).

## R7 — Station combobox: reuse existing primitives, no new dependency

**Decision**: Build `StationCombobox` from the existing `components/ui/combobox.tsx` + `command.tsx` +
`popover.tsx` primitives, backed by a debounced `GET /api/warehouse/stations?search=` query (mirroring
`useSystemsCatalogSearch`). The Transfer modal uses the existing `components/ui/dialog.tsx`.

**Rationale**: Assumption in the spec ("reuses the existing combobox component pattern") + Principle IV.
All needed primitives already exist in `components/ui/`; the search/debounce/query pattern is already
proven in `AddInventoryDialog` + `useSystemsCatalogSearch`. No new dependency is introduced (the
constitution's licence/security review is therefore not triggered).

**Alternatives considered**: A bespoke control or a new combobox library (rejected — duplicates existing
capability, violates YAGNI and the spec assumption).
